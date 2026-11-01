using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.DependencyInjection;
using BililiveRecorder.Desktop.Infrastructure;
using BililiveRecorder.Desktop.Localization;
using BililiveRecorder.Desktop.ViewModels;
using BililiveRecorder.Desktop.Views;
using BililiveRecorder.Flv;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop
{
    public partial class App : Application
    {
        /// <summary>当前主窗口（尚未创建时为 null）。</summary>
        public static MainWindow? MainWindowOrNull => App.Instance.mainWindow;

        /// <summary>选定的录制工作目录。</summary>
        public static string WorkDirectory { get; private set; } = string.Empty;

        /// <summary>应用 DI 容器（工具页获取 ICookieTester / UserScriptRunner 等服务用）。</summary>
        public static IServiceProvider? ServicesOrNull => App.Instance.services;

        private static App Instance => (App)Current!;

        private WindowIcon appIcon = null!;
        private IServiceProvider? services;
        private IRecorder? recorder;
        private MainWindowViewModel? viewModel;
        private MainWindow? mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            this.appIcon = new WindowIcon(AssetLoader.Open(new Uri("avares://BililiveRecorder.Desktop/ico.ico")));
            desktop.ShutdownRequested += this.Desktop_ShutdownRequested;

            // 先启动软件，再确定工作目录（与 WPF 版一致）：
            // 能自动确定（命令行参数 / path.json 记住的路径 / 当前目录）就直接进主界面，
            // 否则显示首启目录选择窗口
            var resolver = new WorkDirectoryResolver();
            var auto = Program.AskPath
                ? Infrastructure.WorkDirectoryResolver.AutoPathResult.None
                : resolver.TryGetAutoPath(Program.PathArgument);

            StartResult? initialError = null;
            if (auto.WorkPath is not null)
            {
                var result = this.StartRecorder(auto.WorkPath, rememberPath: !auto.FromArgument, skipAsking: true);
                if (result == StartResult.Success)
                    return;
                initialError = result;
            }

            var selector = new SelectWorkDirectoryWindow(
                this.appIcon,
                prefillPath: auto.WorkPath ?? resolver.Read().Path,
                initialError,
                start: (path, skipAsking) => this.StartRecorder(path, rememberPath: true, skipAsking: skipAsking));
            selector.Show();
        }

        /// <summary>校验目录、加载配置、构建 DI 并显示主窗口。失败时返回错误原因，由调用方展示。</summary>
        private StartResult StartRecorder(string path, bool rememberPath, bool skipAsking)
        {
            var logger = Log.ForContext<App>();
            try
            {
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(path);
                }
                catch (Exception)
                {
                    return StartResult.PathNotSupported;
                }

                if (!Directory.Exists(fullPath))
                    return StartResult.PathDoesNotExist;

                if (!WorkDirectoryResolver.IsUsableDirectory(fullPath))
                    return StartResult.PathContainsFiles;

                var config = ConfigParser.LoadFromDirectory(fullPath);
                if (config is null)
                    return StartResult.FailedToLoadConfig;
                config.Global.WorkDirectory = fullPath;

                // 与 WPF 版共用同一个互斥体标识，避免两个程序同时写同一个目录
                if (!SingleInstance.CheckMutex(fullPath))
                    return StartResult.AlreadyRunning;

                if (rememberPath)
                {
                    var resolver = new WorkDirectoryResolver();
                    resolver.Write(new WorkDirectoryResolver.WorkDirectoryData { Path = fullPath, SkipAsking = skipAsking });
                }

                this.services = new ServiceCollection()
                    .AddSingleton<ILogger>(Log.Logger)
                    .AddRecorderConfig(config)
                    .AddFlv()
                    .AddRecorder()
                    .BuildServiceProvider();

                this.recorder = this.services.GetRequiredService<IRecorder>();
                this.viewModel = new MainWindowViewModel(this.recorder, LocalizationService.Instance);
                this.mainWindow = new MainWindow
                {
                    DataContext = this.viewModel,
                    Icon = this.appIcon,
                };

                WorkDirectory = fullPath;

                this.SetupTrayIcon(this.recorder, this.mainWindow);
                this.SetupStreamStartedToast(this.recorder);
                SingleInstance.StartActivationWatcher(() => ShowMainWindow(this.mainWindow));

                // 先显示主窗口再关闭选择窗口，避免窗口数归零触发退出
                this.mainWindow.Show();
                if (Program.StartMinimized)
                    this.mainWindow.WindowState = WindowState.Minimized;

                logger.Debug("Work directory: {WorkDirectory}", fullPath);
                return StartResult.Success;
            }
            catch (Exception ex)
            {
                // 互斥锁可能在失败前已被本进程持有，必须释放，
                // 否则用户在目录选择窗口重试时会误报"已在另一个进程中运行"
                SingleInstance.Cleanup();

                // 释放可能已创建了一半的录制器（已开始连接弹幕服务器），避免重试后重复连接
                try
                {
                    this.viewModel?.RoomList.Dispose();
                    this.recorder?.Dispose();
                }
                catch (Exception) { }
                try
                {
                    (this.services as IDisposable)?.Dispose();
                }
                catch (Exception) { }
                this.viewModel = null;
                this.recorder = null;
                this.services = null;
                this.mainWindow = null;

                logger.Error(ex, "Error starting recorder");
                return StartResult.UnknownError;
            }
        }

        private void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            try
            {
                this.viewModel?.RoomList.Dispose();
                this.recorder?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error disposing recorder");
            }
            try
            {
                (this.services as IDisposable)?.Dispose();
            }
            catch (Exception) { }
            Log.CloseAndFlush();
        }

        private void SetupTrayIcon(IRecorder recorder, MainWindow mainWindow)
        {
            var logger = Log.ForContext<App>();
            var menu = new NativeMenu();

            void AddItem(string text, EventHandler handler)
            {
                var item = new NativeMenuItem(text);
                item.Click += handler;
                menu.Items.Add(item);
            }

            AddItem(LocalizationService.Instance["TaskbarIconControl_MenuItem_OpenMainWindow"], (s, e) => ShowMainWindow(mainWindow));

            menu.Items.Add(new NativeMenuItemSeparator());

            AddItem("全部" + LocalizationService.Instance["RoomCard_Menu_StartRecording"], (s, e) =>
            {
                foreach (var room in recorder.Rooms)
                    if (room.Streaming && !room.Recording)
                        room.StartRecord();
            });
            AddItem("全部" + LocalizationService.Instance["RoomCard_Menu_StopRecording"], (s, e) =>
            {
                foreach (var room in recorder.Rooms)
                    if (room.Recording)
                        room.StopRecord();
            });

            menu.Items.Add(new NativeMenuItemSeparator());

            AddItem(LocalizationService.Instance["Global_Quit"], (s, e) =>
            {
                mainWindow.SuppressHideToTray = true;
                mainWindow.Close();
            });

            var trayIcon = new TrayIcon
            {
                Icon = this.appIcon,
                IsVisible = true,
                Menu = menu,
                ToolTipText = MakeTrayToolTip(recorder),
            };
            trayIcon.Clicked += (s, e) => ShowMainWindow(mainWindow);
            TrayIcon.SetIcons(this, new TrayIcons { trayIcon });

            void UpdateToolTip()
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        trayIcon.ToolTipText = MakeTrayToolTip(recorder);
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(ex, "Error updating tray tooltip");
                    }
                });
            }
            recorder.RecordSessionStarted += (s, e) => UpdateToolTip();
            recorder.RecordSessionEnded += (s, e) => UpdateToolTip();
        }

        private void SetupStreamStartedToast(IRecorder recorder)
        {
            recorder.StreamStarted += (s, room) =>
            {
                try
                {
                    if (!recorder.Config.Global.WpfNotifyStreamStart)
                        return;
                }
                catch (Exception)
                {
                    return;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        new StreamStartedToast(room).Show();
                    }
                    catch (Exception ex)
                    {
                        Log.ForContext<App>().Warning(ex, "Error showing stream started toast");
                    }
                });
            };
        }

        private static string MakeTrayToolTip(IRecorder recorder)
        {
            var title = LocalizationService.Instance["TaskbarIconControl_Title"];
            if (recorder.Config.Global.WpfDisableTrayToolTip)
                return title;

            var count = recorder.Rooms.Count(x => x.Recording);
            return $"{title} Desktop — {count} {LocalizationService.Instance["RoomCard_Status_Recording"]}";
        }

        private static void ShowMainWindow(MainWindow window)
        {
            Dispatcher.UIThread.Post(() =>
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
            });
        }
    }
}
