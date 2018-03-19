using System;
using System.Linq;
using System.Net.Sockets;

namespace BililiveRecorder.Core
{
    internal static class Utils
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

        public static void ApplyTo<T>(this T val1, T val2)
        {
            foreach (var p in val1.GetType().GetProperties())
            {
                var val = p.GetValue(val1);
                if (!val.Equals(p.GetValue(val2)))
                    p.SetValue(val2, val);
            }
        }
    }
}
