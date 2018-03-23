using System;
using System.Linq;

namespace BililiveRecorder.FlvProcessor
{
    internal static class Utils
    {
        /// <summary>
        /// 转换字节序。实际上通常是把 BE 转成 LE
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static byte[] ToBE(this byte[] b)
        {
            if (BitConverter.IsLittleEndian)
                return b.Reverse().ToArray();
            else
                return b;

        }
    }
}
