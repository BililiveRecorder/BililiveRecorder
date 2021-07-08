"use strict";
const fs = require("fs");
const data = require("./build_config.data.js");

const CODE_HEADER =
    `// ******************************
//  GENERATED CODE, DO NOT EDIT.
//  RUN FORMATTER AFTER GENERATE
// ******************************
using System.ComponentModel;
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

console.log("记得 format Config.gen.cs")

/** 进行一个json schema的生成 */
const sharedConfig = {};
const globalConfig = {};
const roomConfig = {};
function tEval(str) {
    try {
        return eval(str);
    } catch {
        return str;
    }
}
function switchType(name, type, defVal) {
    switch (type) {
        case "RecordMode":
            return { type: "integer", default: 0, enum: [0, 1], "description": "0: Standard\n1: Raw" };
        case "CuttingMode":
            return { type: "integer", default: 0, enum: [0, 1, 2], "description": "0: 禁用\n1: 根据时间切割\n2: 根据文件大小切割" };
        case "uint":
            return { type: "integer", minimum: 0, maximum: 4294967295, default: tEval(defVal) };
        case "int":
            return { type: "integer", minimum: -2147483648, maximum: 2147483647, default: tEval(defVal) };
        case "bool":
            return { type: "boolean", default: tEval(defVal) };
        case "string":
            if (name === 'Cookie') {
                return { type: "string", pattern: "^(\S+=\S+;? ?)*$", maxLength: 4096, };
            }
            return { type: "string", default: defVal === 'string.Empty' ? '' : tEval(defVal.replace(/^@/, '')) };
        default:
            return { type, default: defVal };
    }
}
function insert(target, { name, type, desc, default: defVal/*, nullable */ }) {
    const typeObj = switchType(name, type, defVal);
    if (defVal === 'default') delete typeObj['default'];
    target[name] = {
        "description": desc,
        "type": "object",
        "additionalProperties": false,
        "properties": {
            "HasValue": { "type": "boolean", "default": true }, "Value": typeObj
        }
    };
}
data.room.filter(x => !x.without_global).forEach(v => insert(sharedConfig, v));
data.room.filter(x => x.without_global).forEach(v => insert(roomConfig, v));
data.global.forEach(v => insert(globalConfig, v));


fs.writeFileSync("./config.schema.json", "// GENERATED CODE, DO NOT EDIT.\n" + JSON.stringify({
    "$schema": "http://json-schema.org/schema",
    "definitions": {
        "global-config": {
            "description": "全局配置",
            "additionalProperties": false,
            "properties": { ...sharedConfig, ...globalConfig }
        },
        "room-config": {
            "description": "单个房间配置",
            "additionalProperties": false,
            "properties": { ...sharedConfig, ...roomConfig }
        }
    },
    "type": "object",
    "additionalProperties": false,
    "required": [
        "$schema",
        "version"
    ],
    "properties": {
        "$schema": {
            "type": "string",
            "default": "https://raw.githubusercontent.com/Bililive/BililiveRecorder/dev-1.3/BililiveRecorder.Core/Config/V2/config.schema.json"
        },
        "version": {
            "const": 2
        },
        "global": {
            "$ref": "#/definitions/global-config"
        },
        "rooms": {
            "type": "array",
            "items": {
                "$ref": "#/definitions/room-config"
            }
        }
    }
}, null, 4));
