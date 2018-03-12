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

## flv处理模块

- 对外提供的API应该继承 IDisposable
- 插入自定义FLV文件头
- 提供一个对Stream友好的写入字节流的接口
- 提供一个 Copy 方法（用于 Clip 功能）
- 输出位置应当尽量由调用方决定（但不能直接接受 Stream ，因为要重写覆盖文件头）