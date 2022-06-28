namespace BililiveRecorder.Core.Config.V3
{
    public interface IFileNameConfig
    {
        public string? FileNameRecordTemplate { get; }

        public string? WorkDirectory { get; }
    }
}
