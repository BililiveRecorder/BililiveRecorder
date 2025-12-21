using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Danmaku;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Recording;
using BililiveRecorder.Core.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Serilog;
using Serilog.Events;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core
{
    internal class Room : IRoom
    {
        private const int HR_ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
        private const int HR_ERROR_DISK_FULL = unchecked((int)0x80070070);

        private readonly object recordStartLock = new object();
        private readonly SemaphoreSlim recordRetryDelaySemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Timer timer;

        private readonly IServiceScope scope;
        private readonly ILogger loggerWithoutContext;
        private readonly IDanmakuClient danmakuClient;
        private readonly IApiClient apiClient;
        private readonly IBasicDanmakuWriter basicDanmakuWriter;
        private readonly IRecordTaskFactory recordTaskFactory;
        private readonly UserScriptRunner userScriptRunner;
        private readonly CancellationTokenSource cts;
        private readonly CancellationToken ct;

        private ILogger logger;
        private bool disposedValue;

        private int shortId;
        private string name = string.Empty;
        private long uid;
        private string title = string.Empty;
        private string areaNameParent = string.Empty;
        private string areaNameChild = string.Empty;
        private bool danmakuConnected;
        private bool streaming;
        private bool autoRecordForThisSession = true;
        private bool nextRecordShouldUseRawMode = false;

        private IRecordTask? recordTask;
        private DateTimeOffset danmakuClientConnectTime;
        private readonly ManualResetEventSlim danmakuConnectHoldOff = new();

        private static readonly TimeSpan danmakuClientReconnectNoDelay = TimeSpan.FromMinutes(1);
        private static readonly HttpClient coverDownloadHttpClient = new HttpClient();

        static Room()
        {
            coverDownloadHttpClient.Timeout = TimeSpan.FromSeconds(10);
            coverDownloadHttpClient.DefaultRequestHeaders.UserAgent.Clear();
        }

        public Room(IServiceScope scope, RoomConfig roomConfig, int initDelayFactor, ILogger logger, IDanmakuClient danmakuClient, IApiClient apiClient, IBasicDanmakuWriter basicDanmakuWriter, IRecordTaskFactory recordTaskFactory, UserScriptRunner userScriptRunner)
        {
            this.scope = scope ?? throw new ArgumentNullException(nameof(scope));
            this.RoomConfig = roomConfig ?? throw new ArgumentNullException(nameof(roomConfig));
            this.loggerWithoutContext = logger?.ForContext<Room>() ?? throw new ArgumentNullException(nameof(logger));
            this.logger = this.loggerWithoutContext.ForContext(LoggingContext.RoomId, this.RoomConfig.RoomId);
            this.danmakuClient = danmakuClient ?? throw new ArgumentNullException(nameof(danmakuClient));
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.basicDanmakuWriter = basicDanmakuWriter ?? throw new ArgumentNullException(nameof(basicDanmakuWriter));
            this.recordTaskFactory = recordTaskFactory ?? throw new ArgumentNullException(nameof(recordTaskFactory));
            this.userScriptRunner = userScriptRunner ?? throw new ArgumentNullException(nameof(userScriptRunner));

            this.timer = new Timer(this.RoomConfig.TimingCheckInterval * 1000d);
            this.cts = new CancellationTokenSource();
            this.ct = this.cts.Token;

            this.PropertyChanged += this.Room_PropertyChanged;
            this.RoomConfig.PropertyChanged += this.RoomConfig_PropertyChanged;

            this.timer.Elapsed += this.Timer_Elapsed;

            this.danmakuClient.StatusChanged += this.DanmakuClient_StatusChanged;
            this.danmakuClient.DanmakuReceived += this.DanmakuClient_DanmakuReceived;
            this.danmakuClient.BeforeHandshake = this.DanmakuClient_BeforeHandshake;

            _ = Task.Run(async () =>
            {
                await Task.Delay(1500 + (initDelayFactor * 500));
                this.timer.Start();
                await this.RefreshRoomInfoAsync();
            });
        }

        public int ShortId { get => this.shortId; private set => this.SetField(ref this.shortId, value); }
        public string Name { get => this.name; private set => this.SetField(ref this.name, value); }
        public long Uid { get => this.uid; private set => this.SetField(ref this.uid, value); }
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
                        // 手动触发录制，启动录制前再刷新一次房间信息
                        this.CreateAndStartNewRecordTask(skipFetchRoomInfo: false);
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
                // 如果直播状态从 false 改成 true，Room_PropertyChanged 会触发录制
                await this.FetchRoomInfoAsync().ConfigureAwait(false);

                this.StartDamakuConnection(delay: false);
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
                this.logger.Debug("拉取房间信息成功: {@room}", room);

                this.RoomConfig.RoomId = room.Room.RoomId;
                this.ShortId = room.Room.ShortId;
                this.Uid = room.Room.Uid;
                this.Title = room.Room.Title;
                this.AreaNameParent = room.Room.ParentAreaName;
                this.AreaNameChild = room.Room.AreaName;
                this.Streaming = room.Room.LiveStatus == 1;

                this.Name = room.User.BaseInfo.Name;

                this.RawBilibiliApiJsonData = room.RawBilibiliApiJsonData;

                // allow danmaku client to connect
                this.danmakuConnectHoldOff.Set();
            }
        }

        public void MarkNextRecordShouldUseRawMode()
        {
            this.nextRecordShouldUseRawMode = true;
        }

        private static readonly TimeSpan TitleRegexMatchTimeout = TimeSpan.FromSeconds(0.5);

        /// <exception cref="ArgumentException" />
        /// <exception cref="RegexMatchTimeoutException" />
        private bool DoesTitleAllowRecord()
        {
            // 按新行分割的正则表达式
            var patterns = this.RoomConfig.TitleFilterPatterns?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (patterns is null || patterns.Length == 0)
                return true;

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(input: this.Title, pattern: pattern, options: RegexOptions.None, matchTimeout: TitleRegexMatchTimeout))
                    return false;
            }

            return true;
        }

        ///
        private void CreateAndStartNewRecordTask(bool skipFetchRoomInfo = false)
        {
            lock (this.recordStartLock)
            {
                if (this.disposedValue)
                    return;

                if (!this.Streaming)
                    return;

                if (this.recordTask != null)
                    return;

                try
                {
                    if (!this.DoesTitleAllowRecord())
                    {
                        this.logger.Information("标题匹配到跳过录制设置中的规则，不录制");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "检查标题是否匹配跳过录制正则表达式时出错");
                }

                var task = this.recordTaskFactory.CreateRecordTask(this, this.nextRecordShouldUseRawMode ? RecordMode.RawData : null);
                this.nextRecordShouldUseRawMode = false;

                task.IOStats += this.RecordTask_IOStats;
                task.RecordingStats += this.RecordTask_RecordingStats;
                task.RecordFileOpening += this.RecordTask_RecordFileOpening;
                task.RecordFileClosed += this.RecordTask_RecordFileClosed;
                task.RecordSessionEnded += this.RecordTask_RecordSessionEnded;
                this.recordTask = task;
                this.Stats.Reset();
                this.OnPropertyChanged(nameof(this.Recording));

                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!skipFetchRoomInfo)
                            await this.FetchRoomInfoAsync();

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

                        if (ex is IOException ioex && (ioex.HResult == HR_ERROR_DISK_FULL || ioex.HResult == HR_ERROR_HANDLE_DISK_FULL))
                        {
                            this.logger.Warning("因为硬盘空间已满，本次不再自动重试启动录制。");
                            return;
                        }
                        else if (ex is BilibiliApiResponseCodeNotZeroException notzero && notzero.Code == 19002005)
                        {
                            // 房间已加密
                            this.logger.Warning("房间已加密，无密码获取不到直播流，本次不再自动重试启动录制。");
                            return;
                        }
                        else
                        {
                            // 请求直播流出错时的重试逻辑
                            _ = Task.Run(() => this.RestartAfterRecordTaskFailedAsync(RestartRecordingReason.GenericRetry));
                            return;
                        }
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
                    _ = this.recordRetryDelaySemaphoreSlim.Release();
                }

                // 如果状态是非直播中，跳过重试尝试。当状态切换到直播中时会开始新的录制任务。
                if (!this.Streaming || !this.AutoRecordForThisSession)
                    return;

                // 启动录制时更新房间信息
                if (this.Streaming && this.AutoRecordForThisSession)
                    this.CreateAndStartNewRecordTask(skipFetchRoomInfo: false);
            }
            catch (Exception ex)
            {
                this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "重试开始录制时出错");
                _ = Task.Run(() => this.RestartAfterRecordTaskFailedAsync(restartRecordingReason));
            }
        }

        ///
        private void StartDamakuConnection(bool delay = true) =>
            _ = Task.Run(async () =>
            {
                if (this.disposedValue)
                    return;
                try
                {
                    if (delay)
                    {
                        try
                        {
                            await Task.Delay((int)this.RoomConfig.TimingDanmakuRetry, this.ct).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                            // 房间已被删除
                            return;
                        }
                    }

                    // 至少要等到获取到一次房间信息后才能连接弹幕服务器。
                    // 理论上不专门写这么个逻辑也应该没有问题的，但为了保证连接时肯定有主播 uid 信息还是加上吧。
                    // 这里同步堵塞等待了，因为 ManualResetEventSlim 没有 Async Wait
                    // 并且这里运行在一个新的 Task 里，同步等待的影响也不是很大。
                    if (!this.danmakuConnectHoldOff.Wait(TimeSpan.FromSeconds(2)))
                    {
                        // 如果 2 秒后还没有获取到房间信息直接返回
                        // 房间信息获取成功后会自动再次尝试连接弹幕服务器所以不用担心会导致不连接弹幕服务器
                        this.logger.Debug("暂无房间信息，不连接弹幕服务器");
                        return;
                    }

                    await this.danmakuClient.ConnectAsync(this.RoomConfig.RoomId, this.RoomConfig.DanmakuTransport, this.ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.Write(ex is ExecutionRejectedException ? LogEventLevel.Verbose : LogEventLevel.Warning, ex, "连接弹幕服务器时出错");

                    if (!this.ct.IsCancellationRequested)
                        this.StartDamakuConnection(delay: true);
                }
            });

        #endregion

        #region Event Handlers

        ///
        private void RecordTask_IOStats(object? sender, IOStatsEventArgs e)
        {
            this.logger.Verbose("IO stats: {@stats}", e);

            this.Stats.StreamHost = e.StreamHost;
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
        private void RecordTask_RecordingStats(object? sender, RecordingStatsEventArgs e)
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
        private void RecordTask_RecordFileClosed(object? sender, RecordFileClosedEventArgs e)
        {
            this.basicDanmakuWriter.Disable();

            RecordFileClosed?.Invoke(this, e);
        }

        ///
        private void RecordTask_RecordFileOpening(object? sender, RecordFileOpeningEventArgs e)
        {
            if (this.RoomConfig.RecordDanmaku)
                this.basicDanmakuWriter.EnableWithPath(Path.ChangeExtension(e.FullPath, "xml"), this);
            else
                this.basicDanmakuWriter.Disable();

            if (this.RoomConfig.SaveStreamCover)
            {
                _ = Task.Run(() => this.SaveStreamCoverAsync(e.FullPath));
            }

            RecordFileOpening?.Invoke(this, e);
        }

        private async Task SaveStreamCoverAsync(string flvFullPath)
        {
            const int MAX_ATTEMPT = 3;
            var attempt = 0;
        retry:
            try
            {
                var coverUrl = this.RawBilibiliApiJsonData?["room_info"]?["cover"]?.ToObject<string>();

                if (string.IsNullOrWhiteSpace(coverUrl))
                {
                    this.logger.Information("没有直播间封面信息");
                    return;
                }

                var targetPath = Path.ChangeExtension(flvFullPath, "cover" + Path.GetExtension(coverUrl));

                var stream = await coverDownloadHttpClient.GetStreamAsync(coverUrl).ConfigureAwait(false);
                using var file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(file).ConfigureAwait(false);

                this.logger.Debug("直播间封面已成功从 {CoverUrl} 保存到 {FilePath}", coverUrl, targetPath);
            }
            catch (Exception ex)
            {
                if (attempt++ < MAX_ATTEMPT)
                {
                    this.logger.Debug(ex, "保存直播间封面时出错, 次数 {Attempt}", attempt);
                    goto retry;
                }

                this.logger.Warning(ex, "保存直播间封面时出错");
            }
        }

        ///
        private void RecordTask_RecordSessionEnded(object? sender, EventArgs e)
        {
            Guid id;
            lock (this.recordStartLock)
            {
                id = this.recordTask?.SessionId ?? default;
                this.recordTask = null;
                _ = Task.Run(async () =>
                {
                    await Task.Yield();

                    // 录制结束退出后的重试逻辑
                    // 比 RestartAfterRecordTaskFailedAsync 少了等待时间

                    // 如果状态是非直播中，跳过重试尝试。当状态切换到直播中时会开始新的录制任务。
                    if (!this.Streaming || !this.AutoRecordForThisSession)
                        return;

                    try
                    {
                        // 开始录制前刷新房间信息
                        if (this.Streaming && this.AutoRecordForThisSession)
                            this.CreateAndStartNewRecordTask(skipFetchRoomInfo: false);
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

        private string? DanmakuClient_BeforeHandshake(string json)
        {
            var danmakuAuthenticateWithStreamerUid = this.RoomConfig.DanmakuAuthenticateWithStreamerUid;
            if (danmakuAuthenticateWithStreamerUid)
            {
                var obj = JObject.Parse(json);
                obj["uid"] = this.Uid;
                obj.Remove("key");
                obj.Remove("buvid");
                json = obj.ToString(Formatting.None);
            }

            var scriptUpdatedJson = this.userScriptRunner.CallOnDanmakuHandshake(this.logger, this, json);

            if (scriptUpdatedJson is not null)
                return scriptUpdatedJson;
            else if (danmakuAuthenticateWithStreamerUid)
                return json;
            else
                return null;
        }

        private void DanmakuClient_DanmakuReceived(object? sender, Api.Danmaku.DanmakuReceivedEventArgs e)
        {
            var d = e.Danmaku;

            switch (d.MsgType)
            {
                case Api.Danmaku.DanmakuMsgType.LiveStart:
                    this.logger.Debug("推送直播开始");
                    this.Streaming = true;
                    break;
                case Api.Danmaku.DanmakuMsgType.LiveEnd:
                    this.logger.Debug("推送直播结束");
                    this.Streaming = false;
                    break;
                case Api.Danmaku.DanmakuMsgType.RoomChange:
                    this.logger.Debug("推送房间信息变更, {Title}, {AreaNameParent}, {AreaNameChild}", d.Title, d.ParentAreaName, d.AreaName);
                    this.Title = d.Title ?? this.Title;
                    this.AreaNameParent = d.ParentAreaName ?? this.AreaNameParent;
                    this.AreaNameChild = d.AreaName ?? this.AreaNameChild;
                    break;
                case Api.Danmaku.DanmakuMsgType.RoomLock:
                    this.logger.Information("直播间被封禁");
                    break;
                case Api.Danmaku.DanmakuMsgType.CutOff:
                    this.logger.Information("直播被管理员切断");
                    break;
                default:
                    break;
            }

            _ = Task.Run(async () => await this.basicDanmakuWriter.WriteAsync(d));
        }

        private void DanmakuClient_StatusChanged(object? sender, Api.Danmaku.StatusChangedEventArgs e)
        {
            this.DanmakuConnected = e.Connected;
            if (e.Connected)
            {
                this.danmakuClientConnectTime = DateTimeOffset.UtcNow;
                this.logger.Information("弹幕服务器已连接");
            }
            else
            {
                this.logger.Information("与弹幕服务器的连接被断开");

                // 如果连接弹幕服务器的时间在至少 1 分钟之前，重连时不需要等待
                // 针对偶尔的网络波动的优化，如果偶尔断开了尽快重连，减少漏录的弹幕量
                this.StartDamakuConnection(delay: !((DateTimeOffset.UtcNow - this.danmakuClientConnectTime) > danmakuClientReconnectNoDelay));
                this.danmakuClientConnectTime = DateTimeOffset.MaxValue;
            }
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            this.StartDamakuConnection(delay: false);

            // 如果开启了自动录制 或者 还没有获取过第一次房间信息
            if (this.RoomConfig.AutoRecord || !this.danmakuConnectHoldOff.IsSet)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 定时主动检查不需要错误重试
                        await this.FetchRoomInfoAsync().ConfigureAwait(false);

                        // 刚更新了房间信息不需要再获取一次
                        if (this.Streaming && this.AutoRecordForThisSession && this.RoomConfig.AutoRecord)
                            this.CreateAndStartNewRecordTask(skipFetchRoomInfo: true);
                    }
                    catch (Exception) { }
                });
            }
        }


        private void Room_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.Streaming):
                    if (this.Streaming)
                    {
                        // 如果开播状态是通过广播消息获取的，本地的直播间信息就不是最新的，需要重新获取。
                        if (this.AutoRecordForThisSession && this.RoomConfig.AutoRecord)
                            this.CreateAndStartNewRecordTask(skipFetchRoomInfo: false);
                    }
                    else
                    {
                        this.AutoRecordForThisSession = true;
                    }
                    break;
                case nameof(this.Title):
                    if (this.RoomConfig.CuttingByTitle)
                    {
                        this.SplitOutput();
                    }
                    break;
                default:
                    break;
            }
        }

        private void RoomConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.RoomConfig.RoomId):
                    this.logger = this.loggerWithoutContext.ForContext(LoggingContext.RoomId, this.RoomConfig.RoomId);
                    break;
                case nameof(this.RoomConfig.TimingCheckInterval):
                    this.timer.Interval = this.RoomConfig.TimingCheckInterval * 1000d;
                    break;
                case nameof(this.RoomConfig.AutoRecord):
                    if (this.RoomConfig.AutoRecord)
                    {
                        this.AutoRecordForThisSession = true;

                        // 启动录制时更新一次房间信息
                        if (this.Streaming && this.AutoRecordForThisSession)
                            this.CreateAndStartNewRecordTask(skipFetchRoomInfo: false);
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
