using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for Updater.xaml
    /// </summary>
    public partial class UpdateBarUserControl : UserControl
    {
        public UpdateBarUserControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public bool Display
        {
            get { return (bool)GetValue(DisplayProperty); }
            set { SetValue(DisplayProperty, value); }
        }
        public static readonly DependencyProperty DisplayProperty =
            DependencyProperty.Register("Display", typeof(bool), typeof(UpdateBarUserControl), new UIPropertyMetadata(false));

        public bool ShowProgressBar
        {
            get { return (bool)GetValue(ShowProgressBarProperty); }
            set { SetValue(ShowProgressBarProperty, value); }
        }
        public static readonly DependencyProperty ShowProgressBarProperty =
            DependencyProperty.Register("ShowProgressBar", typeof(bool), typeof(UpdateBarUserControl), new UIPropertyMetadata(false));

        public string MainText
        {
            get { return (string)GetValue(MainTextProperty); }
            set { SetValue(MainTextProperty, value); }
        }
        public static readonly DependencyProperty MainTextProperty =
            DependencyProperty.Register("MainText", typeof(string), typeof(UpdateBarUserControl), new UIPropertyMetadata("有新更新可用！"));

        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(UpdateBarUserControl), new UIPropertyMetadata("下载更新"));

        public string ProgressText
        {
            get { return (string)GetValue(ProgressTextProperty); }
            set { SetValue(ProgressTextProperty, value); }
        }
        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register("ProgressText", typeof(string), typeof(UpdateBarUserControl), new UIPropertyMetadata("0KiB / 0KiB - 0%"));

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(UpdateBarUserControl), new UIPropertyMetadata(0d));


        public event RoutedEventHandler MainButtonClick;


        private void CloseUpdateBar(object sender, MouseButtonEventArgs e)
        {
            Display = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainButtonClick?.Invoke(sender, e);
        }
    }
}
