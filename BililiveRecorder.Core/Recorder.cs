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

        public void AddRoom(int roomid)
        {
            if (roomid <= 0)
                throw new ArgumentOutOfRangeException(nameof(roomid), "房间号需要大于0");
            var rr = new RecordedRoom(Settings, roomid);
            rr.RecordInfo.SavePath = Settings.SavePath;
            Rooms.Add(rr);
        }

    }
}
