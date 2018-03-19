using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BililiveRecorder.Core
{
    public class RecordInfo
    {
        private static readonly Random random = new Random();

        public DirectoryInfo SavePath;

        public string StreamFilePrefix = "录制";
        public string ClipFilePrefix = "片段";

        public string StreamName = "某直播间";

        public string GetStreamFilePath()
            => Path.Combine(SavePath.FullName, $@"{StreamFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv");

        public string GetClipFilePath()
            => Path.Combine(SavePath.FullName, $@"{ClipFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv");

        public RecordInfo() { }

    }
}
