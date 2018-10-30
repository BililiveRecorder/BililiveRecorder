using System;
using System.Collections.ObjectModel;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvStreamProcessor : IDisposable
    {
        event TagProcessedEvent TagProcessed;
        event StreamFinalizedEvent StreamFinalized;

        ObservableCollection<IFlvClipProcessor> Clips { get; }

        IFlvMetadata Metadata { get; set; }

        Func<string> GetStreamFileName { get; }
        Func<string> GetClipFileName { get; }

        IFlvStreamProcessor Initialize(Func<string> getStreamFileName, Func<string> getClipFileName, EnabledFeature enabledFeature);

        uint ClipLengthPast { get; set; }
        uint ClipLengthFuture { get; set; }

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
