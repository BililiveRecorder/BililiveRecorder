using System;
using System.Threading;
using System.Windows;
using BililiveRecorder.WPF.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using ModernWpf.Controls;
using WPFLocalizeExtension.Engine;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for NewMainWindow.xaml
    /// </summary>
    public partial class NewMainWindow : Window
    {
        public string SoftwareVersion { get; }

        public NewMainWindow()
        {
            this.SoftwareVersion = BuildInfo.Version + " " + BuildInfo.HeadShaShort;
                       
            LocalizeDictionary.Instance.OutputMissingKeys = true;
            LocalizeDictionary.Instance.MissingKeyEvent += this.Instance_MissingKeyEvent;

            this.InitializeComponent();

            // this.Title = "B站录播姬 " + BuildInfo.Version + " " + BuildInfo.HeadShaShort;

            SingleInstance.NotificationReceived += this.SingleInstance_NotificationReceived;
        }

        private void Instance_MissingKeyEvent(object sender, MissingKeyEventArgs e)
        {
            MessageBox.Show("Missing: " + e.Key);
        }

        private void SingleInstance_NotificationReceived(object sender, EventArgs e) => this.SuperActivateAction();

        public event EventHandler NativeBeforeWindowClose;

        internal Action<string, string, BalloonIcon> ShowBalloonTipCallback { get; set; }

        internal void CloseWithoutConfirmAction()
        {
            this.CloseConfirmed = true;
            this.Close();
        }

        internal void SuperActivateAction()
        {
            try
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Topmost = true;
                this.Activate();
                this.Topmost = false;
                this.Focus();
            }
            catch (Exception)
            { }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                this.ShowBalloonTipCallback?.Invoke("B站录播姬", "录播姬已最小化到托盘，左键单击图标恢复界面", BalloonIcon.Info);
            }
        }

        #region Confirm Close Window

        private bool CloseConfirmed = false;

        private readonly SemaphoreSlim CloseWindowSemaphoreSlim = new SemaphoreSlim(1, 1);

        public bool PromptCloseConfirm { get; set; } = true;

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.PromptCloseConfirm && !this.CloseConfirmed)
            {
                e.Cancel = true;
                if (this.CloseWindowSemaphoreSlim.Wait(0))
                {
                    try
                    {
                        if (await new CloseWindowConfirmDialog().ShowAsync() == ContentDialogResult.Primary)
                        {
                            this.CloseConfirmed = true;
                            this.CloseWindowSemaphoreSlim.Release();
                            this.Close();
                            return;
                        }
                    }
                    catch (Exception) { }
                    this.CloseWindowSemaphoreSlim.Release();
                }
            }
            else
            {
                SingleInstance.NotificationReceived -= this.SingleInstance_NotificationReceived;
                NativeBeforeWindowClose?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        #endregion
    }
}
