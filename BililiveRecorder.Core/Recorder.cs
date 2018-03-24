using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BililiveRecorder.Core
{
    public class Recorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ObservableCollection<RecordedRoom> Rooms { get; } = new ObservableCollection<RecordedRoom>();
        public Settings Settings { get; } = new Settings();

        public Recorder()
        {

        }

        /// <summary>
        /// 添加直播间到录播姬
        /// </summary>
        /// <param name="roomid">房间号（支持短号）</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void AddRoom(int roomid)
        {
            if (roomid <= 0)
                throw new ArgumentOutOfRangeException(nameof(roomid), "房间号需要大于0");
            Rooms.Add(new RecordedRoom(Settings, roomid));
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
    }
}
