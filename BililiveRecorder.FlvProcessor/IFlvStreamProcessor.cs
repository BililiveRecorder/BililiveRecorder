using System;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvStreamProcessor : IDisposable
    {
        event TagProcessedEvent TagProcessed;
        event StreamFinalizedEvent StreamFinalized;

        IFlvMetadata Metadata { get; set; }
        Func<string> GetFileName { get; set; }
        uint Clip_Past { get; set; }
        uint Clip_Future { get; set; }
        int LasttimeRemovedTimestamp { get; }
        int MaxTimeStamp { get; }
        int BaseTimeStamp { get; }
        int TagVideoCount { get; }
        int TagAudioCount { get; }

        void AddBytes(byte[] data);
        IFlvClipProcessor Clip();
        void FinallizeFile();
    }
}
