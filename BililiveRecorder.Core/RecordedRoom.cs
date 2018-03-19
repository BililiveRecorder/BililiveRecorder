using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using BililiveRecorder.FlvProcessor;

namespace BililiveRecorder.Core
{
    public class RecordedRoom : INotifyPropertyChanged
    {
        public int roomID { get; private set; }
        public RoomInfo roomInfo { get; private set; }
        public RecordInfo recordInfo { get; private set; }

        public RecordStatus Status;

        public StreamMonitor streamMonitor;
        public FlvStreamProcessor Processor; // FlvProcessor
        public readonly ObservableCollection<FlvClipProcessor> Clips = new ObservableCollection<FlvClipProcessor>();
        private HttpWebRequest webRequest;
        private readonly Settings _settings;

        public RecordedRoom(Settings settings)
        {
            _settings = settings;

            _settings.PropertyChanged += _settings_PropertyChanged;

            Processor.TagProcessed += Processor_TagProcessed;
            Processor.StreamFinalized += Processor_StreamFinalized;
            Processor.GetFileName = recordInfo.GetStreamFilePath;
            streamMonitor.StreamStatusChanged += StreamMonitor_StreamStatusChanged;

            UpdateRoomInfo();
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
            if (e.status.isStreaming)
            {
                // TODO: 失败重试逻辑 & 掉线重连逻辑
                _StartRecord();
            }
        }

        public void StartRecord()
        {
            throw new NotImplementedException();
        }

        private void _StartRecord()
        {
            // throw new NotImplementedException();
            if (webRequest != null)
            {
                //TODO: cleanup
                webRequest = null;
            }
            if (Processor != null)
            {
                //TODO: cleanup
                Processor = null;
            }


            string flv_path = BililiveAPI.GetPlayUrl(roomInfo.RealRoomid);

            webRequest = WebRequest.CreateHttp(flv_path);
            _SetupFlvRequest(webRequest);
            HttpWebResponse response = webRequest.GetResponse() as HttpWebResponse;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                //TODO: Log
                response.Close();
                webRequest = null;
                return;
            }
            Stream flvStream = response.GetResponseStream();
            const int BUF_SIZE = 1024 * 8;// 8 KiB
            byte[] buffer = new byte[BUF_SIZE];

            AsyncCallback callback = null;

            callback = ar =>
            {
                try
                {
                    int bytesRead = flvStream.EndRead(ar);

                    if (bytesRead == 0)
                    {
                        // TODO: connection closed
                    }
                    else
                    {
                        if (bytesRead != buffer.Length)
                        {
                            Processor.AddBytes(buffer.Take(bytesRead).ToArray());
                        }
                        else
                        {
                            Processor.AddBytes(buffer);
                        }


                        Console.Write('#');

                        flvStream.BeginRead(buffer, 0, BUF_SIZE, callback, null);
                    }
                }
                catch (Exception e)
                {
                    throw; //TODO
                }
            };

            flvStream.BeginRead(buffer, 0, BUF_SIZE, callback, null);
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

        void StartWebRequest()
        {

            webRequest.BeginGetResponse(new AsyncCallback(FinishWebRequest), webRequest);
        }

        void FinishWebRequest(IAsyncResult result)
        {
            HttpWebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result) as HttpWebResponse;
        }

        public bool UpdateRoomInfo()
        {
            try
            {
                roomInfo = BililiveAPI.GetRoomInfo(roomID);
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
            clip.GetFileName = recordInfo.GetClipFilePath;
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
    }
}
