using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv
{
    internal static class StreamExtensions
    {
        private const int BUFFER_SIZE = 4 * 1024;
        private static readonly ThreadLocal<byte[]> t_buffer = new ThreadLocal<byte[]>(() => new byte[BUFFER_SIZE]);

        internal static async Task<int> SkipBytesAsync(this Stream stream, int length)
        {
            if (length < 0) { throw new ArgumentOutOfRangeException("length must be non-negative"); }
            if (length == 0) { return 0; }
            if (null == stream) { throw new ArgumentNullException(nameof(stream)); }
            if (!stream.CanRead) { throw new ArgumentException("cannot read stream", nameof(stream)); }

            if (stream.CanSeek)
            {
                if (stream.Length - stream.Position >= length)
                {
                    stream.Position += length;
                    return length;
                }
                return 0;
            }

            var buffer = t_buffer.Value!;
            var total = 0;

            while (length > BUFFER_SIZE)
            {
                var read = await stream.ReadAsync(buffer, 0, BUFFER_SIZE);
                total += read;
                if (read != BUFFER_SIZE) { return total; }
                length -= BUFFER_SIZE;
            }

            total += await stream.ReadAsync(buffer, 0, length);
            return total;
        }

        internal static async Task<bool> CopyBytesAsync(this Stream from, Stream to, long length)
        {
            if (null == from) { throw new ArgumentNullException(nameof(from)); }
            if (null == to) { throw new ArgumentNullException(nameof(to)); }
            if (length < 0) { throw new ArgumentOutOfRangeException("length must be non-negative"); }
            if (length == 0) { return true; }
            if (!from.CanRead) { throw new ArgumentException("cannot read stream", nameof(from)); }
            if (!to.CanWrite) { throw new ArgumentException("cannot write stream", nameof(to)); }

            var buffer = t_buffer.Value!;

            while (length > BUFFER_SIZE)
            {
                if (BUFFER_SIZE != await from.ReadAsync(buffer, 0, BUFFER_SIZE))
                {
                    return false;
                }
                await to.WriteAsync(buffer, 0, BUFFER_SIZE);
                length -= BUFFER_SIZE;
            }

            if (length != await from.ReadAsync(buffer, 0, (int)length))
            {
                return false;
            }

            await to.WriteAsync(buffer, 0, (int)length);

            return true;
        }

        internal static async Task<bool> CopyBytesAsync(this Stream stream, byte[] target)
        {
            if (null == stream) { throw new ArgumentNullException(nameof(stream)); }
            if (null == target) { throw new ArgumentNullException(nameof(target)); }
            if (!stream.CanRead) { throw new ArgumentException("cannot read stream", nameof(stream)); }
            if (target.Length < 1) return true;

            var head = 0;
            while (head < target.Length)
            {
                var read = await stream.ReadAsync(target, head, target.Length - head);
                head += read;
                if (read == 0)
                    return false;
            }

            return true;
        }

        internal static byte[] ToBE(this byte[] b)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        internal static bool SequenceEqual(this Stream self, Stream? other)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (self == other)
                return true;

            if (self.Length != (other?.Length ?? -1))
                return false;

            self.Seek(0, SeekOrigin.Begin);
            other!.Seek(0, SeekOrigin.Begin);

            int b;
            while ((b = self.ReadByte()) != -1)
                if (b != other!.ReadByte())
                    return false;

            return true;
        }
    }
}
