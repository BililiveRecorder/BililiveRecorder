using System;
using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string SoftwareVersion { get; }

        public string WindowTitle => $"mikufans 录播姬 - {SoftwareVersion}";

        public MainWindow()
        {
            this.SoftwareVersion = GitVersionInformation.FullSemVer;
            this.InitializeComponent();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? NativeBeforeWindowClose;

        internal void CloseWithoutConfirmAction()
        {
            this.CloseConfirmed = true;
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(this.Close);
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

        public bool HideToTray { get; set; } = false;
        public bool HideToTrayBlockedByContentDialog { get; set; } = false;

        private void Window_StateChanged()
        {
            if (this.HideToTray && !this.HideToTrayBlockedByContentDialog && this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                // Note: System tray notification would be implemented here if supported
            }
        }

        #region Confirm Close Window

        private bool CloseConfirmed = false;
        private readonly SemaphoreSlim CloseWindowSemaphoreSlim = new SemaphoreSlim(1, 1);
        public bool PromptCloseConfirm { get; set; } = true;

        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            if (this.PromptCloseConfirm && !this.CloseConfirmed)
            {
                e.Cancel = true;

                if (await this.CloseWindowSemaphoreSlim.WaitAsync(0))
                {
                    try
                    {
                        // Show close confirmation dialog
                        var dialog = new Controls.CloseWindowConfirmDialog();
                        var result = await dialog.ShowAsync();
                        if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
                        {
                            this.CloseConfirmed = true;
                            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(this.Close);
                            return;
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        this.CloseWindowSemaphoreSlim.Release();
                    }
                }
            }
            else
            {
                Log.Logger.ForContext<MainWindow>().Debug("Window Closing");
                NativeBeforeWindowClose?.Invoke(this, EventArgs.Empty);
            }
            
            base.OnClosing(e);
        }

        #endregion
    }
}
