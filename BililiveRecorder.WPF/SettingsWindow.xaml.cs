using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using System.Windows;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public ConfigV1 Config { get; set; } = new ConfigV1();

        public SettingsWindow(MainWindow mainWindow, ConfigV1 config)
        {
            Owner = mainWindow;
            config.CopyPropertiesTo(Config);
            DataContext = Config;
            InitializeComponent();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            // if (!_CheckSavePath())
            // {
            //     return;
            // }

            DialogResult = true;
            Close();
        }

        // private bool _CheckSavePath()
        // {
        //     if (string.IsNullOrWhiteSpace(Config.SavePath))
        //     {
        //         MessageBox.Show("请设置一个录像保存路径", "保存路径不能为空", MessageBoxButton.OK, MessageBoxImage.Warning);
        //         return false;
        //     }
        //     return true;
        // }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            // if (!_CheckSavePath())
            // {
            //     return;
            // }

            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // var fileDialog = new CommonOpenFileDialog()
            // {
            //     IsFolderPicker = true,
            //     Multiselect = false,
            //     Title = "选择录制路径",
            //     AddToMostRecentlyUsedList = false,
            //     EnsurePathExists = true,
            //     NavigateToShortcut = true,
            //     InitialDirectory = Config.SavePath,
            // };
            // if (fileDialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            // {
            //     Config.SavePath = fileDialog.FileName;
            // }
        }
    }
}
