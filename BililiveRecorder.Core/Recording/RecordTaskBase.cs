using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Scripting;
using BililiveRecorder.Core.Templating;
using Serilog;
using Timer = System.Timers.Timer;
using static BililiveRecorder.Core.Api.Model.RoomPlayInfo;

namespace BililiveRecorder.Core.Recording
{
    internal abstract class RecordTaskBase : IRecordTask
    {
        private const string HttpHeaderAccept = "*/*";
        private const string HttpHeaderOrigin = Api.Http.HttpApiClient.HttpHeaderOrigin;
        private const string HttpHeaderReferer = Api.Http.HttpApiClient.HttpHeaderReferer;
        private const string HttpHeaderUserAgent = Api.Http.HttpApiClient.HttpHeaderUserAgent;

        private const int timer_inverval = 2;
        protected readonly Timer timer = new Timer(1000 * timer_inverval);
        protected readonly Random random = new Random();
        protected readonly CancellationTokenSource cts = new CancellationTokenSource();
        protected readonly CancellationToken ct;

        protected readonly IRoom room;
        protected readonly ILogger logger;
        protected readonly IApiClient apiClient;
        private readonly FileNameGenerator fileNameGenerator;
        private readonly UserScriptRunner userScriptRunner;

        private int partIndex = 0;

        protected string? streamHost;
        protected string? streamHostFull;
        protected bool started = false;
        protected bool timeoutTriggered = false;
        protected int qn;

        /// <summary>输出文件扩展名，FLV 为 ".flv"，HLS fMP4 为 ".mp4"，HLS TS 为 ".ts"</summary>
        private string outputFileExtension = ".flv";
        private int hlsSplitRequested;
        private int hlsSessionEndedNotified;

        private void NotifyHlsSessionEnded()
        {
            if (Interlocked.Exchange(ref this.hlsSessionEndedNotified, 1) == 0)
                this.OnRecordSessionEnded(EventArgs.Empty);
        }

        private readonly object ioStatsLock = new();
        protected int ioNetworkDownloadedBytes;

        protected Stopwatch ioDiskStopwatch = new();
        protected object ioDiskStatsLock = new();
        protected TimeSpan ioDiskWriteDuration;
        protected int ioDiskWrittenBytes;

        private DateTimeOffset ioStatsLastTrigger;
        private TimeSpan durationSinceNoDataReceived;

        protected RecordTaskBase(IRoom room, ILogger logger, IApiClient apiClient, UserScriptRunner userScriptRunner)
        {
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.userScriptRunner = userScriptRunner ?? throw new ArgumentNullException(nameof(userScriptRunner));

            this.fileNameGenerator = new FileNameGenerator(room.RoomConfig, logger);
            this.ct = this.cts.Token;

            this.timer.Elapsed += this.Timer_Elapsed_TriggerIOStats;
        }

        public Guid SessionId { get; } = Guid.NewGuid();

        public int Qn => this.qn;

        #region Events

        public event EventHandler<IOStatsEventArgs>? IOStats;
        public event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        public event EventHandler? RecordSessionEnded;

        protected void OnIOStats(IOStatsEventArgs e) => IOStats?.Invoke(this, e);
        protected void OnRecordingStats(RecordingStatsEventArgs e) => RecordingStats?.Invoke(this, e);
        protected void OnRecordFileOpening(RecordFileOpeningEventArgs e) => RecordFileOpening?.Invoke(this, e);
        protected void OnRecordFileClosed(RecordFileClosedEventArgs e) => RecordFileClosed?.Invoke(this, e);
        protected void OnRecordSessionEnded(EventArgs e) => RecordSessionEnded?.Invoke(this, e);

        #endregion

        public virtual void RequestStop() => this.cts.Cancel();

        public virtual void SplitOutput() => Interlocked.Exchange(ref this.hlsSplitRequested, 1);

        public async virtual Task StartAsync()
        {
            if (this.started)
                throw new InvalidOperationException("Only one StartAsync call allowed per instance.");
            this.started = true;

            var (fullUrl, codecQn, hlsFormat) = await this.FetchStreamUrlAsync(this.room.RoomConfig.RoomId).ConfigureAwait(false);

            this.qn = codecQn.Qn;
            this.streamHost = new Uri(fullUrl).Host;
            var qnDesc = StreamQualityNumber.MapToString(codecQn.Qn);

            if (hlsFormat is not null)
            {
                this.outputFileExtension = hlsFormat == "fmp4" ? ".mp4" : ".ts";
                this.logger.Information("直播服务器 {Host} 不提供 FLV 流，改用 HLS ({Format}) 录制，输出格式 {Extension}，画质 {Qn} ({QnDescription})",
                    this.streamHost, hlsFormat, this.outputFileExtension, codecQn, qnDesc);
                this.logger.Debug("HLS 播放列表地址 {Url}", fullUrl);
                // HLS 录制在后台运行，与 FLV 的 StartRecordingLoop 行为一致（StartAsync 立即返回）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await this.StartHlsRecordingAsync(fullUrl).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "HLS 录制任务异常退出");
                        this.RequestStop();
                        this.NotifyHlsSessionEnded();
                    }
                });
                return;
            }

            this.logger.Information("连接直播服务器 {Host} 录制画质 {Qn} ({QnDescription})", this.streamHost, codecQn, qnDesc);
            this.logger.Debug("直播流地址 {Url}", fullUrl);

            var stream = await this.GetStreamAsync(fullUrl: fullUrl, timeout: (int)this.room.RoomConfig.TimingStreamConnect).ConfigureAwait(false);

            this.ioStatsLastTrigger = DateTimeOffset.UtcNow;
            this.durationSinceNoDataReceived = TimeSpan.Zero;

            this.ct.Register(state => _ = Task.Run(async () =>
            {
                try
                {
                    if (state is not WeakReference<Stream> weakRef)
                        return;

                    await Task.Delay(1000);

                    if (weakRef.TryGetTarget(out var weakStream))
                    {
#if NET6_0_OR_GREATER
                        await weakStream.DisposeAsync();
#else
                        weakStream.Dispose();
#endif
                    }
                }
                catch (Exception)
                { }
            }), state: new WeakReference<Stream>(stream), useSynchronizationContext: false);

            this.StartRecordingLoop(stream);
        }

        protected abstract void StartRecordingLoop(Stream stream);

        private void Timer_Elapsed_TriggerIOStats(object? sender, ElapsedEventArgs e)
        {
            int networkDownloadBytes, diskWriteBytes;
            TimeSpan durationDiff, diskWriteDuration;
            DateTimeOffset startTime, endTime;


            lock (this.ioStatsLock) // 锁 timer elapsed 事件本身防止并行运行
            {
                // networks
                networkDownloadBytes = Interlocked.Exchange(ref this.ioNetworkDownloadedBytes, 0); // 锁网络统计
                endTime = DateTimeOffset.UtcNow;
                startTime = this.ioStatsLastTrigger;
                this.ioStatsLastTrigger = endTime;
                durationDiff = endTime - startTime;

                this.durationSinceNoDataReceived = networkDownloadBytes > 0 ? TimeSpan.Zero : this.durationSinceNoDataReceived + durationDiff;

                // disks
                lock (this.ioDiskStatsLock) // 锁硬盘统计
                {
                    diskWriteDuration = this.ioDiskWriteDuration;
                    diskWriteBytes = this.ioDiskWrittenBytes;
                    this.ioDiskWriteDuration = TimeSpan.Zero;
                    this.ioDiskWrittenBytes = 0;
                }
            }

            var netMbps = networkDownloadBytes * (8d / 1024d / 1024d) / durationDiff.TotalSeconds;
            var diskMBps = diskWriteBytes / (1024d * 1024d) / diskWriteDuration.TotalSeconds;

            this.OnIOStats(new IOStatsEventArgs
            {
                StreamHost = this.streamHost,
                NetworkBytesDownloaded = networkDownloadBytes,
                Duration = durationDiff,
                StartTime = startTime,
                EndTime = endTime,
                NetworkMbps = netMbps,
                DiskBytesWritten = diskWriteBytes,
                DiskWriteDuration = diskWriteDuration,
                DiskMBps = diskMBps,
            });

            if ((!this.timeoutTriggered) && (this.durationSinceNoDataReceived.TotalMilliseconds > this.room.RoomConfig.TimingWatchdogTimeout))
            {
                this.timeoutTriggered = true;
                this.logger.Warning("检测到录制卡住，可能是网络或硬盘原因，将会主动断开连接");
                this.RequestStop();
            }
        }

        protected (string fullPath, string relativePath) CreateFileName()
        {
            this.partIndex++;

            var output = this.fileNameGenerator.CreateFilePath(new FileNameTemplateContext
            {
                Name = FileNameGenerator.RemoveInvalidFileName(this.room.Name, ignore_slash: false),
                Title = FileNameGenerator.RemoveInvalidFileName(this.room.Title, ignore_slash: false),
                RoomId = this.room.RoomConfig.RoomId,
                ShortId = this.room.ShortId,
                Uid = this.room.Uid,
                AreaParent = FileNameGenerator.RemoveInvalidFileName(this.room.AreaNameParent, ignore_slash: false),
                AreaChild = FileNameGenerator.RemoveInvalidFileName(this.room.AreaNameChild, ignore_slash: false),
                PartIndex = this.partIndex,
                Qn = this.qn,
                Json = this.room.RawBilibiliApiJsonData,
            }, fileExtension: this.outputFileExtension == ".flv" ? null : this.outputFileExtension);

            return (output.FullPath!, output.RelativePath);
        }

        #region Api Requests

        private HttpClient CreateHttpClient()
        {
            var handler = Api.Http.HttpClientWithBindAddress.CreateHandler(
                this.room.RoomConfig.NetworkTransportBindAddress,
                this.room.RoomConfig.NetworkTransportUseSystemProxy,
                allowAutoRedirect: false);

            var httpClient = new HttpClient(handler);
            var headers = httpClient.DefaultRequestHeaders;
            headers.Add("Accept", HttpHeaderAccept);
            headers.Add("Origin", HttpHeaderOrigin);
            headers.Add("Referer", HttpHeaderReferer);
            headers.Add("User-Agent", HttpHeaderUserAgent);
            return httpClient;
        }

        internal static readonly char[] QnParseSeparator = new[] { ',', '，', '、', ' ' };
        private static IReadOnlyList<StreamCodecQn> ParseAllowedQn(string? allowedQn)
        {
            if (string.IsNullOrWhiteSpace(allowedQn)) return Array.Empty<StreamCodecQn>();

            var qns = allowedQn!.Split(QnParseSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(static x =>
                {
                    if (int.TryParse(x, out var num))
                    {
                        return new StreamCodecQn
                        {
                            Qn = num,
                            Codec = StreamCodec.AVC
                        };
                    }
                    else if (x.StartsWith("avc", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(x[3..], out num))
                        {
                            return new StreamCodecQn
                            {
                                Qn = num,
                                Codec = StreamCodec.AVC
                            };
                        }
                    }
                    else if (x.StartsWith("hevc", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(x[4..], out num))
                        {
                            return new StreamCodecQn
                            {
                                Qn = num,
                                Codec = StreamCodec.HEVC
                            };
                        }
                    }

                    // invalid
                    return new StreamCodecQn
                    {
                        Qn = -1,
                        Codec = StreamCodec.AVC
                    };
                })
                .Where(x => x.Qn >= 0)
                .ToList();

            return qns;
        }

        private readonly record struct HlsStreamCandidate(StreamCodecQn CodecQn, string FormatName);

        protected async Task<(string url, StreamCodecQn codecQn, string? hlsFormat)> FetchStreamUrlAsync(int roomid)
        {
            var allowedQn = ParseAllowedQn(this.room.RoomConfig.RecordingQuality);

            // 优先使用用户脚本获取直播流地址
            if (this.userScriptRunner.CallOnFetchStreamUrl(this.logger, roomid, allowedQn) is { } urlFromScript)
            {
                this.logger.Information("使用用户脚本返回的直播流地址 {Url}", urlFromScript);
                return (urlFromScript, new StreamCodecQn { Codec = StreamCodec.AVC, Qn = -1 }, null);
            }

            const int DefaultQn = 10000;
            var codecs = await this.apiClient.GetStreamUrlCodecsAsync(roomid: roomid, qn: DefaultQn).ConfigureAwait(false);
            //?? throw new Exception("no supported stream url, qn: " + DefaultQn);

            var allAvailableCodecQn = new List<StreamCodecQn>();

            if (codecs.FlvAvc is not null)
            {
                allAvailableCodecQn.AddRange(codecs.FlvAvc.AcceptQn.Select(x => new StreamCodecQn
                {
                    Codec = StreamCodec.AVC,
                    Qn = x
                }));
            }
            if (codecs.FlvHevc is not null)
            {
                allAvailableCodecQn.AddRange(codecs.FlvHevc.AcceptQn.Select(x => new StreamCodecQn
                {
                    Codec = StreamCodec.HEVC,
                    Qn = x
                }));
            }

            StreamCodecQn? selectedCodecQn = null;
            // Select first avaiable qn
            foreach (var qn in allowedQn)
            {
                if (allAvailableCodecQn.Contains(qn))
                {
                    selectedCodecQn = qn;
                    break;
                }
            }

            if (selectedCodecQn is not null)
            {
                this.logger.Debug("设置画质 {QnSettings}, 可用画质 {AcceptQn}, 最终选择 {SelectedQn}", allowedQn, allAvailableCodecQn, selectedCodecQn);
                var (flvUrl, flvCodecQn) = await this.FetchFlvStreamUrlAsync(roomid, codecs, selectedCodecQn.Value, DefaultQn).ConfigureAwait(false);
                return (flvUrl, flvCodecQn, null);
            }

            // FLV 不可用时尝试 HLS (http_hls) 直播流，部分直播间只提供 HLS
            var hlsCandidate = this.SelectHlsCandidate(codecs, allowedQn);
            if (hlsCandidate is not null)
            {
                this.logger.Debug("FLV 无可用画质，改用 HLS 流。设置画质 {QnSettings}", allowedQn);
                var (hlsUrl, hlsCodecQn) = await this.FetchHlsStreamUrlAsync(roomid, codecs, hlsCandidate.Value, DefaultQn).ConfigureAwait(false);
                return (hlsUrl, hlsCodecQn, hlsCandidate.Value.FormatName);
            }

            this.logger.Information("没有符合设置要求的画质，稍后再试。设置画质 {QnSettings}, 可用画质 {AcceptQn}", allowedQn, allAvailableCodecQn);
            throw new NoMatchingQnValueException();
        }

        private async Task<(string url, StreamCodecQn codecQn)> FetchFlvStreamUrlAsync(int roomid, StreamUrlCodecs codecs, StreamCodecQn selectedCodecQn, int defaultQn)
        {
            if (selectedCodecQn.Qn != defaultQn)
            {
                // 最终选择的 qn 与默认不同，需要重新请求一次
                codecs = await this.apiClient.GetStreamUrlCodecsAsync(roomid: roomid, qn: selectedCodecQn.Qn).ConfigureAwait(false);
            }

            var item = selectedCodecQn.Codec switch
            {
                StreamCodec.AVC => codecs.FlvAvc,
                StreamCodec.HEVC => codecs.FlvHevc,
                _ => throw new Exception("unknown codec")
            };

            if (item is null)
                throw new Exception("no supported stream url for " + selectedCodecQn);

            if (item.CurrentQn != selectedCodecQn.Qn)
                this.logger.Warning("返回的直播流地址的画质是 {CurrentQn} 而不是请求的 {SelectedQn}", item.CurrentQn, selectedCodecQn);

            return (this.BuildStreamUrl(item), new StreamCodecQn { Codec = selectedCodecQn.Codec, Qn = item.CurrentQn });
        }

        private HlsStreamCandidate? SelectHlsCandidate(StreamUrlCodecs codecs, IReadOnlyList<StreamCodecQn> allowedQn)
        {
            // fmp4 优先于 ts，同画质下优先 fmp4
            var candidates = new List<HlsStreamCandidate>();
            void AddCandidates(IReadOnlyList<HlsCodecItem> items, StreamCodec codec)
            {
                foreach (var formatPref in new[] { "fmp4", "ts" })
                {
                    foreach (var e in items.Where(x => x.FormatName == formatPref))
                    {
                        foreach (var qn in e.Item.AcceptQn)
                            candidates.Add(new HlsStreamCandidate(new StreamCodecQn { Codec = codec, Qn = qn }, e.FormatName));
                    }
                }
                // 其他格式兜底
                foreach (var e in items.Where(x => x.FormatName is not ("fmp4" or "ts")))
                {
                    foreach (var qn in e.Item.AcceptQn)
                        candidates.Add(new HlsStreamCandidate(new StreamCodecQn { Codec = codec, Qn = qn }, e.FormatName));
                }
            }
            AddCandidates(codecs.HlsAvc, StreamCodec.AVC);
            AddCandidates(codecs.HlsHevc, StreamCodec.HEVC);

            foreach (var qn in allowedQn)
            {
                var match = candidates.FirstOrDefault(x => x.CodecQn.Equals(qn));
                if (match.FormatName is not null)
                    return match;
            }

            // 用户设置的画质没有匹配项时，选择第一个可用画质
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private async Task<(string url, StreamCodecQn codecQn)> FetchHlsStreamUrlAsync(int roomid, StreamUrlCodecs codecs, HlsStreamCandidate candidate, int defaultQn)
        {
            if (candidate.CodecQn.Qn != defaultQn)
            {
                // 最终选择的 qn 与默认不同，需要重新请求一次
                codecs = await this.apiClient.GetStreamUrlCodecsAsync(roomid: roomid, qn: candidate.CodecQn.Qn).ConfigureAwait(false);
            }

            var items = candidate.CodecQn.Codec == StreamCodec.AVC ? codecs.HlsAvc : codecs.HlsHevc;
            var entry = items.FirstOrDefault(x => x.FormatName == candidate.FormatName);
            if (entry.Item is null)
                entry = items.FirstOrDefault(x => x.FormatName == "fmp4");
            if (entry.Item is null && items.Count > 0)
                entry = items[0];

            var item = entry.Item;
            if (item is null)
                throw new Exception("no supported hls stream url for " + candidate.CodecQn);

            if (item.CurrentQn != candidate.CodecQn.Qn)
                this.logger.Warning("返回的直播流地址的画质是 {CurrentQn} 而不是请求的 {SelectedQn}", item.CurrentQn, candidate.CodecQn);

            var url = this.BuildStreamUrl(item);
            return (url, new StreamCodecQn { Codec = candidate.CodecQn.Codec, Qn = item.CurrentQn });
        }

        private string BuildStreamUrl(CodecItem item)
        {
            var url_infos = item.UrlInfos;
            if (url_infos is null || url_infos.Length == 0)
                throw new Exception("no url_info");

            // https:// xy0x0x0x0xy.mcdn.bilivideo.cn:486
            var url_infos_without_mcdn = url_infos.Where(x => !x.Host.Contains(".mcdn.")).ToArray();

            var url_info = url_infos_without_mcdn.Length != 0
                ? url_infos_without_mcdn[this.random.Next(url_infos_without_mcdn.Length)]
                : url_infos[this.random.Next(url_infos.Length)];

            return url_info.Host + item.BaseUrl + url_info.Extra;
        }

        protected async Task<Stream> GetStreamAsync(string fullUrl, int timeout)
        {
            var client = this.CreateHttpClient();

            var streamHostInfoBuilder = new StringBuilder();

            while (true)
            {
                var allowedAddressFamily = this.room.RoomConfig.NetworkTransportAllowedAddressFamily;
                HttpRequestMessage request;
                Uri originalUri;

                if (this.userScriptRunner.CallOnTransformStreamUrl(this.logger, fullUrl) is { } scriptResult)
                {
                    var (scriptUrl, scriptIp) = scriptResult;

                    this.logger.Debug("用户脚本重定向了直播流地址 {NewUrl}, 旧地址 {OldUrl}", scriptUrl, fullUrl);

                    fullUrl = scriptUrl;
                    originalUri = new Uri(fullUrl);


                    if (scriptIp is not null)
                    {
                        this.logger.Debug("用户脚本指定了服务器 IP {IP}", scriptIp);

                        var uri = new Uri(fullUrl);
                        var builder = new UriBuilder(uri)
                        {
                            Host = scriptIp
                        };

                        request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
                        request.Headers.Host = uri.IsDefaultPort ? uri.Host : uri.Host + ":" + uri.Port;

                        streamHostInfoBuilder.Append(originalUri.Host);
                        streamHostInfoBuilder.Append(" [");
                        streamHostInfoBuilder.Append(scriptIp);
                        streamHostInfoBuilder.Append(']');

                        goto sendRequest;
                    }
                }
                else
                {
                    originalUri = new Uri(fullUrl);
                }

                if (allowedAddressFamily == AllowedAddressFamily.System)
                {
                    this.logger.Debug("NetworkTransportAllowedAddressFamily is System");
                    request = new HttpRequestMessage(HttpMethod.Get, originalUri);

                    streamHostInfoBuilder.Append(originalUri.Host);
                }
                else
                {
                    var ips = await Dns.GetHostAddressesAsync(originalUri.DnsSafeHost);

                    var filtered = ips.Where(x => allowedAddressFamily switch
                    {
                        AllowedAddressFamily.Ipv4 => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork,
                        AllowedAddressFamily.Ipv6 => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6,
                        AllowedAddressFamily.Any => true,
                        _ => false
                    }).ToArray();

                    var selected = filtered[this.random.Next(filtered.Length)];

                    this.logger.Debug("指定直播服务器地址 {DnsHost}: {SelectedIp}, Allowed: {AllowedAddressFamily}, {IPAddresses}", originalUri.DnsSafeHost, selected, allowedAddressFamily, ips);

                    streamHostInfoBuilder.Append(originalUri.Host);
                    streamHostInfoBuilder.Append(" [");
                    streamHostInfoBuilder.Append(selected);
                    streamHostInfoBuilder.Append(']');

                    if (selected is null)
                    {
                        throw new Exception("DNS 没有返回符合要求的 IP 地址");
                    }

                    var builder = new UriBuilder(originalUri)
                    {
                        Host = selected.ToString()
                    };

                    request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
                    request.Headers.Host = originalUri.IsDefaultPort ? originalUri.Host : originalUri.Host + ":" + originalUri.Port;
                }

            sendRequest:

                var resp = await client.SendAsync(request,
                     HttpCompletionOption.ResponseHeadersRead,
                     new CancellationTokenSource(timeout).Token)
                     .ConfigureAwait(false);

                switch (resp.StatusCode)
                {
                    case HttpStatusCode.OK:
                        {
                            this.logger.Information("开始接收直播流");
                            this.streamHostFull = streamHostInfoBuilder.ToString();
                            var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            return stream;
                        }
                    case HttpStatusCode.Moved:
                    case HttpStatusCode.Redirect:
                        {
                            fullUrl = new Uri(originalUri, resp.Headers.Location!).ToString();
                            this.logger.Debug("跳转到 {Url}, 原文本 {Location}", fullUrl, resp.Headers.Location!.OriginalString);
                            resp.Dispose();
                            streamHostInfoBuilder.Append('\n');
                            break;
                        }
                    default:
                        throw new Exception(string.Format("尝试下载直播流时服务器返回了 ({0}){1}", resp.StatusCode, resp.ReasonPhrase));
                }
            }
        }
        #endregion

        #region HLS Recording

        /// <summary>
        /// 通过 HLS (m3u8) 录制直播。将初始化分片与媒体分片按顺序写入单个文件：
        /// fMP4 输出 .mp4（init segment + fragments 顺序拼接即为可播放的 fragmented MP4），
        /// TS 输出 .ts。
        /// </summary>
        private async Task StartHlsRecordingAsync(string playlistUrl)
        {
            var handler = Api.Http.HttpClientWithBindAddress.CreateHandler(
                this.room.RoomConfig.NetworkTransportBindAddress,
                this.room.RoomConfig.NetworkTransportUseSystemProxy,
                allowAutoRedirect: true);
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("Accept", HttpHeaderAccept);
            client.DefaultRequestHeaders.Add("Origin", HttpHeaderOrigin);
            client.DefaultRequestHeaders.Add("Referer", HttpHeaderReferer);
            client.DefaultRequestHeaders.Add("User-Agent", HttpHeaderUserAgent);

            var playlistUri = new Uri(playlistUrl);
            var isFmp4 = this.outputFileExtension == ".mp4";

            this.ioStatsLastTrigger = DateTimeOffset.UtcNow;
            this.durationSinceNoDataReceived = TimeSpan.Zero;
            this.timer.Start();

            var sessionStart = DateTimeOffset.UtcNow;
            var totalInputBytes = 0L;
            var totalOutputBytes = 0L;
            var sessionDurationSec = 0d;

            FileStream? fileStream = null;
            RecordFileOpeningEventArgs? openingArgs = null;
            double fileDurationSec = 0;
            long fileSize = 0;

            long lastWrittenSeq = long.MinValue;
            long lastPlaylistMediaSeq = long.MinValue;
            string? currentInitKey = null;
            byte[]? initData = null;
            var pollDelay = TimeSpan.FromSeconds(1);

            async Task CloseFileAsync()
            {
                if (fileStream is null) return;
                var f = fileStream;
                fileStream = null;
                this.ioDiskStopwatch.Restart();
#if NET6_0_OR_GREATER
                await f.DisposeAsync().ConfigureAwait(false);
#else
                f.Dispose();
#endif
                this.ioDiskStopwatch.Stop();
                this.ioDiskStopwatch.Reset();

                if (openingArgs is not null)
                {
                    this.OnRecordFileClosed(new RecordFileClosedEventArgs(this.room)
                    {
                        SessionId = this.SessionId,
                        FullPath = openingArgs.FullPath,
                        RelativePath = openingArgs.RelativePath,
                        FileOpenTime = openingArgs.FileOpenTime,
                        FileCloseTime = DateTimeOffset.Now,
                        Duration = fileDurationSec,
                        FileSize = fileSize,
                    });
                    openingArgs = null;
                }
            }

            async Task OpenNewFileAsync()
            {
                var (fullPath, relativePath) = this.CreateFileName();
                try { _ = Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!); } catch (Exception) { }

                var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                fileStream = stream;
                fileDurationSec = 0;
                fileSize = 0;

                openingArgs = new RecordFileOpeningEventArgs(this.room)
                {
                    SessionId = this.SessionId,
                    FullPath = fullPath,
                    RelativePath = relativePath,
                    FileOpenTime = DateTimeOffset.Now,
                    Qn = this.qn,
                    QnDescription = StreamQualityNumber.MapToString(this.qn),
                };
                this.OnRecordFileOpening(openingArgs);
                this.logger.Information("新建录制文件 {Path}", fullPath);

                // fMP4 需要在文件开头写入初始化分片
                if (isFmp4 && initData is not null)
                {
                    await stream.WriteAsync(initData, 0, initData.Length).ConfigureAwait(false);
                    fileSize += initData.Length;
                }
            }

            async Task<byte[]?> DownloadAsync(Uri uri)
            {
                HttpResponseMessage resp;
                try
                {
                    resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, this.ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (this.ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "HLS 请求 {Uri} 失败", uri);
                    return null;
                }

                using (resp)
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        this.logger.Warning("HLS 下载 {Uri} 返回 {StatusCode}", uri, resp.StatusCode);
                        return null;
                    }
                    try
                    {
#if NET6_0_OR_GREATER
                        return await resp.Content.ReadAsByteArrayAsync(this.ct).ConfigureAwait(false);
#else
                        return await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
                    }
                    catch (OperationCanceledException) when (this.ct.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "HLS 读取 {Uri} 数据失败", uri);
                        return null;
                    }
                }
            }

            bool ShouldSplitFile()
            {
                if (Interlocked.Exchange(ref this.hlsSplitRequested, 0) == 1)
                    return true;
                return this.room.RoomConfig.CuttingMode switch
                {
                    CuttingMode.ByTime => fileDurationSec * 1000 > this.room.RoomConfig.CuttingNumber * 60u * 1000u,
                    CuttingMode.BySize => fileSize / (1024d * 1024d) > this.room.RoomConfig.CuttingNumber,
                    _ => false,
                };
            }

            try
            {
                this.logger.Information("开始 HLS 录制 {Url}", playlistUrl);

                while (!this.ct.IsCancellationRequested)
                {
                    string playlistText;
                    try
                    {
                        var data = await DownloadAsync(playlistUri).ConfigureAwait(false);
                        if (data is null) { await Task.Delay(2000, this.ct).ConfigureAwait(false); continue; }
                        playlistText = Encoding.UTF8.GetString(data);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "获取 HLS 播放列表失败");
                        try { await Task.Delay(2000, this.ct).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
                        continue;
                    }

                    HlsPlaylist playlist;
                    try
                    {
                        playlist = HlsPlaylist.Parse(playlistText);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "解析 HLS 播放列表失败");
                        try { await Task.Delay(2000, this.ct).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
                        continue;
                    }

                    if (playlist.IsEncrypted)
                    {
                        this.logger.Error("HLS 直播流已加密，无法录制");
                        break;
                    }

                    pollDelay = TimeSpan.FromSeconds(Math.Max(0.5, Math.Min(playlist.TargetDuration, 3)));

                    // 直播流重新开始（分片序列号回退）时关闭当前文件并重新累计
                    if (playlist.Segments.Count > 0 && playlist.MediaSequence < lastPlaylistMediaSeq)
                    {
                        this.logger.Information("HLS 分片序列号回退，直播流可能已重新开始，关闭当前录制文件");
                        await CloseFileAsync().ConfigureAwait(false);
                        lastWrittenSeq = long.MinValue;
                        currentInitKey = null;
                        initData = null;
                    }
                    if (playlist.Segments.Count > 0)
                        lastPlaylistMediaSeq = playlist.MediaSequence;

                    // 初始化分片（fMP4 的 EXT-X-MAP）
                    if (playlist.MapUri != currentInitKey)
                    {
                        currentInitKey = playlist.MapUri;
                        initData = null;
                        if (playlist.MapUri is not null)
                        {
                            try
                            {
                                var mapData = await DownloadAsync(new Uri(playlistUri, playlist.MapUri)).ConfigureAwait(false);
                                if (mapData is not null)
                                {
                                    initData = mapData;
                                    Interlocked.Add(ref this.ioNetworkDownloadedBytes, mapData.Length);
                                    Interlocked.Add(ref totalInputBytes, mapData.Length);
                                    Interlocked.Add(ref totalOutputBytes, mapData.Length);
                                }
                            }
                            catch (OperationCanceledException) { break; }
                            catch (Exception ex)
                            {
                                this.logger.Warning(ex, "下载 HLS 初始化分片失败");
                            }
                        }
                    }

                    foreach (var seg in playlist.Segments)
                    {
                        if (seg.Sequence <= lastWrittenSeq || this.ct.IsCancellationRequested)
                            continue;

                        byte[]? segData;
                        try
                        {
                            segData = await DownloadAsync(new Uri(playlistUri, seg.Uri)).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { break; }
                        catch (Exception ex)
                        {
                            this.logger.Warning(ex, "下载 HLS 分片 {Uri} 失败", seg.Uri);
                            segData = null;
                        }

                        if (segData is null)
                        {
                            // 跳过失败的分片，避免阻塞后续分片
                            lastWrittenSeq = seg.Sequence;
                            continue;
                        }

                        if (fileStream is null)
                            await OpenNewFileAsync().ConfigureAwait(false);

                        var writeSw = Stopwatch.StartNew();
                        await fileStream!.WriteAsync(segData, 0, segData.Length, this.ct).ConfigureAwait(false);
                        writeSw.Stop();

                        fileSize += segData.Length;
                        fileDurationSec += seg.Duration;
                        sessionDurationSec += seg.Duration;
                        lastWrittenSeq = seg.Sequence;

                        Interlocked.Add(ref this.ioNetworkDownloadedBytes, segData.Length);
                        Interlocked.Add(ref totalInputBytes, segData.Length);
                        Interlocked.Add(ref totalOutputBytes, segData.Length);

                        lock (this.ioDiskStatsLock)
                        {
                            this.ioDiskWriteDuration += writeSw.Elapsed;
                            this.ioDiskWrittenBytes += segData.Length;
                        }

                        var tsMs = (int)(fileDurationSec * 1000);
                        var sessionTsMs = (int)(sessionDurationSec * 1000);

                        // 上报录制统计（供 UI/统计使用）
                        var now = DateTimeOffset.UtcNow;
                        this.OnRecordingStats(new RecordingStatsEventArgs
                        {
                            SessionDuration = (now - sessionStart).TotalMilliseconds,
                            TotalInputBytes = totalInputBytes,
                            TotalOutputBytes = totalOutputBytes,
                            CurrentFileSize = fileSize,
                            SessionMaxTimestamp = sessionTsMs,
                            FileMaxTimestamp = tsMs,
                            AddedDuration = seg.Duration * 1000,
                            PassedTime = pollDelay.TotalMilliseconds,
                            DurationRatio = pollDelay.TotalMilliseconds > 0 ? seg.Duration * 1000 / pollDelay.TotalMilliseconds : 0,
                            InputVideoBytes = totalInputBytes,
                            OutputVideoBytes = totalOutputBytes,
                        });

                        if (ShouldSplitFile())
                        {
                            this.logger.Information("达到切分条件，关闭当前录制文件");
                            await CloseFileAsync().ConfigureAwait(false);
                        }
                    }

                    if (playlist.EndList)
                    {
                        this.logger.Information("HLS 播放列表已结束");
                        break;
                    }

                    try { await Task.Delay(pollDelay, this.ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "HLS 录制发生错误");
            }
            finally
            {
                try { await CloseFileAsync().ConfigureAwait(false); }
                catch (Exception ex) { this.logger.Warning(ex, "关闭 HLS 录制文件时出错"); }
                client.Dispose();
                this.timer.Stop();
                this.RequestStop();
                this.NotifyHlsSessionEnded();
                this.logger.Information("录制结束");
            }
        }

        #endregion
    }
}
