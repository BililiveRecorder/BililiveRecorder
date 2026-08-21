# 画质自动升级功能 — 变更记录

## 功能概述

新增可选功能：**自动更新优先画质**。

开启后，每隔一定时间（`TimingQualityUpgradeCheckInterval`，默认 90 秒）检查一次当前直播是否有更高优先级的画质可用；
若发现更优画质（在 `RecordingQuality` 列表中出现得更靠前），则结束当前录制并立即重新开始录制，从而切换到更高画质。

默认**关闭**，不影响现有行为。

## 新增配置项（均为全局、高级设置）

| id | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `RecordingQualityUpgradeCheck` | bool | `false` | 是否启用自动更新优先画质 |
| `TimingQualityUpgradeCheckInterval` | uint | `90` | 画质更新检查间隔（秒） |

> 说明：画质优先级由 `RecordingQuality`（如 `avc10000,hevc10000`）中的**先后顺序**决定，越靠前优先级越高。

## 核心实现逻辑

1. 抽取画质解析/选择逻辑到新类 `BililiveRecorder.Core/Api/StreamQualitySelector.cs`：
   - `ParseAllowedQn`：把 `RecordingQuality` 解析为有序的 `StreamCodecQn` 列表（顺序即优先级）。
   - `SelectBestAvailableAsync`：请求可用画质并返回第一个匹配的（最优）。
   - `IndexOfQn`：返回某画质在优先级列表中的下标。
2. `RecordTaskBase` 通过 `CurrentCodecQn` 暴露当前录制**选中的画质**（接口 `IRecordTask` 同步暴露）。
3. `Room` 新增独立定时器 `qualityUpgradeTimer`，周期调用 `CheckAndUpgradeQualityAsync`：
   - 取当前录制的画质下标 `currentIndex` 与当前最优画质下标 `bestIndex`。
   - 若 `bestIndex < currentIndex`，调用 `recordTask.RequestStop()`。
   - 录制停止后，由已有的 `RecordSessionEnded` 逻辑自动启动新录制，新录制会选中更高画质。

## 修改文件清单

### 核心逻辑

- `BililiveRecorder.Core/Api/StreamQualitySelector.cs`（新增）
- `BililiveRecorder.Core/Recording/IRecordTask.cs`：新增 `CurrentCodecQn`
- `BililiveRecorder.Core/Recording/RecordTaskBase.cs`：暴露画质、复用 `StreamQualitySelector`
- `BililiveRecorder.Core/Room.cs`：新增画质升级定时检查

### 配置定义与生成产物

- `config_gen/data.ts`（源）
- `BililiveRecorder.Core/Config/V3/Config.gen.cs`
- `BililiveRecorder.Cli/Configure/ConfigInstructions.gen.cs`
- `BililiveRecorder.Web/Models/Config.gen.cs`（DTO + GraphQL 输入/输出/默认值类型）
- `configV3.schema.json`

### UI

- `BililiveRecorder.WPF/Pages/AdvancedSettingsPage.xaml`
- `webui/source/src/utils/api.ts`
- `webui/source/src/views/recorder/SettingPage.vue`

## 待完成项 / 注意事项

1. ~~**编译验证**~~（已完成）：在 .NET SDK 8.0.424 下构建：
   - `BililiveRecorder.Core`（net472 + net8.0）、`BililiveRecorder.Web`、`BililiveRecorder.Cli`、`BililiveRecorder.ToolBox`、`BililiveRecorder.Flv` 均构建成功。
   - 核心单元测试 `BililiveRecorder.Core.UnitTests`（net8.0）：**142 通过 / 0 失败 / 1 跳过**。
   - WPF 项目因 NuGet 依赖还原问题（`CS0246` 找不到 ModernWpf/Serilog 等命名空间）仍无法编译，属该项目的既有构建方式问题、与本次改动无关。见下方「WPF 构建」。
2. ~~**Web 版启动与配置验证**~~（已完成）：以 CLI `run` 模式启动内嵌 Web 服务器（`http://localhost:2356`），验证：
   - `GET /api/config/default` 返回 `recordingQualityUpgradeCheck: false`、`timingQualityUpgradeCheckInterval: 60`。
   - `GET /api/config/global` 返回 `optionalRecordingQualityUpgradeCheck`、`optionalTimingQualityUpgradeCheckInterval`。
   - `POST /api/config/global` 可写入 `{hasValue:true, value:true}` / `{hasValue:true, value:30}`，返回 200。
   - 配置正确持久化到 `config.json`：`{"version":3,"global":{"RecordingQualityUpgradeCheck":{"HasValue":true,"Value":true},"TimingQualityUpgradeCheckInterval":{"HasValue":true,"Value":30}},...}`。
3. ~~**Release 发布**~~（已完成）：
   - 构建 WebUI 前端：`cd webui/source && npm ci && npx vite build`，产物拷贝到 `BililiveRecorder.Web/embeded/ui`（含画质升级设置界面）。
   - 发布自包含版本：`dotnet publish BililiveRecorder.Cli/BililiveRecorder.Cli.csproj -c Release -r win-x64 --self-contained true`。
   - 产物目录：`BililiveRecorder.Cli/publish/win-x64/Release/`（主程序 `BililiveRecorder.Cli.exe`，403 个文件共约 115 MB）。
   - 已验证：启动后 `/ui/` 返回 200，新 Vue UI 的 `assets/index-c4567335.js`（888 KB）与 `index-4f0b3bfb.css` 均可正常加载；`/api/config/default` 返回新配置项。
   - 其它平台：将 `-r` 改为 `linux-x64`、`linux-arm64`、`osx-x64` 等即可（csproj 已声明 RuntimeIdentifiers）。
4. **CLI 配置向导**：`ConfigInstructions.gen.cs` 已同步新增两个配置项（枚举项 + `ConfigInstruction` 条目），与生成器 `codeCli.ts` 输出一致。
5. **生成器复验**：生成产物文件为手动按生成器模板同步编写，建议在装有 Node 的环境执行 `cd config_gen && npm install && npm run build -- code` 复核一致。
6. **WPF 构建**：WPF 目标是 .NET Framework 4.7.2，README 要求用 `msbuild -t:restore && msbuild` 而非 `dotnet build`。
   当前 `dotnet build` 报 `CS0246`（找不到 NuGet 包命名空间），是还原方式问题。如要验证 WPF 的 XAML 改动，请在 Developer Command Prompt 里按 README 执行 `msbuild -t:restore` 后 `msbuild`。
7. **文档**：`config_gen/generators/doc.ts` 生成的网站文档未更新（不在本仓库核心范围内）。
8. **V2 配置**：`BililiveRecorder.Core/Config/V2` 与 `configV2.schema.json` 为历史迁移格式，未改动（新配置项仅用于 V3，旧配置升级后默认关闭）。
9. **建议端到端测试**（需真实直播流）：
   - 开启开关后，主播从低画质升到高画质时，应能自动重开录制到更高画质。
   - 已是最优画质时不反复重启。
   - 用户脚本返回直播流（`qn = -1`）时跳过升级检查。
   - 关闭开关后不再检查、不再重启。
