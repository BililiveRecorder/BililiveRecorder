using System.Windows;
using System.Windows.Markup;

namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for SettingWithDefault.xaml
    /// </summary>
    [ContentProperty("InnerContent")]
    public partial class SettingWithDefault
    {
        public SettingWithDefault()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty HeaderProperty
           = DependencyProperty.Register("Header",
               typeof(string),
               typeof(SettingWithDefault),
               new FrameworkPropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)this.GetValue(HeaderProperty);
            set => this.SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty IsSettingNotUsingDefaultProperty
            = DependencyProperty.Register("IsSettingNotUsingDefault",
                typeof(bool),
                typeof(SettingWithDefault),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsSettingNotUsingDefault
        {
            get => (bool)this.GetValue(IsSettingNotUsingDefaultProperty);
            set => this.SetValue(IsSettingNotUsingDefaultProperty, value);
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(object), typeof(SettingWithDefault));

        public object InnerContent
        {
            get => this.GetValue(InnerContentProperty);
            set => this.SetValue(InnerContentProperty, value);
        }
    }
}
