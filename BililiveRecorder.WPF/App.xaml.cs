using System.Windows;

#nullable enable
namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (e != null)
                e.Cancel = true;
            (Current.MainWindow as NewMainWindow)?.CloseWithoutConfirmAction();
        }
    }
}
