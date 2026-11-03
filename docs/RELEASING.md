# 发布流程

维护者发布新版本时按以下顺序操作：

1. 更新 `VERSION`、程序集版本、变更记录和版本说明。
2. 运行 `scripts/test.ps1`，确认全部测试通过。
3. 运行 `scripts/package-release.ps1` 生成发布压缩包。
4. 使用 Windows Defender 扫描生成的可执行文件和压缩包。
5. 计算并复核压缩包 SHA256。
6. 创建形如 `v0.7.1` 的 Git 标签或在 GitHub Release 页面创建新标签。
7. 在 GitHub 创建同名 Release，粘贴 `RELEASE_NOTES.md` 内容。
8. 上传 Windows 压缩包、`SHA256SUMS.txt` 和版本说明，并检查 Release 内容。
9. 在另一台 Windows 电脑或干净用户账户中完成一次解压、启动与托盘恢复检查。

发布包中不应包含个人设置、测试截图原稿、生成工具日志、绝对路径或 `settings.json`。
