using System.Text.RegularExpressions;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage
    {
        public AboutPage()
        {
            this.InitializeComponent();

            if (!string.IsNullOrEmpty(GitVersionInformation.CommitDate))
            {
                var match = Regex.Match(GitVersionInformation.CommitDate, @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$");
                if (match.Success)
                {
                    this.CopyrightTextBlock.Text = $" © {match.Groups["year"].Value} Genteure";
                }
            }
        }
    }
}
