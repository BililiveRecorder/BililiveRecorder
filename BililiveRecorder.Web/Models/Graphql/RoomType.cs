using BililiveRecorder.Core;
using GraphQL.Types;

namespace BililiveRecorder.Web.Models.Graphql
{
    internal class RoomType : ObjectGraphType<IRoom>
    {
        public RoomType()
        {
            this.Field(x => x.ObjectId);
            this.Field(x => x.RoomConfig, type: typeof(RoomConfigType));
            this.Field(x => x.ShortId);
            this.Field(x => x.Name);
            this.Field(x => x.Title);
            this.Field(x => x.AreaNameParent);
            this.Field(x => x.AreaNameChild);
            this.Field(x => x.Recording);
            this.Field(x => x.Streaming);
            this.Field(x => x.AutoRecordForThisSession);
            this.Field(x => x.DanmakuConnected);
            this.Field("ioStats", x => x.Stats, type: typeof(IOStatsType));
            this.Field("recordingStats", x => x.Stats, type: typeof(RecordingStatsType));
        }
    }
}
