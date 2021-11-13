import { ConfigEntry } from "../types"
import fs from "fs"
import { resolve } from "path"
import { data } from "../data";
import { trimEnd } from "../utils";

export default function doc(path: string): void {
    if (!fs.statSync(resolve(path, '_config.yml'))) {
        console.error('Check your path');
        return;
    }
    if (!fs.statSync(resolve(path, 'index.html'))) {
        console.error('Check your path');
        return;
    }

    const targetPath = resolve(path, '_includes/generated_settings_list.md')

    const text = buildMarkdown(data)

    fs.writeFileSync(targetPath, text, { encoding: 'utf8' });
}

function buildMarkdown(data: ConfigEntry[]): string {
    let result = '';

    // 目录
    result += "## 目录\n\n"
    result += data.filter(x => !x.advancedConfig).map(x => `- [${x.description}](#${x.description})`).join('\n')
    result += '\n\n'

    // 一般设置项目列表
    result += data.filter(x => !x.advancedConfig).map(x =>
        `### ${x.description}

键名: \`${x.name}\`  
类型: \`${trimEnd(x.type, '?')}\`  
默认设置: \`${x.defaultValueDescription ?? x.defaultValue}\`

${x.markdown}
`).join('\n')
    result += '\n\n'

    // 高级设置说明
    result += "## 高级设置\n\n"
    result += "<span style=\"color:red\">重要说明</span>：一般用户通常不需要也不应该修改高级设置。  \n对各个 Timing 的修改可能会导致被B站服务器屏蔽、不能及时开始录制等问题。\n\n"
    result += "显示高级设置的方法：右键双击界面左下角的设置按钮\n\n"

    // 高级设置项目列表
    result += data.filter(x => x.advancedConfig).map(x =>
        `### ${x.description}

键名: \`${x.name}\`  
类型: \`${trimEnd(x.type, '?')}\`  
默认设置: \`${x.defaultValueDescription ?? x.defaultValue}\`

${x.markdown}
`).join('\n')

    return result;
}
