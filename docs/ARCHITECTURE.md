# 项目架构

本文面向准备阅读、维护或二次开发“歇一喵”的贡献者。

## 技术概览

- 平台：Windows 10/11
- 语言：C#
- UI：WPF
- 运行时：.NET Framework 4.x
- 发布形式：单个可执行文件，图片资源嵌入程序集
- 外部依赖：无 NuGet 包、无服务端、无数据库

## 目录结构

```text
assets/                 正式猫猫图片与动画帧
docs/                   产品、隐私、构建与设计文档
scripts/                测试和发布打包脚本
src/                    应用源代码
tools/                  图标构建辅助代码
build.ps1               主构建脚本
app.manifest            Windows 应用清单
```

## 主要模块

| 模块 | 职责 |
|---|---|
| `Program.cs` | 程序入口、单实例、命令行自测与运行环境初始化 |
| `AppController.cs` | 主流程协调、托盘菜单、窗口生命周期和全局快捷键 |
| `PetWindow.cs` / `CatControl.cs` | 桌宠窗口、动画、拖动、互动和移动行为 |
| `ReminderEngine.cs` | 喝水、活动、吃饭、回宿舍和待办的到期判断 |
| `ActivityTracker.cs` | 实际使用时间、空闲与离开状态判断 |
| `ProtectionDetector.cs` | 全屏与会议软件保护判断 |
| `DisplayManager.cs` | 多显示器坐标、可见区域、跨屏和边缘吸附 |
| `SettingsWindow.cs` / `EditorWindows.cs` | 设置、待办、分类、奖励等编辑界面 |
| `CareCompanionWindow.cs` / `CareContent.cs` | 猫猫休息站及纯友谊向文字内容 |
| `SettingsStore.cs` / `Models.cs` | 本地设置持久化、默认数据与数据迁移 |
| `SelfTests.cs` | 内置回归测试与功能检查 |

## 数据流

1. `Program` 启动后加载或创建本地设置。
2. `AppController` 创建桌宠、托盘和提醒引擎。
3. `ActivityTracker` 提供实际使用时长与离开状态。
4. `ReminderEngine` 根据设置判断提醒是否到期。
5. 提醒选择经过二次确认后写回本地状态与成长记录。
6. `DisplayManager` 负责所有窗口在多屏环境中的可见位置。

程序不需要联网；运行状态和用户内容只在本机设置文件中流转。

## 设计约束

- 桌宠与提醒窗口必须始终可恢复、可隐藏，并保持在至少一个显示器的可见区域内。
- 所有提醒保持静音，不申请管理员权限。
- 全局快捷键注册失败时，应保留托盘菜单作为替代入口。
- 设置结构变更应提供默认值与兼容迁移，避免旧版本数据导致启动失败。
- 正式发布前必须运行 `scripts/test.ps1` 和内置 `--self-test`。

## 版本与发布

当前版本号保存在根目录 `VERSION`。功能变化记录在 `CHANGELOG.md`，面向用户的发布摘要位于 `RELEASE_NOTES.md`。
