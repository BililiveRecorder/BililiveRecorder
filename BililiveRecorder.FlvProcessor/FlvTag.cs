using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvTag
    {
        public TagType TagType = 0;
        public int TagSize = 0;
        public int TimeStamp = 0;
        public byte[] StreamId = new byte[3];

        public byte[] Data = null;

        public byte[] ToBytes(bool useDataSize)
        {
            var tag = new byte[11];
            tag[0] = (byte)TagType;
            var size = BitConverter.GetBytes(useDataSize ? Data.Length : TagSize).ToBE();
            Buffer.BlockCopy(size, 1, tag, 1, 3);

            byte[] timing = new byte[4];
            Buffer.BlockCopy(BitConverter.GetBytes(this.TimeStamp).ToBE(), 0, timing, 0, timing.Length);
            Buffer.BlockCopy(timing, 1, tag, 4, 3);
            Buffer.BlockCopy(timing, 0, tag, 7, 1);

            Buffer.BlockCopy(StreamId, 0, tag, 8, 3);

            return tag;
        }
    }
}
