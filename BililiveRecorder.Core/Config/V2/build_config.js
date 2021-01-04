"use strict";
const fs = require("fs");
const data = require("./build_config.data.js");

const CODE_HEADER =
    `// ******************************
//  GENERATED CODE, DO NOT EDIT.
//  RUN FORMATTER AFTER GENERATE
// ******************************
using System.ComponentModel;
using BililiveRecorder.FlvProcessor;
using HierarchicalPropertyDefault;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V2
{
`;

const CODE_FOOTER = `}\n`;

let result = CODE_HEADER;

function write_property(r) {
    result += `/// <summary>\n/// ${r.desc}\n/// </summary>\n`
    result += `public ${r.type}${!!r.nullable ? "?" : ""} ${r.name} { get => this.GetPropertyValue<${r.type}>(); set => this.SetPropertyValue(value); }\n`
    result += `public bool Has${r.name} { get => this.GetPropertyHasValue(nameof(this.${r.name})); set => this.SetPropertyHasValue<${r.type}>(value, nameof(this.${r.name})); }\n`
    result += `[JsonProperty(nameof(${r.name})), EditorBrowsable(EditorBrowsableState.Never)]\n`
    result += `public Optional<${r.type}${!!r.nullable ? "?" : ""}> Optional${r.name} { get => this.GetPropertyValueOptional<${r.type}>(nameof(this.${r.name})); set => this.SetPropertyValueOptional(value, nameof(this.${r.name})); }\n\n`
}

function write_readonly_property(r) {
    result += `/// <summary>\n/// ${r.desc}\n/// </summary>\n`
    result += `public ${r.type}${!!r.nullable ? "?" : ""} ${r.name} => this.GetPropertyValue<${r.type}>();\n\n`
}

{
    result += "[JsonObject(MemberSerialization.OptIn)]\n"
    result += "public sealed partial class RoomConfig : HierarchicalObject<GlobalConfig, RoomConfig>\n"
    result += "{\n";

    data.room.forEach(r => write_property(r))
    data.global.forEach(r => write_readonly_property(r))

    result += "}\n\n"
}

{
    result += "[JsonObject(MemberSerialization.OptIn)]\n"
    result += "public sealed partial class GlobalConfig : HierarchicalObject<DefaultConfig, GlobalConfig>\n"
    result += "{\n";

    data.global
        .concat(data.room.filter(x => !x.without_global))
        .forEach(r => write_property(r))

    result += "}\n\n"
}

{
    result += `public sealed partial class DefaultConfig
    {    
    internal static readonly DefaultConfig Instance = new DefaultConfig();
    private DefaultConfig() {}\n\n`;

    data.global
        .concat(data.room.filter(x => !x.without_global))
        .forEach(r => {
            result += `public ${r.type} ${r.name} => ${r.default};\n\n`
        })

    result += "}\n\n"
}

result += CODE_FOOTER;

fs.writeFileSync("./Config.gen.cs", result, {
    encoding: "utf8"
});
