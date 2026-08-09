using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop
{
    public partial class MainWindow : Window
    {
        public string SoftwareVersion { get; }

        public MainWindow()
        {
            this.SoftwareVersion = "2.17.3"; // TODO: Get from GitVersionInformation
            this.InitializeComponent();
            this.Closing += Window_Closing;
        }

        public event EventHandler? NativeBeforeWindowClose;

        internal void CloseWithoutConfirmAction()
        {
            this.CloseConfirmed = true;
            Dispatcher.UIThread.Post(() => this.Close());
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
            }
            catch (Exception)
            { }
        }

        public bool HideToTray { get; set; } = false;
        public bool PromptCloseConfirm { get; set; } = true;
        private bool CloseConfirmed = false;
        private readonly SemaphoreSlim CloseWindowSemaphoreSlim = new SemaphoreSlim(1, 1);

        private async void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (this.PromptCloseConfirm && !this.CloseConfirmed)
            {
                e.Cancel = true;

                if (await this.CloseWindowSemaphoreSlim.WaitAsync(0))
                {
                    try
                    {
                        // TODO: Show close confirm dialog
                        this.CloseConfirmed = true;
                        Dispatcher.UIThread.Post(() => this.Close());
                        return;
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
                return;
            }
        }
    }
}