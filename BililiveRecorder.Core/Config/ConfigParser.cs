using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NLog;

#nullable enable
namespace BililiveRecorder.Core.Config
{
    public static class ConfigParser
    {
        private const string CONFIG_FILE_NAME = "config.json";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static V2.ConfigV2? LoadFrom(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return null;

                var filepath = Path.Combine(directory, CONFIG_FILE_NAME);

                if (!File.Exists(filepath))
                {
                    logger.Debug("Config file does not exist. \"{path}\"", filepath);
                    return new V2.ConfigV2();
                }

                logger.Debug("Loading config from path \"{path}\".", filepath);
                var json = File.ReadAllText(filepath, Encoding.UTF8);
                return LoadJson(json);

            }
            catch (Exception ex)
            {
                logger.Error(ex, "从文件加载设置时出错");
                return null;
            }
        }

        public static V2.ConfigV2? LoadJson(string json)
        {
            try
            {
                logger.Debug("Config json: {config}", json);

                var configBase = JsonConvert.DeserializeObject<ConfigBase>(json);
                switch (configBase)
                {
                    case V1.ConfigV1Wrapper v1:
                        {
                            logger.Debug("读取到 config v1");
#pragma warning disable CS0612
                            var v1Data = JsonConvert.DeserializeObject<V1.ConfigV1>(v1.Data);
#pragma warning restore CS0612
                            var newConfig = ConfigMapper.Map1To2(v1Data);

                            return newConfig;
                        }
                    case V2.ConfigV2 v2:
                        logger.Debug("读取到 config v2");
                        return v2;
                    default:
                        logger.Error("读取到不支持的设置版本");
                        return null;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "解析设置时出错");
                return null;
            }
        }

        public static bool SaveTo(string directory, V2.ConfigV2 config)
        {
            if (config.DisableConfigSave)
            {
                logger.Debug("Skipping write config because DisableConfigSave is true.");
                return true;
            }

            var json = SaveJson(config);
            try
            {
                if (!Directory.Exists(directory))
                    return false;

                var filepath = Path.Combine(directory, CONFIG_FILE_NAME);

                if (json is not null)
                    WriteAllTextWithBackup(filepath, json);

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "保存设置时出错（写入文件）");
                return false;
            }
        }

        public static string? SaveJson(V2.ConfigV2 config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);
                return json;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "保存设置时出错（序列化）");
                return null;
            }
        }

        // https://stackoverflow.com/q/25366534 with modification
        private static void WriteAllTextWithBackup(string path, string contents)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, contents);
                return;
            }

            var ext = Path.GetExtension(path);

            var tempPath = Path.Combine(Path.GetDirectoryName(path), Path.ChangeExtension(path, RandomString(6) + ext));
            var backupPath = Path.ChangeExtension(path, "backup" + ext);

            // delete any existing backups
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            // get the bytes
            var data = Encoding.UTF8.GetBytes(contents);

            // write the data to a temp file
            using (var tempFile = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                tempFile.Write(data, 0, data.Length);

            // replace the contents
            File.Replace(tempPath, path, backupPath);
        }
        private static readonly Random random = new Random();
        private static string RandomString(int length) => new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length).Select(s => s[random.Next(s.Length)]).ToArray());

    }
}
