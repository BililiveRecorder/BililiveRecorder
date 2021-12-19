import { ConfigEntry, ConfigEntryType } from "../types"
import { trimEnd } from "../utils";

export default function (data: ConfigEntry[]): string {
    let result = `using System.Collections.Generic;
    using System.ComponentModel;
    using BililiveRecorder.Core.Config;
    using BililiveRecorder.Core.Config.V3;
    
    namespace BililiveRecorder.Cli.Configure
    {`;

    result += `
    public enum GlobalConfigProperties
    {
        [Description("[grey]Exit[/]")]
        Exit,
        ${data.filter(x => x.configType != 'roomOnly').map(x => x.name).join(",\n")}
    }`;

    result += `
    public enum RoomConfigProperties
    {
        [Description("[grey]Exit[/]")]
        Exit,
        ${data.filter(x => x.configType != 'globalOnly').map(x => x.name).join(",\n")}
    }`;


    result += `
    public static class ConfigInstructions
    {
    public static Dictionary<GlobalConfigProperties, ConfigInstructionBase<GlobalConfig>> GlobalConfig = new();
    public static Dictionary<RoomConfigProperties, ConfigInstructionBase<RoomConfig>> RoomConfig = new();
    
    static ConfigInstructions()
    {
        ${data
            .filter(x => x.configType != 'roomOnly')
            .map(r => `GlobalConfig.Add(GlobalConfigProperties.${r.name}, new ConfigInstruction<GlobalConfig, ${trimEnd(r.type, '?')}>(config => config.Has${r.name} = false, (config, value) => config.${r.name} = value) { Name = "${r.name}", CanBeOptional = true });`)
            .join("\n")
        }
    
        ${data.filter(x => x.configType != 'globalOnly').map(r => `RoomConfig.Add(RoomConfigProperties.${r.name}, new ConfigInstruction<RoomConfig, ${trimEnd(r.type, '?')}>(config => config.Has${r.name} = false, (config, value) => config.${r.name} = value) { Name = "${r.name}", CanBeOptional = ${r.configType != 'roomOnly'} });`).join("\n")}
    }
    }
    `

    result += `\n}\n`;
    return result;
}
