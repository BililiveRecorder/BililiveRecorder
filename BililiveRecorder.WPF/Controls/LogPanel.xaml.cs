using System.Windows;
using System.Windows.Controls;

#nullable enable
namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for LogPanel.xaml
    /// </summary>
    public partial class LogPanel : UserControl
    {
        public LogPanel()
        {
            this.InitializeComponent();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView) return;
            if (listView.View is not GridView view) return;

            var w = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 105 - 60 - 105;

            view.Columns[0].Width = 105;
            view.Columns[1].Width = 60;
            view.Columns[2].Width = 105;
            view.Columns[3].Width = w;
        }
    }
}
