using BililiveRecorder.Core;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int MAX_LOG_ROW = 25;

        public Recorder Recorder { get; set; }
        public ObservableCollection<string> Logs { get; set; } =
            new ObservableCollection<string>()
            {
                "注：按鼠标右键复制日志",
                "网站： https://rec.danmuji.org",
            };

        public static void AddLog(string message) => _AddLog?.Invoke(message);
        private static Action<string> _AddLog;

        public MainWindow()
        {
            _AddLog = (message) => Log.Dispatcher.Invoke(() => { Logs.Add(message); while (Logs.Count > MAX_LOG_ROW) Logs.RemoveAt(0); });

            InitializeComponent();

            Recorder = new Recorder();
            DataContext = this;

            if (Debugger.IsAttached)
                new DebugConsole(this).Show();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            LoadRooms();
            Task.Run(() => CheckVersion());
        }

        private void CheckVersion()
        {
            UpdateBar.MainButtonClick += UpdateBar_MainButtonClick;

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                ad.CheckForUpdateCompleted += Ad_CheckForUpdateCompleted;
                ad.CheckForUpdateAsync();
            }
        }

        private Action UpdateAction;
        private void UpdateBar_MainButtonClick(object sender, RoutedEventArgs e) => UpdateAction?.Invoke();

        private void Ad_CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            if (e.Error != null)
            {
                logger.Error(e.Error, "检查版本更新出错");
                return;
            }
            if (e.Cancelled)
                return;
            if (e.UpdateAvailable)
            {
                if (e.IsUpdateRequired)
                {
                    BeginUpdate();
                }
                else
                {
                    UpdateAction = () => BeginUpdate();
                    UpdateBar.MainText = string.Format("发现新版本: {0} 大小: {1}KiB", e.AvailableVersion, e.UpdateSizeBytes / 1024);
                    UpdateBar.ButtonText = "下载更新";
                    UpdateBar.Display = true;
                }
            }
        }

        private void BeginUpdate()
        {
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            ad.UpdateCompleted += Ad_UpdateCompleted;
            ad.UpdateProgressChanged += Ad_UpdateProgressChanged;
            ad.UpdateAsync();
            UpdateBar.ProgressText = "0KiB / 0KiB - 0%";
            UpdateBar.Progress = 0;
            UpdateBar.Display = true;
            UpdateBar.ShowProgressBar = true;
        }

        private void Ad_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            UpdateBar.Progress = e.BytesCompleted / e.BytesTotal;
            UpdateBar.ProgressText = string.Format("{0}KiB / {1}KiB - {2}%", e.BytesCompleted / 1024, e.BytesTotal / 1024, e.BytesCompleted / e.BytesTotal);
        }

        private void Ad_UpdateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                UpdateBar.Display = false;
                return;
            }
            if (e.Error != null)
            {
                UpdateBar.Display = false;
                logger.Error(e.Error, "下载更新时出现错误");
                return;
            }

            UpdateAction = () =>
            {
                System.Windows.Forms.Application.Restart();
            };
            UpdateBar.MainText = "更新已下载好，要现在重启软件吗？";
            UpdateBar.ButtonText = "重启软件";
            UpdateBar.ShowProgressBar = false;
        }

        private void LoadSettings()
        {
            // TODO: Load Settings
            Recorder.Settings.Clip_Future = 10;
            Recorder.Settings.Clip_Past = 20;
            Recorder.Settings.SavePath = @"D:\录播姬测试保存";
        }

        private void LoadRooms()
        {
            Recorder.AddRoom(7275510);
            Recorder.Rooms[0].Start();
        }

        private void TextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                Clipboard.SetText(textBlock.Text);
            }
        }

        private void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 不再需要了（？）
        }

        /// <summary>
        /// 触发回放剪辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clip_Click(object sender, RoutedEventArgs e)
        {
            var rr = _GetSenderAsRecordedRoom(sender);
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
            Task.Run(() => Recorder.RemoveRoom(rr));
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

        }

        private RecordedRoom _GetSenderAsRecordedRoom(object sender) => (sender as Button).DataContext as RecordedRoom;
    }
}
