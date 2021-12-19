using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Templating;
using Serilog;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core.Recording
{
    public abstract class RecordTaskBase : IRecordTask
    {
        private const string HttpHeaderAccept = "*/*";
        private const string HttpHeaderOrigin = "https://live.bilibili.com";
        private const string HttpHeaderReferer = "https://live.bilibili.com/";
        private const string HttpHeaderUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36";

        private const int timer_inverval = 2;
        protected readonly Timer timer = new Timer(1000 * timer_inverval);
        protected readonly Random random = new Random();
        protected readonly CancellationTokenSource cts = new CancellationTokenSource();
        protected readonly CancellationToken ct;

        protected readonly IRoom room;
        protected readonly ILogger logger;
        protected readonly IApiClient apiClient;
        private readonly FileNameGenerator fileNameGenerator;

        protected string? streamHost;
        protected bool started = false;
        protected bool timeoutTriggered = false;

        private readonly object fillerStatsLock = new object();
        protected int fillerDownloadedBytes;
        private DateTimeOffset fillerStatsLastTrigger;
        private TimeSpan durationSinceNoDataReceived;

        protected RecordTaskBase(IRoom room, ILogger logger, IApiClient apiClient, FileNameGenerator fileNameGenerator)
        {
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.fileNameGenerator = fileNameGenerator ?? throw new ArgumentNullException(nameof(fileNameGenerator));
            this.ct = this.cts.Token;

            this.timer.Elapsed += this.Timer_Elapsed_TriggerNetworkStats;
        }

        public Guid SessionId { get; } = Guid.NewGuid();

        #region Events

        public event EventHandler<NetworkingStatsEventArgs>? NetworkingStats;
        public event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        public event EventHandler? RecordSessionEnded;

        protected void OnNetworkingStats(NetworkingStatsEventArgs e) => NetworkingStats?.Invoke(this, e);
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

            this.streamHost = new Uri(fullUrl).Host;
            var qnDesc = qn switch
            {
                20000 => "4K",
                10000 => "原画",
                401 => "蓝光(杜比)",
                400 => "蓝光",
                250 => "超清",
                150 => "高清",
                80 => "流畅",
                _ => "未知"
            };
            this.logger.Information("连接直播服务器 {Host} 录制画质 {Qn} ({QnDescription})", this.streamHost, qn, qnDesc);
            this.logger.Debug("直播流地址 {Url}", fullUrl);

            var stream = await this.GetStreamAsync(fullUrl: fullUrl, timeout: (int)this.room.RoomConfig.TimingStreamConnect).ConfigureAwait(false);

            this.fillerStatsLastTrigger = DateTimeOffset.UtcNow;
            this.durationSinceNoDataReceived = TimeSpan.Zero;

            this.ct.Register(state => Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000);
                    if (((WeakReference<Stream>)state).TryGetTarget(out var weakStream))
                        weakStream.Dispose();
                }
                catch (Exception)
                { }
            }), state: new WeakReference<Stream>(stream), useSynchronizationContext: false);

            this.StartRecordingLoop(stream);
        }

        protected abstract void StartRecordingLoop(Stream stream);

        private void Timer_Elapsed_TriggerNetworkStats(object sender, ElapsedEventArgs e)
        {
            int bytes;
            TimeSpan diff;
            DateTimeOffset start, end;

            lock (this.fillerStatsLock)
            {
                bytes = Interlocked.Exchange(ref this.fillerDownloadedBytes, 0);
                end = DateTimeOffset.UtcNow;
                start = this.fillerStatsLastTrigger;
                this.fillerStatsLastTrigger = end;
                diff = end - start;

                this.durationSinceNoDataReceived = bytes > 0 ? TimeSpan.Zero : this.durationSinceNoDataReceived + diff;
            }

            var mbps = bytes * (8d / 1024d / 1024d) / diff.TotalSeconds;

            this.OnNetworkingStats(new NetworkingStatsEventArgs
            {
                BytesDownloaded = bytes,
                Duration = diff,
                StartTime = start,
                EndTime = end,
                Mbps = mbps
            });

            if ((!this.timeoutTriggered) && (this.durationSinceNoDataReceived.TotalMilliseconds > this.room.RoomConfig.TimingWatchdogTimeout))
            {
                this.timeoutTriggered = true;
                this.logger.Warning("直播服务器未断开连接但停止发送直播数据，将会主动断开连接");
                this.RequestStop();
            }
        }

        protected (string fullPath, string relativePath) CreateFileName() => this.fileNameGenerator.CreateFilePath(new FileNameGenerator.FileNameContextData
        {
            Name = this.room.Name,
            Title = this.room.Title,
            RoomId = this.room.RoomConfig.RoomId,
            ShortId = this.room.ShortId,
            AreaParent = this.room.AreaNameParent,
            AreaChild = this.room.AreaNameChild,
        });

        #region Api Requests

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
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
            const int DefaultQn = 10000;
            var selected_qn = DefaultQn;
            var codecItem = await this.apiClient.GetCodecItemInStreamUrlAsync(roomid: roomid, qn: DefaultQn).ConfigureAwait(false);

            if (codecItem is null)
                throw new Exception("no supported stream url, qn: " + DefaultQn);

            var qns = this.room.RoomConfig.RecordingQuality?.Split(new[] { ',', '，', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var num) ? num : -1)
                .Where(x => x > 0)
                .ToArray()
                ?? Array.Empty<int>();

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
            var client = CreateHttpClient();

            while (true)
            {
                var resp = await client.GetAsync(fullUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    new CancellationTokenSource(timeout).Token)
                    .ConfigureAwait(false);

                switch (resp.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        {
                            this.logger.Information("开始接收直播流");
                            var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            return stream;
                        }
                    case System.Net.HttpStatusCode.Moved:
                    case System.Net.HttpStatusCode.Redirect:
                        {
                            fullUrl = resp.Headers.Location.OriginalString;
                            this.logger.Debug("跳转到 {Url}", fullUrl);
                            resp.Dispose();
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
