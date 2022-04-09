using BililiveRecorder.Core;
using GraphQL.Types;

namespace BililiveRecorder.Web.Models.Graphql
{
    public class RecordingStatsType : ObjectGraphType<RoomStats>
    {
        public RecordingStatsType()
        {
            this.Field(x => x.SessionDuration, type: typeof(TimeSpanMillisecondsGraphType));
            this.Field(x => x.TotalInputBytes);
            this.Field(x => x.TotalOutputBytes);
            this.Field(x => x.CurrentFileSize);
            this.Field(x => x.SessionMaxTimestamp, type: typeof(TimeSpanMillisecondsGraphType));
            this.Field(x => x.FileMaxTimestamp, type: typeof(TimeSpanMillisecondsGraphType));
            this.Field(x => x.AddedDuration);
            this.Field(x => x.PassedTime);
            this.Field(x => x.DurationRatio);
            this.Field(x => x.InputVideoBytes);
            this.Field(x => x.InputAudioBytes);
            this.Field(x => x.OutputVideoFrames);
            this.Field(x => x.OutputAudioFrames);
            this.Field(x => x.OutputVideoBytes);
            this.Field(x => x.OutputAudioBytes);
            this.Field(x => x.TotalInputVideoBytes);
            this.Field(x => x.TotalInputAudioBytes);
            this.Field(x => x.TotalOutputVideoFrames);
            this.Field(x => x.TotalOutputAudioFrames);
            this.Field(x => x.TotalOutputVideoBytes);
            this.Field(x => x.TotalOutputAudioBytes);
        }
    }
}
