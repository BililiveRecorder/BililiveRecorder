using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvClipProcessor
    {
        public readonly FlvMetadata Header;
        public readonly List<FlvTag> Tags;
        private int target = -1;

        public Func<string> GetFileName;

        public FlvClipProcessor(FlvMetadata header, List<FlvTag> past, int future)
        {
            Header = header;
            Tags = past;
            target = Tags[Tags.Count - 1].TimeStamp + (future * FlvStreamProcessor.SEC_TO_MS);
        }

        public void AddTag(FlvTag tag)
        {
            Tags.Add(tag);
            if (tag.TimeStamp >= target)
            {
                FinallizeFile();
            }
        }

        public void FinallizeFile()
        {
            using (var fs = new FileStream(GetFileName(), FileMode.CreateNew, FileAccess.ReadWrite))
            {
                fs.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);
                fs.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                Header.Meta["duration"] = Tags[Tags.Count - 1].TimeStamp / 1000.0;
                Header.Meta["lasttimestamp"] = (double)Tags[Tags.Count - 1].TimeStamp;

                var t = new FlvTag
                {
                    TagType = TagType.DATA,
                    Data = Header.ToBytes()
                };
                var b = t.ToBytes(true);
                fs.Write(b, 0, b.Length);
                fs.Write(t.Data, 0, t.Data.Length);
                fs.Write(BitConverter.GetBytes(t.Data.Length + b.Length).ToBE(), 0, 4);

                int timestamp = Tags[0].TimeStamp;

                Tags.ForEach(tag =>
                {
                    tag.TimeStamp -= timestamp;
                    var vs = tag.ToBytes(true);
                    fs.Write(vs, 0, vs.Length);
                    fs.Write(tag.Data, 0, tag.Data.Length);
                    fs.Write(BitConverter.GetBytes(tag.Data.Length + vs.Length).ToBE(), 0, 4);
                });

                fs.Close();
            }
            Tags.Clear();

            ClipFinalized?.Invoke(this, new ClipFinalizedArgs() { ClipProcessor = this });
        }

        public event ClipFinalizedEvent ClipFinalized;
    }
}
