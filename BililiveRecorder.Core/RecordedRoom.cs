using BililiveRecorder.Core.Config;
using BililiveRecorder.FlvProcessor;
using NLog;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public class RecordedRoom : IRecordedRoom
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Random random = new Random();

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

        public bool IsMonitoring => StreamMonitor.Receiver.IsConnected;
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

        private HttpWebResponse _response;
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
            var r = StreamMonitor.Start();
            TriggerPropertyChanged(nameof(IsMonitoring));
            return r;
        }

        public void Stop()
        {
            StreamMonitor.Stop();
            TriggerPropertyChanged(nameof(IsMonitoring));
        }

        private void StreamMonitor_StreamStatusChanged(object sender, StreamStatusChangedArgs e)
        {
            if (StartupTask?.IsCompleted ?? true)
            {
                StartupTask = _StartRecordAsync(e.type);
            }
        }

        public void StartRecord()
        {
            StreamMonitor.Check(TriggerType.Manual);
        }

        public void StopRecord()
        {
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
                        _response?.Close();
                        _response?.Dispose();
                        StreamDownloadTask?.Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(RealRoomid, LogLevel.Error, "在尝试停止录制时发生错误，请检查网络连接是否稳定", ex);
            }
        }

        private async Task _StartRecordAsync(TriggerType triggerType)
        {
            if (IsRecording)
            {
                logger.Log(RealRoomid, LogLevel.Debug, "已经在录制中了");
                return;
            }

            HttpWebRequest request = null;

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            try
            {

                string flv_path = BililiveAPI.GetPlayUrl(RealRoomid);

                request = WebRequest.CreateHttp(flv_path);
                request.Accept = "*/*";
                request.AllowAutoRedirect = true;
                request.Referer = "https://live.bilibili.com";
                request.Headers["Origin"] = "https://live.bilibili.com";
                request.UserAgent = Utils.UserAgent;

                _response = await request.GetResponseAsync() as HttpWebResponse;

                if (_response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Log(RealRoomid, LogLevel.Info, string.Format("尝试下载直播流时服务器返回了 ({0}){1}", _response.StatusCode, _response.StatusDescription));
                    _response.Close();
                    request = null;
                    if (_response.StatusCode == HttpStatusCode.NotFound)
                    {
                        logger.Log(RealRoomid, LogLevel.Info, "将在15秒后重试");
                        StreamMonitor.CheckAfterSeconeds(15); // TODO: 重试次数和时间
                    }
                    return;
                }
                else
                {
                    if (triggerType == TriggerType.HttpApiRecheck)
                    {
                        triggerType = TriggerType.HttpApi;
                    }

                    Processor = newIFlvStreamProcessor().Initialize(GetStreamFilePath, GetClipFilePath, _config.EnabledFeature);
                    Processor.ClipLengthFuture = _config.ClipLengthFuture;
                    Processor.ClipLengthPast = _config.ClipLengthPast;

                    _stream = _response.GetResponseStream();
                    _stream.ReadTimeout = 3 * 1000;

                    StreamDownloadTask = Task.Run(async () =>
                    {
                        try
                        {
                            const int BUF_SIZE = 1024 * 8;
                            byte[] buffer = new byte[BUF_SIZE];
                            while (!token.IsCancellationRequested)
                            {
                                int bytesRead = await _stream.ReadAsync(buffer, 0, BUF_SIZE, token);
                                _UpdateDownloadSpeed(bytesRead);
                                if (bytesRead == 0)
                                {
                                    // 录制已结束
                                    // TODO: 重试次数和时间
                                    // TODO: 用户操作停止时不重新继续

                                    logger.Log(RealRoomid, LogLevel.Info,
                                        (token.IsCancellationRequested ? "用户操作" : "直播已结束") + "，停止录制。"
                                        + (triggerType != TriggerType.HttpApiRecheck ? "将在15秒后重试启动。" : ""));
                                    if (triggerType != TriggerType.HttpApiRecheck)
                                    {
                                        StreamMonitor.CheckAfterSeconeds(15);
                                    }
                                    break;
                                }
                                else
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
                            } // while(true)
                              // outside of while
                        }
                        finally
                        {
                            _CleanupFlvRequest();
                        }
                    });

                    TriggerPropertyChanged(nameof(IsRecording));
                }
            }
            catch (Exception ex)
            {
                _CleanupFlvRequest();
                logger.Log(RealRoomid, LogLevel.Warn, "启动直播流下载出错。" + (triggerType != TriggerType.HttpApiRecheck ? "将在15秒后重试启动。" : ""), ex);
                if (triggerType != TriggerType.HttpApiRecheck)
                {
                    StreamMonitor.CheckAfterSeconeds(15);
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
                request = null;
                _stream?.Dispose();
                _stream = null;
                _response?.Dispose();
                _response = null;

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
                    DownloadSpeedPersentage = (Processor.CurrentTimestamp - _lastUpdateTimestamp) / passedSeconds / 1000; // ((RecordedTime/1000) / RealTime)%
                    _lastUpdateTimestamp = Processor.CurrentTimestamp;
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
            Stop();
            StopRecord();
        }

        private string GetStreamFilePath() => Path.Combine(_config.WorkDirectory, RealRoomid.ToString(), "record",
            $@"record-{RealRoomid}-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{random.Next(100, 999)}.flv".RemoveInvalidFileName());

        private string GetClipFilePath() => Path.Combine(_config.WorkDirectory, RealRoomid.ToString(), "clip",
            $@"clip-{RealRoomid}-{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-{random.Next(100, 999)}.flv".RemoveInvalidFileName());

        public event PropertyChangedEventHandler PropertyChanged;
        protected void TriggerPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
