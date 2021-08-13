export function trimEnd(text: string, trimChar: string): string {
    return text.slice(-1) === trimChar
        ? text.slice(0, -1)
        : text;
}