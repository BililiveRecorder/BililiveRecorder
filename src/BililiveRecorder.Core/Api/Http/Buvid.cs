using System;
using System.Text;

namespace BililiveRecorder.Core.Api.Http
{
    internal class Buvid
    {
        private static readonly Random _random = new Random();

        public static string GenerateLocalId()
        {
            var sb = new StringBuilder(46);
            const string CHARS = "0123456789ABCDEF";

            for (var i = 0; i < 8; i++) sb.Append(CHARS[_random.Next(CHARS.Length)]);
            sb.Append('-');
            for (var i = 0; i < 4; i++) sb.Append(CHARS[_random.Next(CHARS.Length)]);
            sb.Append('-');
            for (var i = 0; i < 4; i++) sb.Append(CHARS[_random.Next(CHARS.Length)]);
            sb.Append('-');
            for (var i = 0; i < 4; i++) sb.Append(CHARS[_random.Next(CHARS.Length)]);
            sb.Append('-');
            for (var i = 0; i < 12; i++) sb.Append(CHARS[_random.Next(CHARS.Length)]);

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            sb.Append((now % 100000).ToString("D5"));
            sb.Append("infoc");

            return sb.ToString();
        }
    }
}
