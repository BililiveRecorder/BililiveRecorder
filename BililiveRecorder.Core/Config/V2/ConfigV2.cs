using System;
using System.Collections.Generic;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V2
{
    [Obsolete("Use Config v3")]
    internal class ConfigV2 : ConfigBase
    {
        public override int Version => 2;

        [JsonProperty("global")]
        public GlobalConfig Global { get; set; } = new GlobalConfig();

        [JsonProperty("rooms")]
        public List<RoomConfig> Rooms { get; set; } = new List<RoomConfig>();

        [JsonIgnore]
        public bool DisableConfigSave { get; set; } = false; // for CLI
    }

    [Obsolete("Use Config v3")]
    internal partial class RoomConfig
    {
        public RoomConfig() : base(x => x.AutoMap(p => new[] { "Has" + p.Name }))
        { }

        internal void SetParent(GlobalConfig? config) => this.Parent = config;

        public string? WorkDirectory => this.GetPropertyValue<string>();
    }

    [Obsolete("Use Config v3")]
    internal partial class GlobalConfig
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
