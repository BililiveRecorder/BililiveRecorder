function tryEvalValue(str) {
    try {
        return eval(str);
    } catch {
        return str;
    }
}

function mapTypeToJsonSchema(name, type, defVal) {
    switch (type) {
        case "RecordMode":
            return { type: "integer", default: 0, enum: [0, 1], "description": "0: Standard\n1: Raw" };
        case "CuttingMode":
            return { type: "integer", default: 0, enum: [0, 1, 2], "description": "0: 禁用\n1: 根据时间切割\n2: 根据文件大小切割" };
        case "uint":
            return { type: "integer", minimum: 0, maximum: 4294967295, default: tryEvalValue(defVal) };
        case "int":
            return { type: "integer", minimum: -2147483648, maximum: 2147483647, default: tryEvalValue(defVal) };
        case "bool":
            return { type: "boolean", default: tryEvalValue(defVal) };
        case "string":
            if (name === 'Cookie') {
                return { type: "string", pattern: "^(\S+=\S+;? ?)*$", maxLength: 4096, };
            }
            return { type: "string", default: defVal === 'string.Empty' ? '' : tryEvalValue(defVal.replace(/^@/, '')) };
        default:
            return { type, default: defVal };
    }
}

function insert(target, { name, type, desc, default: defVal/*, nullable */ }) {
    const typeObj = mapTypeToJsonSchema(name, type, defVal);
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

export default function generate_json_schema(data) {
    const sharedConfig = {};
    const globalConfig = {};
    const roomConfig = {};

    data.room.filter(x => !x.without_global).forEach(v => insert(sharedConfig, v));
    data.room.filter(x => x.without_global).forEach(v => insert(roomConfig, v));
    data.global.forEach(v => insert(globalConfig, v));

    const schema = {
        "$comment": "GENERATED CODE, DO NOT EDIT MANUALLY.",
        "$schema": "http://json-schema.org/schema",
        "definitions": {
            "global-config": {
                "description": "全局配置",
                "additionalProperties": false,
                "properties": { ...globalConfig, ...sharedConfig }
            },
            "room-config": {
                "description": "单个房间配置",
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
