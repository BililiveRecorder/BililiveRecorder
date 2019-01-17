using BililiveRecorder.Core.Config;
using BililiveRecorder.FlvProcessor;
using DnsClient;
using NLog;
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

namespace BililiveRecorder.Core
{
    public class RecordedRoom : IRecordedRoom
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Random random = new Random();

        private static readonly LookupClient lookupClient = new LookupClient()
        {
            ThrowDnsErrors = true,
        };

        private int _roomid;
        private int _realRoomid;
        private string _streamerName;

        public int Roomid
        {
            get => _roomid;
            private set
            {
                if (value == _roomid) { return; }
                _roomid = value;
                TriggerPropertyChanged(nameof(Roomid));
            }
        }
        public int RealRoomid
        {
            get => _realRoomid;
            private set
            {
                if (value == _realRoomid) { return; }
                _realRoomid = value;
                TriggerPropertyChanged(nameof(RealRoomid));
            }
        }
        public string StreamerName
        {
            get => _streamerName;
            private set
            {
                if (value == _streamerName) { return; }
                _streamerName = value;
                TriggerPropertyChanged(nameof(StreamerName));
            }
        }

        public bool IsMonitoring => StreamMonitor.IsMonitoring;
        public bool IsRecording => !(StreamDownloadTask?.IsCompleted ?? true);

        private readonly Func<IFlvStreamProcessor> newIFlvStreamProcessor;
        private IFlvStreamProcessor _processor;
        public IFlvStreamProcessor Processor
        {
            get => _processor;
            private set
            {
                if (value == _processor) { return; }
                _processor = value;
                TriggerPropertyChanged(nameof(Processor));
            }
        }

        private ConfigV1 _config { get; }
        public IStreamMonitor StreamMonitor { get; }

        private bool _retry = true;
        private HttpResponseMessage _response;
        private Stream _stream;
        private Task StartupTask = null;
        public Task StreamDownloadTask = null;
        public CancellationTokenSource cancellationTokenSource = null;

        private double _DownloadSpeedPersentage = 0;
        private double _DownloadSpeedKiBps = 0;
        private long _lastUpdateSize = 0;
        private int _lastUpdateTimestamp = 0;
        public DateTime LastUpdateDateTime { get; private set; } = DateTime.Now;
        public double DownloadSpeedPersentage
        {
            get { return _DownloadSpeedPersentage; }
            private set { if (value != _DownloadSpeedPersentage) { _DownloadSpeedPersentage = value; TriggerPropertyChanged(nameof(DownloadSpeedPersentage)); } }
        }
        public double DownloadSpeedKiBps
        {
            get { return _DownloadSpeedKiBps; }
            private set { if (value != _DownloadSpeedKiBps) { _DownloadSpeedKiBps = value; TriggerPropertyChanged(nameof(DownloadSpeedKiBps)); } }
        }

        public RecordedRoom(ConfigV1 config,
            Func<int, IStreamMonitor> newIStreamMonitor,
            Func<IFlvStreamProcessor> newIFlvStreamProcessor,
            int roomid)
        {
            this.newIFlvStreamProcessor = newIFlvStreamProcessor;

            _config = config;

            Roomid = roomid;

            {
                var roomInfo = BililiveAPI.GetRoomInfo(Roomid);
                RealRoomid = roomInfo.RealRoomid;
                StreamerName = roomInfo.Username;
            }

            StreamMonitor = newIStreamMonitor(RealRoomid);
            StreamMonitor.StreamStatusChanged += StreamMonitor_StreamStatusChanged;
        }

        public bool Start()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(RecordedRoom));
            }

            var r = StreamMonitor.Start();
            TriggerPropertyChanged(nameof(IsMonitoring));
            return r;
        }

        public void Stop()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(RecordedRoom));
            }

            StreamMonitor.Stop();
            TriggerPropertyChanged(nameof(IsMonitoring));
        }

        private void StreamMonitor_StreamStatusChanged(object sender, StreamStatusChangedArgs e)
        {
            // if (StartupTask?.IsCompleted ?? true)
            if (!IsRecording && (StartupTask?.IsCompleted ?? true))
            {
                StartupTask = _StartRecordAsync();
            }
        }

        public void StartRecord()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(RecordedRoom));
            }

            StreamMonitor.Check(TriggerType.Manual);
        }

        public void StopRecord()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(RecordedRoom));
            }

            _retry = false;
            try
            {
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    if (!(StreamDownloadTask?.Wait(TimeSpan.FromSeconds(2)) ?? true))
                    {
                        logger.Log(RealRoomid, LogLevel.Warn, "尝试强制关闭连接，请检查网络连接是否稳定");

                        _stream?.Close();
                        _stream?.Dispose();
                        _response?.Dispose();
                        StreamDownloadTask?.Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(RealRoomid, LogLevel.Error, "在尝试停止录制时发生错误，请检查网络连接是否稳定", ex);
            }
            finally
            {
                _retry = true;
            }
        }

        private async Task _StartRecordAsync()
        {
            if (IsRecording)
            {
                logger.Log(RealRoomid, LogLevel.Debug, "已经在录制中了");
                return;
            }

            // HttpWebRequest request = null;

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            try
            {
                using (var client = new HttpClient())
                {
                    var raw_uri = new Uri(BililiveAPI.GetPlayUrl(RealRoomid));

                    client.Timeout = TimeSpan.FromMilliseconds(_config.TimingStreamConnect);

                    client.DefaultRequestHeaders.Host = raw_uri.Host;
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    client.DefaultRequestHeaders.UserAgent.Clear();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(Utils.UserAgent);
                    client.DefaultRequestHeaders.Referrer = new Uri("https://live.bilibili.com");
                    client.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");

                    var ips = lookupClient.Query(raw_uri.DnsSafeHost, QueryType.A).Answers?.ARecords()?.ToArray();
                    var ip = ips[random.Next(0, ips.Count())].Address;

                    logger.Log(RealRoomid, LogLevel.Info, "连接直播服务器 " + raw_uri.Host + " (" + ip + ")");
                    logger.Log(RealRoomid, LogLevel.Debug, "直播流地址: " + raw_uri.ToString());

                    _response = await client.GetAsync(new UriBuilder(raw_uri) { Host = ip.ToString() }.Uri, HttpCompletionOption.ResponseHeadersRead);
                }

                if (_response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Log(RealRoomid, LogLevel.Info, string.Format("尝试下载直播流时服务器返回了 ({0}){1}", _response.StatusCode, _response.ReasonPhrase));

                    StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)_config.TimingStreamRetry);

                    _CleanupFlvRequest();
                    return;
                }
                else
                {
                    Processor = newIFlvStreamProcessor().Initialize(GetStreamFilePath, GetClipFilePath, _config.EnabledFeature, _config.CuttingMode);
                    Processor.ClipLengthFuture = _config.ClipLengthFuture;
                    Processor.ClipLengthPast = _config.ClipLengthPast;
                    Processor.CuttingNumber = _config.CuttingNumber;
                    Processor.OnMetaData += (sender, e) =>
                    {
                        e.Metadata["BililiveRecorder"] = new Dictionary<string, object>()
                        {
                            {
                                "starttime",
                                DateTime.UtcNow
                            },
                            {
                                "version",
                                "TEST"
                            },
                            {
                                "roomid",
                                RealRoomid.ToString()
                            },
                            {
                                "streamername",
                                StreamerName
                            },
                        };
                    };

                    _stream = await _response.Content.ReadAsStreamAsync();
                    _stream.ReadTimeout = 3 * 1000;

                    StreamDownloadTask = Task.Run(_ReadStreamLoop);
                    TriggerPropertyChanged(nameof(IsRecording));
                }
            }
            catch (TaskCanceledException)
            {
                // client.GetAsync timed out
                // useless exception message :/

                _CleanupFlvRequest();
                logger.Log(RealRoomid, LogLevel.Warn, "连接直播服务器超时。");
                StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)_config.TimingStreamRetry);
            }
            catch (Exception ex)
            {
                _CleanupFlvRequest();
                logger.Log(RealRoomid, LogLevel.Warn, "启动直播流下载出错。" + (_retry ? "将重试启动。" : ""), ex);
                if (_retry)
                {
                    StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)_config.TimingStreamRetry);
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
                        int bytesRead = await _stream.ReadAsync(buffer, 0, BUF_SIZE, token);
                        _UpdateDownloadSpeed(bytesRead);
                        if (bytesRead != 0)
                        {
                            if (bytesRead != BUF_SIZE)
                            {
                                Processor.AddBytes(buffer.Take(bytesRead).ToArray());
                            }
                            else
                            {
                                Processor.AddBytes(buffer);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    logger.Log(RealRoomid, LogLevel.Info,
                           (token.IsCancellationRequested ? "用户操作" : "直播已结束") + "，停止录制。"
                           + (_retry ? "将重试启动。" : ""));
                    if (_retry)
                    {
                        StreamMonitor.Check(TriggerType.HttpApiRecheck, (int)_config.TimingStreamRetry);
                    }
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException && token.IsCancellationRequested) { return; }

                    logger.Log(RealRoomid, LogLevel.Warn, "录播发生错误", e);
                }
                finally
                {
                    _CleanupFlvRequest();
                }
            }
            void _CleanupFlvRequest()
            {
                if (Processor != null)
                {
                    Processor.FinallizeFile();
                    Processor.Dispose();
                    Processor = null;
                }
                _stream?.Dispose();
                _stream = null;
                _response?.Dispose();
                _response = null;

                _lastUpdateTimestamp = 0;
                DownloadSpeedKiBps = 0d;
                DownloadSpeedPersentage = 0d;
                TriggerPropertyChanged(nameof(IsRecording));
            }
            void _UpdateDownloadSpeed(int bytesRead)
            {
                DateTime now = DateTime.Now;
                double passedSeconds = (now - LastUpdateDateTime).TotalSeconds;
                _lastUpdateSize += bytesRead;
                if (passedSeconds > 1.5)
                {
                    DownloadSpeedKiBps = _lastUpdateSize / passedSeconds / 1024; // KiB per sec
                    DownloadSpeedPersentage = (DownloadSpeedPersentage / 2) + ((Processor.TotalMaxTimestamp - _lastUpdateTimestamp) / passedSeconds / 1000 / 2); // ((RecordedTime/1000) / RealTime)%
                    _lastUpdateTimestamp = Processor.TotalMaxTimestamp;
                    _lastUpdateSize = 0;
                    LastUpdateDateTime = now;
                }
            }
        }

        // Called by API or GUI
        public void Clip()
        {
            Processor?.Clip();
        }

        public void Shutdown()
        {
            Dispose(true);
        }

        private string GetStreamFilePath() => Path.Combine(_config.WorkDirectory, RealRoomid.ToString(), "record",
            $@"record-{RealRoomid}-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{random.Next(100, 999)}.flv".RemoveInvalidFileName());

        private string GetClipFilePath() => Path.Combine(_config.WorkDirectory, RealRoomid.ToString(), "clip",
            $@"clip-{RealRoomid}-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{random.Next(100, 999)}.flv".RemoveInvalidFileName());

        public event PropertyChangedEventHandler PropertyChanged;
        protected void TriggerPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                    StopRecord();
                    Processor?.Dispose();
                    StreamMonitor?.Dispose();
                    _response?.Dispose();
                    _stream?.Dispose();
                    cancellationTokenSource?.Dispose();
                }

                Processor = null;
                _response = null;
                _stream = null;
                cancellationTokenSource = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
        }
        #endregion
    }
}
