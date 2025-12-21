using System;
using System.Windows;
using System.Windows.Controls;

namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for TaskbarIconControl.xaml
    /// </summary>
    public partial class TaskbarIconControl : UserControl
    {
        public TaskbarIconControl()
        {
            this.InitializeComponent();

            using var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/BililiveRecorder.WPF;component/ico.ico")).Stream;
            this.TaskbarIcon.Icon = new System.Drawing.Icon(iconStream);

            // AddHandler(NewMainWindow.ShowBalloonTipEvent, (RoutedEventHandler)UserControl_ShowBalloonTip);
            if (Application.Current.MainWindow is NewMainWindow nmw)
            {
                nmw.ShowBalloonTipCallback = (title, msg, sym) =>
                {
                    this.TaskbarIcon.ShowBalloonTip(title, msg, sym);
                };
            }
        }

        private void TaskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            // RaiseEvent(new RoutedEventArgs(NewMainWindow.SuperActivateEvent));
            (Application.Current.MainWindow as NewMainWindow)?.SuperActivateAction();
        }

        private void MenuItem_OpenMainWindow_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as NewMainWindow)?.SuperActivateAction();
        }

        private void MenuItem_Quit_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as NewMainWindow)?.CloseWithoutConfirmAction();
        }

        /*
        private void UserControl_ShowBalloonTip(object sender, RoutedEventArgs e)
        {
            var f = e as NewMainWindow.ShowBalloonTipRoutedEventArgs;
            TaskbarIcon.ShowBalloonTip(f.Title, f.Message, f.Symbol);
        }
        */
    }
}
