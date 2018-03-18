using System;
using System.Linq;

namespace BililiveRecorder.FlvProcessor
{
    internal static class Utils
    {
        internal static byte[] ToBE(this byte[] b)
        {
            if (BitConverter.IsLittleEndian)
                return b.Reverse().ToArray();
            else
                return b;

        }
    }
}
