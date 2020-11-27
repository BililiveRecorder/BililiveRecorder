using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// TimedMessageBox.xaml 的交互逻辑
    /// </summary>
    public partial class TimedMessageBox : Window, INotifyPropertyChanged
    {
        public string Message { get => _message; set => SetField(ref _message, value); }
        private string _message = string.Empty;

        public int CountDown { get => _countdown; set => SetField(ref _countdown, value); }
        private int _countdown = 10;

        private DispatcherTimer timer;

        public TimedMessageBox()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1), IsEnabled = false };
            timer.Tick += (sender, e) =>
            {
                CountDown -= 1;
                if (CountDown <= 0)
                {
                    Cancel();
                    timer.Stop();
                    timer = null;
                }
            };

            DataContext = this;

            InitializeComponent();
        }

        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            timer.Stop();
            timer = null;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e) => Cancel();

        private void Cancel()
        {
            DialogResult = false;
            timer.Stop();
            timer = null;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timer?.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            field = value; OnPropertyChanged(propertyName); return true;
        }
    }
}
