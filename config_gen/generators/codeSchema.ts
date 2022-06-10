import { ConfigEntry, ConfigEntryType } from "../types"

function mapTypeToJsonSchema(id: string, type: string, defaultValue: any) {
    switch (type) {
        case "RecordMode":
            return { type: "integer", default: 0, enum: [0, 1], "description": "0: Standard\n1: Raw" };
        case "CuttingMode":
            return { type: "integer", default: 0, enum: [0, 1, 2], "description": "0: 禁用\n1: 根据时间切割\n2: 根据文件大小切割" };
        case "AllowedAddressFamily":
            return { type: "integer", default: 0, enum: [-1, 0, 1, 2], "description": "-1: 由系统决定\n0: 任意 IPv4 或 IPv6\n1: 仅 IPv4\n2: IPv6" };
        case "uint":
            return { type: "integer", minimum: 0, maximum: 4294967295, default: defaultValue };
        case "int":
            return { type: "integer", minimum: -2147483648, maximum: 2147483647, default: defaultValue };
        case "bool":
            return { type: "boolean", default: defaultValue };
        case "string":
        case "string?":
            if (id === 'Cookie') {
                return { type: "string", pattern: "^(\S+=\S+;? ?)*$", maxLength: 4096, };
            }
            return { type: "string", default: defaultValue };
        default:
            return { type, default: defaultValue };
    }
}

function buildProperty(target: { [i: string]: any }, config: ConfigEntry) {
    const typeObj = mapTypeToJsonSchema(config.id, config.type, config.default);

    if (config.default === 'default')
        delete typeObj['default'];

    target[config.id] = {
        description: config.name
            + '\n默认: ' + config.default,
        markdownDescription: config.name
            + '  \n默认: `' + config.default
            + ' `\n\n',
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
                "default": "https://raw.githubusercontent.com/BililiveRecorder/BililiveRecorder/dev/configV3.schema.json"
            },
            "version": {
                "const": 3
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
