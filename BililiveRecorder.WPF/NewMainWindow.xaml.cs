using System;
using System.Threading;
using System.Windows;
using BililiveRecorder.WPF.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using ModernWpf.Controls;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for NewMainWindow.xaml
    /// </summary>
    public partial class NewMainWindow : Window
    {
        public NewMainWindow()
        {
            InitializeComponent();

            Title = "B站录播姬 " + BuildInfo.Version + " " + BuildInfo.HeadShaShort;

            SingleInstance.NotificationReceived += (sender, e) => SuperActivateAction();
        }

        public event EventHandler NativeBeforeWindowClose;

        internal Action<string, string, BalloonIcon> ShowBalloonTipCallback { get; set; }

        internal void CloseWithoutConfirmAction()
        {
            CloseConfirmed = true;
            Close();
        }

        internal void SuperActivateAction()
        {
            Show();
            WindowState = WindowState.Normal;
            Topmost = true;
            Activate();
            Topmost = false;
            Focus();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                ShowBalloonTipCallback?.Invoke("B站录播姬", "录播姬已最小化到托盘，左键单击图标恢复界面", BalloonIcon.Info);
            }
        }

        #region Confirm Close Window

        private bool CloseConfirmed = false;

        private readonly SemaphoreSlim CloseWindowSemaphoreSlim = new SemaphoreSlim(1, 1);

        public bool PromptCloseConfirm { get; set; } = true;

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (PromptCloseConfirm && !CloseConfirmed)
            {
                e.Cancel = true;
                if (CloseWindowSemaphoreSlim.Wait(0))
                {
                    try
                    {
                        if (await new CloseWindowConfirmDialog().ShowAsync() == ContentDialogResult.Primary)
                        {
                            CloseConfirmed = true;
                            CloseWindowSemaphoreSlim.Release();
                            Close();
                            return;
                        }
                    }
                    catch (Exception) { }
                    CloseWindowSemaphoreSlim.Release();
                }
            }
            else
            {
                NativeBeforeWindowClose?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        #endregion
    }
}
