using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using BililiveRecorder.Flv.Amf;

namespace BililiveRecorder.Flv.Writer
{
    /// <summary>
    /// 输出时用于生成 OnMetaData 中的 keyframes。直接建 object tree 再序列化太太太太慢了
    /// </summary>
    internal class KeyframesScriptDataValue : IScriptDataValue
    {
        /* 
         * 最少能保存大约 6300 * 2 second = 3.5 hour 的关键帧索引
         * 如果以 5 秒计算则 6300 * 5 second = 8.75 hour
         */

        private const int MaxDataCount = 6300;
        private const double MinInterval = 1900;

        private const string Keyframes = "keyframes";
        private const string Times = "times";
        private const string FilePositions = "filepositions";
        private const string Spacer = "spacer";

        private static readonly byte[] EndBytes = new byte[] { 0, 0, 9 };

        private static readonly byte[] TimesBytes = Encoding.UTF8.GetBytes(Times);
        private static readonly byte[] FilePositionsBytes = Encoding.UTF8.GetBytes(FilePositions);
        private static readonly byte[] SpacerBytes = Encoding.UTF8.GetBytes(Spacer);

        public ScriptDataType Type => ScriptDataType.Object;

        private readonly List<Data> KeyframesData = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddData(double time_in_ms, double filePosition)
        {
            var keyframesData = this.KeyframesData;
            if (keyframesData.Count < MaxDataCount && (keyframesData.Count == 0 || ((time_in_ms - keyframesData[keyframesData.Count - 1].Time) > MinInterval)))
            {
                keyframesData.Add(new Data(time: time_in_ms / 1000d, filePosition: filePosition));
            }
        }

        /// <summary>
        /// <see cref="ScriptDataObject.WriteTo(Stream)"/>
        /// <see cref="ScriptDataStrictArray.WriteTo(Stream)"/>
        /// <see cref="ScriptDataNumber.WriteTo(Stream)"/>
        /// </summary>
        /// <param name="stream"></param>
        public void WriteTo(Stream stream)
        {
            stream.WriteByte((byte)this.Type);

            var keyframesData = this.KeyframesData;
            var buffer = new byte[sizeof(double)];

            {
                // key
                WriteKey(stream, TimesBytes);

                // array
                WriteStrictArray(stream, (uint)keyframesData.Count);

                // value
                for (var i = 0; i < keyframesData.Count; i++)
                {
                    stream.WriteByte((byte)ScriptDataType.Number);
                    BinaryPrimitives.WriteInt64BigEndian(buffer, BitConverter.DoubleToInt64Bits(keyframesData[i].Time));
                    stream.Write(buffer);
                }
            }

            {
                // key
                WriteKey(stream, FilePositionsBytes);

                // array
                WriteStrictArray(stream, (uint)keyframesData.Count);

                // value
                for (var i = 0; i < keyframesData.Count; i++)
                {
                    stream.WriteByte((byte)ScriptDataType.Number);
                    BinaryPrimitives.WriteInt64BigEndian(buffer, BitConverter.DoubleToInt64Bits(keyframesData[i].FilePosition));
                    stream.Write(buffer);
                }
            }

            {
                // key
                WriteKey(stream, SpacerBytes);

                // array
                var count = 2u * (uint)(MaxDataCount - keyframesData.Count);
                WriteStrictArray(stream, count);

                // value
                BinaryPrimitives.WriteInt64BigEndian(buffer, BitConverter.DoubleToInt64Bits(double.NaN));
                for (var i = 0; i < count; i++)
                {
                    stream.WriteByte((byte)ScriptDataType.Number);
                    stream.Write(buffer);
                }
            }

            stream.Write(EndBytes);
        }

        public readonly struct Data
        {
            public readonly double Time;
            public readonly double FilePosition;

            public Data(double time, double filePosition)
            {
                this.Time = time;
                this.FilePosition = filePosition;
            }
        }

        /// <summary>
        /// <see cref="ScriptDataStrictArray.WriteTo(Stream)"/>
        /// </summary>
        /// <param name="stream"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteStrictArray(Stream stream, uint count)
        {
            stream.WriteByte((byte)ScriptDataType.StrictArray);

            var buffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, count);
            stream.Write(buffer);
        }

        /// <summary>
        /// <see cref="ScriptDataObject.WriteTo(Stream)"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteKey(Stream stream, string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            if (bytes.Length > ushort.MaxValue)
                throw new AmfException($"Cannot write more than {ushort.MaxValue} into ScriptDataString");

            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)bytes.Length);

            stream.Write(buffer);
            stream.Write(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteKey(Stream stream, byte[] bytes)
        {
            //if (bytes.Length > ushort.MaxValue)
            //    throw new AmfException($"Cannot write more than {ushort.MaxValue} into ScriptDataString");

            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)bytes.Length);

            stream.Write(buffer);
            stream.Write(bytes);
        }
    }
}
