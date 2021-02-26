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
            this.VersionTextBlock.Text = " " + GitVersionInformation.FullSemVer;
            this.VersionTextBlock.ToolTip = GitVersionInformation.InformationalVersion;
        }
    }
}
