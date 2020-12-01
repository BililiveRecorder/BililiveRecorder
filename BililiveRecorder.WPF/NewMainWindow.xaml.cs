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

            SuperActivate += NewMainWindow_SuperActivate;
            CloseWithoutConfirm += NewMainWindow_CloseWithoutConfirm;
            SingleInstance.NotificationReceived += (sender, e) => SuperActivateAction();
        }

        internal Action<string, string, BalloonIcon> ShowBalloonTipCallback { get; set; }

        private void NewMainWindow_CloseWithoutConfirm(object sender, RoutedEventArgs e)
        {
            CloseWithoutConfirmAction();
        }

        internal void CloseWithoutConfirmAction()
        {
            CloseConfirmed = true;
            Close();
        }

        private void NewMainWindow_SuperActivate(object sender, RoutedEventArgs e)
        {
            SuperActivateAction();
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
                // RaiseEvent(new RoutedEventArgs(ShowBalloonTipEvent));
                // RaiseEvent(new ShowBalloonTipRoutedEventArgs(ShowBalloonTipEvent)
                // {
                //     Title = "B站录播姬",
                //     Message = "录播姬已最小化到托盘，左键单击图标恢复界面。",
                //     Symbol = BalloonIcon.Info
                // });
            }
        }

        #region Routed Events

        public static readonly RoutedEvent BeforeWindowCloseEvent
            = EventManager.RegisterRoutedEvent("BeforeWindowClose", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(NewMainWindow));

        public event RoutedEventHandler BeforeWindowClose
        {
            add { AddHandler(BeforeWindowCloseEvent, value); }
            remove { RemoveHandler(BeforeWindowCloseEvent, value); }
        }

        public static readonly RoutedEvent CloseWithoutConfirmEvent
            = EventManager.RegisterRoutedEvent("CloseWithoutConfirm", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NewMainWindow));

        public event RoutedEventHandler CloseWithoutConfirm
        {
            add { AddHandler(CloseWithoutConfirmEvent, value); }
            remove { RemoveHandler(CloseWithoutConfirmEvent, value); }
        }

        public static readonly RoutedEvent SuperActivateEvent
            = EventManager.RegisterRoutedEvent("SuperActivate", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NewMainWindow));

        public event RoutedEventHandler SuperActivate
        {
            add { AddHandler(SuperActivateEvent, value); }
            remove { RemoveHandler(SuperActivateEvent, value); }
        }

        public delegate void ShowBalloonTipRoutedEventHandler(object sender, ShowBalloonTipRoutedEventArgs e);

        public static readonly RoutedEvent ShowBalloonTipEvent
           = EventManager.RegisterRoutedEvent("ShowBalloonTip", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(NewMainWindow));

        public event RoutedEventHandler ShowBalloonTip
        {
            add { AddHandler(ShowBalloonTipEvent, value); }
            remove { RemoveHandler(ShowBalloonTipEvent, value); }
        }

        public class ShowBalloonTipRoutedEventArgs : RoutedEventArgs
        {
            public string Title { get; set; }
            public string Message { get; set; }
            public BalloonIcon Symbol { get; set; }

            public ShowBalloonTipRoutedEventArgs() { }
            public ShowBalloonTipRoutedEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
            public ShowBalloonTipRoutedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }
        }

        #endregion

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
                RaiseEvent(new RoutedEventArgs(BeforeWindowCloseEvent));
                return;
            }
        }

        #endregion

    }
}
