using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for LogPage.xaml
    /// </summary>
    public partial class LogPage
    {
        public LogPage()
        {
            this.InitializeComponent();
            this.VersionTextBlock.Text = " " + BuildInfo.Version + " " + BuildInfo.HeadShaShort;
        }

        private void TextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is TextBlock textBlock)
                {
                    Clipboard.SetText(textBlock.Text);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as ScrollViewer)?.ScrollToEnd();
        }
    }
}
