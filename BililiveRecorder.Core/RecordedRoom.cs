using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Callback;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.FlvProcessor;
using NLog;

namespace BililiveRecorder.Core
{
    public class RecordedRoom : IRecordedRoom
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Random random = new Random();
        private static readonly Version VERSION_1_0 = new Version(1, 0);

        private int _shortRoomid;
        private string _streamerName;
        private string _title;
        private bool _isStreaming;

        public int ShortRoomId
        {
            get => this._shortRoomid;
            private set
            {
                if (value == this._shortRoomid) { return; }
                this._shortRoomid = value;
                this.TriggerPropertyChanged(nameof(this.ShortRoomId));
            }
        }
        public int RoomId
        {
            get => this.RoomConfig.RoomId;
            private set
            {
                if (value == this.RoomConfig.RoomId) { return; }
                this.RoomConfig.RoomId = value;
                this.TriggerPropertyChanged(nameof(this.RoomId));
            }
        }
        public string StreamerName
        {
            get => this._streamerName;
            private set
            {
                if (value == this._streamerName) { return; }
                this._streamerName = value;
                this.TriggerPropertyChanged(nameof(this.StreamerName));
            }
        }
        public string Title
        {
            get => this._title;
            private set
            {
                if (value == this._title) { return; }
                this._title = value;
                this.TriggerPropertyChanged(nameof(this.Title));
            }
        }

        public bool IsMonitoring => this.StreamMonitor.IsMonitoring;
        public bool IsRecording => !(this.StreamDownloadTask?.IsCompleted ?? true);
        public bool IsDanmakuConnected => this.StreamMonitor.IsDanmakuConnected;
        public bool IsStreaming
        {
            get => this._isStreaming;
            private set
            {
                if (value == this._isStreaming) { return; }
                this._isStreaming = value;
                this.TriggerPropertyChanged(nameof(this.IsStreaming));
            }
        }

        public RoomConfig RoomConfig { get; }

        private RecordEndData recordEndData;
        public event EventHandler<RecordEndData> RecordEnded;

        private readonly IBasicDanmakuWriter basicDanmakuWriter;
        private readonly Func<IFlvStreamProcessor> newIFlvStreamProcessor;
        private IFlvStreamProcessor _processor;
        public IFlvStreamProcessor Processor
        {
            get => this._processor;
            private set
            {
                if (value == this._processor) { return; }
                this._processor = value;
                this.TriggerPropertyChanged(nameof(this.Processor));
            }
        }

        private BililiveAPI BililiveAPI { get; }
        public IStreamMonitor StreamMonitor { get; }

        private bool _retry = true;
        private HttpResponseMessage _response;
        private Stream _stream;
        private Task StartupTask = null;
        private readonly object StartupTaskLock = new object();
        public Task StreamDownloadTask = null;
        public CancellationTokenSource cancellationTokenSource = null;

        private double _DownloadSpeedPersentage = 0;
        private double _DownloadSpeedMegaBitps = 0;
        private long _lastUpdateSize = 0;
        private int _lastUpdateTimestamp = 0;
        public DateTime LastUpdateDateTime { get; private set; } = DateTime.Now;
        public double DownloadSpeedPersentage
        {
            get { return this._DownloadSpeedPersentage; }
            private set { if (value != this._DownloadSpeedPersentage) { this._DownloadSpeedPersentage = value; this.TriggerPropertyChanged(nameof(this.DownloadSpeedPersentage)); } }
        }
        public double DownloadSpeedMegaBitps
        {
            get { return this._DownloadSpeedMegaBitps; }
            private set { if (value != this._DownloadSpeedMegaBitps) { this._DownloadSpeedMegaBitps = value; this.TriggerPropertyChanged(nameof(this.DownloadSpeedMegaBitps)); } }
        }

        public Guid Guid { get; } = Guid.NewGuid();

        // TODO: 重构 DI
        public RecordedRoom(Func<RoomConfig, IBasicDanmakuWriter> newBasicDanmakuWriter,
            Func<RoomConfig, IStreamMonitor> newIStreamMonitor,
            Func<IFlvStreamProcessor> newIFlvStreamProcessor,
            BililiveAPI bililiveAPI,
            RoomConfig roomConfig)
        {
            this.RoomConfig = roomConfig;
            this.StreamerName = "获取中...";

            this.BililiveAPI = bililiveAPI;

            this.newIFlvStreamProcessor = newIFlvStreamProcessor;

            this.basicDanmakuWriter = newBasicDanmakuWriter(this.RoomConfig);

            this.StreamMonitor = newIStreamMonitor(this.RoomConfig);
            this.StreamMonitor.RoomInfoUpdated += this.StreamMonitor_RoomInfoUpdated;
            this.StreamMonitor.StreamStarted += this.StreamMonitor_StreamStarted;
            this.StreamMonitor.ReceivedDanmaku += this.StreamMonitor_ReceivedDanmaku;
            this.StreamMonitor.PropertyChanged += this.StreamMonitor_PropertyChanged;

            this.PropertyChanged += this.RecordedRoom_PropertyChanged;

            this.StreamMonitor.FetchRoomInfoAsync();

            if (this.RoomConfig.AutoRecord)
                this.Start();
        }

        private void RecordedRoom_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(this.IsMonitoring):
                    this.RoomConfig.AutoRecord = this.IsMonitoring;
                    break;
                default:
                    break;
            }
        }

        private void StreamMonitor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IStreamMonitor.IsDanmakuConnected):
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsDanmakuConnected)));
                    break;
                default:
                    break;
            }
        }

        private void StreamMonitor_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            switch (e.Danmaku.MsgType)
            {
                case MsgTypeEnum.LiveStart:
                    this.IsStreaming = true;
                    break;
                case MsgTypeEnum.LiveEnd:
                    this.IsStreaming = false;
                    break;
                default:
                    break;
            }
            this.basicDanmakuWriter.Write(e.Danmaku);
        }

        private void StreamMonitor_RoomInfoUpdated(object sender, RoomInfoUpdatedArgs e)
        {
            // TODO: StreamMonitor 里的 RoomInfoUpdated Handler 也会设置一次 RoomId
            // 暂时保持不变，此处的 RoomId 需要触发 PropertyChanged 事件
            this.RoomId = e.RoomInfo.RoomId;
            this.ShortRoomId = e.RoomInfo.ShortRoomId;
            this.StreamerName = e.RoomInfo.UserName;
            this.Title = e.RoomInfo.Title;
            this.IsStreaming = e.RoomInfo.IsStreaming;
        }

        public bool Start()
        {
            // TODO: 重构: 删除 Start() Stop() 通过 RoomConfig.AutoRecord 控制监控状态和逻辑
            if (this.disposedValue) throw new ObjectDisposedException(nameof(RecordedRoom));

            var r = this.StreamMonitor.Start();
            this.TriggerPropertyChanged(nameof(this.IsMonitoring));
            return r;
        }

        public void Stop()
        {
            // TODO: 见 Start()
            if (this.disposedValue) throw new ObjectDisposedException(nameof(RecordedRoom));

            this.StreamMonitor.Stop();
            this.TriggerPropertyChanged(nameof(this.IsMonitoring));
        }

        public void RefreshRoomInfo()
        {
            if (this.disposedValue) throw new ObjectDisposedException(nameof(RecordedRoom));
            this.StreamMonitor.FetchRoomInfoAsync();
        }

        private void StreamMonitor_StreamStarted(object sender, StreamStartedArgs e)
        {
            lock (this.StartupTaskLock)
                if (!this.IsRecording && (this.StartupTask?.IsCompleted ?? true))
                    this.StartupTask = this._StartRecordAsync();
        }

        public void StartRecord()
        {
            if (this.disposedValue) throw new ObjectDisposedException(nameof(RecordedRoom));
            this.StreamMonitor.Check(TriggerType.Manual);
        }

        public void StopRecord()
        {
            if (this.disposedValue) throw new ObjectDisposedException(nameof(RecordedRoom));

            this._retry = false;
            try
            {
                if (this.cancellationTokenSource != null)
                {
                    this.cancellationTokenSource.Cancel();
                    if (!(this.StreamDownloadTask?.Wait(TimeSpan.FromSeconds(2)) ?? true))
                    {
                        logger.Log(this.RoomId, LogLevel.Warn, "停止录制超时，尝试强制关闭连接，请检查网络连接是否稳定");

                        this._stream?.Close();
                        this._stream?.Dispose();
                        this._response?.Dispose();
                        this.StreamDownloadTask?.Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(this.RoomId, LogLevel.Warn, "在尝试停止录制时发生错误，请检查网络连接是否稳定", ex);
            }
            finally
            {
                this._retry = true;
            }
        }

        private async Task _StartRecordAsync()
        {
            if (this.IsRecording)
            {
                // TODO: 这里逻辑可能有问题，StartupTask 会变成当前这个已经结束的
                logger.Log(this.RoomId, LogLevel.Warn, "已经在录制中了");
                return;
            }

            this.cancellationTokenSource = new CancellationTokenSource();
            var token = this.cancellationTokenSource.Token;
            try
            {
                var flv_path = await this.BililiveAPI.GetPlayUrlAsync(this.RoomId);
                if (string.IsNullOrWhiteSpace(flv_path))
                {
                    if (this._retry)
                    {
                        this.StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)this.RoomConfig.TimingStreamRetry);
                    }
                    return;
                }

            unwrap_redir:

                using (var client = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                }))
                {

                    client.Timeout = TimeSpan.FromMilliseconds(this.RoomConfig.TimingStreamConnect);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    client.DefaultRequestHeaders.UserAgent.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(Utils.UserAgent);
                    client.DefaultRequestHeaders.Referrer = new Uri("https://live.bilibili.com");
                    client.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");


                    logger.Log(this.RoomId, LogLevel.Info, "连接直播服务器 " + new Uri(flv_path).Host);
                    logger.Log(this.RoomId, LogLevel.Debug, "直播流地址: " + flv_path);

                    this._response = await client.GetAsync(flv_path, HttpCompletionOption.ResponseHeadersRead);
                }

                if (this._response.StatusCode == HttpStatusCode.Redirect || this._response.StatusCode == HttpStatusCode.Moved)
                {
                    // workaround for missing Referrer
                    flv_path = this._response.Headers.Location.OriginalString;
                    this._response.Dispose();
                    goto unwrap_redir;
                }
                else if (this._response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Log(this.RoomId, LogLevel.Info, string.Format("尝试下载直播流时服务器返回了 ({0}){1}", this._response.StatusCode, this._response.ReasonPhrase));

                    this.StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)this.RoomConfig.TimingStreamRetry);

                    _CleanupFlvRequest();
                    return;
                }
                else
                {
                    this.Processor = this.newIFlvStreamProcessor().Initialize(this.GetStreamFilePath, this.GetClipFilePath, this.RoomConfig.EnabledFeature, this.RoomConfig.CuttingMode);
                    this.Processor.ClipLengthFuture = this.RoomConfig.ClipLengthFuture;
                    this.Processor.ClipLengthPast = this.RoomConfig.ClipLengthPast;
                    this.Processor.CuttingNumber = this.RoomConfig.CuttingNumber;
                    this.Processor.StreamFinalized += (sender, e) => { this.basicDanmakuWriter.Disable(); };
                    this.Processor.FileFinalized += (sender, size) =>
                    {
                        if (this.recordEndData is null) return;
                        var data = this.recordEndData;
                        this.recordEndData = null;

                        data.EndRecordTime = DateTimeOffset.Now;
                        data.FileSize = size;
                        RecordEnded?.Invoke(this, data);
                    };
                    this.Processor.OnMetaData += (sender, e) =>
                    {
                        e.Metadata["BililiveRecorder"] = new Dictionary<string, object>()
                        {
                            {
                                "starttime",
                                DateTime.UtcNow
                            },
                            {
                                "version",
                                BuildInfo.Version + " " + BuildInfo.HeadShaShort
                            },
                            {
                                "roomid",
                                this.RoomId.ToString()
                            },
                            {
                                "streamername",
                                this.StreamerName
                            },
                        };
                    };

                    this._stream = await this._response.Content.ReadAsStreamAsync();

                    try
                    {
                        if (this._response.Headers.ConnectionClose == false || (this._response.Headers.ConnectionClose is null && this._response.Version != VERSION_1_0))
                            this._stream.ReadTimeout = 3 * 1000;
                    }
                    catch (InvalidOperationException) { }

                    this.StreamDownloadTask = Task.Run(_ReadStreamLoop);
                    this.TriggerPropertyChanged(nameof(this.IsRecording));
                }
            }
            catch (TaskCanceledException)
            {
                // client.GetAsync timed out
                // useless exception message :/

                _CleanupFlvRequest();
                logger.Log(this.RoomId, LogLevel.Warn, "连接直播服务器超时。");
                this.StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)this.RoomConfig.TimingStreamRetry);
            }
            catch (Exception ex)
            {
                _CleanupFlvRequest();
                logger.Log(this.RoomId, LogLevel.Error, "启动直播流下载出错。" + (this._retry ? "将重试启动。" : ""), ex);
                if (this._retry)
                {
                    this.StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)this.RoomConfig.TimingStreamRetry);
                }
            }
            return;

            async Task _ReadStreamLoop()
            {
                try
                {
                    const int BUF_SIZE = 1024 * 8;
                    byte[] buffer = new byte[BUF_SIZE];
                    while (!token.IsCancellationRequested)
                    {
                        int bytesRead = await this._stream.ReadAsync(buffer, 0, BUF_SIZE, token);
                        _UpdateDownloadSpeed(bytesRead);
                        if (bytesRead != 0)
                        {
                            if (bytesRead != BUF_SIZE)
                            {
                                this.Processor.AddBytes(buffer.Take(bytesRead).ToArray());
                            }
                            else
                            {
                                this.Processor.AddBytes(buffer);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    logger.Log(this.RoomId, LogLevel.Info,
                        (token.IsCancellationRequested ? "本地操作结束当前录制。" : "服务器关闭直播流，可能是直播已结束。")
                        + (this._retry ? "将重试启动。" : ""));
                    if (this._retry)
                    {
                        this.StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)this.RoomConfig.TimingStreamRetry);
                    }
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException && token.IsCancellationRequested) { return; }

                    logger.Log(this.RoomId, LogLevel.Warn, "录播发生错误", e);
                }
                finally
                {
                    _CleanupFlvRequest();
                }
            }
            void _CleanupFlvRequest()
            {
                if (this.Processor != null)
                {
                    this.Processor.FinallizeFile();
                    this.Processor.Dispose();
                    this.Processor = null;
                }
                this._stream?.Dispose();
                this._stream = null;
                this._response?.Dispose();
                this._response = null;

                this._lastUpdateTimestamp = 0;
                this.DownloadSpeedMegaBitps = 0d;
                this.DownloadSpeedPersentage = 0d;
                this.TriggerPropertyChanged(nameof(this.IsRecording));
            }
            void _UpdateDownloadSpeed(int bytesRead)
            {
                DateTime now = DateTime.Now;
                double passedSeconds = (now - this.LastUpdateDateTime).TotalSeconds;
                this._lastUpdateSize += bytesRead;
                if (passedSeconds > 1.5)
                {
                    this.DownloadSpeedMegaBitps = this._lastUpdateSize / passedSeconds * 8d / 1_000_000d; // mega bit per second
                    this.DownloadSpeedPersentage = (this.DownloadSpeedPersentage / 2) + ((this.Processor.TotalMaxTimestamp - this._lastUpdateTimestamp) / passedSeconds / 1000 / 2); // ((RecordedTime/1000) / RealTime)%
                    this._lastUpdateTimestamp = this.Processor.TotalMaxTimestamp;
                    this._lastUpdateSize = 0;
                    this.LastUpdateDateTime = now;
                }
            }
        }

        // Called by API or GUI
        public void Clip() => this.Processor?.Clip();

        public void Shutdown() => this.Dispose(true);

        private (string fullPath, string relativePath) GetStreamFilePath()
        {
            var path = this.FormatFilename(this.RoomConfig.RecordFilenameFormat);

            // 有点脏的写法，不过凑合吧
            if (this.RoomConfig.RecordDanmaku)
            {
                var xmlpath = Path.ChangeExtension(path.fullPath, "xml");
                this.basicDanmakuWriter.EnableWithPath(xmlpath, this);
            }

            this.recordEndData = new RecordEndData
            {
                RoomId = RoomId,
                Title = Title,
                Name = StreamerName,
                StartRecordTime = DateTimeOffset.Now,
                RelativePath = path.relativePath,
            };

            return path;
        }

        private string GetClipFilePath() => this.FormatFilename(this.RoomConfig.ClipFilenameFormat).fullPath;

        private (string fullPath, string relativePath) FormatFilename(string formatString)
        {
            DateTime now = DateTime.Now;
            string date = now.ToString("yyyyMMdd");
            string time = now.ToString("HHmmss");
            string randomStr = random.Next(100, 999).ToString();

            var relativePath = formatString
                .Replace(@"{date}", date)
                .Replace(@"{time}", time)
                .Replace(@"{random}", randomStr)
                .Replace(@"{roomid}", this.RoomId.ToString())
                .Replace(@"{title}", this.Title.RemoveInvalidFileName())
                .Replace(@"{name}", this.StreamerName.RemoveInvalidFileName());

            if (!relativePath.EndsWith(".flv", StringComparison.OrdinalIgnoreCase))
                relativePath += ".flv";

            relativePath = relativePath.RemoveInvalidFileName(ignore_slash: true);
            var workDirectory = this.RoomConfig.WorkDirectory;
            var fullPath = Path.Combine(workDirectory, relativePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!CheckPath(workDirectory, Path.GetDirectoryName(fullPath)))
            {
                logger.Log(this.RoomId, LogLevel.Warn, "录制文件位置超出允许范围，请检查设置。将写入到默认路径。");
                relativePath = Path.Combine(this.RoomId.ToString(), $"{this.RoomId}-{date}-{time}-{randomStr}.flv");
                fullPath = Path.Combine(workDirectory, relativePath);
            }

            if (new FileInfo(relativePath).Exists)
            {
                logger.Log(this.RoomId, LogLevel.Warn, "录制文件名冲突，请检查设置。将写入到默认路径。");
                relativePath = Path.Combine(this.RoomId.ToString(), $"{this.RoomId}-{date}-{time}-{randomStr}.flv");
                fullPath = Path.Combine(workDirectory, relativePath);
            }

            return (fullPath, relativePath);
        }

        private static bool CheckPath(string parent, string child)
        {
            DirectoryInfo di_p = new DirectoryInfo(parent);
            DirectoryInfo di_c = new DirectoryInfo(child);

            if (di_c.FullName == di_p.FullName)
                return true;

            bool isParent = false;
            while (di_c.Parent != null)
            {
                if (di_c.Parent.FullName == di_p.FullName)
                {
                    isParent = true;
                    break;
                }
                else
                    di_c = di_c.Parent;
            }
            return isParent;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void TriggerPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.Stop();
                    this.StopRecord();
                    this.Processor?.FinallizeFile();
                    this.Processor?.Dispose();
                    this.StreamMonitor?.Dispose();
                    this._response?.Dispose();
                    this._stream?.Dispose();
                    this.cancellationTokenSource?.Dispose();
                    this.basicDanmakuWriter?.Dispose();
                }

                this.Processor = null;
                this._response = null;
                this._stream = null;
                this.cancellationTokenSource = null;

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            this.Dispose(true);
        }
        #endregion
    }
}
