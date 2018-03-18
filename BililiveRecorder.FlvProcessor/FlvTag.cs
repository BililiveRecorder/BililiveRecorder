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

        public byte[] ToBytes()
        {
            throw new NotImplementedException();
        }
    }
}
