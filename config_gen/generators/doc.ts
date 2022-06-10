import { ConfigEntry } from "../types"
import { statSync, writeFileSync } from "fs";
import { resolve } from "path"
import { data } from "../data";
import { trimEnd } from "../utils";

export default function doc(path: string): void {
    if (!statSync(resolve(path, 'mkdocs.yml'))) {
        console.error('Check your path');
        return;
    }
    if (!statSync(resolve(path, 'docs/user/settings.md'))) {
        console.error('Check your path');
        return;
    }

    const targetPath = resolve(path, 'data/brec_settings.json')

    const text = buildJson(data)

    writeFileSync(targetPath, text, { encoding: 'utf8' });
}

function buildJson(data: ConfigEntry[]): string {
    return JSON.stringify(data, null, 2);
}
