using BililiveRecorder.Core.Templating;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
        private static readonly ILogger logger = Log.ForContext<SettingsPage>();

        private static readonly FileNameGenerator.FileNameContextData data = new()
        {
            Name = "3号直播间",
            RoomId = 23058,
            ShortId = 3,
            Title = "哔哩哔哩音悦台",
            AreaParent = "电台",
            AreaChild = "唱见电台"
        };

        private readonly FileNameGenerator? fileNameGenerator;

        public SettingsPage() : this((FileNameGenerator?)(RootPage.ServiceProvider?.GetService(typeof(FileNameGenerator))))
        {
        }

        public SettingsPage(FileNameGenerator? fileNameGenerator)
        {
            this.fileNameGenerator = fileNameGenerator;

            this.InitializeComponent();
        }

        private void TestFileNameTemplate_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.fileNameGenerator is not { } fileNameGenerator)
                return;

            logger.Debug("Test file name template start");
            var (_, relativePath) = fileNameGenerator.CreateFilePath(data);
            logger.Debug("Test file name template end");

            this.FileNameTestResult.Visibility = System.Windows.Visibility.Visible;
            this.FileNameTestResult.Text = relativePath;
        }
    }
}
