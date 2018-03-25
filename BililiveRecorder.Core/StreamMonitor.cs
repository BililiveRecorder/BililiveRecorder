using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public class StreamMonitor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public int Roomid { get; private set; } = 0;
        public event StreamStatusChangedEvent StreamStatusChanged;
        public readonly DanmakuReceiver receiver = new DanmakuReceiver();
        private CancellationTokenSource TokenSource = null;

        public StreamMonitor(int roomid)
        {
            Roomid = roomid;

            receiver.Disconnected += Receiver_Disconnected;
            receiver.ReceivedDanmaku += Receiver_ReceivedDanmaku;
            receiver.ReceivedRoomCount += Receiver_ReceivedRoomCount;
        }

        private void Receiver_ReceivedRoomCount(object sender, ReceivedRoomCountArgs e)
        {
            logger.Log(Roomid, LogLevel.Trace, "直播间人气: " + e.UserCount.ToString());
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

        private void Receiver_Disconnected(object sender, DisconnectEvtArgs e)
        {
            logger.Warn(e.Error, "弹幕连接被断开，将每30秒尝试重连一次");
            bool connect_result = false;
            while (!connect_result && !TokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(1000 * 30); // 备注：这是运行在 ReceiveMessageLoop 线程上的
                logger.Log(Roomid, LogLevel.Info, "重连弹幕服务器...");
                connect_result = receiver.Connect(Roomid);
            }

            if (connect_result)
            {
                logger.Log(Roomid, LogLevel.Info, "弹幕服务器重连成功");
            }
        }

        private void _StartRecord(TriggerType status)
        {
            Task.Run(() => StreamStatusChanged?.Invoke(this, new StreamStatusChangedArgs() { type = status }));
        }

        private void HttpCheck()
        {
            try
            {
                if (BililiveAPI.GetRoomInfo(Roomid).isStreaming)
                    _StartRecord(TriggerType.HttpApi);
            }
            catch (Exception ex)
            {
                logger.Log(Roomid, LogLevel.Warn, "获取直播间开播状态出错", ex);
            }
        }

        public bool Start()
        {
            if (!receiver.IsConnected)
                if (!receiver.Connect(Roomid))
                    return false;
            logger.Log(Roomid, LogLevel.Info, "弹幕服务器连接成功");

            // Run 96 times a day.
            if (TokenSource == null)
            {
                TokenSource = new CancellationTokenSource();
                Repeat.Interval(TimeSpan.FromMinutes(15), HttpCheck, TokenSource.Token);
            }
            return true;
        }

        public void Stop()
        {
            if (receiver.IsConnected)
                receiver.Disconnect();
            TokenSource?.Cancel();
            TokenSource = null;
        }

        public void Check(TriggerType type = TriggerType.HttpApiRecheck)
        {
            var info = BililiveAPI.GetRoomInfo(Roomid);
            if (info.isStreaming)
            {
                _StartRecord(type);
            }
        }

        public void CheckAfterSeconeds(int seconds, TriggerType type = TriggerType.HttpApiRecheck)
        {
            if (seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds), "不能小于0");

            Task.Run(() =>
            {
                Task.Delay(seconds * 1000).Wait();
                Check(type);
            });
        }
    }
}
