using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using BililiveRecorder.WPF.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using ModernWpf.Controls;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;

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

            Pages.AnnouncementPage.CultureInfo = CultureInfo.CurrentUICulture;
            LocalizeDictionary.Instance.Culture = CultureInfo.CurrentUICulture;
#if DEBUG
            LocalizeDictionary.Instance.OutputMissingKeys = true;
            LocalizeDictionary.Instance.MissingKeyEvent += (object sender, MissingKeyEventArgs e) => MessageBox.Show("Missing: " + e.Key);
#endif

            this.InitializeComponent();

            SingleInstance.NotificationReceived += this.SingleInstance_NotificationReceived;
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
                var title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:TaskbarIconControl_Title");
                var body = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:TaskbarIconControl_MinimizedNotification");
                this.ShowBalloonTipCallback?.Invoke(title, body, BalloonIcon.Info);
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
