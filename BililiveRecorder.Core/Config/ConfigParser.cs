using Newtonsoft.Json;
using System;
using System.IO;
using NLog;

namespace BililiveRecorder.Core.Config
{
    public static class ConfigParser
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static bool Load(string directory, ConfigV1 config = null)
        {
            if (!Directory.Exists(directory))
            {
                return false;
            }

            var filepath = Path.Combine(directory, "config.json");
            if (File.Exists(filepath))
            {
                try
                {
                    var cw = JsonConvert.DeserializeObject<ConfigWrapper>(File.ReadAllText(filepath));
                    switch (cw.Version)
                    {
                        case 1:
                            {
                                var v1 = JsonConvert.DeserializeObject<ConfigV1>(cw.Data);
                                v1.CopyPropertiesTo(config);
                                return true;
                                // (v1.ToV2()).CopyPropertiesTo(config);
                            }
                        /**
                         * case 2:
                         *     {
                         *         var v2 = JsonConvert.DeserializeObject<ConfigV2>(cw.Data);
                         *         v2.CopyPropertiesTo(config);
                         *         return true;
                         *     }
                         * */
                        default:
                            // version not supported
                            // TODO: return status enum
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to parse config!");
                    return false;
                }
            }
            else
            {
                new ConfigV1().CopyPropertiesTo(config);
                return true;
            }
        }

        public static bool Save(string directory, ConfigV1 config = null)
        {
            if (config == null) { config = new ConfigV1(); }
            if (!Directory.Exists(directory))
            {
                // User should create the directory
                // TODO: return enum
                return false;
            }
            var filepath = Path.Combine(directory, "config.json");
            try
            {
                var data = JsonConvert.SerializeObject(config);
                var cw = JsonConvert.SerializeObject(new ConfigWrapper()
                {
                    Version = 1,
                    Data = data
                });
                File.WriteAllText(filepath, cw);
                return true;
            }
            catch (Exception)
            {
                return false;
                // TODO: Log Exception
            }
        }
    }
}
