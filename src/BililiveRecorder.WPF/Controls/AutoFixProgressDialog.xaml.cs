using System.Threading;
using System.Windows;

#nullable enable
namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for AutoFixProgressDialog.xaml
    /// </summary>
    public partial class AutoFixProgressDialog
    {
        public AutoFixProgressDialog()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register(nameof(Progress), typeof(int), typeof(AutoFixProgressDialog), new PropertyMetadata(0));

        public int Progress
        {
            get => (int)this.GetValue(ProgressProperty);
            set => this.SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty CancelButtonVisibilityProperty =
            DependencyProperty.Register(nameof(CancelButtonVisibility), typeof(Visibility), typeof(AutoFixProgressDialog), new PropertyMetadata(Visibility.Collapsed));

        public Visibility CancelButtonVisibility
        {
            get => (Visibility)this.GetValue(CancelButtonVisibilityProperty);
            set => this.SetValue(CancelButtonVisibilityProperty, value);
        }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e) => this.CancellationTokenSource?.Cancel();
    }
}
