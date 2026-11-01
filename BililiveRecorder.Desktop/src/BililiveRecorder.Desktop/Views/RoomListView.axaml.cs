using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BililiveRecorder.Desktop.Controls;
using BililiveRecorder.Desktop.ViewModels;
using FluentAvalonia.UI.Controls;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class RoomListView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<RoomListView>();

        public RoomListView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.RoomListView_DataContextChanged;
        }

        private RoomListViewModel? ViewModel => this.DataContext as RoomListViewModel;

        private void RoomListView_DataContextChanged(object? sender, EventArgs e)
        {
            if (this.ViewModel is { } vm)
            {
                ((System.Collections.Specialized.INotifyCollectionChanged)vm.Rooms).CollectionChanged -= this.Rooms_CollectionChanged;
                ((System.Collections.Specialized.INotifyCollectionChanged)vm.Rooms).CollectionChanged += this.Rooms_CollectionChanged;
                this.AttachItemHandlers();
            }
        }

        private void Rooms_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.AttachItemHandlers();
        }

        private void AttachItemHandlers()
        {
            if (this.ViewModel is not { } vm)
                return;

            foreach (var item in vm.Rooms)
            {
                item.ShowSettingsRequested -= this.Item_ShowSettingsRequested;
                item.RemoveRequested -= this.Item_RemoveRequested;
                item.ShowSettingsRequested += this.Item_ShowSettingsRequested;
                item.RemoveRequested += this.Item_RemoveRequested;
            }
        }

        /// <summary>
        /// 卡片右上角"⋮"按钮弹出操作菜单（左键点击）。
        /// </summary>
        private void CardMoreButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Control control)
                this.ShowRoomMenu(control, showAtPointer: false);
        }

        /// <summary>
        /// 卡片上任意位置的右键菜单。
        /// 不走 Avalonia 内置的 ContextMenu/ContextFlyout：它们只在右键命中的正是控件本身时才弹出
        /// （Control.OnPointerReleased 里的 <c>e.Source == this</c> 判断），右键点在标题等子元素上不会触发，
        /// 而 PointerPressed 会从子元素冒泡到卡片，所以在这里自己处理。
        /// </summary>
        private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Control control)
                return;

            if (!e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
                return;

            e.Handled = true;
            this.ShowRoomMenu(control, showAtPointer: true);
        }

        /// <summary>
        /// 弹出房间操作菜单。用 MenuFlyout 而不是 ContextMenu：本项目的 FluentAvalonia 主题下
        /// ContextMenu 弹不出来（上一版右键、⋮ 都是这么失效的），MenuFlyout 与顶部"☰"菜单是同一套机制。
        /// </summary>
        /// <param name="anchor">菜单的定位控件（卡片或"⋮"按钮），同时提供 DataContext。</param>
        /// <param name="showAtPointer">true = 在鼠标位置弹出（右键）；false = 贴着 anchor（"⋮"按钮）。</param>
        private void ShowRoomMenu(Control anchor, bool showAtPointer)
        {
            if (this.ViewModel is not { } vm || anchor.DataContext is not RoomListItemViewModel item)
                return;

            var loc = vm.Loc;
            var flyout = new MenuFlyout();
            var items = flyout.Items;

            void AddWithIcon(string key, ICommand? command, StreamGeometry icon, IBrush? fill = null)
                => items.Add(this.CreateMenuItem(loc[key], command, icon, fill));

            // 菜单结构与 WPF 版卡片菜单一致（每项带图标）
            if (item.Recording)
                AddWithIcon("RoomCard_Menu_StopRecording", item.StopRecordCommand, RoomCardIcons.StopCircleOutline);
            else
                AddWithIcon("RoomCard_Menu_StartRecording", item.StartRecordCommand, RoomCardIcons.PlayCircleOutline);

            items.Add(new Separator());

            AddWithIcon("RoomCard_Menu_RefreshInfo", item.RefreshCommand, RoomCardIcons.Refresh);
            AddWithIcon("RoomCard_Menu_OpenInBrowser", item.OpenInBrowserCommand, RoomCardIcons.OpenInNew);

            items.Add(new Separator());
            AddWithIcon("RoomCard_Menu_Settings", item.ShowSettingsCommand, RoomCardIcons.CogOutline);

            // “全局设置”跳转到主窗口设置页（WPF 版此项同样未本地化）
            var globalSettingsItem = this.CreateMenuItem("全局设置", null, RoomCardIcons.CogOutline);
            globalSettingsItem.Click += (_, _) => App.MainWindowOrNull?.NavigateToSettings();
            items.Add(globalSettingsItem);

            items.Add(new Separator());

            // 单选样式（同 WPF 版 RadioMenuItem）：选中的项前显示小圆点，无其他图标
            items.Add(new MenuItem
            {
                Header = loc["RoomCard_Menu_EnableAutoRecord"],
                Command = item.EnableAutoRecordCommand,
                Icon = this.BuildAutoRecordMenuIcon(item.AutoRecord),
            });
            items.Add(new MenuItem
            {
                Header = loc["RoomCard_Menu_DisableAutoRecord"],
                Command = item.DisableAutoRecordCommand,
                Icon = this.BuildAutoRecordMenuIcon(!item.AutoRecord),
            });

            items.Add(new Separator());
            AddWithIcon("Global_Copy", item.CopyRoomIdCommand, RoomCardIcons.ContentCopy);
            AddWithIcon("RoomCard_Menu_Delete", item.RemoveCommand, RoomCardIcons.Delete, Brushes.DarkRed);

            try
            {
                // ShowAt(control, true) = 在鼠标位置弹出（等价于 ContextFlyout 的行为）
                flyout.ShowAt(anchor, showAtPointer);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error showing room card menu");
            }
        }

        private void SplitButton_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is RoomListItemViewModel item)
                item.Room.SplitOutput();
        }

        /// <summary>创建带图标的菜单项（图标与 WPF 版一致，使用矢量路径）。</summary>
        private MenuItem CreateMenuItem(string header, ICommand? command, StreamGeometry icon, IBrush? fill = null)
        {
            return new MenuItem
            {
                Header = header,
                Command = command,
                Icon = new Avalonia.Controls.Shapes.Path
                {
                    Data = icon,
                    Width = 16,
                    Height = 16,
                    Stretch = Stretch.Uniform,
                    Fill = fill ?? this.DefaultIconFill(),
                },
            };
        }

        private IBrush DefaultIconFill()
        {
            if (this.TryFindResource("SystemControlForegroundBaseHighBrush", out var value) && value is IBrush brush)
                return brush;
            return Brushes.Gray;
        }

        /// <summary>
        /// 构建“自动录制/不自动录制”菜单项图标：仅选中的项前显示一个小圆点，无其他图标。
        /// 固定 9 宽保证两项文字对齐。
        /// </summary>
        private Grid BuildAutoRecordMenuIcon(bool isSelected)
        {
            var grid = new Grid { Width = 9 };

            var dot = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = this.DefaultIconFill(),
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = isSelected,
            };
            grid.Children.Add(dot);

            return grid;
        }

        /// <summary>“添加房间”卡片：输入框内按回车等同点击“确定”。</summary>
        private void AddCardInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            if (this.ViewModel is { } vm)
                vm.AddRoomCommand.Execute(null);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Item_ShowSettingsRequested(object? sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                if (sender is not RoomListItemViewModel item)
                    return;

                var window = new PerRoomSettingsWindow(item.Room);
                var owner = App.MainWindowOrNull;
                if (owner is not null)
                    window.Show(owner);
                else
                    window.Show();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error opening per room settings window");
            }
            await Task.CompletedTask;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Item_RemoveRequested(object? sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                if (sender is not RoomListItemViewModel item || this.ViewModel is not RoomListViewModel vm)
                    return;

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return;

                var dialog = new ContentDialog
                {
                    Title = vm.Loc["DeleteRoomConfirmDialog_Title"],
                    Content = $"{item.Name} ({item.Room.RoomConfig.RoomId})",
                    PrimaryButtonText = vm.Loc["Global_Confirm"],
                    CloseButtonText = vm.Loc["Global_Cancel"],
                    DefaultButton = ContentDialogButton.Close,
                };

                var result = await dialog.ShowAsync(topLevel);
                if (result == ContentDialogResult.Primary)
                    vm.RemoveRoom(item);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error showing delete room confirm dialog");
            }
        }

        private void Sort_Click(object? sender, RoutedEventArgs e)
        {
            if (this.ViewModel is not RoomListViewModel vm)
                return;

            if ((sender as Control)?.Tag is string tag && Enum.TryParse<SortedBy>(tag, out var sortBy))
                vm.SortBy = sortBy;
        }

        private void OpenWorkDirectory_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var workDir = App.WorkDirectory;
                if (!string.IsNullOrEmpty(workDir) && Directory.Exists(workDir))
                    Process.Start(new ProcessStartInfo(workDir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error opening work directory");
            }
        }

        private void OpenLogFolder_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                Process.Start(new ProcessStartInfo(logDir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error opening log folder");
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RefreshAll_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                if (this.ViewModel is not RoomListViewModel vm)
                    return;

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return;

                var dialog = new ContentDialog
                {
                    Title = vm.Loc["RoomListPage_Menu_RefreshAllRoomInfo"],
                    Content = "录播姬会自动检测直播间状态，不需要手动刷新。\n频繁刷新直播间状态、短时间内大量请求 mikufans 直播 API 可能会导致你的 IP 被屏蔽，完全无法录播。\n\n本功能是特殊情况下确实需要刷新所有直播间信息时使用的。\n\n是否要刷新所有直播间的信息？（每个直播间会发送一个请求）",
                    PrimaryButtonText = vm.Loc["Global_Confirm"],
                    CloseButtonText = vm.Loc["Global_Cancel"],
                    DefaultButton = ContentDialogButton.Close,
                };

                if (await dialog.ShowAsync(topLevel) == ContentDialogResult.Primary)
                    await vm.RefreshAllRoomsAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error refreshing all rooms");
            }
        }

        private void ChangeWorkPath_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                    return;

                Process.Start(new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    ArgumentList = { "run", "--ask-path" },
                });

                var mainWindow = App.MainWindowOrNull;
                if (mainWindow is not null)
                {
                    mainWindow.SuppressHideToTray = true;
                    mainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error restarting to change work path");
            }
        }
    }
}
