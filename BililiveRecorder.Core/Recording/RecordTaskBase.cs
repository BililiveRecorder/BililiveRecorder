using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Event;
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

        protected string? streamHost;
        protected bool started = false;
        protected bool timeoutTriggered = false;

        private readonly object fillerStatsLock = new object();
        protected int fillerDownloadedBytes;
        private DateTimeOffset fillerStatsLastTrigger;
        private TimeSpan durationSinceNoDataReceived;

        protected RecordTaskBase(IRoom room, ILogger logger, IApiClient apiClient)
        {
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

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

            var fullUrl = await this.FetchStreamUrlAsync(this.room.RoomConfig.RoomId).ConfigureAwait(false);

            this.streamHost = new Uri(fullUrl).Host;
            this.logger.Information("连接直播服务器 {Host}", this.streamHost);
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

        #region File Name

        protected (string fullPath, string relativePath) CreateFileName()
        {
            var formatString = this.room.RoomConfig.RecordFilenameFormat!;

            var now = DateTime.Now;
            var date = now.ToString("yyyyMMdd");
            var time = now.ToString("HHmmss");
            var ms = now.ToString("fff");
            var randomStr = this.random.Next(100, 999).ToString();

            var relativePath = formatString
                .Replace(@"{date}", date)
                .Replace(@"{time}", time)
                .Replace(@"{ms}", ms)
                .Replace(@"{random}", randomStr)
                .Replace(@"{roomid}", this.room.RoomConfig.RoomId.ToString())
                .Replace(@"{title}", RemoveInvalidFileName(this.room.Title))
                .Replace(@"{name}", RemoveInvalidFileName(this.room.Name))
                .Replace(@"{parea}", RemoveInvalidFileName(this.room.AreaNameParent))
                .Replace(@"{area}", RemoveInvalidFileName(this.room.AreaNameChild))
                ;

            if (!relativePath.EndsWith(".flv", StringComparison.OrdinalIgnoreCase))
                relativePath += ".flv";

            relativePath = RemoveInvalidFileName(relativePath, ignore_slash: true);
            var workDirectory = this.room.RoomConfig.WorkDirectory;
            var fullPath = Path.Combine(workDirectory, relativePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!CheckIsWithinPath(workDirectory!, Path.GetDirectoryName(fullPath)))
            {
                this.logger.Warning("录制文件位置超出允许范围，请检查设置。将写入到默认路径。");
                relativePath = Path.Combine(this.room.RoomConfig.RoomId.ToString(), $"{this.room.RoomConfig.RoomId}-{date}-{time}-{randomStr}.flv");
                fullPath = Path.Combine(workDirectory, relativePath);
            }

            if (File.Exists(fullPath))
            {
                this.logger.Warning("录制文件名冲突，请检查设置。将写入到默认路径。");
                relativePath = Path.Combine(this.room.RoomConfig.RoomId.ToString(), $"{this.room.RoomConfig.RoomId}-{date}-{time}-{randomStr}.flv");
                fullPath = Path.Combine(workDirectory, relativePath);
            }

            return (fullPath, relativePath);
        }

        internal static string RemoveInvalidFileName(string input, bool ignore_slash = false)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                if (!ignore_slash || c != '\\' && c != '/')
                    input = input.Replace(c, '_');
            return input;
        }

        internal static bool CheckIsWithinPath(string parent, string child)
        {
            if (parent is null || child is null)
                return false;

            parent = parent.Replace('/', '\\');
            if (!parent.EndsWith("\\"))
                parent += "\\";
            parent = Path.GetFullPath(parent);

            child = child.Replace('/', '\\');
            if (!child.EndsWith("\\"))
                child += "\\";
            child = Path.GetFullPath(child);

            return child.StartsWith(parent, StringComparison.Ordinal);
        }

        #endregion

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

        protected async Task<string> FetchStreamUrlAsync(int roomid)
        {
            var apiResp = await this.apiClient.GetStreamUrlAsync(roomid: roomid).ConfigureAwait(false);
            var url_data = apiResp?.Data?.PlayurlInfo?.Playurl?.Streams;

            if (url_data is null)
                throw new Exception("playurl is null");

            var url_http_stream_flv_avc =
                url_data.FirstOrDefault(x => x.ProtocolName == "http_stream")
                ?.Formats?.FirstOrDefault(x => x.FormatName == "flv")
                ?.Codecs?.FirstOrDefault(x => x.CodecName == "avc");

            if (url_http_stream_flv_avc is null)
                throw new Exception("no supported stream url");

            if (url_http_stream_flv_avc.CurrentQn != 10000)
                this.logger.Warning("当前录制的画质是 {CurrentQn}", url_http_stream_flv_avc.CurrentQn);

            var url_infos = url_http_stream_flv_avc.UrlInfos;
            if (url_infos is null || url_infos.Length == 0)
                throw new Exception("no url_info");

            // https:// xy0x0x0x0xy.mcdn.bilivideo.cn:486
            var url_infos_without_mcdn = url_infos.Where(x => !x.Host.Contains(".mcdn.")).ToArray();

            var url_info = url_infos_without_mcdn.Length != 0
                ? url_infos_without_mcdn[this.random.Next(url_infos_without_mcdn.Length)]
                : url_infos[this.random.Next(url_infos.Length)];

            var fullUrl = url_info.Host + url_http_stream_flv_avc.BaseUrl + url_info.Extra;
            return fullUrl;
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
