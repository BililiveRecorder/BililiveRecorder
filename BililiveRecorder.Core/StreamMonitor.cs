using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public class StreamMonitor
    {
        public int Roomid { get; private set; } = 0;
        public event StreamStatusChangedEvent StreamStatusChanged;
        public readonly DanmakuReceiver receiver = new DanmakuReceiver();

        public StreamMonitor(int roomid)
        {
            Roomid = roomid;

            receiver.Connected += Receiver_Connected;
            receiver.Disconnected += Receiver_Disconnected;
            receiver.LogMessage += Receiver_LogMessage;
            receiver.ReceivedDanmaku += Receiver_ReceivedDanmaku;
            receiver.ReceivedRoomCount += Receiver_ReceivedRoomCount;

        }

        private void Receiver_ReceivedRoomCount(object sender, ReceivedRoomCountArgs e)
        {
            throw new NotImplementedException();
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            switch (e.Danmaku.MsgType)
            {
                case MsgTypeEnum.LiveStart:
                    _StartRecord(TriggerType.Danmaku);
                    break;
                case MsgTypeEnum.LiveEnd:
                    break;
                default:
                    break;
            }
        }

        private void Receiver_LogMessage(object sender, LogMessageArgs e)
        {
            // TODO: Log
        }

        private void Receiver_Disconnected(object sender, DisconnectEvtArgs e)
        {
            if (e.Error != null)
            {

            }
        }

        private void Receiver_Connected(object sender, ConnectedEvtArgs e)
        { }

        private void _StartRecord(TriggerType status)
        {
            Task.Run(() => StreamStatusChanged?.Invoke(this, new StreamStatusChangedArgs() { status = status }));
        }

        public void Start()
        {
            if (receiver.Connect(Roomid))
            {
                var info = BililiveAPI.GetRoomInfo(Roomid);
                if (info.isStreaming)
                {
                    _StartRecord(TriggerType.HttpApi);
                }
            }
            else
            {
                throw receiver.Error;
            }

        }

        public void Stop()
        {
            if (receiver.isConnected)
            {
                receiver.Disconnect();
            }
        }

        public void Check()
        {
            var info = BililiveAPI.GetRoomInfo(Roomid);
            if (info.isStreaming)
            {
                _StartRecord(TriggerType.HttpApiRecheck);
            }
        }

        public void CheckAfterSeconeds(int seconds)
        {
            if (seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds), "不能小于0");

            Task.Run(() =>
            {
                Task.Delay(seconds * 1000).Wait();
                Check();
            });
        }
    }
}
