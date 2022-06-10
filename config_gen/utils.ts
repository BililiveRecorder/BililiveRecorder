import { ConfigEntry } from "./types";

export function trimEnd(text: string, trimChar: string): string {
    return text.slice(-1) === trimChar
        ? text.slice(0, -1)
        : text;
}

export function getConfigDefaultValueText(config: ConfigEntry): string {
    if (config.type != "string?") {
        return config.default.toString();
    } else {
        return `@"${config.default.toString().replaceAll('"', '""')}"`;
    }
}
