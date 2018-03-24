using NLog;
using System;
using System.Linq;
using System.Net.Sockets;

namespace BililiveRecorder.Core
{
    public static class Utils
    {
        internal static byte[] ToBE(this byte[] b)
        {
            if (BitConverter.IsLittleEndian) return b.Reverse().ToArray(); else return b;
        }

        internal static void ReadB(this NetworkStream stream, byte[] buffer, int offset, int count)
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

        public static void ApplyTo(this Settings val1, Settings val2)
        {
            foreach (var p in val1.GetType().GetProperties())
            {
                var val = p.GetValue(val1);
                if (!val.Equals(p.GetValue(val2)))
                    p.SetValue(val2, val);
            }
        }

        internal static void Log(this Logger logger, int id, LogLevel level, string message, Exception exception = null)
        {
            var log = new LogEventInfo()
            {
                Level = level,
                Message = message,
                Exception = exception,
            };
            log.Properties["roomid"] = id;
            logger.Log(log);
        }
    }
}
