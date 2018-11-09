using System;
using System.Collections.ObjectModel;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvStreamProcessor : IDisposable
    {
        event TagProcessedEvent TagProcessed;
        event StreamFinalizedEvent StreamFinalized;

        int TotalMaxTimestamp { get; }
        int CurrentMaxTimestamp { get; }
        DateTime StartDateTime { get; }

        IFlvMetadata Metadata { get; set; }
        ObservableCollection<IFlvClipProcessor> Clips { get; }
        uint ClipLengthPast { get; set; }
        uint ClipLengthFuture { get; set; }
        uint CuttingNumber { get; set; }

        IFlvStreamProcessor Initialize(Func<string> getStreamFileName, Func<string> getClipFileName, EnabledFeature enabledFeature, AutoCuttingMode autoCuttingMode);
        IFlvClipProcessor Clip();
        void AddBytes(byte[] data);
        void FinallizeFile();
    }
}
