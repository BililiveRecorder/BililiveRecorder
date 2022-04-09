using BililiveRecorder.Core;
using GraphQL.Types;

namespace BililiveRecorder.Web.Models.Graphql
{
    public class IOStatsType : ObjectGraphType<RoomStats>
    {
        public IOStatsType()
        {
            this.Field(x => x.StartTime);
            this.Field(x => x.EndTime);
            this.Field(x => x.Duration, type: typeof(TimeSpanMillisecondsGraphType));
            this.Field(x => x.NetworkBytesDownloaded);
            this.Field(x => x.NetworkMbps);
            this.Field(x => x.DiskWriteDuration, type: typeof(TimeSpanMillisecondsGraphType));
            this.Field(x => x.DiskBytesWritten);
            this.Field(x => x.DiskMBps);
        }
    }
}
