using System.Collections.Generic;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V3
{
    public sealed class ConfigV3 : ConfigBase
    {
        public override int Version => 3;

        [JsonProperty("global")]
        public GlobalConfig Global { get; set; } = new GlobalConfig();

        [JsonProperty("rooms")]
        public List<RoomConfig> Rooms { get; set; } = new List<RoomConfig>();

        // for CLI
        [JsonIgnore]
        public bool DisableConfigSave { get; set; } = false;

        // for CLI
        [JsonIgnore]
        public string? ConfigPathOverride { get; set; }
    }

    public partial class RoomConfig : IFileNameConfig
    {
        public RoomConfig() : base(x => x.AutoMap(p => new[] { "Has" + p.Name }))
        { }

        internal void SetParent(GlobalConfig? config) => this.Parent = config;

        public string? WorkDirectory => this.GetPropertyValue<string>();
    }

    public partial class GlobalConfig : IFileNameConfig
    {
        public GlobalConfig() : base(x => x.AutoMap(p => new[] { "Has" + p.Name }))
        {
            this.Parent = DefaultConfig.Instance;
        }

        /// <summary>
        /// 当前工作目录
        /// </summary>
        public string? WorkDirectory
        {
            get => this.GetPropertyValue<string>();
            set => this.SetPropertyValue(value);
        }
    }
}
