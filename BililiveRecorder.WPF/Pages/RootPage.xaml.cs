using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.DependencyInjection;
using BililiveRecorder.WPF.Controls;
using BililiveRecorder.WPF.Models;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using Serilog;
using Path = System.IO.Path;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for RootPage.xaml
    /// </summary>
    public partial class RootPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<RootPage>();

        internal static string? CommandArgumentRecorderPath = null;
        internal static bool CommandArgumentFirstRun = false; // TODO
        internal static bool CommandArgumentAskPath = false;

        private readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type>();
        private readonly WorkDirectoryLoader workDirectoryLoader = new WorkDirectoryLoader();
        private readonly NavigationTransitionInfo transitionInfo = new DrillInNavigationTransitionInfo();

        private int SettingsClickCount = 0;

        internal static IServiceProvider? ServiceProvider { get; private set; }
        private ServiceProvider serviceProvider;
        internal RootModel Model { get; private set; }

        public RootPage()
        {
            void AddType(Type t) => this.PageMap.Add(t.Name, t);
            AddType(typeof(RoomListPage));
            AddType(typeof(SettingsPage));
            AddType(typeof(LogPage));
            AddType(typeof(AboutPage));
            AddType(typeof(AdvancedSettingsPage));
            AddType(typeof(AnnouncementPage));
            AddType(typeof(ToolboxAutoFixPage));

            this.Model = new RootModel();
            this.DataContext = this.Model;

            {
                var services = new ServiceCollection();
                services
                    .AddFlv()
                    .AddRecorder()
                    ;

                this.serviceProvider = services.BuildServiceProvider();
            }

            this.InitializeComponent();
            this.AdvancedSettingsPageItem.Visibility = Visibility.Hidden;

#if DEBUG
            this.DebugBuildIcon.Visibility = Visibility.Visible;
#endif

            var mw = Application.Current.MainWindow as NewMainWindow;
            if (mw is not null)
                mw.NativeBeforeWindowClose += this.RootPage_NativeBeforeWindowClose;

            Loaded += this.RootPage_Loaded;
        }

        private void RootPage_NativeBeforeWindowClose(object sender, EventArgs e)
        {
            this.Model.Dispose();
            SingleInstance.Cleanup();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RootPage_Loaded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            // 上次选择的路径信息
            var pathInfo = this.workDirectoryLoader.Read();
            // 第一次尝试从命令行和配置文件自动选择路径
            var first_time = true;
            // 如果是从命令行参数传入的路径，则不保存选择的路径到文件
            var from_argument = false;

            // 路径选择错误
            var error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.None;
            // 最终选择的路径
            string path;

            while (true)
            {
                try
                {
                    // 获取一个路径
                    if (first_time)
                    {
                        // while 循环第一次运行时检查命令行参数、和上次选择记住的路径
                        try
                        {
                            first_time = false;

                            if (!string.IsNullOrWhiteSpace(CommandArgumentRecorderPath))
                            {
                                // 如果有参数直接跳到检查路径
                                path = Path.GetFullPath(CommandArgumentRecorderPath);
                                from_argument = true; // 用于控制不写入文件保存
                            }
                            else if (pathInfo.SkipAsking && !CommandArgumentAskPath)
                            {
                                // 上次选择了“不再询问”
                                path = pathInfo.Path;
                            }
                            else
                            {
                                // 无命令行参数 和 记住选择
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
                        var lastdir = pathInfo.Path;

                        // 显示路径选择界面
                        var dialog = new WorkDirectorySelectorDialog
                        {
                            Error = error,
                            Path = lastdir,
                            SkipAsking = pathInfo.SkipAsking
                        };
                        var dialogResult = await dialog.ShowAsync();
                        switch (dialogResult)
                        {
                            case ContentDialogResult.Primary:
                                break;
                            case ContentDialogResult.Secondary:
                                return;
                            case ContentDialogResult.None:
                            default:
                                (Application.Current.MainWindow as NewMainWindow)!.CloseWithoutConfirmAction();
                                return;
                        }

                        pathInfo.SkipAsking = dialog.SkipAsking;

                        try
                        { path = Path.GetFullPath(dialog.Path); }
                        catch (Exception)
                        {
                            error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.PathNotSupported;
                            continue;
                        }
                    }
                    // 获取一个路径结束

                    var configFilePath = Path.Combine(path, "config.json");

                    if (!Directory.Exists(path))
                    {
                        error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.PathDoesNotExist;
                        continue;
                    }
                    else if (!Directory.EnumerateFiles(path).Any())
                    {
                        // 可用的空文件夹
                    }
                    else if (!File.Exists(configFilePath))
                    {
                        error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.PathContainsFiles;
                        continue;
                    }

                    // 已经选定工作目录

                    // 如果不是从命令行参数传入的路径，写入 lastdir_path 记录
                    try
                    {
                        if (!from_argument)
                        {
                            pathInfo.Path = path;
                            this.workDirectoryLoader.Write(pathInfo);
                        }
                    }
                    catch (Exception) { }

                    // 加载配置文件
                    var config = ConfigParser.LoadFrom(path);
                    if (config is null)
                    {
                        error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.FailedToLoadConfig;
                        continue;
                    }
                    config.Global.WorkDirectory = path;

                    // 检查已经在同目录运行的其他进程
                    if (!SingleInstance.CheckMutex(path))
                    {
                        // 有已经在其他目录运行的进程，已经通知该进程，本进程退出
                        (Application.Current.MainWindow as NewMainWindow)!.CloseWithoutConfirmAction();
                        return;
                    }

                    // 无已经在同目录运行的进程
                    this.serviceProvider = this.BuildServiceProvider(config, logger);
                    ServiceProvider = this.serviceProvider;
                    var recorder = this.serviceProvider.GetRequiredService<IRecorder>();

                    this.Model.Recorder = recorder;
                    this.RoomListPageNavigationViewItem.IsEnabled = true;
                    this.SettingsPageNavigationViewItem.IsEnabled = true;
                    (Application.Current.MainWindow as NewMainWindow)!.HideToTray = true;

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(150);
                        _ = this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, method: new Action(() =>
                        {
                            this.RoomListPageNavigationViewItem.IsSelected = true;
                        }));
                    });

                    break;
                }
                catch (Exception ex)
                {
                    error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.UnknownError;
                    logger.Warning(ex, "选择工作目录时发生了未知错误");
                    continue;
                }
            }
        }

        private ServiceProvider BuildServiceProvider(Core.Config.V2.ConfigV2 config, ILogger logger) => new ServiceCollection()
            .AddSingleton(logger)
            .AddRecorderConfig(config)
            .AddFlv()
            .AddRecorder()
            .BuildServiceProvider();

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
