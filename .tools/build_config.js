"use strict";
import { spawn } from "child_process";
import { stdout, stderr } from "process";
import { writeFileSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from 'url';

import data from "./config_data.js"

import generate_cli_configure from "./generate_cli_configure.js";
import generate_json_schema from "./generate_json_schema.js"
import generate_core_config from "./generate_core_config.js"
import generate_web_config from "./generate_web_config.js"

const baseDirectory = dirname(fileURLToPath(import.meta.url));

const DO_NOT_EDIT_COMMENT = `// ******************************
//  GENERATED CODE, DO NOT EDIT MANUALLY.
//  SEE .tools/build_config.js
// ******************************\n\n`

// ---------------------------------------------
//                 SCHEMA
// ---------------------------------------------

console.log("[node] writing json schema...")

const json_schema_path = resolve(baseDirectory, '../configV2.schema.json');

const json_schema_code = generate_json_schema(data);

writeFileSync(json_schema_path, json_schema_code, {
    encoding: "utf8"
});

// ---------------------------------------------
//                  CORE
// ---------------------------------------------

console.log("[node] writing core config...")

const core_config_path = resolve(baseDirectory, '../BililiveRecorder.Core/Config/V2/Config.gen.cs');

const core_config_code = generate_core_config(data);

writeFileSync(core_config_path, DO_NOT_EDIT_COMMENT + core_config_code, {
    encoding: "utf8"
});

// ---------------------------------------------
//                  CLI
// ---------------------------------------------

console.log("[node] writing cli configure config...")

const cli_config_path = resolve(baseDirectory, '../BililiveRecorder.Cli/Configure/ConfigInstructions.gen.cs');

const cli_config_code = generate_cli_configure(data);

writeFileSync(cli_config_path, DO_NOT_EDIT_COMMENT + cli_config_code, {
    encoding: "utf8"
});

// ---------------------------------------------
//                  WEB
// ---------------------------------------------
/* disabled
console.log("[node] writing web config...")

const web_config_path = resolve(baseDirectory, '../BililiveRecorder.Web.Schemas/Types/Config.gen.cs');

const web_config_code = generate_web_config(data);

writeFileSync(web_config_path, DO_NOT_EDIT_COMMENT + web_config_code, {
    encoding: "utf8"
});
*/
// ---------------------------------------------
//                 FORMAT
// ---------------------------------------------

console.log("[node] formatting...")

let format = spawn('dotnet',
    [
        'tool',
        'run',
        'dotnet-format',
        '--',
        '--include',
        './BililiveRecorder.Core/Config/V2/Config.gen.cs',
        './BililiveRecorder.Cli/Configure/ConfigInstructions.gen.cs',
        // './BililiveRecorder.Web.Schemas/Types/Config.gen.cs'
    ],
    {
        cwd: resolve(baseDirectory, "..")
    })

format.stdout.on('data', function (data) {
    stdout.write('[dotnet-format] ' + data.toString());
});

format.stderr.on('data', function (data) {
    stderr.write('[dotnet-format] ' + data.toString());
});

format.on('exit', function (code) {
    console.log('[node] format done code ' + code.toString());
});
