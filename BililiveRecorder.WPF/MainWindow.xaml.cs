using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int MAX_LOG_ROW = 25;
        private const string LAST_WORK_DIR_FILE = "lastworkdir";

        private IContainer Container { get; set; }
        private ILifetimeScope RootScope { get; set; }

        public Recorder Recorder { get; set; }
        public ObservableCollection<string> Logs { get; set; } =
            new ObservableCollection<string>()
            {
                "当前版本：" + BuildInfo.Version,
                "注：按鼠标右键复制日志",
                "网站： https://rec.danmuji.org",
            };

        public static void AddLog(string message) => _AddLog?.Invoke(message);
        private static Action<string> _AddLog;

        public MainWindow()
        {
            _AddLog = (message) => Log.Dispatcher.Invoke(() => { Logs.Add(message); while (Logs.Count > MAX_LOG_ROW) { Logs.RemoveAt(0); } });

            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            Container = builder.Build();
            RootScope = Container.BeginLifetimeScope("recorder_root");

            Recorder = RootScope.Resolve<Recorder>();

            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title += "     版本号: " + BuildInfo.Version + "  " + BuildInfo.HeadShaShort;

            string workdir = string.Empty;
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

            if (!Recorder.Initialize(workdir))
            {
                MessageBox.Show("初始化错误", "录播姬", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-2);
                return;
            }

            // Task.Run(() => CheckVersion());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _AddLog = null;
            Recorder.Shutdown();
            try
            {
                File.WriteAllText(LAST_WORK_DIR_FILE, Recorder.Config.WorkDirectory);
            }
            catch (Exception) { }
        }

        #region - 更新检查 -

        #endregion

        private void TextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                Clipboard.SetText(textBlock.Text);
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

            Task.Run(() => rr.Start());
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

            Task.Run(() => rr.Stop());
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
            var rr = _GetSenderAsRecordedRoom(sender);
            if (rr == null)
            {
                return;
            }

            Recorder.RemoveRoom(rr);
        }

        /// <summary>
        /// 全部直播间启用自动录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableAllAutoRec(object sender, RoutedEventArgs e)
        {
            Recorder.Rooms.ToList().ForEach(rr => Task.Run(() => rr.Start()));
        }

        /// <summary>
        /// 全部直播间禁用自动录制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisableAllAutoRec(object sender, RoutedEventArgs e)
        {
            Recorder.Rooms.ToList().ForEach(rr => Task.Run(() => rr.Stop()));
        }

        /// <summary>
        /// 添加直播间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddRoomidButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(AddRoomidTextBox.Text, out int roomid))
            {
                if (roomid > 0)
                {
                    Recorder.AddRoom(roomid);
                }
                else
                {
                    logger.Info("房间号是大于0的数字！");
                }
            }
            else
            {
                logger.Info("房间号是数字！");
            }
            AddRoomidTextBox.Text = string.Empty;
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
        }

        private IRecordedRoom _GetSenderAsRecordedRoom(object sender) => (sender as Button)?.DataContext as IRecordedRoom;


    }
}
