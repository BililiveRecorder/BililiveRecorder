using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public class Recorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ObservableCollection<RecordedRoom> Rooms { get; } = new ObservableCollection<RecordedRoom>();
        public Settings Settings { get; } = new Settings();

        private CancellationTokenSource tokenSource;

        public Recorder()
        {
            tokenSource = new CancellationTokenSource();
            Repeat.Interval(TimeSpan.FromSeconds(6), DownloadWatchdog, tokenSource.Token);
        }

        private void DownloadWatchdog()
        {
            try
            {
                Rooms.ToList().ForEach(room =>
                {
                    if (room.IsRecording)
                    {
                        if (DateTime.Now - room.LastUpdateDateTime > TimeSpan.FromSeconds(3))
                        {
                            logger.Warn("服务器停止提供 {0} 直播间的直播数据，通常是录制时网络不稳定导致，将会断开重连", room.Roomid);
                            room.StopRecord();
                            room.StreamMonitor.CheckAfterSeconeds(1, TriggerType.HttpApi);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "直播流下载监控出错");
            }
        }

        /// <summary>
        /// 添加直播间到录播姬
        /// </summary>
        /// <param name="roomid">房间号（支持短号）</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void AddRoom(int roomid, bool enabled = false)
        {
            if (roomid <= 0)
                throw new ArgumentOutOfRangeException(nameof(roomid), "房间号需要大于0");
            var rr = new RecordedRoom(Settings, roomid);
            if (enabled)
                Task.Run(() => rr.Start());
            Rooms.Add(rr);
        }

        /// <summary>
        /// 从录播姬移除直播间
        /// </summary>
        /// <param name="rr">直播间</param>
        public void RemoveRoom(RecordedRoom rr)
        {
            rr.Stop();
            rr.StopRecord();
            Rooms.Remove(rr);
        }

        public void Shutdown()
        {
            tokenSource.Cancel();

            Rooms.ToList().ForEach(rr =>
            {
                rr.Stop();
                rr.StopRecord();
            });
        }
    }
}
