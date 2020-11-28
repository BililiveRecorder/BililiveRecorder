using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;
using BililiveRecorder.WPF.Controls;
using BililiveRecorder.WPF.Models;
using CommandLine;
using ModernWpf.Controls;
using Path = System.IO.Path;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for RootPage.xaml
    /// </summary>
    public partial class RootPage : UserControl
    {
        private readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type>();
        private readonly string lastdir_path = Path.Combine(Path.GetDirectoryName(typeof(RootPage).Assembly.Location), "lastdir.txt");

        private IContainer Container { get; set; }
        private ILifetimeScope RootScope { get; set; }

        private int SettingsClickCount = 0;

        internal RootModel Model { get; private set; }

        public RootPage()
        {
            void AddType(Type t) => PageMap.Add(t.Name, t);
            AddType(typeof(RoomListPage));
            AddType(typeof(LogPage));
            AddType(typeof(SettingsPage));
            AddType(typeof(AdvancedSettingsPage));

            Model = new RootModel();
            DataContext = Model;

            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            Container = builder.Build();
            RootScope = Container.BeginLifetimeScope("recorder_root");

            InitializeComponent();
            AdvancedSettingsPageItem.Visibility = Visibility.Hidden;

            Loaded += RootPage_Loaded;
        }

        private async void RootPage_Loaded(object sender, RoutedEventArgs e)
        {
            bool first_time = true;

            var recorder = RootScope.Resolve<IRecorder>();
            var error = string.Empty;
            string path;
            while (true)
            {
                CommandLineOption commandLineOption = null;
                if (first_time)
                {
                    first_time = false;
                    Parser.Default
                        .ParseArguments<CommandLineOption>(Environment.GetCommandLineArgs())
                        .WithParsed(x => commandLineOption = x);

                    if (!string.IsNullOrWhiteSpace(commandLineOption.WorkDirectory))
                    {
                        path = Path.GetFullPath(commandLineOption.WorkDirectory);
                        goto check_path;
                    }
                }

                string lastdir = string.Empty;
                try
                {
                    if (File.Exists(lastdir_path))
                    {
                        lastdir = File.ReadAllText(lastdir_path).Replace("\r", "").Replace("\n", "").Trim();
                    }
                }
                catch (Exception) { }
                var w = new WorkDirectorySelectorDialog
                {
                    Error = error,
                    Path = lastdir
                };
                var result = await w.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    RaiseEvent(new RoutedEventArgs(NewMainWindow.CloseWithoutConfirmEvent));
                    return;
                }

                path = Path.GetFullPath(w.Path);
            check_path:
                var config = Path.Combine(path, "config.json");

                if (!Directory.Exists(path))
                {
                    error = "目录不存在";
                    continue;
                }
                else if (!Directory.EnumerateFiles(path).Any())
                {
                    // 可用的空文件夹
                }
                else if (!File.Exists(config))
                {
                    error = "目录已有其他文件";
                    continue;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(commandLineOption?.WorkDirectory))
                    {
                        File.WriteAllText(lastdir_path, path);
                    }
                }
                catch (Exception) { }

                // 检查已经在同目录运行的其他进程
                if (SingleInstance.CheckMutex(path))
                {
                    if (recorder.Initialize(path))
                    {
                        Model.Recorder = recorder;
                        RoomListPageNavigationViewItem.IsSelected = true;
                        break;
                    }
                    else
                    {
                        error = "配置文件加载失败";
                        continue;
                    }
                }
                else
                {
                    RaiseEvent(new RoutedEventArgs(NewMainWindow.CloseWithoutConfirmEvent));
                    return;
                }
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            SettingsClickCount = 0;
            if (args.IsSettingsSelected)
            {
                MainFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                var selectedItemTag = (string)selectedItem.Tag;
                if (PageMap.ContainsKey(selectedItemTag))
                {
                    var pageType = PageMap[selectedItemTag];
                    MainFrame.Navigate(pageType);
                }
            }
        }

        private void NavigationViewItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (++SettingsClickCount > 3)
            {
                SettingsClickCount = 0;
                AdvancedSettingsPageItem.Visibility = AdvancedSettingsPageItem.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void UserControl_BeforeWindowClose(object sender, RoutedEventArgs e)
        {
            Model.Dispose();
            SingleInstance.Cleanup();
        }
    }
}
