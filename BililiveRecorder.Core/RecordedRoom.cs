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

        public RecordStatus Status
        {
            get => _status;
            set => SetField(ref _status, value, nameof(Status));
        }
        private RecordStatus _status;

        public StreamMonitor streamMonitor;
        public FlvStreamProcessor Processor; // FlvProcessor
        public readonly ObservableCollection<FlvClipProcessor> Clips = new ObservableCollection<FlvClipProcessor>();
        private HttpWebRequest webRequest;
        private Stream flvStream;
        private readonly Settings _settings;

        public RecordedRoom(Settings settings, int roomid)
        {
            _settings = settings;
            _settings.PropertyChanged += _settings_PropertyChanged;

            Roomid = roomid;

            UpdateRoomInfo();

            streamMonitor = new StreamMonitor(RealRoomid);
            streamMonitor.StreamStatusChanged += StreamMonitor_StreamStatusChanged;
        }

        public bool Start()
        {
            var r = streamMonitor.Start();
            if (r)
                Status = RecordStatus.Waiting;
            return r;
        }

        public void Stop()
        {
            streamMonitor.Stop();
            Status = RecordStatus.Idle;
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
        }

        private void StreamMonitor_StreamStatusChanged(object sender, StreamStatusChangedArgs e)
        {
            _StartRecord(e.type);
        }

        public void StartRecord()
        {
            _StartRecord(TriggerType.Manual);
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
                Status = RecordStatus.Recording;

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

                    flvStream = response.GetResponseStream();
                    const int BUF_SIZE = 1024 * 8;// 8 KiB
                    byte[] buffer = new byte[BUF_SIZE];

                    void callback(IAsyncResult ar)
                    {
                        try
                        {
                            int bytesRead = flvStream.EndRead(ar);

                            if (bytesRead == 0)
                            {
                                _CleanupFlvRequest();
                                logger.Log(RealRoomid, LogLevel.Info, "直播流下载连接已关闭。" + (triggerType != TriggerType.HttpApiRecheck ? "将在30秒后重试启动。" : ""));
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
                            logger.Log(RealRoomid, LogLevel.Info, "直播流下载连接出错。" + (triggerType != TriggerType.HttpApiRecheck ? "将在30秒后重试启动。" : ""), ex);
                            if (triggerType != TriggerType.HttpApiRecheck)
                                streamMonitor.CheckAfterSeconeds(30);
                        }
                    }

                    flvStream.BeginRead(buffer, 0, BUF_SIZE, callback, null);
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

        private void _CleanupFlvRequest()
        {
            Status = RecordStatus.Waiting;
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
        }

        private static void _SetupFlvRequest(HttpWebRequest r)
        {
            r.Accept = "*/*";
            r.AllowAutoRedirect = true;
            r.Connection = "keep-alive";
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
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
