/** 设置类型 */
export type ConfigEntryType =
    /** 仅全局设置 */
    "globalOnly"
    /** 可单独房间设置或全局设置 */
    | "room"
    /** 只能单独房间设置 */
    | "roomOnly"

export interface ConfigEntry {
    /** 名字 */
    readonly name: string,
    /** 说明 */
    readonly description: string,
    /** 代码类型 */
    readonly type: string,
    /** 设置类型 */
    readonly configType: ConfigEntryType
    /** Web API 只读属性 */
    readonly webReadonly?: boolean,
    /** 是否为高级设置（隐藏设置），默认为 false */
    readonly advancedConfig?: boolean,
    /** 默认值 */
    readonly defaultValue: string,
    /** 文档显示用默认值，默认使用 defaultValue */
    readonly defaultValueDescription?: string,
    /** XML 注释，默认使用 description */
    readonly xmlComment?: string,
    /** Markdown 格式的说明文档 */
    readonly markdown: string,
}