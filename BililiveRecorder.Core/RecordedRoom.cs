using BililiveRecorder.FlvProcessor;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace BililiveRecorder.Core
{
    public class RecordedRoom : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public int Roomid { get; private set; }
        public int RealRoomid { get => RoomInfo?.RealRoomid ?? Roomid; }
        public string StreamerName { get => RoomInfo?.Username ?? string.Empty; }
        public RoomInfo RoomInfo { get; private set; }
        public RecordInfo RecordInfo { get; private set; }

        public bool IsMonitoring => streamMonitor.receiver.IsConnected;
        public bool IsRecording => flvStream != null;

        public FlvStreamProcessor Processor; // FlvProcessor
        public ObservableCollection<FlvClipProcessor> Clips { get; private set; } = new ObservableCollection<FlvClipProcessor>();

        private Settings _settings { get; }
        private StreamMonitor streamMonitor { get; }
        private HttpWebRequest webRequest;
        private Stream flvStream;
        private bool flv_shutdown = false;

        public double DownloadSpeedKiBps
        {
            get { return _DownloadSpeedKiBps; }
            set { if (value != _DownloadSpeedKiBps) { _DownloadSpeedKiBps = value; TriggerPropertyChanged(nameof(DownloadSpeedKiBps)); } }
        }
        private double _DownloadSpeedKiBps = 0;
        private DateTime lastUpdateDateTime;
        private long lastUpdateSize = 0;

        public RecordedRoom(Settings settings, int roomid)
        {
            _settings = settings;
            _settings.PropertyChanged += _settings_PropertyChanged;

            Roomid = roomid;

            UpdateRoomInfo();

            RecordInfo = new RecordInfo(StreamerName)
            {
                SavePath = _settings.SavePath
            };

            streamMonitor = new StreamMonitor(RealRoomid);
            streamMonitor.StreamStatusChanged += StreamMonitor_StreamStatusChanged;
        }

        public bool Start()
        {
            var r = streamMonitor.Start();
            TriggerPropertyChanged(nameof(IsMonitoring));
            return r;
        }

        public void Stop()
        {
            streamMonitor.Stop();
            TriggerPropertyChanged(nameof(IsMonitoring));
        }

        private void _settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_settings.Clip_Past))
            {
                if (Processor != null)
                    Processor.Clip_Past = _settings.Clip_Past;
            }
            else if (e.PropertyName == nameof(_settings.Clip_Future))
            {
                if (Processor != null)
                    Processor.Clip_Future = _settings.Clip_Future;
            }
            else if (e.PropertyName == nameof(_settings.SavePath))
            {
                if (RecordInfo != null)
                    RecordInfo.SavePath = _settings.SavePath;
            }
        }

        private void StreamMonitor_StreamStatusChanged(object sender, StreamStatusChangedArgs e)
        {
            _StartRecord(e.type);
        }

        public void StartRecord()
        {
            streamMonitor.Check(TriggerType.Manual);
            // _StartRecord(TriggerType.Manual);
        }

        public void StopRecord()
        {
            if (flvStream != null)
            {
                flv_shutdown = true;
                flvStream.Close();
            }
        }

        private void _StartRecord(TriggerType triggerType)
        {
            /* *
             * if (recording) return;
             * try catch {
             *     if type == retry return;
             *     else retry()
             * }
             * */

            if (webRequest != null || flvStream != null || Processor != null)
            {
                logger.Log(RealRoomid, LogLevel.Debug, "已经在录制中了");
                return;
            }

            try
            {
                flv_shutdown = false;

                string flv_path = BililiveAPI.GetPlayUrl(RoomInfo.RealRoomid);

                webRequest = WebRequest.CreateHttp(flv_path);
                _SetupFlvRequest(webRequest);
                HttpWebResponse response = webRequest.GetResponse() as HttpWebResponse;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Log(RealRoomid, LogLevel.Info, string.Format("尝试下载直播流时服务器返回了 ({0}){1}", response.StatusCode, response.StatusDescription));
                    response.Close();
                    webRequest = null;
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        logger.Log(RealRoomid, LogLevel.Info, "将在30秒后重试");
                        streamMonitor.CheckAfterSeconeds(30);
                    }
                    return;
                }
                else
                {
                    // response.StatusCode == HttpStatusCode.OK
                    Processor = new FlvStreamProcessor(RecordInfo.GetStreamFilePath());
                    Processor.TagProcessed += Processor_TagProcessed;
                    Processor.StreamFinalized += Processor_StreamFinalized;
                    Processor.GetFileName = RecordInfo.GetStreamFilePath;
                    Processor.Clip_Future = _settings.Clip_Future;
                    Processor.Clip_Past = _settings.Clip_Past;

                    flvStream = response.GetResponseStream();
                    const int BUF_SIZE = 1024 * 8;// 8 KiB
                    byte[] buffer = new byte[BUF_SIZE];

                    void callback(IAsyncResult ar)
                    {
                        try
                        {
                            int bytesRead = flvStream.EndRead(ar);

                            _UpdateDownloadSpeed(bytesRead);

                            if (bytesRead == 0)
                            {
                                _CleanupFlvRequest();
                                logger.Log(RealRoomid, LogLevel.Info, "直播已结束，停止录制。" + (triggerType != TriggerType.HttpApiRecheck ? "将在30秒后重试启动。" : ""));
                                if (triggerType != TriggerType.HttpApiRecheck)
                                    streamMonitor.CheckAfterSeconeds(30);
                            }
                            else
                            {
                                if (bytesRead != buffer.Length)
                                    Processor.AddBytes(buffer.Take(bytesRead).ToArray());
                                else
                                    Processor.AddBytes(buffer);

                                flvStream.BeginRead(buffer, 0, BUF_SIZE, callback, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            _CleanupFlvRequest();
                            if (!flv_shutdown)
                            {
                                logger.Log(RealRoomid, LogLevel.Info, "直播流下载连接出错。" + (triggerType != TriggerType.HttpApiRecheck ? "将在30秒后重试启动。" : ""), ex);
                                if (triggerType != TriggerType.HttpApiRecheck)
                                    streamMonitor.CheckAfterSeconeds(30);
                            }
                            else
                            {
                                logger.Log(RealRoomid, LogLevel.Info, "直播流下载已结束。");
                            }
                        }
                    }

                    flvStream.BeginRead(buffer, 0, BUF_SIZE, callback, null);
                    TriggerPropertyChanged(nameof(IsRecording));
                }
            }
            catch (Exception ex)
            {
                _CleanupFlvRequest();
                logger.Log(RealRoomid, LogLevel.Warn, "启动直播流下载出错。" + (triggerType != TriggerType.HttpApiRecheck ? "将在30秒后重试启动。" : ""), ex);
                if (triggerType != TriggerType.HttpApiRecheck)
                    streamMonitor.CheckAfterSeconeds(30);
            }
        }

        private void _UpdateDownloadSpeed(int bytesRead)
        {
            DateTime now = DateTime.Now;
            double sec = (now - lastUpdateDateTime).TotalSeconds;
            lastUpdateSize += bytesRead;
            if (sec > 1)
            {
                var speed = lastUpdateSize / sec;
                lastUpdateDateTime = now;
                lastUpdateSize = 0;
                DownloadSpeedKiBps = speed / 1024; // KiB per sec
            }
        }

        private void _CleanupFlvRequest()
        {
            if (Processor != null)
            {
                Processor.FinallizeFile();
                Processor.Dispose();
                Processor = null;
            }
            webRequest = null;
            if (flvStream != null)
            {
                flvStream.Close();
                flvStream.Dispose();
                flvStream = null;
            }
            DownloadSpeedKiBps = 0d;
            TriggerPropertyChanged(nameof(IsRecording));
        }

        private static void _SetupFlvRequest(HttpWebRequest r)
        {
            r.Accept = "*/*";
            r.AllowAutoRedirect = true;
            // r.Connection = "keep-alive";
            r.Referer = "https://live.bilibili.com";
            r.Headers["Origin"] = "https://live.bilibili.com";
            r.UserAgent = "Mozilla/5.0 BililiveRecorder/0.0.0.0 (+https://github.com/Bililive/BililiveRecorder;bliverec@genteure.com)";
        }

        public bool UpdateRoomInfo()
        {
            try
            {
                RoomInfo = BililiveAPI.GetRoomInfo(Roomid);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RealRoomid)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StreamerName)));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        // Called by API or GUI
        public void Clip()
        {
            if (Processor == null) return;
            var clip = Processor.Clip();
            clip.ClipFinalized += CallBack_ClipFinalized;
            clip.GetFileName = RecordInfo.GetClipFilePath;
            Clips.Add(clip);
        }

        private void CallBack_ClipFinalized(object sender, ClipFinalizedArgs e)
        {
            if (Clips.Remove(e.ClipProcessor))
            {
                Debug.WriteLine("Clip Finalized");
            }
            else
            {
                Debug.WriteLine("Warning! Clip Finalized but was not in Collection.");
            }
        }

        private void Processor_TagProcessed(object sender, TagProcessedArgs e)
        {
            Clips.ToList().ForEach(fcp => fcp.AddTag(e.Tag));
        }

        private void Processor_StreamFinalized(object sender, StreamFinalizedArgs e)
        {
            Clips.ToList().ForEach(fcp => fcp.FinallizeFile());
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void TriggerPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
