using System;
using System.IO;

namespace BililiveRecorder.Flv
{
    internal static class BinaryConvertUtilities
    {
        private static readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("X2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        internal static string ByteArrayToHexString(byte[] bytes) => ByteArrayToHexString(bytes, 0, bytes.Length);

        internal static string ByteArrayToHexString(byte[] bytes, int start, int length)
        {
            var lookup32 = _lookup32;
            var result = new char[length * 2];
            for (var i = start; i < length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        internal static byte[] HexStringToByteArray(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        internal static string StreamToHexString(Stream stream)
        {
            var lookup32 = _lookup32;
            stream.Seek(0, SeekOrigin.Begin);
            var result = new char[stream.Length * 2];
            for (var i = 0; i < stream.Length; i++)
            {
                var val = lookup32[stream.ReadByte()];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        internal static MemoryStream HexStringToMemoryStream(string hex)
        {
            var stream = new MemoryStream(hex.Length / 2);
            for (var i = 0; i < hex.Length; i += 2)
                stream.WriteByte(Convert.ToByte(hex.Substring(i, 2), 16));
            return stream;
        }
    }
}
