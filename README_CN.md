# B站录播姬

[![编译状态](https://ci.appveyor.com/api/projects/status/1n4822yitgtu7ht7?svg=true)](https://ci.appveyor.com/project/Genteure/bililiverecorder)
![Pull Request Welcome](https://img.shields.io/badge/Pull%20request-welcome-brightgreen.svg)

## 安装 & 使用

参见网站 [rec.danmuji.org](https://rec.danmuji.org)

## 功能

- 使用简单
- 自动修复视频时间戳
- 可以主播开播后自动录制
- 可以同时录制多个直播间
- 可以录制“即时回放剪辑”（类似 [Twitch 的 Clip](https://help.twitch.tv/customer/portal/articles/2442508-how-to-use-clips)）
- 纯 C# 实现，无 Native 依赖
- 开源！

## 入门 & 开发

开发之前，你需要

- Visual Studio 2017 并安装 .NET Core 开发环境
- PowerShell

项目中有两个文件是由 [编译前脚本](./CI/patch_buildinfo.ps1) 生成的，打开项目后先编译整个项目（执行脚本）可以消除 Visual Studio 给出的错误提醒。

项目 | 类型 | 备注
:---:|:---:|:---
BililiveRecorder.WPF | .NET Framework 4.6.2
BililiveRecorder.Core | .NET Standard 2.0
BililiveRecorder.FlvProcessor | .NET Standard 2.0
BililiveRecorder.Server | .NET Core 2.0 | 预留坑，将来填

如果你想研究这个项目的源代码，或修改功能的话：

- WPF 界面建议从 `BililiveRecorder.WPF/MainWindow.xaml` 开始看
- 录制逻辑建议从 `BililiveRecorder.Core/Recorder.cs` 开始看
- FLV数据处理建议从 `BililiveRecorder.FlvProcessor/FlvStreamProcessor.cs` 开始看

## 参考资料 & 鸣谢

- [coreyauger/flv-streamer-2-file](https://github.com/coreyauger/flv-streamer-2-file): 在FLV处理方面参考了很多
- [zyzsdy/biliroku](https://github.com/zyzsdy/biliroku): 第一个B站直播录播工具
- [Video File Format Specification Version 10.pdf](https://wwwimages2.adobe.com/content/dam/acom/en/devnet/flv/video_file_format_spec_v10.pdf): FLV视频文件格式规范
