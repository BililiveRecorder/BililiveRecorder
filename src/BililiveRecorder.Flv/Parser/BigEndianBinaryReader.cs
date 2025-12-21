using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace BililiveRecorder.Flv.Parser
{
    /// <inheritdoc/>
    public class BigEndianBinaryReader : BinaryReader
    {
        /// <inheritdoc/>
        public BigEndianBinaryReader(Stream input) : base(input)
        {
        }

        /// <inheritdoc/>
        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        /// <inheritdoc/>
        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        /// <inheritdoc/>
        public override Stream BaseStream => base.BaseStream;

        /// <inheritdoc/>
        public override void Close() => base.Close();

        /// <inheritdoc/>
        public override bool Equals(object? obj) => base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc/>
        public override int PeekChar() => base.PeekChar();

        /// <inheritdoc/>
        public override int Read() => base.Read();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int index, int count) => base.Read(buffer, index, count);

        /// <inheritdoc/>
        public override int Read(char[] buffer, int index, int count) => base.Read(buffer, index, count);

        /// <inheritdoc/>
        public override bool ReadBoolean() => base.ReadBoolean();

        /// <inheritdoc/>
        public override byte ReadByte() => base.ReadByte();

        /// <inheritdoc/>
        public override byte[] ReadBytes(int count) => base.ReadBytes(count);

        /// <inheritdoc/>
        public override char ReadChar() => base.ReadChar();

        /// <inheritdoc/>
        public override char[] ReadChars(int count) => base.ReadChars(count);

        /// <inheritdoc/>
        public override decimal ReadDecimal() => BitConverter.IsLittleEndian ? throw new NotSupportedException("not supported") : base.ReadDecimal();

        /// <inheritdoc/>
        public override double ReadDouble() => BitConverter.IsLittleEndian
                ? BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(base.ReadBytes(sizeof(double))))
                : base.ReadDouble();

        /// <inheritdoc/>
        public override short ReadInt16() => BitConverter.IsLittleEndian ? BinaryPrimitives.ReadInt16BigEndian(base.ReadBytes(sizeof(short))) : base.ReadInt16();

        /// <inheritdoc/>
        public override int ReadInt32() => BitConverter.IsLittleEndian ? BinaryPrimitives.ReadInt32BigEndian(base.ReadBytes(sizeof(int))) : base.ReadInt32();

        /// <inheritdoc/>
        public override long ReadInt64() => BitConverter.IsLittleEndian ? BinaryPrimitives.ReadInt64BigEndian(base.ReadBytes(sizeof(long))) : base.ReadInt64();

        /// <inheritdoc/>
        public override sbyte ReadSByte() => base.ReadSByte();

        /// <inheritdoc/>
        public override float ReadSingle() => BitConverter.IsLittleEndian
                ? Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(base.ReadBytes(sizeof(float))))
                : base.ReadSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float Int32BitsToSingle(int value) => *(float*)&value;

        /// <inheritdoc/>
        public override string ReadString() => base.ReadString();

        /// <inheritdoc/>
        public override ushort ReadUInt16() => BitConverter.IsLittleEndian ? BinaryPrimitives.ReadUInt16BigEndian(base.ReadBytes(sizeof(ushort))) : base.ReadUInt16();

        /// <inheritdoc/>
        public override uint ReadUInt32() => BitConverter.IsLittleEndian ? BinaryPrimitives.ReadUInt32BigEndian(base.ReadBytes(sizeof(uint))) : base.ReadUInt32();

        /// <inheritdoc/>
        public override ulong ReadUInt64() => BitConverter.IsLittleEndian ? BinaryPrimitives.ReadUInt64BigEndian(base.ReadBytes(sizeof(ulong))) : base.ReadUInt64();

        /// <inheritdoc/>
        public override string? ToString() => base.ToString();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) => base.Dispose(disposing);

        /// <inheritdoc/>
        protected override void FillBuffer(int numBytes) => base.FillBuffer(numBytes);
    }
}
