namespace BililiveRecorder.Flv.Pipeline
{
    public enum CommentType
    {
        Other = 0,
        Logging,
        Unrepairable,
        TimestampJump,
        TimestampOffset,
        DecodingHeader,
        RepeatingData,
        OnMetaData,
    }
}
