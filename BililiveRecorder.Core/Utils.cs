using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace BililiveRecorder.Core
{
    public static class Utils
    {
        internal static byte[] ToBE(this byte[] b)
        {
            if (BitConverter.IsLittleEndian)
            {
                return b.Reverse().ToArray();
            }
            else
            {
                return b;
            }
        }

        internal static void ReadB(this NetworkStream stream, byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException();
            }

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

        internal static string RemoveInvalidFileName(this string name, bool ignore_slash = false)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (ignore_slash && (c == '\\' || c == '/'))
                    continue;
                name = name.Replace(c, '_');
            }
            return name;
        }

        public static bool CopyPropertiesTo<T>(this T val1, T val2) where T : class
        {
            if (val1 == null || val2 == null || val1 == val2) { return false; }
            foreach (var p in val1.GetType().GetProperties())
            {
                if (Attribute.IsDefined(p, typeof(DoNotCopyProperty)))
                {
                    continue;
                }

                var val = p.GetValue(val1);
                if (!val.Equals(p.GetValue(val2)))
                {
                    p.SetValue(val2, val);
                }
            }
            return true;
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        public class DoNotCopyProperty : Attribute { }

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

        private static string _useragent;
        internal static string UserAgent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_useragent))
                {
                    string version = typeof(Utils).Assembly.GetName().Version.ToString();
                    _useragent = $"Mozilla/5.0 BililiveRecorder/{version} (+https://github.com/Bililive/BililiveRecorder;bliverec@danmuji.org)";
                }
                return _useragent;
            }
        }

    }
}
