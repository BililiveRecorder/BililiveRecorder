using BililiveRecorder.Core.Config;
using System;
using System.IO;

namespace BililiveRecorder.Core
{
    public class RecordInfo : IRecordInfo
    {
        private static readonly Random random = new Random();

        private ConfigV1 Config { get; }
        public string SavePath { get => Config.WorkDirectory; }

        public string StreamFilePrefix { get; set; } = "录制";
        public string ClipFilePrefix { get; set; } = "剪辑";

        public string StreamName { get; set; } = "某直播间";

        public string GetStreamFilePath()
            => Path.Combine(SavePath, RemoveInvalidFileName($@"{StreamFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv"));

        public string GetClipFilePath()
            => Path.Combine(SavePath, RemoveInvalidFileName($@"{ClipFilePrefix}-{StreamName}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{random.Next(100, 999)}.flv"));

        private static string RemoveInvalidFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name;
        }

        public RecordInfo(string name, ConfigV1 config)
        {
            StreamName = name;
            Config = config;
        }

    }
}
