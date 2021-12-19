import { ConfigEntry, ConfigEntryType } from "../types"
import { trimEnd } from "../utils";

export default function (data: ConfigEntry[]): string {
    let result = `using System.ComponentModel;
using HierarchicalPropertyDefault;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V3
{
`;

    function write_property(r: ConfigEntry) {
        result += `/// <summary>\n/// ${r.xmlComment ?? r.description}\n/// </summary>\n`;
        result += `public ${r.type} ${r.name} { get => this.GetPropertyValue<${trimEnd(r.type, '?')}>(); set => this.SetPropertyValue(value); }\n`;
        result += `public bool Has${r.name} { get => this.GetPropertyHasValue(nameof(this.${r.name})); set => this.SetPropertyHasValue<${trimEnd(r.type, '?')}>(value, nameof(this.${r.name})); }\n`;
        result += `[JsonProperty(nameof(${r.name})), EditorBrowsable(EditorBrowsableState.Never)]\n`;
        result += `public Optional<${r.type}> Optional${r.name} { get => this.GetPropertyValueOptional<${trimEnd(r.type, '?')}>(nameof(this.${r.name})); set => this.SetPropertyValueOptional(value, nameof(this.${r.name})); }\n\n`;
    }

    function write_readonly_property(r: ConfigEntry) {
        result += `/// <summary>\n/// ${r.xmlComment ?? r.description}\n/// </summary>\n`;
        result += `public ${r.type} ${r.name} => this.GetPropertyValue<${trimEnd(r.type, '?')}>();\n\n`;
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
                result += `public ${trimEnd(r.type, '?')} ${r.name} => ${r.defaultValue};\n\n`;
            });

        result += "}\n\n";
    }

    result += `}\n`;
    return result;
}
