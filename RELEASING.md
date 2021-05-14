# 新版本发布流程备忘

- 在 `dev--3` 分支完成修改的功能，实际运行测试一遍
- 在 GitHub 创建 Release 并发布
  - GitHub Actions 会自动补充 Assets
- 从 Artifacts 里下载 `WPF-NupkgReleases`
- 运行 `squirrel --releasify "/path/to/BililiveRecorder.{version}.nupkg" -r "/path/to/soft.danmuji.org/BililiveRecorder" --framework-version net472 --no-msi`
- commit push soft.danmuji.org
- 修改 `rec.danmuji.org` 上的更新日志
