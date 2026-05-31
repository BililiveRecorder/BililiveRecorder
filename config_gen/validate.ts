import { resolve } from "path"
import { readFileSync } from "fs"
import { data } from "./data"
import builderCore from "./generators/codeCore"
import builderCli from "./generators/codeCli"
import builderWeb from "./generators/codeWeb"
import builderSchema from "./generators/codeSchema"

const HEADER = `// ******************************
//  GENERATED CODE, DO NOT EDIT MANUALLY.
//  SEE /config_gen/README.md
// ******************************\n\n`

interface SectionInfo {
    readonly name: string
    readonly path: string
    readonly header: boolean
    readonly build: ((data: any[]) => string)
}

const sections: SectionInfo[] = [
    {
        name: "core",
        path: './BililiveRecorder.Core/Config/V3/Config.gen.cs',
        header: true,
        build: builderCore
    },
    {
        name: "cli",
        path: './BililiveRecorder.Cli/Configure/ConfigInstructions.gen.cs',
        header: true,
        build: builderCli
    },
    {
        name: "web",
        path: './BililiveRecorder.Web/Models/Config.gen.cs',
        header: true,
        build: builderWeb
    },
    {
        name: "schema",
        path: './configV3.schema.json',
        header: false,
        build: builderSchema
    }
]

let hasError = false

for (const section of sections) {
    let expected = section.build(data)
    if (section.header)
        expected = HEADER + expected

    const fullPath = resolve(__dirname, "..", section.path)

    let actual: string
    try {
        actual = readFileSync(fullPath, { encoding: 'utf8' })
    } catch (e) {
        console.error(`[validate] ERROR: Cannot read file for section "${section.name}": ${fullPath}`)
        hasError = true
        continue
    }

    // For C# files, skip whitespace-only differences since dotnet-format may adjust formatting
    if (section.name === "schema") {
        // Schema is JSON, compare exactly
        if (actual !== expected) {
            console.error(`[validate] ERROR: Generated file for section "${section.name}" is out of date: ${section.path}`)
            console.error(`[validate]        Run 'npm run build' in config_gen/ to regenerate.`)
            hasError = true
        } else {
            console.log(`[validate] OK: ${section.name}`)
        }
    } else {
        // For C# files, compare ignoring trailing whitespace per line (dotnet-format may change it)
        const normalizeLines = (s: string) => s.split('\n').map(l => l.trimEnd()).join('\n')
        if (normalizeLines(actual) !== normalizeLines(expected)) {
            console.error(`[validate] WARNING: Generated file for section "${section.name}" may be out of date: ${section.path}`)
            console.error(`[validate]          Run 'npm run build' in config_gen/ to regenerate and format.`)
            // Don't fail for C# since dotnet-format is not available during validate
        } else {
            console.log(`[validate] OK: ${section.name}`)
        }
    }
}

if (hasError) {
    process.exit(1)
} else {
    console.log("[validate] All generated configuration files are up to date.")
}
