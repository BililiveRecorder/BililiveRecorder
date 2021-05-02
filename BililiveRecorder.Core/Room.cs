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
using Polly;
using Serilog;
using Serilog.Events;
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
        private bool danmakuConnected;
        private bool streaming;
        private bool autoRecordForThisSession = true;

        private IRecordTask? recordTask;
        private DateTimeOffset recordTaskStartTime;

        public Room(IServiceScope scope, RoomConfig roomConfig, int initDelayFactor, ILogger logger, IDanmakuClient danmakuClient, IApiClient apiClient, IBasicDanmakuWriter basicDanmakuWriter, IRecordTaskFactory recordTaskFactory)
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

            this.PropertyChanged += this.Room_PropertyChanged;
            this.RoomConfig.PropertyChanged += this.RoomConfig_PropertyChanged;

            this.timer.Elapsed += this.Timer_Elapsed;

            this.danmakuClient.StatusChanged += this.DanmakuClient_StatusChanged;
            this.danmakuClient.DanmakuReceived += this.DanmakuClient_DanmakuReceived;

            _ = Task.Run(async () =>
            {
                await Task.Delay(1500 + (initDelayFactor * 500));
                this.timer.Start();
                await this.RefreshRoomInfoAsync();
            });
        }

        public int ShortId { get => this.shortId; private set => this.SetField(ref this.shortId, value); }
        public string Name { get => this.name; private set => this.SetField(ref this.name, value); }
        public string Title { get => this.title; private set => this.SetField(ref this.title, value); }
        public string AreaNameParent { get => this.areaNameParent; private set => this.SetField(ref this.areaNameParent, value); }
        public string AreaNameChild { get => this.areaNameChild; private set => this.SetField(ref this.areaNameChild, value); }

        public bool Streaming { get => this.streaming; private set => this.SetField(ref this.streaming, value); }

        public bool AutoRecordForThisSession { get => this.autoRecordForThisSession; private set => this.SetField(ref this.autoRecordForThisSession, value); }

        public bool DanmakuConnected { get => this.danmakuConnected; private set => this.SetField(ref this.danmakuConnected, value); }

        public bool Recording => this.recordTask != null;

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
            if (this.disposedValue)
                return;

            lock (this.recordStartLock)
            {
                this.recordTask?.SplitOutput();
            }
        }

        public void StartRecord()
        {
            if (this.disposedValue)
                return;

            lock (this.recordStartLock)
            {
                this.AutoRecordForThisSession = true;

                _ = Task.Run(() =>
                {
                    try
                    {
                        this.CreateAndStartNewRecordTask();
                    }
                    catch (Exception ex)
                    {
                        this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "尝试开始录制时出错");
                    }
                });
            }
        }

        public void StopRecord()
        {
            if (this.disposedValue)
                return;

            lock (this.recordStartLock)
            {
                this.AutoRecordForThisSession = false;

                if (this.recordTask == null)
                    return;

                this.recordTask.RequestStop();
            }
        }

        public async Task RefreshRoomInfoAsync()
        {
            if (this.disposedValue)
                return;

            try
            {
                await this.FetchUserInfoAsync().ConfigureAwait(false);
                await this.FetchRoomInfoAsync().ConfigureAwait(false);
                this.StartDamakuConnection(delay: false);

                if (this.Streaming && this.AutoRecordForThisSession && this.RoomConfig.AutoRecord)
                    this.CreateAndStartNewRecordTask();
            }
            catch (Exception ex)
            {
                this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "刷新房间信息时出错");
            }
        }

        #region Recording

        /// <exception cref="Exception"/>
        private async Task FetchRoomInfoAsync()
        {
            if (this.disposedValue)
                return;
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
            if (this.disposedValue)
                return;
            var user = await this.apiClient.GetUserInfoAsync(this.RoomConfig.RoomId).ConfigureAwait(false);
            this.Name = user.Data?.Info?.Name ?? this.Name;
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
                this.Stats.Reset();
                this.OnPropertyChanged(nameof(this.Recording));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.recordTask.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "启动录制出错");

                        this.recordTask = null;
                        this.OnPropertyChanged(nameof(this.Recording));

                        // 请求直播流出错时的重试逻辑
                        _ = Task.Run(this.RestartAfterRecordTaskFailedAsync);

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
        private async Task RestartAfterRecordTaskFailedAsync()
        {
            if (this.disposedValue)
                return;
            if (!this.Streaming || !this.AutoRecordForThisSession)
                return;

            try
            {
                if (!await this.recordRetryDelaySemaphoreSlim.WaitAsync(0).ConfigureAwait(false))
                    return;

                try
                {
                    await Task.Delay((int)this.RoomConfig.TimingStreamRetry, this.ct).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // 房间已经被删除
                    return;
                }
                finally
                {
                    this.recordRetryDelaySemaphoreSlim.Release();
                }

                if (!this.Streaming || !this.AutoRecordForThisSession)
                    return;

                await this.FetchRoomInfoAsync().ConfigureAwait(false);

                if (this.Streaming && this.AutoRecordForThisSession)
                    this.CreateAndStartNewRecordTask();
            }
            catch (Exception ex)
            {
                this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "重试开始录制时出错");
                _ = Task.Run(this.RestartAfterRecordTaskFailedAsync);
            }
        }

        ///
        private void StartDamakuConnection(bool delay = true) =>
            Task.Run(async () =>
            {
                if (this.disposedValue)
                    return;
                try
                {
                    if (delay)
                        try
                        {
                            await Task.Delay((int)this.RoomConfig.TimingDanmakuRetry, this.ct).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                            // 房间已被删除
                            return;
                        }

                    await this.danmakuClient.ConnectAsync(this.RoomConfig.RoomId, this.ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "连接弹幕服务器时出错");

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
            else
                this.basicDanmakuWriter.Disable();

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
                _ = Task.Run(async () =>
                {
                    // 录制结束退出后的重试逻辑
                    // 比 RestartAfterRecordTaskFailedAsync 少了等待时间

                    if (!this.Streaming || !this.AutoRecordForThisSession)
                        return;

                    try
                    {
                        await this.FetchRoomInfoAsync().ConfigureAwait(false);

                        if (this.Streaming && this.AutoRecordForThisSession)
                            this.CreateAndStartNewRecordTask();
                    }
                    catch (Exception ex)
                    {
                        this.logger.Write(LogEventLevel.Warning, ex, "重试开始录制时出错");
                        _ = Task.Run(this.RestartAfterRecordTaskFailedAsync);
                    }
                });
            }

            this.basicDanmakuWriter.Disable();

            this.OnPropertyChanged(nameof(this.Recording));
            this.Stats.Reset();

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

            _ = this.basicDanmakuWriter.WriteAsync(d);
        }

        private void DanmakuClient_StatusChanged(object sender, Api.Danmaku.StatusChangedEventArgs e)
        {
            if (e.Connected)
            {
                this.DanmakuConnected = true;
                this.logger.Information("弹幕服务器已连接");
            }
            else
            {
                this.DanmakuConnected = false;
                this.logger.Information("与弹幕服务器的连接被断开");
                this.StartDamakuConnection(delay: true);
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.StartDamakuConnection(delay: false);

            if (this.RoomConfig.AutoRecord)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 定时主动检查不需要错误重试
                        await this.FetchRoomInfoAsync().ConfigureAwait(false);

                        if (this.Streaming && this.AutoRecordForThisSession && this.RoomConfig.AutoRecord)
                            this.CreateAndStartNewRecordTask();
                    }
                    catch (Exception) { }
                });
            }
        }

        private void Room_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.Streaming):
                    if (this.Streaming)
                    {
                        if (this.AutoRecordForThisSession && this.RoomConfig.AutoRecord)
                            this.CreateAndStartNewRecordTask();
                    }
                    else
                    {
                        this.AutoRecordForThisSession = true;
                    }
                    break;
                default:
                    break;
            }
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
                case nameof(this.RoomConfig.AutoRecord):
                    if (this.RoomConfig.AutoRecord)
                    {
                        this.AutoRecordForThisSession = true;
                        if (this.Streaming && this.AutoRecordForThisSession)
                            this.CreateAndStartNewRecordTask();
                    }
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
