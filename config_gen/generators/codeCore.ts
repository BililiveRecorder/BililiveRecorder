import { ConfigEntry, ConfigEntryType } from "../types"
import { trimEnd } from "../utils";
import { getConfigDefaultValueText } from "../utils";

export default function (data: ConfigEntry[]): string {
    let result = `using System.ComponentModel;
using HierarchicalPropertyDefault;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V3
{
`;

    function write_property(r: ConfigEntry) {
        result += `/// <summary>\n/// ${r.name}\n/// </summary>\n`;
        result += `public ${r.type} ${r.id} { get => this.GetPropertyValue<${trimEnd(r.type, '?')}>(); set => this.SetPropertyValue(value); }\n`;
        result += `public bool Has${r.id} { get => this.GetPropertyHasValue(nameof(this.${r.id})); set => this.SetPropertyHasValue<${trimEnd(r.type, '?')}>(value, nameof(this.${r.id})); }\n`;
        result += `[JsonProperty(nameof(${r.id})), EditorBrowsable(EditorBrowsableState.Never)]\n`;
        result += `public Optional<${r.type}> Optional${r.id} { get => this.GetPropertyValueOptional<${trimEnd(r.type, '?')}>(nameof(this.${r.id})); set => this.SetPropertyValueOptional(value, nameof(this.${r.id})); }\n\n`;
    }

    function write_readonly_property(r: ConfigEntry) {
        result += `/// <summary>\n/// ${r.name}\n/// </summary>\n`;
        result += `public ${r.type} ${r.id} => this.GetPropertyValue<${trimEnd(r.type, '?')}>();\n\n`;
    }

    {
        result += "[JsonObject(MemberSerialization.OptIn)]\n";
        result += "public sealed partial class RoomConfig : HierarchicalObject<GlobalConfig, RoomConfig>\n";
        result += "{\n";

        data.filter(x => x.configType != 'globalOnly').forEach(r => write_property(r));
        data.filter(x => x.configType == 'globalOnly').forEach(r => write_readonly_property(r));

        result += "}\n\n";
    }

    {
        result += "[JsonObject(MemberSerialization.OptIn)]\n";
        result += "public sealed partial class GlobalConfig : HierarchicalObject<DefaultConfig, GlobalConfig>\n";
        result += "{\n";

        data.filter(x => x.configType != 'roomOnly').forEach(r => write_property(r));

        result += "}\n\n";
    }

    {
        result += `public sealed partial class DefaultConfig
{    
public static readonly DefaultConfig Instance = new DefaultConfig();
private DefaultConfig() {}\n\n`;

        data
            .filter(x => x.configType != 'roomOnly')
            .forEach(r => {
                result += `public ${trimEnd(r.type, '?')} ${r.id} => ${getConfigDefaultValueText(r)};\n\n`;
            });

        result += "}\n\n";
    }

    result += `}\n`;
    return result;
}
