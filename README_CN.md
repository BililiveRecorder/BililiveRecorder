# B站录播姬

[![编译状态](https://ci.appveyor.com/api/projects/status/1n4822yitgtu7ht7?svg=true)](https://ci.appveyor.com/project/Genteure/bililiverecorder)
[![当前版本](https://img.shields.io/github/tag/Bililive/BililiveRecorder.svg?label=当前版本)](#)
[![需要帮助的 issue](https://img.shields.io/github/issues/Bililive/BililiveRecorder/help%20wanted.svg?label=需要帮助的%20issue)](https://github.com/Bililive/BililiveRecorder/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)
[![Pull Request Welcome](https://img.shields.io/badge/Pull%20request-欢迎-brightgreen.svg)](#)
[![开源协议](https://img.shields.io/github/license/Bililive/BililiveRecorder.svg?label=开源协议)](#)
[![QQ群 689636812](https://img.shields.io/badge/QQ%E7%BE%A4-689636812-success)](https://jq.qq.com/?_wv=1027&k=5zVwEyf)

## 安装 & 使用

参见网站 [rec.danmuji.org](https://rec.danmuji.org)

## 功能

- 使用简单
- 使录出的文件时间戳从 0 开始
- 录制结束后自动写入总时长信息
- 可以主播开播后自动录制
- 可以同时录制多个直播间
- 纯 C# 实现，无 ffmpeg 等 native 依赖
- 开源！

## 入门 & 开发

开发之前，你需要

- Visual Studio 2017 / 2019 并安装 .NET Core 开发环境
- PowerShell

项目中有两个文件是由 [编译前脚本](./CI/patch_buildinfo.ps1) 生成的，打开项目后先编译整个项目（执行脚本）可以消除 Visual Studio 给出的错误提醒。

项目 | 类型 | 备注
:---:|:---:|:---
BililiveRecorder.WPF | .NET Framework 4.6.2
BililiveRecorder.Core | .NET Standard 2.0
BililiveRecorder.FlvProcessor | .NET Standard 2.0
BililiveRecorder.Server | .NET Core 2.0 | 预留坑，将来填（咕咕咕）

如果你想研究这个项目的源代码，或修改功能的话：

- WPF 界面建议从 `BililiveRecorder.WPF/MainWindow.xaml` 开始看
- 录制逻辑建议从 `BililiveRecorder.Core/Recorder.cs` 开始看
- FLV数据处理建议从 `BililiveRecorder.FlvProcessor/FlvStreamProcessor.cs` 开始看

## Server 版说明

本项目核心逻辑均与 WPF 界面分离，使用 .NET Standard 2.0 而不是 .NET Framework，可以较轻松地改出可在 Linux 上运行的 .NET Core 版本，但因为本人没时间等原因一直没有做。

如果有有能人士有在 Linux 上运行本项目的需求的话可以自行 fork 修改（但我大概不会 merge 回来）

## 参考资料 & 鸣谢

- [Adobe Flash Video File Format Specification 10.1.2.01.pdf](https://www.adobe.com/content/dam/acom/en/devnet/flv/video_file_format_spec_v10_1.pdf)
- [coreyauger/flv-streamer-2-file](https://github.com/coreyauger/flv-streamer-2-file)
- [zyzsdy/biliroku](https://github.com/zyzsdy/biliroku): (大概是)第一个B站直播录播工具
