using System;
using System.IO;
using System.Text;

namespace BililiveRecorder.Core.UnitTests.Danmaku
{
    /// <summary>
    /// 测试用的 protobuf 编码器，用于构造二进制测试数据。
    /// </summary>
    internal sealed class ProtobufTestWriter
    {
        private readonly MemoryStream stream = new MemoryStream();

        public ProtobufTestWriter Varint(int fieldNumber, ulong value)
        {
            this.WriteTag(fieldNumber, 0);
            this.WriteVarint(value);
            return this;
        }

        public ProtobufTestWriter Fixed64(int fieldNumber, ulong value)
        {
            this.WriteTag(fieldNumber, 1);
            var bytes = BitConverter.GetBytes(value);
            this.stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public ProtobufTestWriter Bytes(int fieldNumber, byte[] value)
        {
            this.WriteTag(fieldNumber, 2);
            this.WriteVarint((ulong)value.Length);
            this.stream.Write(value, 0, value.Length);
            return this;
        }

        public ProtobufTestWriter String(int fieldNumber, string value) => this.Bytes(fieldNumber, Encoding.UTF8.GetBytes(value));

        public ProtobufTestWriter Message(int fieldNumber, ProtobufTestWriter value) => this.Bytes(fieldNumber, value.ToArray());

        public ProtobufTestWriter Fixed32(int fieldNumber, uint value)
        {
            this.WriteTag(fieldNumber, 5);
            var bytes = BitConverter.GetBytes(value);
            this.stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public ProtobufTestWriter Float(int fieldNumber, float value) => this.Fixed32(fieldNumber, BitConverter.ToUInt32(BitConverter.GetBytes(value), 0));

        public byte[] ToArray() => this.stream.ToArray();

        private void WriteTag(int fieldNumber, int wireType) => this.WriteVarint((ulong)((fieldNumber << 3) | wireType));

        private void WriteVarint(ulong value)
        {
            while (value >= 0x80)
            {
                this.stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            this.stream.WriteByte((byte)value);
        }
    }
}
