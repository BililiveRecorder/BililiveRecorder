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

        public byte[] Data { get => this._data; set { this._data = value; this.ParseInfo(); } }
        private byte[] _data = null;

        public void SetTimeStamp(int timestamp) => this.TimeStamp = timestamp;

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

            this.IsVideoKeyframe = false;
            this.Profile = -1;
            this.Level = -1;

            if (this.TagType != TagType.VIDEO) { return; }
            if (this.Data.Length < 9) { return; }

            // Not AVC Keyframe
            if (this.Data[0] != 0x17) { return; }

            this.IsVideoKeyframe = true;

            // Isn't AVCDecoderConfigurationRecord
            if (this.Data[1] != 0x00) { return; }
            // version is not 1
            if (this.Data[5] != 0x01) { return; }

            this.Profile = this.Data[6];
            this.Level = this.Data[8];
#if DEBUG
            Debug.WriteLine("Video Profile: " + this.Profile + ", Level: " + this.Level);
#endif
        }

        public byte[] ToBytes(bool useDataSize, int offset = 0)
        {
            var tag = new byte[11];
            tag[0] = (byte)this.TagType;
            var size = BitConverter.GetBytes(useDataSize ? this.Data.Length : this.TagSize).ToBE();
            Buffer.BlockCopy(size, 1, tag, 1, 3);

            byte[] timing = BitConverter.GetBytes(this.TimeStamp - offset).ToBE();
            Buffer.BlockCopy(timing, 1, tag, 4, 3);
            Buffer.BlockCopy(timing, 0, tag, 7, 1);

            Buffer.BlockCopy(this.StreamId, 0, tag, 8, 3);

            return tag;
        }

        public void WriteTo(Stream stream, int offset = 0)
        {
            if (stream != null)
            {
                var vs = this.ToBytes(true, offset);
                stream.Write(vs, 0, vs.Length);
                stream.Write(this.Data, 0, this.Data.Length);
                stream.Write(BitConverter.GetBytes(this.Data.Length + vs.Length).ToBE(), 0, 4);
            }
        }

        private int _ParseIsVideoKeyframe()
        {
            if (this.TagType != TagType.VIDEO) { return 0; }
            if (this.Data.Length < 1) { return -1; }

            const byte mask = 0b00001111;
            const byte compare = 0b00011111;

            return (this.Data[0] | mask) == compare ? 1 : 0;
        }
    }
}
