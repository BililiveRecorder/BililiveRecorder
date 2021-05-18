# B站录播姬

[![Build and Test](https://github.com/Bililive/BililiveRecorder/actions/workflows/build.yml/badge.svg)](https://github.com/Bililive/BililiveRecorder/actions/workflows/build.yml)
[![当前版本](https://img.shields.io/github/tag/Bililive/BililiveRecorder.svg?label=当前版本)](#)
[![开源协议](https://img.shields.io/github/license/Bililive/BililiveRecorder.svg?label=开源协议)](#)
[![QQ群 689636812](https://img.shields.io/badge/QQ%E7%BE%A4-689636812-success)](https://jq.qq.com/?_wv=1027&k=5zVwEyf)
[![Crowdin](https://badges.crowdin.net/bililiverecorder/localized.svg)](https://crowdin.com/project/bililiverecorder)

## 安装 & 使用

参见网站 [rec.danmuji.org](https://rec.danmuji.org)

## 功能

- 使用简单
- 主播开播后自动开始录制
- 同时录制多个直播间
- 自动修复B站直播服务器导致的各种问题
- 工具箱模式，用于修复旧版录播姬或其他软件录的视频文件*
- 纯 C# 实现，无 ffmpeg 等 native 依赖
- 开源！

*仅限未经处理的直接从直播服务器下载的原始FLV文件。 如果录播是用 FFmpeg 录制的或处理过的就无法修复了，FFmpeg 会进一步损坏有问题的文件。

## 开发 & 编译

推荐使用选配了 .NET 5 开发的 Visual Studio 2019，不过其他 IDE 应该也可以。

项目 | 平台 | 备注
:---:|:---:|:---:
BililiveRecorder.WPF | .NET Framework 4.7.2 | Windows only GUI
BililiveRecorder.Cli | .NET 5 | 跨平台命令行
BililiveRecorder.ToolBox | .NET Standard 2.0 | WPF 和 Cli 使用的库
BililiveRecorder.Core | .NET Standard 2.0 | 核心录制逻辑
BililiveRecorder.Flv | .NET Standard 2.0 | 数据处理逻辑

## 版本号

本项目不使用 Semantic Versioning，不保证任何版本之间源代码接口的兼容性。

## 参考资料 & 鸣谢

- [Adobe Flash Video File Format Specification 10.1.2.01.pdf](https://www.adobe.com/content/dam/acom/en/devnet/flv/video_file_format_spec_v10_1.pdf)
- [coreyauger/flv-streamer-2-file](https://github.com/coreyauger/flv-streamer-2-file) 曾在本项目开发早期作为参考
- [zyzsdy/biliroku](https://github.com/zyzsdy/biliroku): (大概是)第一个B站直播录播工具
