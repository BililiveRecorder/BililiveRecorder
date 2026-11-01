using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.WPF.Models;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using NotifyIconBalloon = System.Windows.Forms.ToolTipIcon;

namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// 替代 Hardcodet 的 BalloonIcon，供 ShowBalloonTipCallback 使用。
    /// </summary>
    internal enum BalloonIcon
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// 托盘图标控件。使用官方 System.Windows.Forms.NotifyIcon 实现，
    /// 避免 Hardcodet.NotifyIcon.Wpf 把气泡/tooltip 弹到屏幕左上角的已知 bug。
    /// </summary>
    public partial class TaskbarIconControl : UserControl, IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Popup _trayToolTipPopup;
        private readonly DispatcherTimer _toolTipCloseTimer;
        private bool _disposed;

        public TaskbarIconControl()
        {
            this.InitializeComponent();

            // 承载富 tooltip 与右键菜单的资源
            this._trayToolTipPopup = (Popup)this.FindResource("TrayToolTipPopup");
            this._trayToolTipPopup.PlacementTarget = this;
            this._trayToolTipPopup.MouseLeave += (s, e) => this.HideTrayToolTip();

            var contextMenu = (ContextMenu)this.FindResource("TrayContextMenu");
            contextMenu.PlacementTarget = this;

            this._notifyIcon = new NotifyIcon
            {
                // 从当前 exe 提取图标（WinForms NotifyIcon 需要 System.Drawing.Icon）
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Text = "BililiveRecorder",
                Visible = true,
            };

            this._notifyIcon.MouseMove += this.NotifyIcon_MouseMove;
            this._notifyIcon.MouseClick += this.NotifyIcon_MouseClick;
            this._notifyIcon.MouseDoubleClick += this.NotifyIcon_MouseDoubleClick;

            this._toolTipCloseTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(4),
            };
            this._toolTipCloseTimer.Tick += (s, e) => this.HideTrayToolTip();

            this.Loaded += this.TaskbarIconControl_Loaded;
            this.Unloaded += this.TaskbarIconControl_Unloaded;

            if (Application.Current.MainWindow is NewMainWindow nmw)
            {
                nmw.ShowBalloonTipCallback = (title, msg, sym) =>
                {
                    // 当关闭托盘提示开关打开时，同时抑制气球提示（开播通知、最小化提示等）
                    if (this.IsToolTipDisabled())
                        return;

                    this._notifyIcon.ShowBalloonTip(5000, title, msg, MapBalloonIcon(sym));
                };
            }
        }

        private void TaskbarIconControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RootModel model)
            {
                model.PropertyChanged += this.RootModel_PropertyChanged;
                if (model.Recorder?.Config?.Global != null)
                {
                    model.Recorder.Config.Global.PropertyChanged += this.Global_PropertyChanged;
                }
            }
        }

        private void TaskbarIconControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RootModel model)
            {
                model.PropertyChanged -= this.RootModel_PropertyChanged;
                if (model.Recorder?.Config?.Global != null)
                {
                    model.Recorder.Config.Global.PropertyChanged -= this.Global_PropertyChanged;
                }
            }
        }

        private void Global_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 实时响应“关闭托盘图标悬浮提示”开关：开启时立即隐藏并阻止再次显示
            if (e.PropertyName == nameof(GlobalConfig.WpfDisableTrayToolTip))
            {
                if (this.IsToolTipDisabled())
                {
                    this.HideTrayToolTip();
                }
            }
        }

        private void RootModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RootModel.Recorder) && sender is RootModel model && model.Recorder?.Config?.Global != null)
            {
                model.Recorder.Config.Global.PropertyChanged += this.Global_PropertyChanged;
            }
        }

        #region 托盘事件
        private void NotifyIcon_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.IsToolTipDisabled())
                return;

            if (this._trayToolTipPopup.IsOpen)
                return;

            this.ShowTrayToolTipNearCursor();
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                this.HideTrayToolTip();
                var contextMenu = (ContextMenu)this.FindResource("TrayContextMenu");
                contextMenu.IsOpen = true;
            }
        }

        private void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.HideTrayToolTip();
                this.ShowHideMainWindow();
            }
        }
        #endregion

        #region 富 tooltip 显示/隐藏（自绘 Popup，位置跟随鼠标，绝不会跑到左上角）
        private void ShowTrayToolTipNearCursor()
        {
            if (!GetCursorPos(out POINT cursorPos))
                return;

            // 屏幕坐标直接用于 Placement=Absolute 的 Popup
            this._trayToolTipPopup.HorizontalOffset = cursorPos.X + 12;
            this._trayToolTipPopup.VerticalOffset = cursorPos.Y + 12;
            this._trayToolTipPopup.IsOpen = true;

            this._toolTipCloseTimer.Stop();
        }

        private void HideTrayToolTip()
        {
            this._toolTipCloseTimer.Stop();
            this._trayToolTipPopup.IsOpen = false;
        }
        #endregion

        #region 右键菜单与双击
        private void ShowHideMainWindow()
        {
            if (!(Application.Current.MainWindow is NewMainWindow nmw))
                return;

            if (nmw.Visibility == Visibility.Visible && nmw.WindowState != WindowState.Minimized)
            {
                nmw.Hide();
            }
            else
            {
                nmw.SuperActivateAction();
            }
        }

        private void MenuItemShowHide_Click(object sender, RoutedEventArgs e) => this.ShowHideMainWindow();

        private void MenuItemStartAll_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RootModel model && model.Recorder != null)
            {
                foreach (var room in model.Recorder.Rooms)
                {
                    room.StartRecord();
                }
            }
        }

        private void MenuItemStopAll_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RootModel model && model.Recorder != null)
            {
                foreach (var room in model.Recorder.Rooms)
                {
                    room.StopRecord();
                }
            }
        }

        private void MenuItemOpenWorkDir_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RootModel model && model.Recorder?.Config?.Global != null)
            {
                Process.Start("explorer.exe", model.Recorder.Config.Global.WorkDirectory);
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            // 触发主窗口关闭流程（含退出确认对话框）
            Application.Current.MainWindow?.Close();
        }
        #endregion

        private bool IsToolTipDisabled()
        {
            return this.DataContext is RootModel model
                && model.Recorder?.Config?.Global?.WpfDisableTrayToolTip == true;
        }

        private static NotifyIconBalloon MapBalloonIcon(BalloonIcon sym)
        {
            return sym switch
            {
                BalloonIcon.Warning => NotifyIconBalloon.Warning,
                BalloonIcon.Error => NotifyIconBalloon.Error,
                _ => NotifyIconBalloon.Info,
            };
        }

        #region Win32 辅助
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        #region 释放
        public void Dispose()
        {
            if (this._disposed)
                return;
            this._disposed = true;

            this._toolTipCloseTimer.Stop();
            this.HideTrayToolTip();

            try
            {
                this._notifyIcon.Visible = false;
                this._notifyIcon.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        #endregion
    }
}
