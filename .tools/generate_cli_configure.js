export default function generate_cli_configure(data) {
    let result = `using System.Collections.Generic;
using System.ComponentModel;
using BililiveRecorder.Core.Config.V2;

namespace BililiveRecorder.Cli.Configure
{`;

    result += `
public enum GlobalConfigProperties
{
    [Description("[grey]Exit[/]")]
    Exit,
    ${data.global.concat(data.room.filter(x => !x.without_global)).map(x => x.name).join(",\n")}
}`;

    result += `
public enum RoomConfigProperties
{
    [Description("[grey]Exit[/]")]
    Exit,
    ${data.room.map(x => x.name).join(",\n")}
}`;


    result += `
public static class ConfigInstructions
{
public static Dictionary<GlobalConfigProperties, ConfigInstructionBase<GlobalConfig>> GlobalConfig = new();
public static Dictionary<RoomConfigProperties, ConfigInstructionBase<RoomConfig>> RoomConfig = new();

static ConfigInstructions()
{
    ${data.global
            .concat(data.room.filter(x => !x.without_global))
            .map(r => `GlobalConfig.Add(GlobalConfigProperties.${r.name}, new ConfigInstruction<GlobalConfig, ${r.type}>(config => config.Has${r.name} = false, (config, value) => config.${r.name} = value) { Name = "${r.name}", CanBeOptional = true });`)
            .join("\n")
        }

    ${data.room.map(r => `RoomConfig.Add(RoomConfigProperties.${r.name}, new ConfigInstruction<RoomConfig, ${r.type}>(config => config.Has${r.name} = false, (config, value) => config.${r.name} = value) { Name = "${r.name}", CanBeOptional = ${!r.without_global} });`).join("\n")}
}
}
`

    result += `\n}\n`;
    return result;
}
