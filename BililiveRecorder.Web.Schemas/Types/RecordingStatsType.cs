using BililiveRecorder.Core;
using GraphQL.Types;

namespace BililiveRecorder.Web.Schemas.Types
{
    public class RecordingStatsType : ObjectGraphType<RecordingStats>
    {
        public RecordingStatsType()
        {
            this.Field(x => x.NetworkMbps);
            this.Field(x => x.SessionDuration);
            this.Field(x => x.FileMaxTimestamp);
            this.Field(x => x.SessionMaxTimestamp);
            this.Field(x => x.DurationRatio);
            this.Field(x => x.TotalInputBytes);
            this.Field(x => x.TotalOutputBytes);
        }
    }
}
