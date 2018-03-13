using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using BililiveRecorder.FlvProcessor;

namespace BililiveRecorder.Core
{
    public class RecordedRoom : INotifyPropertyChanged
    {
        public int RoomID;

        public StreamMonitor streamMonitor;
        public FlvStreamProcessor Processor; // FlvProcessor
        public readonly ObservableCollection<FlvClipProcessor> Clips = new ObservableCollection<FlvClipProcessor>();
        private HttpWebRequest webRequest;

        public RecordedRoom()
        {
            Processor.BlockProcessed += Processor_BlockProcessed;
            streamMonitor.StreamStatusChanged += StreamMonitor_StreamStatusChanged;
        }

        private void StreamMonitor_StreamStatusChanged(object sender, StreamStatusChangedArgs e)
        {
            if (e.status.isStreaming)
            {
                // TODO: 失败重试逻辑 & 掉线重连逻辑
                _StartRecord();
            }
        }

        private void _StartRecord()
        {
            throw new NotImplementedException();
        }

        // Called by API or GUI
        public void Clip()
        {
            var clip = Processor.Clip();
            // TODO: 多个线程同时运行，这个位置有可能会导致 Clip 丢数据
            // 考虑在此处加锁， Clip 操作时停止向主 Processor 添加数据
            clip.ClipFinalized += CallBack_ClipFinalized;
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

        private void Processor_BlockProcessed(object sender, BlockProcessedArgs e)
        {
            Clips.ToList().ForEach((fcp) => fcp.AddBlock(e.DataBlock));
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
