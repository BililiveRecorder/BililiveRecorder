using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    public enum SortedBy
    {
        None = 0,
        RoomId,
        Status,
    }

    public partial class RoomListViewModel : ObservableObject, IDisposable
    {
        private static readonly ILogger logger = Log.ForContext<RoomListViewModel>();

        private readonly IRecorder recorder;
        private readonly ILocalizationService loc;
        private bool disposed;

        public ILocalizationService Loc => this.loc;

        public IRecorder Recorder => this.recorder;

        public UiSettingsViewModel UiSettings { get; }

        public ObservableCollection<RoomListItemViewModel> Rooms { get; } = new ObservableCollection<RoomListItemViewModel>();

        /// <summary>卡片墙数据源：所有房间卡片 + 末尾的“添加房间”占位（与 WPF 版一致的卡片墙）。</summary>
        public ObservableCollection<object> CardItems { get; } = new ObservableCollection<object>();

        [ObservableProperty]
        private string? addRoomInput;

        [ObservableProperty]
        private string? statusMessage;

        private SortedBy sortBy;

        public SortedBy SortBy
        {
            get => this.sortBy;
            set
            {
                if (this.sortBy == value)
                    return;
                this.sortBy = value;
                this.OnPropertyChanged(nameof(this.SortBy));
                this.ApplySort();
            }
        }

        public RoomListViewModel(IRecorder recorder, ILocalizationService loc)
        {
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
            this.loc = loc ?? throw new ArgumentNullException(nameof(loc));
            this.UiSettings = new UiSettingsViewModel(recorder, loc);

            foreach (var room in recorder.Rooms)
                this.AddItem(room);

            // Rooms 集合可能在后台线程变化（弹幕/轮询线程），统一调度到 UI 线程
            ((INotifyCollectionChanged)recorder.Rooms).CollectionChanged += this.Rooms_CollectionChanged;

            this.SyncCardItems();
        }

        private void Rooms_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.disposed)
                    return;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                        foreach (IRoom room in e.NewItems)
                            this.AddItem(room);
                        break;
                    case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                        foreach (IRoom room in e.OldItems)
                            this.RemoveItem(room);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var item in this.Rooms)
                            item.Dispose();
                        this.Rooms.Clear();
                        break;
                }

                this.SyncCardItems();
                this.ApplySort();
            });
        }

        private void AddItem(IRoom room)
        {
            if (this.Rooms.Any(x => x.Room == room))
                return;
            var item = new RoomListItemViewModel(room, this.loc, this.UiSettings);
            item.RemoveRequested += this.Item_RemoveRequested;
            this.Rooms.Add(item);
        }

        private void RemoveItem(IRoom room)
        {
            var item = this.Rooms.FirstOrDefault(x => x.Room == room);
            if (item is null)
                return;
            item.RemoveRequested -= this.Item_RemoveRequested;
            item.Dispose();
            this.Rooms.Remove(item);
        }

        private void Item_RemoveRequested(object? sender, EventArgs e)
        {
            // 实际删除由视图在显示确认对话框之后调用 RemoveRoom
        }

        /// <summary>重建卡片墙：房间卡片在前，“添加房间”占位固定在最后。</summary>
        private void SyncCardItems()
        {
            this.CardItems.Clear();
            foreach (var item in this.Rooms)
                this.CardItems.Add(item);
            this.CardItems.Add(AddRoomCardMarker.Instance);
        }

        /// <summary>按当前排序方式重排列表。</summary>
        public void ApplySort()
        {
            try
            {
                System.Collections.Generic.List<RoomListItemViewModel>? ordered = this.SortBy switch
                {
                    SortedBy.RoomId => this.Rooms.OrderBy(x => x.ShortId == 0 ? x.Room.RoomConfig.RoomId : x.ShortId).ToList(),
                    SortedBy.Status => this.Rooms
                        .OrderByDescending(x => x.Recording)
                        .ThenByDescending(x => x.AutoRecord)
                        .ThenByDescending(x => x.Streaming)
                        .ToList(),
                    _ => null,
                };

                if (ordered is null)
                    return;

                // 用 Move 重排，避免整体 Clear+Add 造成选中状态闪烁
                for (var i = 0; i < ordered.Count; i++)
                {
                    if (i >= this.Rooms.Count)
                        break;
                    var index = this.Rooms.IndexOf(ordered[i]);
                    if (index != i && index >= 0)
                    {
                        this.Rooms.Move(index, i);
                        this.CardItems.Move(index, i);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error sorting room list");
            }
        }

        [RelayCommand]
        private void AddRoom()
        {
            var input = (this.AddRoomInput ?? string.Empty).Trim();
            if (input.Length == 0)
                return;

            if (!int.TryParse(input, out var roomid))
            {
                var m = RoomIdFromUrl.Regex.Match(input);
                if (m.Success && m.Groups.Count > 1 && int.TryParse(m.Groups[1].Value, out var result2))
                    roomid = result2;
                else
                {
                    this.StatusMessage = this.loc["AddRoomFailedDialog_ErrorText_InvalidInput"];
                    return;
                }
            }

            if (roomid < 0)
            {
                this.StatusMessage = this.loc["AddRoomFailedDialog_ErrorText_RoomIdNegative"];
                return;
            }
            else if (roomid == 0)
            {
                this.StatusMessage = this.loc["AddRoomFailedDialog_ErrorText_RoomIdZero"];
                return;
            }

            if (this.recorder.Rooms.Any(x => x.RoomConfig.RoomId == roomid || x.ShortId == roomid))
            {
                this.StatusMessage = this.loc["AddRoomFailedDialog_ErrorText_Duplicate"];
                return;
            }

            this.StatusMessage = null;
            this.recorder.AddRoom(roomid);
            this.AddRoomInput = string.Empty;
        }

        /// <summary>由视图在确认删除对话框之后调用。</summary>
        public void RemoveRoom(RoomListItemViewModel item)
        {
            try
            {
                this.recorder.RemoveRoom(item.Room);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error removing room {RoomId}", item.Room.RoomConfig.RoomId);
            }
        }

        /// <summary>刷新所有房间信息（每间一个请求，间隔 500ms）。调用前应由视图确认。</summary>
        public async Task RefreshAllRoomsAsync()
        {
            foreach (var room in this.recorder.Rooms.ToArray())
            {
                try
                {
                    await room.RefreshRoomInfoAsync();
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Error refreshing room info {RoomId}", room.RoomConfig.RoomId);
                }
                await Task.Delay(500);
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                this.recorder.SaveConfig();
                this.StatusMessage = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error saving config");
                this.StatusMessage = "保存配置失败 Failed to save config";
            }
        }

        [RelayCommand]
        private void EnableAutoRecordAll()
        {
            foreach (var room in this.recorder.Rooms)
                room.RoomConfig.AutoRecord = true;

            this.recorder.SaveConfig();
        }

        [RelayCommand]
        private void DisableAutoRecordAll()
        {
            foreach (var room in this.recorder.Rooms)
                room.RoomConfig.AutoRecord = false;

            this.recorder.SaveConfig();
        }

        public void Dispose()
        {
            if (this.disposed)
                return;
            this.disposed = true;
            ((INotifyCollectionChanged)this.recorder.Rooms).CollectionChanged -= this.Rooms_CollectionChanged;
            foreach (var item in this.Rooms)
                item.Dispose();
        }
    }

    /// <summary>卡片墙末尾“添加房间”卡片的占位对象，DataTemplate 按此类型选择模板。</summary>
    public sealed class AddRoomCardMarker
    {
        public static AddRoomCardMarker Instance { get; } = new AddRoomCardMarker();

        private AddRoomCardMarker() { }
    }
}
