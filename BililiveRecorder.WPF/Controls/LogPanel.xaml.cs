using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            ((INotifyCollectionChanged)this.logView.Items).CollectionChanged += this.LogView_CollectionChanged;
        }

        private void LogView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (!this.logView.IsMouseOver && VisualTreeHelper.GetChildrenCount(this.logView) > 0)
                {
                    var border = (Border)VisualTreeHelper.GetChild(this.logView, 0);
                    var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
            }
            catch (Exception)
            { }
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
