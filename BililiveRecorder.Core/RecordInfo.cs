using System;
using System.IO;

namespace BililiveRecorder.Core
{
    public class RecordInfo
    {
        private static readonly Random random = new Random();

        public string SavePath;

        public string StreamFilePrefix = "录制";
        public string ClipFilePrefix = "片段";

        public string StreamName = "某直播间";

        public string GetStreamFilePath()
            => Path.Combine(SavePath, $@"{StreamFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv");

        public string GetClipFilePath()
            => Path.Combine(SavePath, $@"{ClipFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv");

        public RecordInfo(string name)
        {
            StreamName = name;
        }

    }
}
