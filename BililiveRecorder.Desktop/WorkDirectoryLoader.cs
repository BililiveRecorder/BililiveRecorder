using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop
{
    public class WorkDirectoryLoader
    {
        private static readonly ILogger logger = Log.ForContext<WorkDirectoryLoader>();
        private readonly string filePath;

        public WorkDirectoryLoader()
        {
            filePath = Path.Combine(AppContext.BaseDirectory, "path.json");
        }

        public PathInfo Read()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var info = JsonConvert.DeserializeObject<PathInfo>(json);
                    return info ?? new PathInfo();
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to read path.json");
            }
            return new PathInfo();
        }

        public void Write(PathInfo info)
        {
            try
            {
                var json = JsonConvert.SerializeObject(info, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to write path.json");
            }
        }

        public class PathInfo
        {
            public string Path { get; set; } = "";
            public bool SkipAsking { get; set; } = false;
        }
    }
}
