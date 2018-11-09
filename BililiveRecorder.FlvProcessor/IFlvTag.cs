using System.IO;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvTag
    {
        TagType TagType { get; set; }
        int TagSize { get; set; }
        int TimeStamp { get; }
        byte[] StreamId { get; set; }
        bool IsVideoKeyframe { get; }
        byte[] Data { get; set; }

        void SetTimeStamp(int timestamp);
        byte[] ToBytes(bool useDataSize, int offset = 0);
        void WriteTo(Stream stream, int offset = 0);
    }
}
