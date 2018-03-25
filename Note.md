# 关于本项目的备忘随手笔记

- FLV处理模块 | Library
  - 解析文件头
  - 解析数据块
  - 根据时间戳保存一定量的数据（Clip功能）
- 录制任务管理 | Library
  - 对外提供 ObservableCollection
  - 将来的版本再做弹幕录制导出
- Windows WPF 界面 | WPF
  - 绑定 ObservableCollection
- 命令行 + 配置文件 跨平台录制工具 | Standard

- WPF界面
  - `Recorder` 核心录制逻辑在这里
    - `StreamMonitor`
      - `DanmakuReceiver`
    - `HttpWebRequest`
    - `FlvStreamProcessor`
    - `ObservableCollection<FlvClipProcessor>`

## flv处理模块 `FlvStreamProcessor`

- 对外提供的API应该继承 IDisposable
- 插入自定义FLV文件头
- 提供一个对Stream友好的写入字节流的接口
- 提供一个 Clip 方法
  - Clip 方法应当不需要传入参数
  - Clip 的时长应当由主 `FlvStreamProcessor` 设置
  - 被 Clip 后的生成的 `FlvStreamProcessor` 应当拒绝再执行 Clip
  - (?) 主 `FlvStreamProcessor` 应当自动传递处理后的数据给 Clips
- 输出位置应当尽量由调用方决定（但不能直接接受 Stream ，因为要重写覆盖文件头）

## 各种东西的叫法

- 录播姬
- 回放剪辑
- 录制出来的文件类型
  - 录制
  - 剪辑

## Appveyor

- 每次 push dev 分支的时候
  - 编译 Debug 版本
  - 版本号： 0.0.0.{build}
  - 打包上传到 Appveyor 的 artifacts 列表
- 每次 push tag 的时候
  - 编译 Release 版本
  - 版本号： {tag去掉v}.0
  - 执行 publish
  - 复制生成结果 git push 到 soft.danmuji.org
- master 分支手动维护，保持在最后一个 tag 上

## 发布新版本的方法

- 在 dev 分支上
- git add ...
- git commit -m "New Version: v1.0.0"
- git tag v1.0.0
- git push origin
- git push origin v1.0.0
- 在 Github 上开 Pull Request 合并 dev 进 master
