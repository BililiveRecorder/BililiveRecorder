# CI

## 发布 checklist

1. 在 dev 分支完成修改的功能，实际运行测试一遍
2. 修改 `VERSION` 文件并 commit push **文件末尾不能有 CRLF**
3. 在 GitHub 创建 Pull Request 从 dev 合并到 master
4. Merge Pull Request
5. Merge `Bililive/soft.danmuji.org` Pull Request
6. 发布 GitHub Release
7. 修改 `rec.danmuji.org` 上的更新日志
8. Fast-Forward merge back to dev


## 发布新版本流程

- commit 所有代码和文件修改 ...
- ... 并合并到 `dev` 分支
- 修改 `VERSION` 文件到目标版本号 (e.g., `1.1.0`, `1.2.1`)
- git commit
- git push origin dev
- 在 GitHub 上新开 Pull Request 从 dev 合并到 master
  - Appveyor 触发运行测试
- merge 合并分支
  - Appveyor 触发 push master 执行 Release 配置编译
  - TODO: Docker
- Appveyor 编译 (Release Mode)
  - `nuget restore`
  - `git clone /path/to/soft.danmuji.org.git` 并新建分支
  - `msbuild /v:m /p:Configuration=Release /p:SquirrelBuildTarget="/path/to/soft.danmuji.org"`
  - 在 `soft` 仓库 `git push` 并调用 [GitHub API](https://developer.github.com/v3/pulls/#create-a-pull-request) 新建 Pull Request
  - 新建一个包含 `BililiveRecorderSetup.exe` 的 [GitHub Release](https://www.appveyor.com/docs/deployment/github/) 草稿
- 补全 Release 正文并发布
- merge `soft` 仓库的 pull request
