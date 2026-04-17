# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## 项目概述

WPF + .NET 8 Windows 应用启动器，用于快速切换项目文件夹并用不同应用打开。

## 构建与运行

```powershell
# 调试构建
dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj

# 发布前验证测试
dotnet build .\tests\ProjectOpenTools.Tests\ProjectOpenTools.Tests.csproj -p:UseSharedCompilation=false
dotnet vstest .\tests\ProjectOpenTools.Tests\bin\Debug\net8.0-windows\ProjectOpenTools.Tests.dll

# 直接运行（构建后产物在 dist/）
.\dist\项目启动器.exe
```

## 架构

- `MainWindow.xaml.cs` — 主窗口，代码隐藏模式，无 MVVM 框架。持有所有业务服务实例。
- `AppSettings` — 根配置模型，包含 `RecentProjects`、`LauncherApps` 和偏好设置。
- `SettingsStorageService` — JSON 持久化，读写 `%AppData%\ProjectOpenTools\settings.json`。
- `LauncherService` — 启动外部应用，支持参数模板替换（`{projectPath}`）。
- `ProjectHistoryService` — 管理最近项目列表，按时间排序。
- `AppIconService` — 从 exe 文件提取图标。
- `RegistryAppDiscoveryService` — 从 Windows 注册表扫描已安装应用。

构建时自动复制 exe 及运行时文件到 `dist/` 目录。

## 配置存储

`%AppData%\ProjectOpenTools\settings.json` — 首次启动自动创建，丢失后用默认值重置。
