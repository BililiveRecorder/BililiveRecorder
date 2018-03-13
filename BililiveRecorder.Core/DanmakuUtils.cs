using System;
using System.Linq;
using System.Net.Sockets;

namespace BililiveRecorder.Core
{
    internal static class DanmakuUtils
    {
        public static byte[] ToBE(this byte[] b)
        {
            if (BitConverter.IsLittleEndian) return b.Reverse().ToArray(); else return b;
        }

        public static void ReadB(this NetworkStream stream, byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            int read = 0;
            while (read < count)
            {
                var available = stream.Read(buffer, offset, count - read);
                if (available == 0)
                {
                    throw new ObjectDisposedException(null);
                }
                read += available;
                offset += available;
            }
        }
    }
}
