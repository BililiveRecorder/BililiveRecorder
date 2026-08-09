using Avalonia.Controls;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            this.InitializeComponent();
            this.VersionTextBlock.Text = $"版本: 2.17.3"; // TODO: Get from GitVersionInformation
        }
    }
}
