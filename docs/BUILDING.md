# 构建与测试

## 环境要求

- Windows 10 或 Windows 11
- Windows 自带的 .NET Framework 4.x C# 编译器
- Windows PowerShell 5.1 或 PowerShell 7

项目没有 NuGet 依赖，不需要联网恢复软件包。

## 构建

在仓库根目录运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

默认输出位于 `dist\歇一喵.exe`。指定输出目录：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -OutputDirectory .\dist-custom
```

构建脚本会：

1. 查找 64 位或 32 位 .NET Framework C# 编译器；
2. 编译 `src` 下的 C# 文件；
3. 将正式图片资源嵌入程序；
4. 生成应用图标；
5. 输出单个可执行文件。

## 完整测试

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

脚本会执行干净构建、内置回归测试和短时运行冒烟测试。测试结果输出到 `test-results`。

也可以直接运行内置测试：

```powershell
.\dist\歇一喵.exe --self-test .\test-results\自动测试明细.txt
```

## 发布包

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-release.ps1
```

默认在 `release` 目录生成 `XieYiMiao-v0.7.1-windows.zip`、`SHA256SUMS.txt` 和发布说明。若修改版本号，请同时更新：

- `VERSION`
- `src/AssemblyInfo.cs`
- `CHANGELOG.md`
- `RELEASE_NOTES.md`

## 常见问题

- 找不到编译器：确认系统已启用 .NET Framework 4.x；Windows 10/11 通常已包含。
- PowerShell 阻止脚本：使用上述带 `-ExecutionPolicy Bypass` 的一次性命令。
- 快捷键测试失败：关闭其他正在运行的“歇一喵”，并确认 `Ctrl + Alt + M` 未被其他软件占用。
- Windows 首次运行提示未知发布者：这是因为公开版本没有商业代码签名证书，不代表程序需要管理员权限。
