using System;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BililiveRecorder.Core;
using Avalonia.Threading;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    /// <summary>
    /// 开播提醒弹窗：屏幕右下角小窗，8 秒后自动关闭，点击打开直播间。
    /// 跨平台替代 WPF 版的系统 Toast 通知。
    /// </summary>
    public partial class StreamStartedToast : Window
    {
        private const int AutoCloseSeconds = 8;

        private readonly string? roomUrl;
        private Timer? autoCloseTimer;

        public StreamStartedToast()
        {
            this.InitializeComponent();
            this.ShowInTaskbar = false;
        }

        public StreamStartedToast(IRoom room)
            : this()
        {
            this.roomUrl = "https://live.bilibili.com/" + room.RoomConfig.RoomId;
            this.NameText.Text = room.Name + " 开播了";
            this.TitleText.Text = room.Title;
            this.AreaText.Text = $"{room.AreaNameParent} · {room.AreaNameChild}";
            this.Title = this.NameText.Text;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            this.PositionToBottomRight();

            this.autoCloseTimer = new Timer(AutoCloseSeconds * 1000)
            {
                AutoReset = false,
            };
            this.autoCloseTimer.Elapsed += (s, e) => Dispatcher.UIThread.Post(this.Close);
            this.autoCloseTimer.Start();
        }

        private void PositionToBottomRight()
        {
            try
            {
                var screen = this.Screens.ScreenFromVisual(this) ?? this.Screens.Primary;
                if (screen is null)
                    return;

                var area = screen.WorkingArea;
                // WorkingArea 为像素坐标，Position 也是像素坐标
                var x = area.X + area.Width - (int)(this.Bounds.Width * this.RenderScaling) - 16;
                var y = area.Y + area.Height - (int)(this.Bounds.Height * this.RenderScaling) - 16;
                this.Position = new Avalonia.PixelPoint(x, y);
            }
            catch (Exception)
            {
            }
        }

        private void Toast_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (this.roomUrl is not null)
                    Process2.OpenUrl(this.roomUrl);
            }
            catch (Exception)
            {
            }
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            this.autoCloseTimer?.Dispose();
            this.autoCloseTimer = null;
            base.OnClosed(e);
        }

        private static class Process2
        {
            internal static void OpenUrl(string url) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                {
                    UseShellExecute = true,
                });
        }
    }
}
