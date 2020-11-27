using System;
using System.Threading;
using System.Windows;
using BililiveRecorder.WPF.Controls;
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

            Title = "录播姬  " + BuildInfo.Version + " " + BuildInfo.HeadShaShort;

            BeforeWindowClose += NewMainWindow_BeforeWindowClose;
            SingleInstance.NotificationReceived += SingleInstance_NotificationReceived;
        }

        private void SingleInstance_NotificationReceived(object sender, EventArgs e)
        {
            WindowState = WindowState.Normal;
            Topmost = true;
            Activate();
            Topmost = false;
            Focus();
        }

        private bool CloseConfirmed = false;

        private readonly SemaphoreSlim CloseWindowSemaphoreSlim = new SemaphoreSlim(1, 1);

        public event EventHandler BeforeWindowClose;

        public bool PromptCloseConfirm { get; set; } = true;

        private void NewMainWindow_BeforeWindowClose(object sender, EventArgs e)
        {
            RootPage?.Shutdown();
            SingleInstance.Cleanup();
        }

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
                BeforeWindowClose?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        public void CloseWithoutConfirm()
        {
            CloseConfirmed = true;
            Close();
        }

        private void RootPage_CloseWindowRequested(object sender, EventArgs e)
        {
            CloseWithoutConfirm();
        }
    }
}
