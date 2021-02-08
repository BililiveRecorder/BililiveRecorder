using System;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.FlvProcessor;

namespace BililiveRecorder.Core
{
    public class RecordedRoomFactory : IRecordedRoomFactory
    {
        private readonly IProcessorFactory processorFactory;
        private readonly BililiveAPI bililiveAPI;

        public RecordedRoomFactory(IProcessorFactory processorFactory, BililiveAPI bililiveAPI)
        {
            this.processorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
            this.bililiveAPI = bililiveAPI ?? throw new ArgumentNullException(nameof(bililiveAPI));
        }

        public IRecordedRoom CreateRecordedRoom(RoomConfig roomConfig)
        {
            var basicDanmakuWriter = new BasicDanmakuWriter(roomConfig);
            var streamMonitor = new StreamMonitor(roomConfig, this.bililiveAPI);
            return new RecordedRoom(basicDanmakuWriter, streamMonitor, this.processorFactory, this.bililiveAPI, roomConfig);
        }
    }
}
