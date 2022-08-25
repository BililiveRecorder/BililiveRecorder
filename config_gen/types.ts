/** 设置类型 */
export type ConfigEntryType =
    /** 仅全局设置 */
    "globalOnly"
    /** 可单独房间设置或全局设置 */
    | "room"
    /** 只能单独房间设置 */
    | "roomOnly"

export type ConfigValueType =
    "string?"
    | "int"
    | "uint"
    | "bool"
    | "RecordMode"
    | "CuttingMode"
    | "AllowedAddressFamily"
    | "DanmakuTransportMode"

export interface ConfigEntry {
    /** 名字 */
    readonly id: string,
    /** 说明 */
    readonly name: string,
    /** 代码类型 */
    readonly type: ConfigValueType,
    /** 设置类型 */
    readonly configType: ConfigEntryType
    /** Web API 只读属性 */
    readonly webReadonly?: boolean,
    /** 是否为高级设置（隐藏设置），默认为 false */
    readonly advancedConfig?: boolean,
    /** 默认值 */
    readonly default: string | number | boolean,
}
