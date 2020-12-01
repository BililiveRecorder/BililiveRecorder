using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for WorkDirectorySelectorDialog.xaml
    /// </summary>
    public partial class WorkDirectorySelectorDialog : INotifyPropertyChanged
    {
        private string error = string.Empty;
        private string path = string.Empty;

        public string Error { get => error; set => SetField(ref error, value); }

        public string Path { get => path; set => SetField(ref path, value); }

        public WorkDirectorySelectorDialog()
        {
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            field = value; OnPropertyChanged(propertyName); return true;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var fileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = "选择录播姬工作目录路径",
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true,
                InitialDirectory = Path,
            };
            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Path = fileDialog.FileName;
            }
        }
    }
}
