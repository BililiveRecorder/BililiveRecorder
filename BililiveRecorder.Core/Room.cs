using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Danmaku;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Recording;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Polly;
using Serilog;
using Serilog.Events;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core
{
    internal class Room : IRoom
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
        private DateTimeOffset danmakuClientConnectTime;
        private static readonly TimeSpan danmakuClientReconnectNoDelay = TimeSpan.FromMinutes(1);

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

        public JObject? RawBilibiliApiJsonData { get; private set; }

        public bool Streaming { get => this.streaming; private set => this.SetField(ref this.streaming, value); }

        public bool AutoRecordForThisSession { get => this.autoRecordForThisSession; private set => this.SetField(ref this.autoRecordForThisSession, value); }

        public bool DanmakuConnected { get => this.danmakuConnected; private set => this.SetField(ref this.danmakuConnected, value); }

        public bool Recording => this.recordTask != null;

        public RoomConfig RoomConfig { get; }
        public RoomStats Stats { get; } = new RoomStats();

        public Guid ObjectId { get; } = Guid.NewGuid();

        public event EventHandler<RecordSessionStartedEventArgs>? RecordSessionStarted;
        public event EventHandler<RecordSessionEndedEventArgs>? RecordSessionEnded;
        public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        public event EventHandler<IOStatsEventArgs>? IOStats;
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
                this.RoomConfig.RoomId = room.Room.RoomId;
                this.ShortId = room.Room.ShortId;
                this.Title = room.Room.Title;
                this.AreaNameParent = room.Room.ParentAreaName;
                this.AreaNameChild = room.Room.AreaName;
                this.Streaming = room.Room.LiveStatus == 1;

                this.Name = room.User.BaseInfo.Name;

                this.RawBilibiliApiJsonData = room.RawBilibiliApiJsonData;
            }
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
                task.IOStats += this.RecordTask_IOStats;
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
                    catch (NoMatchingQnValueException)
                    {
                        this.recordTask = null;
                        this.OnPropertyChanged(nameof(this.Recording));

                        // 无匹配的画质，重试录制之前等待更长时间
                        _ = Task.Run(() => this.RestartAfterRecordTaskFailedAsync(RestartRecordingReason.NoMatchingQnValue));

                        return;
                    }
                    catch (Exception ex)
                    {
                        this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "启动录制出错");

                        this.recordTask = null;
                        this.OnPropertyChanged(nameof(this.Recording));

                        // 请求直播流出错时的重试逻辑
                        _ = Task.Run(() => this.RestartAfterRecordTaskFailedAsync(RestartRecordingReason.GenericRetry));

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
        private async Task RestartAfterRecordTaskFailedAsync(RestartRecordingReason restartRecordingReason)
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
                    var delay = restartRecordingReason switch
                    {
                        RestartRecordingReason.GenericRetry => this.RoomConfig.TimingStreamRetry,
                        RestartRecordingReason.NoMatchingQnValue => this.RoomConfig.TimingStreamRetryNoQn * 1000,
                        _ => throw new InvalidOperationException()
                    };
                    await Task.Delay((int)delay, this.ct).ConfigureAwait(false);
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
                _ = Task.Run(() => this.RestartAfterRecordTaskFailedAsync(restartRecordingReason));
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
        private void RecordTask_IOStats(object sender, IOStatsEventArgs e)
        {
            this.logger.Verbose("IO stats: {@stats}", e);

            this.Stats.StartTime = e.StartTime;
            this.Stats.EndTime = e.EndTime;
            this.Stats.Duration = e.Duration;
            this.Stats.NetworkBytesDownloaded = e.NetworkBytesDownloaded;
            this.Stats.NetworkMbps = e.NetworkMbps;
            this.Stats.DiskWriteDuration = e.DiskWriteDuration;
            this.Stats.DiskBytesWritten = e.DiskBytesWritten;
            this.Stats.DiskMBps = e.DiskMBps;

            IOStats?.Invoke(this, e);
        }

        ///
        private void RecordTask_RecordingStats(object sender, RecordingStatsEventArgs e)
        {
            this.logger.Verbose("Recording stats: {@stats}", e);

            this.Stats.SessionDuration = TimeSpan.FromMilliseconds(e.SessionDuration);
            this.Stats.TotalInputBytes = e.TotalInputBytes;
            this.Stats.TotalOutputBytes = e.TotalOutputBytes;
            this.Stats.CurrentFileSize = e.CurrentFileSize;
            this.Stats.SessionMaxTimestamp = TimeSpan.FromMilliseconds(e.SessionMaxTimestamp);
            this.Stats.FileMaxTimestamp = TimeSpan.FromMilliseconds(e.FileMaxTimestamp);
            this.Stats.AddedDuration = e.AddedDuration;
            this.Stats.PassedTime = e.PassedTime;
            this.Stats.DurationRatio = e.DurationRatio;

            this.Stats.InputVideoBytes = e.InputVideoBytes;
            this.Stats.InputAudioBytes = e.InputAudioBytes;

            this.Stats.OutputVideoFrames = e.OutputVideoFrames;
            this.Stats.OutputAudioFrames = e.OutputAudioFrames;
            this.Stats.OutputVideoBytes = e.OutputVideoBytes;
            this.Stats.OutputAudioBytes = e.OutputAudioBytes;

            this.Stats.TotalInputVideoBytes = e.TotalInputVideoBytes;
            this.Stats.TotalInputAudioBytes = e.TotalInputAudioBytes;

            this.Stats.TotalOutputVideoFrames = e.TotalOutputVideoFrames;
            this.Stats.TotalOutputAudioFrames = e.TotalOutputAudioFrames;
            this.Stats.TotalOutputVideoBytes = e.TotalOutputVideoBytes;
            this.Stats.TotalOutputAudioBytes = e.TotalOutputAudioBytes;

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
                        _ = Task.Run(() => this.RestartAfterRecordTaskFailedAsync(RestartRecordingReason.GenericRetry));
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

            _ = Task.Run(async () => await this.basicDanmakuWriter.WriteAsync(d));
        }

        private void DanmakuClient_StatusChanged(object sender, Api.Danmaku.StatusChangedEventArgs e)
        {
            if (e.Connected)
            {
                this.DanmakuConnected = true;
                this.danmakuClientConnectTime = DateTimeOffset.UtcNow;
                this.logger.Information("弹幕服务器已连接");
            }
            else
            {
                this.DanmakuConnected = false;
                this.logger.Information("与弹幕服务器的连接被断开");

                // 如果连接弹幕服务器的时间在至少 1 分钟之前，重连时不需要等待
                // 针对偶尔的网络波动的优化，如果偶尔断开了尽快重连，减少漏录的弹幕量
                this.StartDamakuConnection(delay: !((DateTimeOffset.UtcNow - this.danmakuClientConnectTime) > danmakuClientReconnectNoDelay));

                this.danmakuClientConnectTime = DateTimeOffset.MaxValue;
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
