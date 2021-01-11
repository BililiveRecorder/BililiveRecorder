using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;
using BililiveRecorder.WPF.Controls;
using BililiveRecorder.WPF.Models;
using CommandLine;
using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using NLog;
using Path = System.IO.Path;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for RootPage.xaml
    /// </summary>
    public partial class RootPage : UserControl
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type>();
        private readonly string lastdir_path = Path.Combine(Path.GetDirectoryName(typeof(RootPage).Assembly.Location), "lastdir.txt");
        private readonly NavigationTransitionInfo transitionInfo = new DrillInNavigationTransitionInfo();

        private IContainer Container { get; set; }
        private ILifetimeScope RootScope { get; set; }

        private int SettingsClickCount = 0;

        internal RootModel Model { get; private set; }

        public RootPage()
        {
            void AddType(Type t) => this.PageMap.Add(t.Name, t);
            AddType(typeof(RoomListPage));
            AddType(typeof(LogPage));
            AddType(typeof(SettingsPage));
            AddType(typeof(AdvancedSettingsPage));
            AddType(typeof(AnnouncementPage));

            this.Model = new RootModel();
            this.DataContext = this.Model;

            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            this.Container = builder.Build();
            this.RootScope = this.Container.BeginLifetimeScope("recorder_root");

            this.InitializeComponent();
            this.AdvancedSettingsPageItem.Visibility = Visibility.Hidden;

            var mw = Application.Current.MainWindow as NewMainWindow;
            if (mw is not null)
                mw.NativeBeforeWindowClose += this.RootPage_NativeBeforeWindowClose;

            Loaded += RootPage_Loaded;
        }

        private void RootPage_NativeBeforeWindowClose(object sender, EventArgs e)
        {
            this.Model.Dispose();
            SingleInstance.Cleanup();
        }

        private async void RootPage_Loaded(object sender, RoutedEventArgs e)
        {
            var recorder = this.RootScope.Resolve<IRecorder>();
            var first_time = true;
            var error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.None;
            string path;
            while (true)
            {
                try
                {
                    CommandLineOption commandLineOption = null;
                    if (first_time)
                    {
                        // while 循环第一次运行时检查命令行参数
                        try
                        {
                            first_time = false;
                            Parser.Default
                                .ParseArguments<CommandLineOption>(Environment.GetCommandLineArgs())
                                .WithParsed(x => commandLineOption = x);

                            if (!string.IsNullOrWhiteSpace(commandLineOption?.WorkDirectory))
                            {
                                // 如果有参数直接跳到检查路径
                                path = Path.GetFullPath(commandLineOption.WorkDirectory);
                            }
                            else
                            {
                                // 无命令行参数
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                            // 出错直接重新来，不显示 error
                            continue;
                        }
                    }
                    else
                    {
                        // 尝试读取上次选择的路径
                        var lastdir = string.Empty;
                        try
                        {
                            if (File.Exists(this.lastdir_path))
                                lastdir = File.ReadAllText(this.lastdir_path).Replace("\r", "").Replace("\n", "").Trim();
                        }
                        catch (Exception) { }

                        // 显示路径选择界面
                        var dialog = new WorkDirectorySelectorDialog
                        {
                            Error = error,
                            Path = lastdir
                        };

                        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                        {
                            (Application.Current.MainWindow as NewMainWindow).CloseWithoutConfirmAction();
                            return;
                        }

                        try
                        { path = Path.GetFullPath(dialog.Path); }
                        catch (Exception)
                        {
                            error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.PathNotSupported;
                            continue;
                        }
                    }

                    var config = Path.Combine(path, "config.json");

                    if (!Directory.Exists(path))
                    {
                        error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.PathDoesNotExist;
                        continue;
                    }
                    else if (!Directory.EnumerateFiles(path).Any())
                    {
                        // 可用的空文件夹
                    }
                    else if (!File.Exists(config))
                    {
                        error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.PathContainsFiles;
                        continue;
                    }

                    // 已经选定工作目录

                    // 如果不是从命令行参数传入的路径，写入 lastdir_path 记录
                    try
                    { if (string.IsNullOrWhiteSpace(commandLineOption?.WorkDirectory)) File.WriteAllText(this.lastdir_path, path); }
                    catch (Exception) { }

                    // 检查已经在同目录运行的其他进程
                    if (SingleInstance.CheckMutex(path))
                    {
                        // 无已经在同目录运行的进程
                        if (recorder.Initialize(path))
                        {
                            this.Model.Recorder = recorder;

                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(100);
                                _ = this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, method: new Action(() =>
                                {
                                    this.RoomListPageNavigationViewItem.IsSelected = true;
                                }));
                            });
                            break;
                        }
                        else
                        {
                            error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.FailedToLoadConfig;
                            continue;
                        }
                    }
                    else
                    {
                        // 有已经在其他目录运行的进程，已经通知该进程，本进程退出
                        (Application.Current.MainWindow as NewMainWindow).CloseWithoutConfirmAction();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.UnknownError;
                    logger.Warn(ex, "选择工作目录时发生了未知错误");
                    continue;
                }
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            this.SettingsClickCount = 0;
            if (args.IsSettingsSelected)
            {
                this.MainFrame.Navigate(typeof(SettingsPage), null, this.transitionInfo);
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                var selectedItemTag = (string)selectedItem.Tag;
                if (selectedItemTag.StartsWith("http"))
                {
                    try
                    {
                        this.MainFrame.Navigate(new Uri(selectedItemTag), null, this.transitionInfo);
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (this.PageMap.ContainsKey(selectedItemTag))
                {
                    var pageType = this.PageMap[selectedItemTag];
                    this.MainFrame.Navigate(pageType, null, this.transitionInfo);
                }
            }
        }

        private void NavigationViewItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (++this.SettingsClickCount > 1)
            {
                this.SettingsClickCount = 0;
                this.AdvancedSettingsPageItem.Visibility = this.AdvancedSettingsPageItem.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                if (sender is not ModernWpf.Controls.Frame frame) return;

                while (frame.BackStackDepth > 0)
                {
                    frame.RemoveBackEntry();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
