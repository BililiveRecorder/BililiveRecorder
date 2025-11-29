using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;

namespace BililiveRecorder.Flv
{
    /// <summary>
    /// H.264 NAL unit
    /// </summary>
    public sealed class H264Nalu
    {
        private H264Nalu()
        {
        }

        public H264Nalu(int startPosition, uint fullSize, H264NaluType type)
        {
            this.StartPosition = startPosition;
            this.FullSize = fullSize;
            this.Type = type;
        }

        public static bool TryParseNalu(Stream data, [NotNullWhen(true)] out List<H264Nalu>? h264Nalus)
        {
            h264Nalus = null;
            var result = new List<H264Nalu>();
            var b = new byte[4];

            data.Seek(5, SeekOrigin.Begin);

            try
            {
                while (data.Position < data.Length)
                {
                    data.Read(b, 0, 4);
                    var size = BinaryPrimitives.ReadUInt32BigEndian(b);
                    if (TryParseNaluType((byte)data.ReadByte(), out var h264NaluType))
                    {
                        var nalu = new H264Nalu((int)(data.Position - 1), size, h264NaluType);
                        data.Seek(size - 1, SeekOrigin.Current);
                        result.Add(nalu);
                    }
                    else
                        return false;
                }
                h264Nalus = result;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParseNaluType(byte firstByte, out H264NaluType h264NaluType)
        {
            if ((firstByte & 0b10000000) != 0)
            {
                h264NaluType = default;
                return false;
            }

            h264NaluType = (H264NaluType)(firstByte & 0b00011111);
            return true;
        }

        /// <summary>
        /// 一个 nal_unit 的开始位置
        /// </summary>
        [XmlAttribute]
        public int StartPosition { get; set; }

        /// <summary>
        /// 一个 nal_unit 的完整长度
        /// </summary>
        [XmlAttribute]
        public uint FullSize { get; set; }

        /// <summary>
        /// nal_unit_type
        /// </summary>
        [XmlAttribute]
        public H264NaluType Type { get; set; }

        /// <summary>
        /// nal_unit data hash
        /// </summary>
        [XmlAttribute]
        public string? NaluHash { get; set; }
    }

    /// <summary>
    /// nal_unit_type
    /// </summary>
    public enum H264NaluType : byte
    {
        Unspecified0 = 0,
        CodedSliceOfANonIdrPicture = 1,
        CodedSliceDataPartitionA = 2,
        CodedSliceDataPartitionB = 3,
        CodedSliceDataPartitionC = 4,
        CodedSliceOfAnIdrPicture = 5,
        Sei = 6,
        Sps = 7,
        Pps = 8,
        AccessUnitDelimiter = 9,
        EndOfSequence = 10,
        EndOfStream = 11,
        FillerData = 12,
        SpsExtension = 13,
        PrefixNalUnit = 14,
        SubsetSps = 15,
        DepthParameterSet = 16,
        Reserved17 = 17,
        Reserved18 = 18,
        SliceLayerWithoutPartitioning = 19,
        SliceLayerExtension20 = 20,
        SliceLayerExtension21 = 21,
        Reserved22 = 22,
        Reserved23 = 23,
        Unspecified24 = 24,
        Unspecified25 = 25,
        Unspecified23 = 23,
        Unspecified27 = 27,
        Unspecified28 = 28,
        Unspecified29 = 29,
        Unspecified30 = 30,
        Unspecified31 = 31,
    }
}
