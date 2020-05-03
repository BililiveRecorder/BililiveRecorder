using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;
using CommandLine;
using Hardcodet.Wpf.TaskbarNotification;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Regex UrlToRoomidRegex = new Regex(@"^https?:\/\/live\.bilibili\.com\/(?<roomid>\d+)(?:[#\?].*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private const int MAX_LOG_ROW = 25;
        private const string LAST_WORK_DIR_FILE = "lastworkdir";

        private IContainer Container { get; set; }
        private ILifetimeScope RootScope { get; set; }

        public IRecorder Recorder { get; set; }
        public ObservableCollection<string> Logs { get; set; } =
            new ObservableCollection<string>()
            {
                "当前版本：" + BuildInfo.Version,
                "网站： https://rec.danmuji.org",
                "更新日志： https://rec.danmuji.org/allposts",
                "问题反馈邮箱： rec@danmuji.org",
                "QQ群： 689636812",
                "",
                "删除直播间按钮在列表右键菜单里",
                "",
                "录制速度比 在 100% 左右说明跟上了主播直播的速度",
                "小于 100% 说明录播电脑的下载带宽不够，跟不上录制直播"
            };

        public static void AddLog(string message) => _AddLog?.Invoke(message);
        private static Action<string> _AddLog;

        public MainWindow()
        {

            _AddLog = (message) =>
                Log.Dispatcher.BeginInvoke(
                    DispatcherPriority.DataBind,
                    new Action(() => { Logs.Add(message); while (Logs.Count > MAX_LOG_ROW) { Logs.RemoveAt(0); } })
                    );

            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            Container = builder.Build();
            RootScope = Container.BeginLifetimeScope("recorder_root");

            Recorder = RootScope.Resolve<IRecorder>();

            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title += " " + BuildInfo.Version + " " + BuildInfo.HeadShaShort;

            bool skip_ui = false;
            string workdir = string.Empty;

            CommandLineOption commandLineOption = null;
            Parser.Default
                .ParseArguments<CommandLineOption>(Environment.GetCommandLineArgs())
                .WithParsed(x => commandLineOption = x);

            if (commandLineOption?.WorkDirectory != null)
            {
                skip_ui = true;
                workdir = commandLineOption.WorkDirectory;
            }

            if (!skip_ui)
            {
                try
                {
                    workdir = File.ReadAllText(LAST_WORK_DIR_FILE);
                }
                catch (Exception) { }
                var wdw = new WorkDirectoryWindow()
                {
                    Owner = this,
                    WorkPath = workdir,
                };

                if (wdw.ShowDialog() == true)
                {
                    workdir = wdw.WorkPath;
                }
                else
                {
                    Environment.Exit(-1);
                    return;
                }
            }

            if (!Recorder.Initialize(workdir))
            {
                if (!skip_ui)
                {
                    MessageBox.Show("初始化错误", "录播姬", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Environment.Exit(-2);
                return;
            }

            NotifyIcon.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (new TimedMessageBox
            {
                Title = "关闭录播姬？",
                Message = "确定要关闭录播姬吗？",
                CountDown = 10,
                Left = Left,
                Top = Top
            }.ShowDialog() == true)
            {
                _AddLog = null;
                Recorder.Shutdown();
                try
                {
                    File.WriteAllText(LAST_WORK_DIR_FILE, Recorder.Config.WorkDirectory);
                }
                catch (Exception) { }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void TextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is TextBlock textBlock)
                {
                    Clipboard.SetText(textBlock.Text);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 触发回放剪辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clip_Click(object sender, RoutedEventArgs e)
        {
            var rr = _GetSenderAsRecordedRoom(sender);
            if (rr == null)
            {
                return;
            }

            Task.Run(() => rr.Clip());
        }

        /// <summary>
        /// 启用自动录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableAutoRec(object sender, RoutedEventArgs e)
        {
            var rr = _GetSenderAsRecordedRoom(sender);
            if (rr == null)
            {
                return;
            }

            Task.Run(() =>
            {
                rr.Start();
                Recorder.SaveConfigToFile();
            });
        }

        /// <summary>
        /// 禁用自动录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisableAutoRec(object sender, RoutedEventArgs e)
        {
            var rr = _GetSenderAsRecordedRoom(sender);
            if (rr == null)
            {
                return;
            }

            Task.Run(() =>
            {
                rr.Stop();
                Recorder.SaveConfigToFile();
            });
        }

        /// <summary>
        /// 手动触发尝试录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TriggerRec(object sender, RoutedEventArgs e)
        {
            var rr = _GetSenderAsRecordedRoom(sender);
            if (rr == null)
            {
                return;
            }

            Task.Run(() => rr.StartRecord());
        }

        /// <summary>
        /// 切断当前录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CutRec(object sender, RoutedEventArgs e)
        {
            var rr = _GetSenderAsRecordedRoom(sender);
            if (rr == null)
            {
                return;
            }

            Task.Run(() => rr.StopRecord());
        }

        /// <summary>
        /// 删除当前房间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveRecRoom(object sender, RoutedEventArgs e)
        {
            var rr = (IRecordedRoom)((DataGrid)((ContextMenu)((MenuItem)sender)?.Parent)?.PlacementTarget)?.SelectedItem;
            if (rr == null)
            {
                return;
            }

            Recorder.RemoveRoom(rr);
            Recorder.SaveConfigToFile();
        }

        private void RefreshRoomInfo(object sender, RoutedEventArgs e)
        {
            var rr = (IRecordedRoom)((DataGrid)((ContextMenu)((MenuItem)sender)?.Parent)?.PlacementTarget)?.SelectedItem;
            if (rr == null)
            {
                return;
            }

            rr.RefreshRoomInfo();
        }

        /// <summary>
        /// 全部直播间启用自动录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnableAllAutoRec(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll(Recorder.ToList().Select(rr => Task.Run(() => rr.Start())));
            Recorder.SaveConfigToFile();
        }

        /// <summary>
        /// 全部直播间禁用自动录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DisableAllAutoRec(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll(Recorder.ToList().Select(rr => Task.Run(() => rr.Stop())));
            Recorder.SaveConfigToFile();
        }

        /// <summary>
        /// 输入框按下回车键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddRoomidTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddRoom();
        }

        /// <summary>
        /// 添加直播间按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddRoomidButton_Click(object sender, RoutedEventArgs e)
        {
            AddRoom();
        }

        private void AddRoom()
        {
            var match = UrlToRoomidRegex.Match(AddRoomidTextBox.Text);
            if (match.Success)
            {
                if (int.TryParse(match.Groups["roomid"].Value, out int roomid))
                {
                    Add(roomid);
                }
                else
                {
                    logger.Warn("添加房间时发生了不应该出现的错误");
                }
            }
            else if (int.TryParse(AddRoomidTextBox.Text, out int roomid))
            {
                Add(roomid);
            }
            else
            {
                logger.Info("房间号是数字！");
            }
            AddRoomidTextBox.Text = string.Empty;

            void Add(int roomid)
            {
                if (roomid > 0)
                {
                    if (Recorder.Any(x => x.RoomId == roomid || x.ShortRoomId == roomid))
                    {
                        logger.Info("该直播间已经添加过了");
                    }
                    else
                    {
                        Recorder.AddRoom(roomid);
                        Recorder.SaveConfigToFile();
                    }
                }
                else
                {
                    logger.Info("房间号是大于0的数字！");
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void ShowSettingsWindow()
        {
            var sw = new SettingsWindow(this, Recorder.Config);
            if (sw.ShowDialog() == true)
            {
                sw.Config.CopyPropertiesTo(Recorder.Config);
            }
            Recorder.SaveConfigToFile();
        }

        private IRecordedRoom _GetSenderAsRecordedRoom(object sender) => (sender as Button)?.DataContext as IRecordedRoom;

        private void Taskbar_Quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                NotifyIcon.ShowBalloonTip("B站录播姬", "录播姬已最小化到托盘，左键单击图标恢复界面。", BalloonIcon.Info);
            }
        }

        private void Taskbar_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Topmost ^= true;
            Topmost ^= true;
            Focus();
        }
    }
}
