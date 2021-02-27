using System.Windows;

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
    }
}
