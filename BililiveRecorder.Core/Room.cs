using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.Core.Danmaku;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Recording;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core
{
    public class Room : IRoom
    {
        private readonly object recordStartLock = new object();
        private readonly SemaphoreSlim recordRetryDelaySemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Timer timer;

        private readonly IServiceScope scope;
        private readonly ILogger loggerWithoutContext;
        private readonly IDanmakuClient danmakuClient;
        private readonly IApiClient apiClient;
        private readonly IBasicDanmakuWriter basicDanmakuWriter;
        private readonly IRecordTaskFactory recordTaskFactory;
        private readonly CancellationTokenSource cts;
        private readonly CancellationToken ct;

        private ILogger logger;
        private bool disposedValue;

        private int shortId;
        private string name = string.Empty;
        private string title = string.Empty;
        private string areaNameParent = string.Empty;
        private string areaNameChild = string.Empty;
        private bool streaming;
        private bool danmakuConnected;

        private IRecordTask? recordTask;
        private DateTimeOffset recordTaskStartTime;

        public Room(IServiceScope scope, RoomConfig roomConfig, ILogger logger, IDanmakuClient danmakuClient, IApiClient apiClient, IBasicDanmakuWriter basicDanmakuWriter, IRecordTaskFactory recordTaskFactory)
        {
            this.scope = scope ?? throw new ArgumentNullException(nameof(scope));
            this.RoomConfig = roomConfig ?? throw new ArgumentNullException(nameof(roomConfig));
            this.loggerWithoutContext = logger?.ForContext<Room>() ?? throw new ArgumentNullException(nameof(logger));
            this.logger = this.loggerWithoutContext.ForContext(LoggingContext.RoomId, this.RoomConfig.RoomId);
            this.danmakuClient = danmakuClient ?? throw new ArgumentNullException(nameof(danmakuClient));
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.basicDanmakuWriter = basicDanmakuWriter ?? throw new ArgumentNullException(nameof(basicDanmakuWriter));
            this.recordTaskFactory = recordTaskFactory ?? throw new ArgumentNullException(nameof(recordTaskFactory));

            this.timer = new Timer(this.RoomConfig.TimingCheckInterval * 1000);
            this.cts = new CancellationTokenSource();
            this.ct = this.cts.Token;

            this.RoomConfig.PropertyChanged += this.RoomConfig_PropertyChanged;
            this.timer.Elapsed += this.Timer_Elapsed;
            this.danmakuClient.StatusChanged += this.DanmakuClient_StatusChanged;
            this.danmakuClient.DanmakuReceived += this.DanmakuClient_DanmakuReceived;

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                await this.RefreshRoomInfoAsync();
            });
        }

        public int ShortId { get => this.shortId; private set => this.SetField(ref this.shortId, value); }
        public string Name { get => this.name; private set => this.SetField(ref this.name, value); }
        public string Title { get => this.title; private set => this.SetField(ref this.title, value); }
        public string AreaNameParent { get => this.areaNameParent; private set => this.SetField(ref this.areaNameParent, value); }
        public string AreaNameChild { get => this.areaNameChild; private set => this.SetField(ref this.areaNameChild, value); }
        public bool Streaming
        {
            get => this.streaming;
            private set
            {
                if (value == this.streaming) return;

                // 从未开播状态切换为开播状态时重置允许录制状态
                var triggerRecord = value && !this.streaming;
                if (triggerRecord)
                    this.AutoRecordAllowedForThisSession = true;

                this.streaming = value;
                this.OnPropertyChanged(nameof(this.Streaming));
                if (triggerRecord && this.RoomConfig.AutoRecord)
                    _ = Task.Run(() => this.CreateAndStartNewRecordTask());
            }
        }
        public bool DanmakuConnected { get => this.danmakuConnected; private set => this.SetField(ref this.danmakuConnected, value); }
        public bool Recording => this.recordTask != null;

        public bool AutoRecordAllowedForThisSession { get; private set; }

        public RoomConfig RoomConfig { get; }
        public RecordingStats Stats { get; } = new RecordingStats();

        public Guid ObjectId { get; } = Guid.NewGuid();

        public event EventHandler<RecordSessionStartedEventArgs>? RecordSessionStarted;
        public event EventHandler<RecordSessionEndedEventArgs>? RecordSessionEnded;
        public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        public event EventHandler<NetworkingStatsEventArgs>? NetworkingStats;
        public event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void SplitOutput()
        {
            lock (this.recordStartLock)
            {
                this.recordTask?.SplitOutput();
            }
        }

        public void StartRecord()
        {
            lock (this.recordStartLock)
            {
                this.AutoRecordAllowedForThisSession = true;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.FetchRoomInfoThenCreateRecordTaskAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "尝试开始录制时出错");
                    }
                });
            }
        }

        public void StopRecord()
        {
            lock (this.recordStartLock)
            {
                this.AutoRecordAllowedForThisSession = false;

                if (this.recordTask == null)
                    return;

                this.recordTask.RequestStop();
            }
        }

        public async Task RefreshRoomInfoAsync()
        {
            try
            {
                await this.FetchUserInfoAsync().ConfigureAwait(false);
                await this.FetchRoomInfoAsync().ConfigureAwait(false);
                this.StartDamakuConnection(delay: false);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "刷新房间信息时出错");
            }
        }

        #region Recording

        /// <exception cref="Exception"/>
        private async Task FetchRoomInfoAsync()
        {
            var room = (await this.apiClient.GetRoomInfoAsync(this.RoomConfig.RoomId).ConfigureAwait(false)).Data;
            if (room != null)
            {
                this.RoomConfig.RoomId = room.RoomId;
                this.ShortId = room.ShortId;
                this.Title = room.Title;
                this.AreaNameParent = room.ParentAreaName;
                this.AreaNameChild = room.AreaName;
                this.Streaming = room.LiveStatus == 1;
            }
        }

        /// <exception cref="Exception"/>
        private async Task FetchUserInfoAsync()
        {
            var user = await this.apiClient.GetUserInfoAsync(this.RoomConfig.RoomId).ConfigureAwait(false);
            this.Name = user.Data?.Info?.Name ?? this.Name;
        }

        /// <exception cref="Exception"/>
        private async Task FetchRoomInfoThenCreateRecordTaskAsync()
        {
            await this.FetchRoomInfoAsync().ConfigureAwait(false);
            this.CreateAndStartNewRecordTask();
        }

        ///
        private void CreateAndStartNewRecordTask()
        {
            lock (this.recordStartLock)
            {
                if (this.disposedValue)
                    return;

                if (!this.Streaming)
                    return;

                if (this.recordTask != null)
                    return;

                var task = this.recordTaskFactory.CreateRecordTask(this);
                task.NetworkingStats += this.RecordTask_NetworkingStats;
                task.RecordingStats += this.RecordTask_RecordingStats;
                task.RecordFileOpening += this.RecordTask_RecordFileOpening;
                task.RecordFileClosed += this.RecordTask_RecordFileClosed;
                task.RecordSessionEnded += this.RecordTask_RecordSessionEnded;
                this.recordTask = task;
                this.recordTaskStartTime = DateTimeOffset.UtcNow;
                this.OnPropertyChanged(nameof(this.Recording));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.recordTask.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("启动录制出错 " + ex.ToString());
                        this.recordTask = null;
                        _ = Task.Run(async () => await this.TryRestartRecordingAsync().ConfigureAwait(false));
                        this.OnPropertyChanged(nameof(this.Recording));
                        return;
                    }
                    RecordSessionStarted?.Invoke(this, new RecordSessionStartedEventArgs(this)
                    {
                        SessionId = this.recordTask.SessionId
                    });
                });
            }
        }

        ///
        private async Task TryRestartRecordingAsync(bool delay = true)
        {
            if (this.AutoRecordAllowedForThisSession)
            {
                try
                {
                    if (delay)
                    {
                        if (!await this.recordRetryDelaySemaphoreSlim.WaitAsync(0).ConfigureAwait(false))
                            return;

                        try
                        {
                            await Task.Delay((int)this.RoomConfig.TimingStreamRetry, this.ct).ConfigureAwait(false);
                        }
                        finally
                        {
                            this.recordRetryDelaySemaphoreSlim.Release();
                        }
                    }

                    if (!this.AutoRecordAllowedForThisSession)
                        return;

                    await this.FetchRoomInfoThenCreateRecordTaskAsync().ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "重试开始录制时出错");
                }
            }
        }

        ///
        private void StartDamakuConnection(bool delay = true) =>
            Task.Run(async () =>
            {
                try
                {
                    if (delay)
                        await Task.Delay((int)this.RoomConfig.TimingDanmakuRetry, this.ct).ConfigureAwait(false);

                    await this.danmakuClient.ConnectAsync(this.RoomConfig.RoomId, this.ct).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "连接弹幕服务器时出错");

                    if (!this.ct.IsCancellationRequested)
                        this.StartDamakuConnection();
                }
            });

        #endregion

        #region Event Handlers

        ///
        private void RecordTask_NetworkingStats(object sender, NetworkingStatsEventArgs e)
        {
            this.logger.Verbose("Networking stats: {@stats}", e);

            this.Stats.NetworkMbps = e.Mbps;

            NetworkingStats?.Invoke(this, e);
        }

        ///
        private void RecordTask_RecordingStats(object sender, RecordingStatsEventArgs e)
        {
            this.logger.Verbose("Recording stats: {@stats}", e);

            var diff = DateTimeOffset.UtcNow - this.recordTaskStartTime;
            this.Stats.SessionDuration = TimeSpan.FromSeconds(Math.Round(diff.TotalSeconds));
            this.Stats.FileMaxTimestamp = TimeSpan.FromMilliseconds(e.FileMaxTimestamp);
            this.Stats.SessionMaxTimestamp = TimeSpan.FromMilliseconds(e.SessionMaxTimestamp);
            this.Stats.DuraionRatio = e.DuraionRatio;

            this.Stats.TotalInputBytes = e.TotalInputVideoByteCount + e.TotalInputAudioByteCount;
            this.Stats.TotalOutputBytes = e.TotalOutputVideoByteCount + e.TotalOutputAudioByteCount;

            RecordingStats?.Invoke(this, e);
        }

        ///
        private void RecordTask_RecordFileClosed(object sender, RecordFileClosedEventArgs e)
        {
            this.basicDanmakuWriter.Disable();
            RecordFileClosed?.Invoke(this, e);
        }

        ///
        private void RecordTask_RecordFileOpening(object sender, RecordFileOpeningEventArgs e)
        {
            if (this.RoomConfig.RecordDanmaku)
                this.basicDanmakuWriter.EnableWithPath(Path.ChangeExtension(e.FullPath, "xml"), this);
            RecordFileOpening?.Invoke(this, e);
        }

        ///
        private void RecordTask_RecordSessionEnded(object sender, EventArgs e)
        {
            Guid id;
            lock (this.recordStartLock)
            {
                id = this.recordTask?.SessionId ?? default;
                this.recordTask = null;
                _ = Task.Run(async () => await this.TryRestartRecordingAsync().ConfigureAwait(false));
            }

            this.OnPropertyChanged(nameof(this.Recording));
            RecordSessionEnded?.Invoke(this, new RecordSessionEndedEventArgs(this)
            {
                SessionId = id
            });
        }

        private void DanmakuClient_DanmakuReceived(object sender, Api.Danmaku.DanmakuReceivedEventArgs e)
        {
            var d = e.Danmaku;

            switch (d.MsgType)
            {
                case Api.Danmaku.DanmakuMsgType.LiveStart:
                    this.Streaming = true;
                    break;
                case Api.Danmaku.DanmakuMsgType.LiveEnd:
                    this.Streaming = false;
                    break;
                case Api.Danmaku.DanmakuMsgType.RoomChange:
                    this.Title = d.Title ?? this.Title;
                    this.AreaNameParent = d.ParentAreaName ?? this.AreaNameParent;
                    this.AreaNameChild = d.AreaName ?? this.AreaNameChild;
                    break;
                default:
                    break;
            }

            this.basicDanmakuWriter.Write(d);
        }

        private void DanmakuClient_StatusChanged(object sender, Api.Danmaku.StatusChangedEventArgs e)
        {
            if (e.Connected)
            {
                this.DanmakuConnected = true;
            }
            else
            {
                this.DanmakuConnected = false;
                this.StartDamakuConnection();
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.RoomConfig.AutoRecord)
                _ = Task.Run(async () => await this.TryRestartRecordingAsync(delay: false).ConfigureAwait(false));
        }

        private void RoomConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.RoomConfig.RoomId):
                    this.logger = this.loggerWithoutContext.ForContext(LoggingContext.RoomId, this.RoomConfig.RoomId);
                    break;
                case nameof(this.RoomConfig.TimingCheckInterval):
                    this.timer.Interval = this.RoomConfig.TimingCheckInterval * 1000;
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region PropertyChanged

        protected void SetField<T>(ref T location, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(location, value))
                return;

            location = value;

            if (propertyName != null)
                this.OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName: propertyName));

        #endregion

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.cts.Cancel();
                    this.cts.Dispose();
                    this.recordTask?.RequestStop();
                    this.basicDanmakuWriter.Disable();
                    this.scope.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Room()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
