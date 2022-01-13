using System;
using System.Buffers;
using System.Buffers.Binary;

namespace BililiveRecorder.Flv
{
    public class Int24
    {
        public static int ReadInt24(ReadOnlySpan<byte> source)
        {
            if (source.Length < 3)
                throw new ArgumentException("source must longer than 3 bytes", nameof(source));

            const int mask = -16777216;
            var buffer = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                buffer[0] = 0;
                buffer[1] = source[0];
                buffer[2] = source[1];
                buffer[3] = source[2];

                var value = BinaryPrimitives.ReadInt32BigEndian(buffer);

                if ((value & 0x00800000) > 0)
                    value |= mask;
                else
                    value &= ~mask;

                return value;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static void WriteInt24(Span<byte> destination, int value)
        {
            if (value is > 8388607 or < -8388608)
                throw new ArgumentOutOfRangeException(nameof(value), "int24 should be between -8388608 and 8388607");

            var buffer = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                BinaryPrimitives.WriteInt32BigEndian(buffer, value);

                destination[0] = buffer[1];
                destination[1] = buffer[2];
                destination[2] = buffer[3];
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
