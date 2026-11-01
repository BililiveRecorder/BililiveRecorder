# BililiveRecorder.Desktop（Avalonia 桌面端）

mikufans录播姬的新桌面端，使用 **Avalonia 11 + FluentAvalonia** 重写 UI 层，**完全复用**现有 `BililiveRecorder.Core` 录制核心，不修改仓库中任何现有工程。

## 功能对等状态

- [x] 应用壳：FluentAvalonia NavigationView 侧边栏；右键"设置"可显示"高级设置"入口
- [x] DI 接线：`AddRecorderConfig` + `AddFlv` + `AddRecorder`，与 WPF 版相同
- [x] 工作目录选择：软件先启动，能自动确定（命令行参数 > `path.json` 记住的路径 > 当前目录）就直接进主界面，否则弹出首启目录选择窗口；`--ask-path` 强制重新选择
- [x] 房间列表页：添加房间（房间号或链接）、删除确认、状态圆点、速度/大小/速率显示、按房间号/状态排序、右键菜单（开始/停止录制、启用/停用自动录制、刷新、浏览器打开、切割输出、单房间设置、复制房间号、删除）、"更多"菜单（保存配置、打开工作目录、打开日志文件夹、刷新所有直播间、更改工作目录）
- [x] 单房间设置窗口：三态覆盖（HasXxx）编辑弹幕/录制模式/标准修复/切割/封面/画质/录制条件
- [x] 设置页：弹幕、录制模式、标准修复、切割、封面、文件名模板（含测试）、界面开关（开播提醒/托盘悬浮提示/更新检查）、录制质量、录制条件、Webhook V1/V2
- [x] 高级设置页：弹幕 FlushInterval / 连接方式 / UID 认证、用户脚本（含测试）、FLV metadata、代理与地址类型、直播 API host、6 项时间参数、Cookie（含测试）
- [x] 日志页：内存环形缓冲（500 条）+ 级别着色 + 自动滚动 + 右键复制
- [x] 关于页：版本（GitVersion）/ 版权 / 链接 / 翻译者 / 依赖库
- [x] 公告页：拉取远程公告并提取文本显示（WPF XAML 无法在 Avalonia 渲染）
- [x] 工具箱：FLV 修复（分析/修复/导出数据，进度 + 取消）、FLV→MP4 转封装（FFmpeg，`lib/miniffmpeg`）、弹幕合并（自动起始时间与偏移）
- [x] 托盘：显示主窗口、全部开始/停止录制、退出；悬浮提示显示录制中数量
- [x] 开播提醒：右下角弹窗（点击打开直播间，8 秒自动关闭），尊重设置开关
- [x] 主题切换（侧边栏底部 ◐）与语言切换（🌐：简中/繁中/英/日，运行时生效）
- [x] 关闭窗口最小化到托盘；录制期间阻止系统睡眠；单实例互斥（与 WPF 版互斥）
- [ ] 自动更新（计划换 Velopack，当前可用"禁用自动检查更新"策略，手动下载更新）
- [ ] Windows 系统级 Toast 通知（当前用应用内弹窗替代）、Sentry 上报

## 构建与运行

要求 .NET SDK（本仓库 `global.json` rollForward 允许新 SDK）。

```bash
cd BililiveRecorder.Desktop
dotnet build
# 运行（不指定目录时首启会弹出目录选择窗口）
dotnet run --project src/BililiveRecorder.Desktop -- run "D:\录播"
```

参数：

- `run <路径>` / `<路径>`：指定录制工作目录（从命令行传入时不会写入 path.json）
- `--hide`：以最小化到托盘方式启动
- `--ask-path`：忽略记住的路径，强制弹出目录选择窗口

日志写在 exe 旁边的 `logs/bilirec.txt`（按天滚动），与 WPF 版一致。

## 技术栈说明

- `net10.0`（FluentAvaloniaUI 2.5.1 起 net10-only；引用 net8.0 的 Core 无兼容问题）
- Avalonia `11.3.*` + FluentAvaloniaUI `2.5.1` + CommunityToolkit.Mvvm + CliWrap（FFmpeg 转封装）
- 本地化资源通过 `<EmbeddedResource Link="...">` 链接自 `BililiveRecorder.WPF/Properties/Strings.*.resx`，图标复用 WPF 的 `ico.ico`
- 配置直接绑定 Core 生成的 ConfigV3 对象（GlobalConfig/RoomConfig 的 `HasXxx` 三态覆盖机制与 WPF 版一致），持久化时机与 WPF 相同（`SaveConfig` / 退出时 Dispose）

## 回滚

整个子目录独立存在，删除 `BililiveRecorder.Desktop/` 即可完全回滚；未修改 `BililiveRecorder.sln` 及任何现有工程文件。
