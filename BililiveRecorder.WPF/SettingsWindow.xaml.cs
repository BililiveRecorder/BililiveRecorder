using BililiveRecorder.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
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
using System.Windows.Shapes;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public Settings Settings { get; set; } = new Settings();

        public SettingsWindow(MainWindow mainWindow, Settings settings)
        {
            Owner = mainWindow;
            settings.ApplyTo(Settings);
            DataContext = Settings;
            InitializeComponent();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (!_CheckSavePath()) return;
            DialogResult = true;
            Close();
        }

        private bool _CheckSavePath()
        {
            if (string.IsNullOrWhiteSpace(Settings.SavePath))
            {
                MessageBox.Show("请设置一个录像保存路径", "保存路径不能为空", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (!_CheckSavePath()) return;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = "选择录制路径",
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true,
                InitialDirectory = Settings.SavePath,
            };
            if (fileDialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                Settings.SavePath = fileDialog.FileName;
            }
        }
    }
}
