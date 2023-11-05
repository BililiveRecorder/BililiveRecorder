using System;
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

        public virtual void SplitOutput() { }

        public async virtual Task StartAsync()
        {
            if (this.started)
                throw new InvalidOperationException("Only one StartAsync call allowed per instance.");
            this.started = true;

            var (fullUrl, qn) = await this.FetchStreamUrlAsync(this.room.RoomConfig.RoomId).ConfigureAwait(false);

            this.qn = qn;
            this.streamHost = new Uri(fullUrl).Host;
            var qnDesc = StreamQualityNumber.MapToString(qn);

            this.logger.Information("连接直播服务器 {Host} 录制画质 {Qn} ({QnDescription})", this.streamHost, qn, qnDesc);
            this.logger.Debug("直播流地址 {Url}", fullUrl);

            var stream = await this.GetStreamAsync(fullUrl: fullUrl, timeout: (int)this.room.RoomConfig.TimingStreamConnect).ConfigureAwait(false);

            this.ioStatsLastTrigger = DateTimeOffset.UtcNow;
            this.durationSinceNoDataReceived = TimeSpan.Zero;

            this.ct.Register(state => Task.Run(async () =>
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
            });

            return (output.FullPath!, output.RelativePath);
        }

        #region Api Requests

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseProxy = this.room.RoomConfig.NetworkTransportUseSystemProxy,
            });
            var headers = httpClient.DefaultRequestHeaders;
            headers.Add("Accept", HttpHeaderAccept);
            headers.Add("Origin", HttpHeaderOrigin);
            headers.Add("Referer", HttpHeaderReferer);
            headers.Add("User-Agent", HttpHeaderUserAgent);
            return httpClient;
        }

        protected async Task<(string url, int qn)> FetchStreamUrlAsync(int roomid)
        {
            var qns = this.room.RoomConfig.RecordingQuality?.Split(new[] { ',', '，', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var num) ? num : -1)
                .Where(x => x > 0)
                .ToArray()
                ?? Array.Empty<int>();

            // 优先使用用户脚本获取直播流地址
            if (this.userScriptRunner.CallOnFetchStreamUrl(this.logger, roomid, qns) is { } urlFromScript)
            {
                this.logger.Information("使用用户脚本返回的直播流地址 {Url}", urlFromScript);
                return (urlFromScript, -1);
            }

            const int DefaultQn = 10000;
            var codecItem = await this.apiClient.GetCodecItemInStreamUrlAsync(roomid: roomid, qn: DefaultQn).ConfigureAwait(false)
                ?? throw new Exception("no supported stream url, qn: " + DefaultQn);

            int selected_qn;
            // Select first avaiable qn
            foreach (var qn in qns)
            {
                if (codecItem.AcceptQn.Contains(qn))
                {
                    selected_qn = qn;
                    goto match_qn_success;
                }
            }

            this.logger.Information("没有符合设置要求的画质，稍后再试。设置画质 {QnSettings}, 可用画质 {AcceptQn}", qns, codecItem.AcceptQn);
            throw new NoMatchingQnValueException();

match_qn_success:
            this.logger.Debug("设置画质 {QnSettings}, 可用画质 {AcceptQn}, 最终选择 {SelectedQn}", qns, codecItem.AcceptQn, selected_qn);

            if (selected_qn != DefaultQn)
            {
                // 最终选择的 qn 与默认不同，需要重新请求一次
                codecItem = await this.apiClient.GetCodecItemInStreamUrlAsync(roomid: roomid, qn: selected_qn).ConfigureAwait(false);

                if (codecItem is null)
                    throw new Exception("no supported stream url, qn: " + selected_qn);
            }

            if (codecItem.CurrentQn != selected_qn || !qns.Contains(codecItem.CurrentQn))
                this.logger.Warning("返回的直播流地址的画质是 {CurrentQn} 而不是请求的 {SelectedQn}", codecItem.CurrentQn, selected_qn);

            var url_infos = codecItem.UrlInfos;
            if (url_infos is null || url_infos.Length == 0)
                throw new Exception("no url_info");

            // https:// xy0x0x0x0xy.mcdn.bilivideo.cn:486
            var url_infos_without_mcdn = url_infos.Where(x => !x.Host.Contains(".mcdn.")).ToArray();

            var url_info = url_infos_without_mcdn.Length != 0
                ? url_infos_without_mcdn[this.random.Next(url_infos_without_mcdn.Length)]
                : url_infos[this.random.Next(url_infos.Length)];

            var fullUrl = url_info.Host + codecItem.BaseUrl + url_info.Extra;
            return (fullUrl, codecItem.CurrentQn);
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
    }
}
