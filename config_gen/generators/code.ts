import { ConfigEntry, ConfigEntryType } from "../types"
import builderCore from "./codeCore"
import builderCli from "./codeCli"
import builderSchema from "./codeSchema"
import builderWeb from "./codeWeb"
import { data } from "../data"
import { resolve } from "path"
import { writeFileSync } from "fs"
import { spawn } from "child_process"
import { stderr, stdout } from "process"

interface SectionInfoMap {
    [key: string]: SectionInfo
}

interface SectionInfo {
    readonly path: string
    readonly format: boolean,
    readonly header: boolean,
    readonly build: ((data: ConfigEntry[]) => string)
}

const map: SectionInfoMap = {
    core: {
        path: './BililiveRecorder.Core/Config/V3/Config.gen.cs',
        format: true,
        header: true,
        build: builderCore
    },
    cli: {
        path: './BililiveRecorder.Cli/Configure/ConfigInstructions.gen.cs',
        format: true,
        header: true,
        build: builderCli
    },
    web: {
        path: './BililiveRecorder.Web/Models/Config.gen.cs',
        format: true,
        header: true,
        build: builderWeb
    },
    schema: {
        path: './configV3.schema.json',
        format: false,
        header: false,
        build: builderSchema
    }
}

const HEADER = `// ******************************
//  GENERATED CODE, DO NOT EDIT MANUALLY.
//  SEE /config_gen/README.md
// ******************************\n\n`

export default function code(sections: string[]): void {
    let formatList: string[] = [];
    for (let i = 0; i < sections.length; i++) {
        const info = map[sections[i]]
        if (!info) continue;

        let text = info.build(data)

        if (info.header)
            text = HEADER + text

        const fullPath = resolve(__dirname, "../..", info.path)

        writeFileSync(fullPath, text, { encoding: 'utf8' })

        if (info.format)
            formatList.push(info.path)
    }

    if (formatList.length > 0) {
        console.log("[node] formatting...")

        let format = spawn('dotnet',
            [
                'tool',
                'run',
                'dotnet-format',
                '--',
                '--include',
                ...formatList
            ],
            {
                cwd: resolve(__dirname, "../..")
            })

        format.stdout.on('data', function (data) {
            stdout.write('[dotnet-format] ' + data.toString());
        });

        format.stderr.on('data', function (data) {
            stderr.write('[dotnet-format] ' + data.toString());
        });

        format.on('exit', function (code) {
            console.log('[node] format done, exit code: ' + code);
        });
    }
}
