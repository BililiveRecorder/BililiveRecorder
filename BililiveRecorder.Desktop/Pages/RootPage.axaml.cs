using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.DependencyInjection;
using BililiveRecorder.Desktop.Controls;
using BililiveRecorder.Desktop.Models;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Path = System.IO.Path;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class RootPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<RootPage>();

        internal static string? CommandArgumentRecorderPath = null;
        internal static bool CommandArgumentFirstRun = false;
        internal static bool CommandArgumentAskPath = false;
        internal static bool CommandArgumentHide = false;

        private readonly Dictionary<string, Type> PageMap = new Dictionary<string, Type>();
        private readonly SlideNavigationTransitionInfo transitionInfo = new SlideNavigationTransitionInfo();

        private int SettingsClickCount = 0;

        internal static IServiceProvider? ServiceProvider { get; private set; }
        private ServiceProvider? serviceProvider;
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
            this.AdvancedSettingsPageItem.IsVisible = false;

#if DEBUG
            this.DebugBuildIcon.IsVisible = true;
#endif

            this.Loaded += this.RootPage_Loaded;

            SwitchToSettingsPage = () =>
            {
                this.SettingsPageNavigationViewItem.IsSelected = true;
            };
        }

        private void RootPage_NativeBeforeWindowClose(object? sender, EventArgs e)
        {
            this.Model.Dispose();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RootPage_Loaded(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var mw = TopLevel.GetTopLevel(this) as MainWindow;
            if (mw is not null)
                mw.NativeBeforeWindowClose += this.RootPage_NativeBeforeWindowClose;

            // Navigate to the announcement page on startup
            await Task.Delay(100);
            this.MainFrame.Navigate(typeof(AnnouncementPage), null, this.transitionInfo);
        }

        private ServiceProvider BuildServiceProvider(Core.Config.V3.ConfigV3 config, ILogger logger) => new ServiceCollection()
            .AddSingleton(logger)
            .AddRecorderConfig(config)
            .AddFlv()
            .AddRecorder()
            .BuildServiceProvider();

        private void NavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
            this.SettingsClickCount = 0;
            if (e.SelectedItem is NavigationViewItem selectedItem)
            {
                var selectedItemTag = selectedItem.Tag as string;
                if (selectedItemTag != null && this.PageMap.ContainsKey(selectedItemTag))
                {
                    var pageType = this.PageMap[selectedItemTag];
                    this.MainFrame.Navigate(pageType, null, this.transitionInfo);
                }
            }
        }

        private void NavigationViewItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (++this.SettingsClickCount > 1)
                {
                    this.SettingsClickCount = 0;
                    this.AdvancedSettingsPageItem.IsVisible = !this.AdvancedSettingsPageItem.IsVisible;
                }
            }
        }

        private void SwitchLightDarkTheme_Click(object? sender, RoutedEventArgs e)
        {
            if (Application.Current != null)
            {
                var currentTheme = Application.Current.ActualThemeVariant;
                Application.Current.RequestedThemeVariant =
                    currentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
            }
        }

        private void SwitchLanguage_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement language switching with Echoes
        }
    }
}
