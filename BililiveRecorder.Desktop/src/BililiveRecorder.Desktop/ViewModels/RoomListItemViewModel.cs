using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    /// <summary>
    /// 包装 IRoom 供 UI 绑定。IRoom 的属性变化可能在后台线程发生，这里统一调度到 UI 线程刷新。
    /// </summary>
    public partial class RoomListItemViewModel : ObservableObject, IDisposable
    {
        private readonly ILocalizationService loc;
        private readonly UiSettingsViewModel uiSettings;
        private bool disposed;

        public IRoom Room { get; }

        public ILocalizationService Loc => this.loc;

        public UiSettingsViewModel UiSettings => this.uiSettings;

        public RoomListItemViewModel(IRoom room, ILocalizationService loc, UiSettingsViewModel uiSettings)
        {
            this.Room = room ?? throw new ArgumentNullException(nameof(room));
            this.loc = loc ?? throw new ArgumentNullException(nameof(loc));
            this.uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            room.PropertyChanged += this.Room_PropertyChanged;
            room.Stats.PropertyChanged += this.RoomStats_PropertyChanged;
        }

        public int RoomId => this.Room.RoomConfig.RoomId;

        public int ShortId => this.Room.ShortId;

        public string DisplayId => this.ShortId != 0 ? this.ShortId.ToString() : this.RoomId.ToString();

        public string Name => this.Room.Name;

        public string Title => this.Room.Title;

        public string AreaText
        {
            get
            {
                var parent = this.Room.AreaNameParent;
                var child = this.Room.AreaNameChild;
                if (string.IsNullOrWhiteSpace(parent))
                    return string.Empty;
                return string.IsNullOrWhiteSpace(child) ? parent : $"{parent} · {child}";
            }
        }

        public bool Recording => this.Room.Recording;

        public bool Streaming => this.Room.Streaming;

        public bool DanmakuConnected => this.Room.DanmakuConnected;

        public bool AutoRecord
        {
            get => this.Room.RoomConfig.AutoRecord;
            set
            {
                this.Room.RoomConfig.AutoRecord = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.ShowMonitoring));
            }
        }

        /// <summary>本次会话是否允许自动录制（用户手动停止后为 false，卡片上显示橙色提示）。</summary>
        public bool AutoRecordForThisSession => this.Room.AutoRecordForThisSession;

        public bool HasShortId => this.ShortId != 0;

        /// <summary>未录制且开启了自动录制时显示“监控中”（与 WPF 版一致）。</summary>
        public bool ShowMonitoring => !this.Recording && this.AutoRecord;

        /// <summary>速度药丸文本，如 "12.34 Mbps"。</summary>
        public string SpeedText => string.Format(this.loc["RoomCard_Status_SpeedIndicator_SpeedInMbps"], this.Room.Stats.NetworkMbps);

        /// <summary>速度药丸的悬浮提示与点击详情（直播服务器、速度比、时长、数据量）。</summary>
        public string StatsTooltipText
        {
            get
            {
                var stats = this.Room.Stats;
                if (double.IsNaN(stats.DurationRatio))
                    return this.loc["RoomCard_Status_SpeedIndicator_NoData"];

                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(stats.StreamHost))
                    sb.Append("直播服务器: ").AppendLine(stats.StreamHost);
                sb.AppendLine(string.Format(this.loc["RoomCard_Status_SpeedIndicator_SpeedInPercentage"], stats.DurationRatio));
                sb.AppendLine(string.Format(this.loc["RoomCard_Status_SpeedIndicator_CurrentFileDuration"], stats.FileMaxTimestamp));
                sb.AppendLine(string.Format(this.loc["RoomCard_Status_SpeedIndicator_TotalFileDuration"], stats.SessionMaxTimestamp));
                sb.AppendLine(string.Format(this.loc["RoomCard_Status_SpeedIndicator_SessionDuration"], stats.SessionDuration));
                sb.AppendLine(string.Format(this.loc["RoomCard_Status_SpeedIndicator_TotalnputBytes"], FormatBytes(stats.TotalInputBytes)));
                sb.Append(string.Format(this.loc["RoomCard_Status_SpeedIndicator_TotalOutputBytes"], FormatBytes(stats.TotalOutputBytes)));
                return sb.ToString();
            }
        }

        private static string FormatBytes(long bytes)
        {
            const long KiB = 1024;
            const long MiB = KiB * 1024;
            const long GiB = MiB * 1024;
            const long TiB = GiB * 1024;
            return bytes switch
            {
                < KiB => $"{bytes} Bytes",
                < MiB => $"{bytes / (double)KiB:F2} KiB",
                < GiB => $"{bytes / (double)MiB:F2} MiB",
                < TiB => $"{bytes / (double)GiB:F2} GiB",
                _ => $"{bytes / (double)TiB:F2} TiB",
            };
        }

        public string StatusText => this.Recording
            ? this.loc["RoomCard_Status_Recording"]
            : this.loc["RoomCard_Status_Monitoring"];

        [RelayCommand]
        private void StartRecord() => this.Room.StartRecord();

        [RelayCommand]
        private void StopRecord() => this.Room.StopRecord();

        [RelayCommand]
        private void SplitOutput() => this.Room.SplitOutput();

        [RelayCommand]
        private Task RefreshAsync() => this.Room.RefreshRoomInfoAsync();

        [RelayCommand]
        private void EnableAutoRecord() => this.AutoRecord = true;

        [RelayCommand]
        private void DisableAutoRecord() => this.AutoRecord = false;

        [RelayCommand]
        private void OpenInBrowser()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://live.bilibili.com/" + this.Room.RoomConfig.RoomId)
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Log.ForContext<RoomListItemViewModel>().Warning(ex, "Error opening room in browser");
            }
        }

        [RelayCommand]
        private async Task CopyRoomIdAsync()
        {
            try
            {
                var topLevel = App.MainWindowOrNull;
                if (topLevel?.Clipboard is not null)
                    await topLevel.Clipboard.SetTextAsync(this.Room.RoomConfig.RoomId.ToString());
            }
            catch (Exception) { }
        }

        [RelayCommand]
        private void Remove() => this.RemoveRequested?.Invoke(this, EventArgs.Empty);

        /// <summary>请求打开单房间设置窗口。</summary>
        public event EventHandler? ShowSettingsRequested;

        [RelayCommand]
        private void ShowSettings() => this.ShowSettingsRequested?.Invoke(this, EventArgs.Empty);

        /// <summary>请求从列表中删除该房间（视图负责显示确认对话框）。</summary>
        public event EventHandler? RemoveRequested;

        private void Room_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (this.disposed)
                return;

            if (Dispatcher.UIThread.CheckAccess())
            {
                this.OnPropertyChanged(string.Empty);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!this.disposed)
                        this.OnPropertyChanged(string.Empty);
                });
            }
        }

        private void RoomStats_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 统计数据可能在后台线程更新，调度到 UI 线程刷新速度显示
            if (Dispatcher.UIThread.CheckAccess())
                this.OnStatsPropertyChanged();
            else
                Dispatcher.UIThread.Post(this.OnStatsPropertyChanged);
        }

        private void OnStatsPropertyChanged()
        {
            if (this.disposed)
                return;
            this.OnPropertyChanged(nameof(this.SpeedText));
            this.OnPropertyChanged(nameof(this.StatsTooltipText));
        }

        public void Dispose()
        {
            this.disposed = true;
            this.Room.PropertyChanged -= this.Room_PropertyChanged;
            this.Room.Stats.PropertyChanged -= this.RoomStats_PropertyChanged;
        }
    }
}
