import { ConfigEntry, ConfigEntryType } from "../types"

function tryEvalValue(str: string) {
    try {
        return eval(str);
    } catch {
        return str;
    }
}
const mapCSharpString = (text: string): string => text === 'string.Empty' ? '' : tryEvalValue(text.replace(/^@/, ''));

function mapTypeToJsonSchema(name: string, type: string, defaultValue: string) {
    switch (type) {
        case "RecordMode":
            return { type: "integer", default: 0, enum: [0, 1], "description": "0: Standard\n1: Raw" };
        case "CuttingMode":
            return { type: "integer", default: 0, enum: [0, 1, 2], "description": "0: 禁用\n1: 根据时间切割\n2: 根据文件大小切割" };
        case "uint":
            return { type: "integer", minimum: 0, maximum: 4294967295, default: tryEvalValue(defaultValue) };
        case "int":
            return { type: "integer", minimum: -2147483648, maximum: 2147483647, default: tryEvalValue(defaultValue) };
        case "bool":
            return { type: "boolean", default: tryEvalValue(defaultValue) };
        case "string":
        case "string?":
            if (name === 'Cookie') {
                return { type: "string", pattern: "^(\S+=\S+;? ?)*$", maxLength: 4096, };
            }
            return { type: "string", default: mapCSharpString(defaultValue) };
        default:
            return { type, default: defaultValue };
    }
}

function buildProperty(target: { [i: string]: any }, config: ConfigEntry) {
    const typeObj = mapTypeToJsonSchema(config.name, config.type, config.defaultValue);

    if (config.defaultValue === 'default')
        delete typeObj['default'];

    target[config.name] = {
        description: config.description
            + '\n默认: ' + (!config.defaultValueDescription ? mapCSharpString(config.defaultValue) : config.defaultValueDescription),
        markdownDescription: config.description
            + '  \n默认: `' + (!config.defaultValueDescription ? mapCSharpString(config.defaultValue) : config.defaultValueDescription)
            + ' `\n\n' + config.markdown,
        type: "object",
        additionalProperties: false,
        properties: {
            HasValue: {
                type: "boolean",
                default: true
            },
            Value: typeObj
        }
    };
}

export default function (data: ConfigEntry[]): string {
    const sharedConfig = {};
    const globalConfig = {};
    const roomConfig = {};

    data.filter(x => x.configType == 'room').forEach(v => buildProperty(sharedConfig, v));
    data.filter(x => x.configType == 'roomOnly').forEach(v => buildProperty(roomConfig, v));
    data.filter(x => x.configType == 'globalOnly').forEach(v => buildProperty(globalConfig, v));

    const schema = {
        "$comment": "GENERATED CODE, DO NOT EDIT MANUALLY.",
        "$schema": "http://json-schema.org/schema",
        "definitions": {
            "global-config": {
                "description": "全局设置",
                "additionalProperties": false,
                "properties": { ...globalConfig, ...sharedConfig }
            },
            "room-config": {
                "description": "房间独立设置",
                "additionalProperties": false,
                "properties": { ...roomConfig, ...sharedConfig }
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
                "default": "https://raw.githubusercontent.com/Bililive/BililiveRecorder/dev-1.3/configV2.schema.json"
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
    }

    return JSON.stringify(schema, null, 2)
}
