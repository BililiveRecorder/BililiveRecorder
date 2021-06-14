using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF
{
    public class WorkDirectoryLoader
    {
        private static readonly ILogger logger = Log.ForContext<WorkDirectoryLoader>();

        private const string fileName = "path.json";
        private readonly string basePath;
        private readonly string filePath;

        public WorkDirectoryLoader()
        {
            var exePath = Assembly.GetEntryAssembly().Location;
            this.basePath = string.IsNullOrWhiteSpace(exePath) ? Environment.CurrentDirectory : Path.GetDirectoryName(exePath);

            if (Regex.IsMatch(this.basePath, @"^.*\\app-\d\.\d\.\d\\?$") && File.Exists(Path.Combine(this.basePath, "..", "Update.exe")))
                this.basePath = Path.Combine(this.basePath, "..");

            this.basePath = Path.GetFullPath(this.basePath);
            this.filePath = Path.Combine(this.basePath, fileName);
        }

        public WorkDirectoryData Read()
        {
            try
            {
                if (!File.Exists(this.filePath))
                {
                    logger.Debug("Path file {FilePath} does not exist", this.filePath);
                    return new WorkDirectoryData();
                }
                else
                {
                    logger.Debug("Reading path file from {FilePath}.", this.filePath);
                    var str = File.ReadAllText(this.filePath);
                    logger.Debug("Path file content: {Content}", str);
                    var obj = JsonConvert.DeserializeObject<WorkDirectoryData>(str);
                    return obj ?? new WorkDirectoryData();
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error reading path file");
                return new WorkDirectoryData();
            }
        }

        public void Write(WorkDirectoryData data)
        {
            try
            {
                logger.Debug("Writing path file at {FilePathj}", this.filePath);
                var str = JsonConvert.SerializeObject(data);
                Core.Config.ConfigParser.WriteAllTextWithBackup(this.filePath, str);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error writing path file at {FilePathj}", this.filePath);
            }
        }

        public class WorkDirectoryData
        {
            public string Path { get; set; } = string.Empty;
            public bool SkipAsking { get; set; }
        }
    }
}
