using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Infrastructure
{
    public enum StartResult
    {
        Success,
        PathNotSupported,
        PathDoesNotExist,
        PathContainsFiles,
        FailedToLoadConfig,
        AlreadyRunning,
        UnknownError,
    }

    /// <summary>
    /// 读取/保存上次选择的录制工作目录（exe 旁边的 path.json，与 WPF 版格式相同），
    /// 以及工作目录可用性校验。
    /// </summary>
    public class WorkDirectoryResolver
    {
        private static readonly ILogger logger = Log.ForContext<WorkDirectoryResolver>();

        private const string FileName = "path.json";
        private readonly string filePath = Path.Combine(AppContext.BaseDirectory, FileName);

        public WorkDirectoryData Read()
        {
            try
            {
                if (!File.Exists(this.filePath))
                {
                    logger.Debug("Path file {FilePath} does not exist", this.filePath);
                    return new WorkDirectoryData();
                }

                logger.Debug("Reading path file from {FilePath}.", this.filePath);
                var str = File.ReadAllText(this.filePath);
                var obj = JsonConvert.DeserializeObject<WorkDirectoryData>(str);
                return obj ?? new WorkDirectoryData();
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
                logger.Debug("Writing path file at {FilePath}", this.filePath);
                File.WriteAllText(this.filePath, JsonConvert.SerializeObject(data));
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error writing path file at {FilePath}", this.filePath);
            }
        }

        /// <summary>
        /// 按 WPF 版相同的顺序尝试自动选择目录：
        /// 命令行参数 > path.json 记住的路径（勾选了"不再询问"）> 当前目录。
        /// 都不可用时返回 WorkPath = null，由调用方显示目录选择窗口。
        /// </summary>
        public AutoPathResult TryGetAutoPath(string? pathArgument)
        {
            if (!string.IsNullOrWhiteSpace(pathArgument))
            {
                try
                {
                    return new AutoPathResult(Path.GetFullPath(pathArgument), FromArgument: true);
                }
                catch (Exception)
                {
                    // 参数路径无效，继续走后面的来源
                }
            }

            var info = this.Read();
            if (info.SkipAsking && !string.IsNullOrWhiteSpace(info.Path) && IsUsableDirectory(info.Path))
            {
                try
                {
                    return new AutoPathResult(Path.GetFullPath(info.Path), FromArgument: false);
                }
                catch (Exception) { }
            }

            if (IsUsableDirectory(Environment.CurrentDirectory))
                return new AutoPathResult(Environment.CurrentDirectory, FromArgument: false);

            return AutoPathResult.None;
        }

        /// <summary>目录存在，且是空文件夹或包含 config.json。</summary>
        public static bool IsUsableDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;

                if (!Directory.EnumerateFiles(path).Any(x => Path.GetFileName(x) != "desktop.ini"))
                    return true; // 可用的空文件夹

                return File.Exists(Path.Combine(path, "config.json"));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public readonly record struct AutoPathResult(string? WorkPath, bool FromArgument)
        {
            public static AutoPathResult None => default;
        }

        public class WorkDirectoryData
        {
            public string Path { get; set; } = string.Empty;
            public bool SkipAsking { get; set; }
        }
    }
}
