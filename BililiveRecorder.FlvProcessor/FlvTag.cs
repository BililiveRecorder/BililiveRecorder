using System;
using System.Diagnostics;
using System.IO;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvTag : IFlvTag
    {
        public TagType TagType { get; set; } = 0;
        public int TagSize { get; set; } = 0;
        public int TimeStamp { get; private set; } = 0;
        public byte[] StreamId { get; set; } = new byte[3];

        public bool IsVideoKeyframe { get; private set; }// _IsVideoKeyframe != -1 ? _IsVideoKeyframe == 1 : 1 == (_IsVideoKeyframe = _ParseIsVideoKeyframe());
        public int Profile { get; private set; } = -1;
        public int Level { get; private set; } = -1;

        public byte[] Data { get => _data; set { _data = value; ParseInfo(); } }
        private byte[] _data = null;

        public void SetTimeStamp(int timestamp) => TimeStamp = timestamp;

        private void ParseInfo()
        {
            /**
             * VIDEODATA:
             *   0x17 (1 byte)
             *     1 = AVC Keyframe
             *     7 = AVC Codec
             * AVCVIDEOPACKET:
             *   0x00 (1 byte)
             *     0 = AVC Header
             *     1 = AVC NALU
             *   0x00
             *   0x00
             *   0x00 (3 bytes)
             *     if(AVC_HEADER) then always 0
             * AVCDecoderConfigurationRecord:
             *   0x01 (1 byte)
             *     configurationVersion must be 1
             *   0x00 (1 byte)
             *     AVCProfileIndication
             *   0x00 (1 byte)
             *     profile_compatibility
             *   0x00 (1 byte)
             *     AVCLevelIndication
             * */

            IsVideoKeyframe = false;
            Profile = -1;
            Level = -1;

            if (TagType != TagType.VIDEO) { return; }
            if (Data.Length < 9) { return; }

            // Not AVC Keyframe
            if (Data[0] != 0x17) { return; }

            IsVideoKeyframe = true;

            // Isn't AVCDecoderConfigurationRecord
            if (Data[1] != 0x00) { return; }
            // version is not 1
            if (Data[5] != 0x01) { return; }

            Profile = Data[6];
            Level = Data[8];
#if DEBUG
            Debug.WriteLine("Video Profile: " + Profile + ", Level: " + Level);
#endif
        }

        public byte[] ToBytes(bool useDataSize, int offset = 0)
        {
            var tag = new byte[11];
            tag[0] = (byte)TagType;
            var size = BitConverter.GetBytes(useDataSize ? Data.Length : TagSize).ToBE();
            Buffer.BlockCopy(size, 1, tag, 1, 3);

            byte[] timing = new byte[4];
            Buffer.BlockCopy(BitConverter.GetBytes(Math.Max(0, TimeStamp - offset)).ToBE(), 0, timing, 0, timing.Length);
            Buffer.BlockCopy(timing, 1, tag, 4, 3);
            Buffer.BlockCopy(timing, 0, tag, 7, 1);

            Buffer.BlockCopy(StreamId, 0, tag, 8, 3);

            return tag;
        }

        public void WriteTo(Stream stream, int offset = 0)
        {
            if (stream != null)
            {
                var vs = ToBytes(true, offset);
                stream.Write(vs, 0, vs.Length);
                stream.Write(Data, 0, Data.Length);
                stream.Write(BitConverter.GetBytes(Data.Length + vs.Length).ToBE(), 0, 4);
            }
        }

        private int _ParseIsVideoKeyframe()
        {
            if (TagType != TagType.VIDEO) { return 0; }
            if (Data.Length < 1) { return -1; }

            const byte mask = 0b00001111;
            const byte compare = 0b00011111;

            return (Data[0] | mask) == compare ? 1 : 0;
        }
    }
}
