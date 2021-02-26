using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.ProcessingRules;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;
using Serilog;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core.Recording
{
    public class RecordTask : IRecordTask
    {
        private const string HttpHeaderAccept = "*/*";
        private const string HttpHeaderOrigin = "https://live.bilibili.com";
        private const string HttpHeaderReferer = "https://live.bilibili.com/";
        private const string HttpHeaderUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36";

        private readonly Random random = new Random();
        private readonly Timer timer = new Timer(1000 * 2);
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly CancellationToken ct;

        private readonly IRoom room;
        private readonly ILogger logger;
        private readonly IApiClient apiClient;
        private readonly IFlvTagReaderFactory flvTagReaderFactory;
        private readonly ITagGroupReaderFactory tagGroupReaderFactory;
        private readonly IFlvProcessingContextWriterFactory writerFactory;
        private readonly ProcessingDelegate pipeline;

        private readonly IFlvWriterTargetProvider targetProvider;
        private readonly StatsRule statsRule;
        private readonly SplitRule splitFileRule;

        private readonly FlvProcessingContext context = new FlvProcessingContext();
        private readonly IDictionary<object, object?> session = new Dictionary<object, object?>();

        private bool started = false;
        private Task? filler;
        private ITagGroupReader? reader;
        private IFlvProcessingContextWriter? writer;

        private readonly object fillerStatsLock = new object();
        private int fillerDownloadedBytes;
        private DateTimeOffset fillerLastStatsTrigger;

        public RecordTask(IRoom room,
                          ILogger logger,
                          IProcessingPipelineBuilder builder,
                          IApiClient apiClient,
                          IFlvTagReaderFactory flvTagReaderFactory,
                          ITagGroupReaderFactory tagGroupReaderFactory,
                          IFlvProcessingContextWriterFactory writerFactory)
        {
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.logger = logger?.ForContext<RecordTask>().ForContext(LoggingContext.RoomId, this.room.RoomConfig.RoomId) ?? throw new ArgumentNullException(nameof(logger));
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.flvTagReaderFactory = flvTagReaderFactory ?? throw new ArgumentNullException(nameof(flvTagReaderFactory));
            this.tagGroupReaderFactory = tagGroupReaderFactory ?? throw new ArgumentNullException(nameof(tagGroupReaderFactory));
            this.writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            this.ct = this.cts.Token;

            this.statsRule = new StatsRule();
            this.splitFileRule = new SplitRule();

            this.statsRule.StatsUpdated += this.StatsRule_StatsUpdated;

            this.pipeline = builder
                .Add(this.splitFileRule)
                .Add(this.statsRule)
                .AddDefault()
                .AddRemoveFillerData()
                .Build();

            this.targetProvider = new WriterTargetProvider(this.room, this.logger.ForContext(LoggingContext.RoomId, this.room.RoomConfig.RoomId), paths =>
            {
                this.logger.ForContext(LoggingContext.RoomId, this.room.RoomConfig.RoomId).Debug("输出路径 {Path}", paths.fullPath);

                var e = new RecordFileOpeningEventArgs(this.room)
                {
                    SessionId = this.SessionId,
                    FullPath = paths.fullPath,
                    RelativePath = paths.relativePath,
                    FileOpenTime = DateTimeOffset.Now,
                };
                RecordFileOpening?.Invoke(this, e);
                return e;
            });

            this.timer.Elapsed += this.Timer_Elapsed_TriggerStats;
        }

        public Guid SessionId { get; } = Guid.NewGuid();

        public event EventHandler<NetworkingStatsEventArgs>? NetworkingStats;
        public event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        public event EventHandler? RecordSessionEnded;

        public void SplitOutput() => this.splitFileRule.SetSplitFlag();

        public void RequestStop() => this.cts.Cancel();

        public async Task StartAsync()
        {
            if (this.started)
                throw new InvalidOperationException("Only one StartAsync call allowed per instance.");
            this.started = true;

            var fullUrl = await this.FetchStreamUrlAsync().ConfigureAwait(false);

            this.logger.Information("连接直播服务器 {Host}", new Uri(fullUrl).Host);
            this.logger.Debug("直播流地址 {Url}", fullUrl);

            var stream = await this.GetStreamAsync(fullUrl).ConfigureAwait(false);

            var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            this.reader = this.tagGroupReaderFactory.CreateTagGroupReader(this.flvTagReaderFactory.CreateFlvTagReader(pipe.Reader));

            this.writer = this.writerFactory.CreateWriter(this.targetProvider);
            this.writer.BeforeScriptTagWrite = this.Writer_BeforeScriptTagWrite;
            this.writer.FileClosed += (sender, e) =>
            {
                var openingEventArgs = (RecordFileOpeningEventArgs)e.State!;
                RecordFileClosed?.Invoke(this, new RecordFileClosedEventArgs(this.room)
                {
                    SessionId = this.SessionId,
                    FullPath = openingEventArgs.FullPath,
                    RelativePath = openingEventArgs.RelativePath,
                    FileOpenTime = openingEventArgs.FileOpenTime,
                    FileCloseTime = DateTimeOffset.Now,
                    Duration = e.Duration,
                    FileSize = e.FileSize,
                });
            };

            this.fillerLastStatsTrigger = DateTimeOffset.UtcNow;
            this.filler = Task.Run(async () => await this.FillPipeAsync(stream, pipe.Writer).ConfigureAwait(false));

            _ = Task.Run(this.RecordingLoopAsync);
        }

        private async Task FillPipeAsync(Stream stream, PipeWriter writer)
        {
            const int minimumBufferSize = 1024;
            this.timer.Start();
            Exception? exception = null;
            try
            {
                while (!this.ct.IsCancellationRequested)
                {
                    var memory = writer.GetMemory(minimumBufferSize);
                    try
                    {
                        var bytesRead = await stream.ReadAsync(memory, this.ct).ConfigureAwait(false);
                        if (bytesRead == 0)
                            break;
                        writer.Advance(bytesRead);
                        Interlocked.Add(ref this.fillerDownloadedBytes, bytesRead);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        break;
                    }

                    var result = await writer.FlushAsync(this.ct).ConfigureAwait(false);
                    if (result.IsCompleted)
                        break;
                }
            }
            finally
            {
                this.timer.Stop();
                stream.Dispose();
                await writer.CompleteAsync(exception).ConfigureAwait(false);
            }
        }

        private void Timer_Elapsed_TriggerStats(object sender, ElapsedEventArgs e)
        {
            int bytes;
            TimeSpan diff;
            DateTimeOffset start, end;

            lock (this.fillerStatsLock)
            {
                bytes = Interlocked.Exchange(ref this.fillerDownloadedBytes, 0);
                end = DateTimeOffset.UtcNow;
                start = this.fillerLastStatsTrigger;
                this.fillerLastStatsTrigger = end;
                diff = end - start;
            }

            var mbps = bytes * 8d / 1024d / 1024d / diff.TotalSeconds;

            NetworkingStats?.Invoke(this, new NetworkingStatsEventArgs
            {
                BytesDownloaded = bytes,
                Duration = diff,
                StartTime = start,
                EndTime = end,
                Mbps = mbps
            });
        }

        private void Writer_BeforeScriptTagWrite(ScriptTagBody scriptTagBody)
        {
            if (scriptTagBody.Values.Count == 2 && scriptTagBody.Values[1] is ScriptDataEcmaArray value)
            {
                var now = DateTimeOffset.Now;
                value["Title"] = (ScriptDataString)this.room.Title;
                value["Artist"] = (ScriptDataString)$"{this.room.Name} ({this.room.RoomConfig.RoomId})";
                value["Comment"] = (ScriptDataString)
                    ($"B站直播间 {this.room.RoomConfig.RoomId} 的直播录像\n" +
                    $"主播名: {this.room.Name}\n" +
                    $"直播标题: {this.room.Title}\n" +
                    $"直播分区: {this.room.AreaNameParent}·{this.room.AreaNameChild}\n" +
                    $"录制时间: {now:O}\n" +
                    $"\n" +
                    $"使用 B站录播姬 录制 https://rec.danmuji.org\n" +
                    $"录播姬版本: {GitVersionInformation.FullSemVer}");
                value["BililiveRecorder"] = new ScriptDataEcmaArray
                {
                    ["RecordedBy"] = (ScriptDataString)"BililiveRecorder B站录播姬",
                    ["RecorderVersion"] = (ScriptDataString)GitVersionInformation.FullSemVer,
                    ["StartTime"] = (ScriptDataDate)now,
                    ["RoomId"] = (ScriptDataString)this.room.RoomConfig.RoomId.ToString(),
                    ["ShortId"] = (ScriptDataString)this.room.ShortId.ToString(),
                    ["Name"] = (ScriptDataString)this.room.Name,
                    ["StreamTitle"] = (ScriptDataString)this.room.Title,
                    ["AreaNameParent"] = (ScriptDataString)this.room.AreaNameParent,
                    ["AreaNameChild"] = (ScriptDataString)this.room.AreaNameChild,
                };
            }
        }

        private async Task RecordingLoopAsync()
        {
            if (this.reader is null) return;
            if (this.writer is null) return;
            try
            {
                while (!this.ct.IsCancellationRequested)
                {
                    var group = await this.reader.ReadGroupAsync(this.ct).ConfigureAwait(false);

                    if (group is null)
                        break;

                    this.context.Reset(group, this.session);

                    await this.pipeline(this.context).ConfigureAwait(false);

                    if (this.context.Comments.Count > 0)
                        this.logger.Debug("修复逻辑输出 {Comments}", string.Join("\n", this.context.Comments));

                    await this.writer.WriteAsync(this.context).ConfigureAwait(false);

                    if (this.context.Output.Any(x => x is PipelineDisconnectAction))
                    {
                        this.logger.Information("根据修复逻辑的要求结束录制");
                        break;
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                this.logger.Debug(ex, "录制被取消");
            }
            catch (IOException ex)
            {
                this.logger.Warning(ex, "录制时发生IO错误");
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "录制时发生未知错误");
            }
            finally
            {
                this.logger.Debug("录制退出");

                this.reader?.Dispose();
                this.reader = null;
                this.writer?.Dispose();
                this.writer = null;
                this.cts.Cancel();

                RecordSessionEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task<Stream> GetStreamAsync(string fullUrl)
        {
            var client = CreateHttpClient();

            while (true)
            {
                var resp = await client.GetAsync(fullUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    new CancellationTokenSource((int)this.room.RoomConfig.TimingStreamConnect).Token)
                    .ConfigureAwait(false);

                switch (resp.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        {
                            this.logger.Debug("开始接收直播流");
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

        private async Task<string> FetchStreamUrlAsync()
        {
            var apiResp = await this.apiClient.GetStreamUrlAsync(this.room.RoomConfig.RoomId).ConfigureAwait(false);
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

            var url_info = url_infos[this.random.Next(url_infos.Length)];

            var fullUrl = url_info.Host + url_http_stream_flv_avc.BaseUrl + url_info.Extra;
            return fullUrl;
        }

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

        private void StatsRule_StatsUpdated(object sender, RecordingStatsEventArgs e)
        {
            if (this.room.RoomConfig.CuttingMode == Config.V2.CuttingMode.ByTime)
            {
                if (e.FileMaxTimestamp > (this.room.RoomConfig.CuttingNumber * (60 * 1000)))
                {
                    this.splitFileRule.SetSplitFlag();
                }
            }

            RecordingStats?.Invoke(this, e);
        }

        internal class WriterTargetProvider : IFlvWriterTargetProvider
        {
            private static readonly Random random = new Random();

            private readonly IRoom room;
            private readonly ILogger logger;
            private readonly Func<(string fullPath, string relativePath), object> OnNewFile;

            private string last_path = string.Empty;

            public WriterTargetProvider(IRoom room, ILogger logger, Func<(string fullPath, string relativePath), object> onNewFile)
            {
                this.room = room ?? throw new ArgumentNullException(nameof(room));
                this.logger = logger?.ForContext<WriterTargetProvider>() ?? throw new ArgumentNullException(nameof(logger));
                this.OnNewFile = onNewFile ?? throw new ArgumentNullException(nameof(onNewFile));
            }

            public bool ShouldCreateNewFile(Stream outputStream, IList<Tag> tags)
            {
                if (this.room.RoomConfig.CuttingMode == Config.V2.CuttingMode.BySize)
                {
                    var pendingSize = tags.Sum(x => (x.Nalus == null ? x.Size : (5 + x.Nalus.Sum(n => n.FullSize + 4))) + (11 + 4));
                    return (outputStream.Length + pendingSize) > (this.room.RoomConfig.CuttingNumber * (1024 * 1024));
                }
                return false;
            }

            public (Stream stream, object state) CreateOutputStream()
            {
                var paths = this.FormatFilename(this.room.RoomConfig.RecordFilenameFormat!);

                try
                { Directory.CreateDirectory(Path.GetDirectoryName(paths.fullPath)); }
                catch (Exception) { }

                this.last_path = paths.fullPath;
                var state = this.OnNewFile(paths);

                var stream = new FileStream(paths.fullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
                return (stream, state);
            }

            public Stream CreateAlternativeHeaderStream()
            {
                var path = string.IsNullOrWhiteSpace(this.last_path)
                    ? Path.ChangeExtension(this.FormatFilename(this.room.RoomConfig.RecordFilenameFormat!).fullPath, "headers.txt")
                    : Path.ChangeExtension(this.last_path, "headers.txt");

                try
                { Directory.CreateDirectory(Path.GetDirectoryName(path)); }
                catch (Exception) { }

                var stream = new FileStream(path, FileMode.Append, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                return stream;
            }

            private (string fullPath, string relativePath) FormatFilename(string formatString)
            {
                var now = DateTime.Now;
                var date = now.ToString("yyyyMMdd");
                var time = now.ToString("HHmmss");
                var randomStr = random.Next(100, 999).ToString();

                var relativePath = formatString
                    .Replace(@"{date}", date)
                    .Replace(@"{time}", time)
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
        }
    }
}
