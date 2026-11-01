using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using BililiveRecorder.Desktop.Localization;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class MainWindow : Window
    {
        /// <summary>退出程序时置为 true，否则关闭窗口只会隐藏到托盘。</summary>
        internal bool SuppressHideToTray { get; set; }

        public MainWindow()
        {
            this.InitializeComponent();

            // 右键"设置"导航项切换"高级设置"入口。
            // 不用 ContextMenu：FluentAvalonia 面板可能吞掉右键事件导致菜单弹不出来，
            // 这里用 handledEventsToo 直接监听，保证事件一定到达
            this.Nav.AddHandler(
                Avalonia.Input.InputElement.PointerReleasedEvent,
                this.Nav_PointerReleased,
                Avalonia.Interactivity.RoutingStrategies.Bubble,
                handledEventsToo: true);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!this.SuppressHideToTray)
            {
                e.Cancel = true;
                this.Hide();
            }

            base.OnClosing(e);
        }

        private void Nav_OnSelectionChanged(object? sender, FluentAvalonia.UI.Controls.NavigationViewSelectionChangedEventArgs e)
        {
            var tag = (e.SelectedItem as FluentAvalonia.UI.Controls.NavigationViewItem)?.Tag as string;

            this.RoomsPanel.IsVisible = tag == "rooms";
            this.SettingsPanel.IsVisible = tag == "settings";
            this.AdvancedSettingsPanel.IsVisible = tag == "advanced";
            this.ToolboxAutoFixPanel.IsVisible = tag == "toolbox-autofix";
            this.ToolboxRemuxPanel.IsVisible = tag == "toolbox-remux";
            this.ToolboxMergerPanel.IsVisible = tag == "toolbox-merger";
            this.LogPanel.IsVisible = tag == "log";
            this.AnnouncementPanel.IsVisible = tag == "announcement";
            this.AboutPanel.IsVisible = tag == "about";
        }

        private void Nav_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton != Avalonia.Input.MouseButton.Right)
                return;

            // 从事件源向上找，判断是否点在"设置"导航项上
            for (var v = e.Source as Avalonia.Visual; v is not null; v = v.GetVisualParent())
            {
                if (v == this.SettingsNavItem)
                {
                    e.Handled = true;
                    this.ToggleAdvancedSettings();
                    return;
                }
            }
        }

        /// <summary>跳转到设置页（房间卡片菜单的“全局设置”使用）。</summary>
        internal void NavigateToSettings()
        {
            this.Nav.SelectedItem = this.SettingsNavItem;
        }

        private void ToggleAdvancedSettings()
        {
            this.AdvancedNavItem.IsVisible = !this.AdvancedNavItem.IsVisible;
            if (this.AdvancedNavItem.IsVisible)
                this.Nav.SelectedItem = this.AdvancedNavItem;
        }

        private void ThemeButton_Click(object? sender, RoutedEventArgs e)
        {
            var current = this.ActualThemeVariant;
            Application.Current!.RequestedThemeVariant =
                current == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        }

        private void Language_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Control)?.Tag is string tag)
                    LocalizationService.Instance.CurrentCulture = new CultureInfo(tag);
            }
            catch (Exception)
            {
            }
        }
    }
}
