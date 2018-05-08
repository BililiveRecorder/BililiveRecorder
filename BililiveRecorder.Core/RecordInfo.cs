using System;
using System.IO;

namespace BililiveRecorder.Core
{
    public class RecordInfo
    {
        private static readonly Random random = new Random();

        public string SavePath;

        public string StreamFilePrefix = "录制";
        public string ClipFilePrefix = "剪辑";

        public string StreamName = "某直播间";

        public string GetStreamFilePath()
            => RemoveInvalidFileName(Path.Combine(SavePath, $@"{StreamFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv"));

        public string GetClipFilePath()
            => RemoveInvalidFileName(Path.Combine(SavePath, $@"{ClipFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv"));

        private static string RemoveInvalidFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public RecordInfo(string name)
        {
            StreamName = name;
        }

    }
}
