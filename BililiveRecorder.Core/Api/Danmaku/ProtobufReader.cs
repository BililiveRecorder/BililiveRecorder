using System;
using System.IO;
using System.Text;

#nullable enable
namespace BililiveRecorder.Core.Api.Danmaku
{
    internal enum ProtobufWireType
    {
        Varint = 0,
        Fixed64 = 1,
        LengthDelimited = 2,
        StartGroup = 3,
        EndGroup = 4,
        Fixed32 = 5,
    }

    /// <summary>
    /// protobuf wire format 读取器，只实现解析弹幕消息需要的功能。未知字段应通过 <see cref="SkipField"/> 跳过，以兼容协议新增的字段。
    /// </summary>
    internal ref struct ProtobufReader
    {
        private readonly ReadOnlySpan<byte> data;
        private int position;

        public ProtobufReader(ReadOnlySpan<byte> data)
        {
            this.data = data;
            this.position = 0;
        }

        /// <summary>
        /// 读取下一个字段的 tag，到达数据末尾时返回 false。
        /// </summary>
        public bool TryReadTag(out int fieldNumber, out ProtobufWireType wireType)
        {
            if (this.position >= this.data.Length)
            {
                fieldNumber = 0;
                wireType = default;
                return false;
            }

            var tag = this.ReadVarint();
            fieldNumber = (int)(tag >> 3);
            wireType = (ProtobufWireType)(tag & 0x7);

            if (fieldNumber == 0)
                throw new InvalidDataException("Invalid protobuf field number 0");

            return true;
        }

        public ulong ReadVarint()
        {
            ulong result = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                if (this.position >= this.data.Length)
                    throw new InvalidDataException("Unexpected end of protobuf data while reading varint");

                var b = this.data[this.position++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return result;
            }
            throw new InvalidDataException("Protobuf varint is too long");
        }

        public long ReadInt64() => (long)this.ReadVarint();

        public bool ReadBool() => this.ReadVarint() != 0;

        public ReadOnlySpan<byte> ReadBytes()
        {
            var length = this.ReadVarint();
            if (length > (ulong)(this.data.Length - this.position))
                throw new InvalidDataException("Protobuf length-delimited field exceeds available data");

            var result = this.data.Slice(this.position, (int)length);
            this.position += (int)length;
            return result;
        }

        public string ReadString()
        {
            var bytes = this.ReadBytes();
            return bytes.IsEmpty ? string.Empty : Encoding.UTF8.GetString(bytes.ToArray());
        }

        public void SkipField(ProtobufWireType wireType)
        {
            switch (wireType)
            {
                case ProtobufWireType.Varint:
                    this.ReadVarint();
                    break;
                case ProtobufWireType.Fixed64:
                    this.Advance(8);
                    break;
                case ProtobufWireType.LengthDelimited:
                    this.ReadBytes();
                    break;
                case ProtobufWireType.Fixed32:
                    this.Advance(4);
                    break;
                default:
                    throw new InvalidDataException($"Unsupported protobuf wire type {wireType}");
            }
        }

        private void Advance(int count)
        {
            if (count > this.data.Length - this.position)
                throw new InvalidDataException("Unexpected end of protobuf data");
            this.position += count;
        }
    }
}
