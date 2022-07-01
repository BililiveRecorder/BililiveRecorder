# 新版本发布流程备忘

- 在 `dev` 分支完成修改的功能，实际运行测试一遍
- 在 GitHub 创建 Release 并发布
  - GitHub Actions 会自动补充 Assets
- 从 Artifacts 里下载 `WPF-NupkgReleases`
- 运行 `squirrel releasify` 命令
- commit push soft.danmuji.org
- 修改 `rec.danmuji.org` 上的更新日志

```powershell
# $pkg="/path/to/BililiveRecorder.{version}.nupkg"
# $dst="/path/to/soft.danmuji.org/BililiveRecorder"
$pkg=""
$dst=""

$icon="./BililiveRecorder.WPF/installer.ico"
$appIcon="./BililiveRecorder.WPF/ico.ico"

squirrel releasify -p "$pkg" -r "$dst" --icon "$icon" --appIcon "$appIcon" -f net472
```
