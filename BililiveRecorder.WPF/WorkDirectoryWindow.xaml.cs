using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// WorkDirectoryWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WorkDirectoryWindow : Window, INotifyPropertyChanged
    {
        public static readonly SolidColorBrush Red = new SolidColorBrush(Color.FromArgb(0xFF, 0xF7, 0x1B, 0x1B));
        public static readonly SolidColorBrush Green = new SolidColorBrush(Color.FromArgb(0xFF, 0x0B, 0xB4, 0x22));

        public WorkDirectoryWindow()
        {
            DataContext = this;
            InitializeComponent();
            PropertyChanged += WorkDirectoryWindow_PropertyChanged;
            //DialogResult = false;
        }

        private void WorkDirectoryWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfirmEnabled))
            {
                if (ConfirmEnabled)
                {
                    StatusColor = Green;
                }
                else
                {
                    StatusColor = Red;
                }
            }
            else if (e.PropertyName == nameof(WorkPath))
            {
                CheckPath();
            }
        }

        private void CheckPath()
        {
            var c = WorkPath;
            if (Directory.Exists(c))
            {
                if (Directory.EnumerateFiles(c).Any())
                {
                    string config = Path.Combine(c, "config.json");
                    if (File.Exists(config))
                    {
                        try
                        {
                            var text = File.ReadAllText(config);
                            var j = JObject.Parse(text);
                            if (j["version"] == null || j["data"] == null)
                            {
                                StatusText = "配置文件损坏";
                                ConfirmEnabled = false;
                            }
                            else
                            {
                                StatusText = "录播姬曾经使用过的目录";
                                ConfirmEnabled = true;
                            }
                        }
                        catch (Exception)
                        {
                            StatusText = "配置文件不可读";
                            ConfirmEnabled = false;
                        }
                    }
                    else
                    {
                        StatusText = "此文件夹已有其他文件";
                        ConfirmEnabled = false;
                    }
                }
                else
                {
                    StatusText = "可用的空文件夹";
                    ConfirmEnabled = true;
                }
            }
            else
            {
                StatusText = "目录不存在";
                ConfirmEnabled = false;
            }
        }

        private string _workPath;
        public string WorkPath { get => _workPath; set => SetField(ref _workPath, value); }

        private string _statusText = "请选择目录";
        public string StatusText { get => _statusText; set => SetField(ref _statusText, value); }

        private SolidColorBrush _statusColor = Red;
        public SolidColorBrush StatusColor { get => _statusColor; set => SetField(ref _statusColor, value); }

        private bool _confirmEnabled;
        public bool ConfirmEnabled { get => _confirmEnabled; set => SetField(ref _confirmEnabled, value); }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = "选择录播姬工作目录路径",
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true,
                InitialDirectory = WorkPath,
            };
            if (fileDialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                WorkPath = fileDialog.FileName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            field = value; OnPropertyChanged(propertyName); return true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
