using System;
using System.IO;
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
            var json = SaveJson(config);
            try
            {
                if (!Directory.Exists(directory))
                    return false;

                var filepath = Path.Combine(directory, CONFIG_FILE_NAME);

                if (json is not null)
                    File.WriteAllText(filepath, json, Encoding.UTF8);

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
    }
}
