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
using ModernWpf;
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
        internal static bool CommandArgumentHide = false;

        private readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type>();
        private readonly WorkDirectoryLoader workDirectoryLoader = new WorkDirectoryLoader();
        private readonly NavigationTransitionInfo transitionInfo = new DrillInNavigationTransitionInfo();

        private int SettingsClickCount = 0;

        internal static IServiceProvider? ServiceProvider { get; private set; }
        private ServiceProvider serviceProvider = null!;
        internal RootModel Model { get; private set; }

        internal static Action? SwitchToSettingsPage { get; private set; }

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
            AddType(typeof(ToolboxRemuxPage));
            AddType(typeof(ToolboxDanmakuMergerPage));

            this.Model = new RootModel();
            this.DataContext = this.Model;

            this.InitializeComponent();
            this.AdvancedSettingsPageItem.Visibility = Visibility.Hidden;

            try
            {
                new System.Globalization.CultureInfo("en-PN");
            }
            catch (Exception)
            {
                this.JokeLangSelectionMenuItem.Visibility = System.Windows.Visibility.Collapsed;
            }

#if DEBUG
            this.DebugBuildIcon.Visibility = Visibility.Visible;
#endif

            var mw = Application.Current.MainWindow as NewMainWindow;
            if (mw is not null)
                mw.NativeBeforeWindowClose += this.RootPage_NativeBeforeWindowClose;

            Loaded += this.RootPage_Loaded;

            SwitchToSettingsPage = () =>
            {
                this.SettingsPageNavigationViewItem.IsSelected = true;
            };
        }

        private void RootPage_NativeBeforeWindowClose(object sender, EventArgs e)
        {
            this.Model.Dispose();
            // FIXME: SingleInstance.Cleanup();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RootPage_Loaded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (CommandArgumentFirstRun)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    _ = this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        MessageBox.Show(Window.GetWindow(this), @"录播姬 安装成功！
之后再运行请使用桌面或开始菜单里的快捷方式。
如需卸载，可在系统设置里操作。

BililiveRecorder Installed!
Please use the shortcut on the desktop or
in the start menu to launch.
You can uninstall me in system settings.", "安装成功 Installed", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }

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
                                logger.Debug("Using path from command argument");
                                path = Path.GetFullPath(CommandArgumentRecorderPath);
                                from_argument = true; // 用于控制不写入文件保存
                            }
                            else if (pathInfo.SkipAsking && !CommandArgumentAskPath)
                            {
                                logger.Debug("Using path from path.json file");
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
                            SkipAsking = pathInfo.SkipAsking,
                            Owner = Application.Current.MainWindow
                        };
                        var dialogResult = await dialog.ShowAndDisableMinimizeToTrayAsync();
                        switch (dialogResult)
                        {
                            case ContentDialogResult.Primary:
                                logger.Debug("Confirm path selected");
                                break;
                            case ContentDialogResult.Secondary:
                                logger.Debug("Toolbox mode selected");
                                return;
                            case ContentDialogResult.None:
                            default:
                                logger.Debug("Exit selected");
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
                    else if (!Directory.EnumerateFiles(path).Any(x => Path.GetFileName(x) != "desktop.ini"))
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
                    var config = ConfigParser.LoadFromDirectory(path);
                    if (config is null)
                    {
                        error = WorkDirectorySelectorDialog.WorkDirectorySelectorDialogError.FailedToLoadConfig;
                        continue;
                    }
                    config.Global.WorkDirectory = path;

                    // 检查已经在同目录运行的其他进程
                    // FIXME: if (!SingleInstance.CheckMutex(path))
                    //{
                    //    // 有已经在其他目录运行的进程，已经通知该进程，本进程退出
                    //    (Application.Current.MainWindow as NewMainWindow)!.CloseWithoutConfirmAction();
                    //    return;
                    //}

                    // 无已经在同目录运行的进程
                    this.serviceProvider = this.BuildServiceProvider(config, logger);
                    ServiceProvider = this.serviceProvider;
                    var recorder = this.serviceProvider.GetRequiredService<IRecorder>();

                    this.Model.Recorder = recorder;
                    this.RoomListPageNavigationViewItem.IsEnabled = true;
                    this.SettingsPageNavigationViewItem.IsEnabled = true;
                    (Application.Current.MainWindow as NewMainWindow)!.HideToTray = true;
                    NetworkChangeDetector.Enable();

                    // 开播提醒系统通知，乱，但它能跑起来 ┑(￣Д ￣)┍
                    recorder.StreamStarted += (sender, room) =>
                    {
                        if (!recorder.Config.Global.WpfNotifyStreamStart)
                            return;

                        _ = StreamStartedNotification.ShowAsync(room);
                    };

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(150);
                        _ = this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, method: new Action(() =>
                        {
                            this.RoomListPageNavigationViewItem.IsSelected = true;

                            if (CommandArgumentHide)
                                Application.Current.MainWindow.WindowState = WindowState.Minimized;
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

        private ServiceProvider BuildServiceProvider(Core.Config.V3.ConfigV3 config, ILogger logger) => new ServiceCollection()
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

        private void SwitchLightDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            if (this.Dispatcher.CheckAccess())
                changeTheme();
            else
                _ = this.Dispatcher.BeginInvoke(changeTheme);

            static void changeTheme()
            {
                ThemeManager.Current.ApplicationTheme =
                    ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark
                    ? ApplicationTheme.Light
                    : ApplicationTheme.Dark;
            };
        }
    }
}
