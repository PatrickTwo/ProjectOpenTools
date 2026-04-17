# 终端命令启动方式设计

## 概述

为应用启动器新增"终端命令"启动模式。用户配置一条命令，点击启动时在当前项目目录打开 Windows Terminal 并自动执行该命令。

## 模型变更

### LauncherAppEntry

```csharp
public sealed class LauncherAppEntry
{
    public string Name { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string ArgumentsTemplate { get; set; } = string.Empty;
    public string LaunchMode { get; set; } = "Executable";  // "Executable" | "TerminalCommand"
    public string CommandText { get; set; } = string.Empty;  // TerminalCommand 用

    [JsonIgnore]
    public ImageSource? IconImage { get; set; }
}
```

**向后兼容**：老配置缺少 `LaunchMode` 时，反序列化默认 `"Executable"`，行为不变。

## 服务变更

### LauncherService

#### LaunchProject — 统一入口

```csharp
public LaunchResult LaunchProject(LauncherAppEntry launcherAppEntry, string projectPath)
{
    var validation = ValidateLaunch(launcherAppEntry, projectPath);
    if (!validation.IsSuccess) return validation;

    if (launcherAppEntry.LaunchMode == "TerminalCommand")
    {
        return LaunchTerminalCommand(launcherAppEntry.CommandText, projectPath);
    }
    return LaunchExecutable(launcherAppEntry, projectPath);
}
```

#### ValidateLaunch — 按模式分流验证

```csharp
public LaunchResult ValidateLaunch(LauncherAppEntry launcherAppEntry, string projectPath)
{
    // 公共校验：项目目录存在
    if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        return new LaunchResult(false, "当前项目路径不存在，请重新选择项目。");

    if (launcherAppEntry.LaunchMode == "TerminalCommand")
    {
        // TerminalCommand 校验
        if (string.IsNullOrWhiteSpace(launcherAppEntry.CommandText))
            return new LaunchResult(false, "终端命令不能为空。");

        string wtPath = GetWindowsTerminalPath();
        if (!File.Exists(wtPath))
            return new LaunchResult(false, $"未找到 Windows Terminal (wt.exe)，请确认已安装。路径：{wtPath}");

        return new LaunchResult(true, "校验通过。");
    }

    // Executable 校验
    if (string.IsNullOrWhiteSpace(launcherAppEntry.ExePath) || !File.Exists(launcherAppEntry.ExePath))
        return new LaunchResult(false, $"应用路径不存在：{launcherAppEntry.ExePath}");

    return new LaunchResult(true, "校验通过。");
}
```

#### BuildArguments — 保留，仅 Executable 用

#### BuildTerminalArguments — 新增

```csharp
public string BuildTerminalArguments(string commandText, string projectPath)
{
    string quotedProjectPath = QuoteArgument(projectPath);
    string quotedCommand = QuoteArgument(commandText);
    return $"new-tab -d {quotedProjectPath} {quotedCommand}";
}

private static string QuoteArgument(string value)
{
    return $"\"{value}\"";
}
```

#### LaunchTerminalCommand — 新增

```csharp
private LaunchResult LaunchTerminalCommand(string commandText, string projectPath)
{
    string arguments = BuildTerminalArguments(commandText, projectPath);
    string wtPath = GetWindowsTerminalPath();

    ProcessStartInfo psi = new ProcessStartInfo
    {
        FileName = wtPath,
        Arguments = arguments,
        UseShellExecute = false,
        WorkingDirectory = projectPath
    };

    Process.Start(psi);
    return new LaunchResult(true, "启动成功。");
}
```

#### GetWindowsTerminalPath

从以下位置查找 wt.exe：
1. `Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\wt.exe")`
2. `Path.Combine(Environment.GetFolderPath(SpecialFolder.ProgramFiles), @"WindowsApps\wt.exe")`

## UI 变更

### LauncherAppEditorWindow.xaml

新增字段：
- `LaunchModeComboBox`：选择 Executable 或 TerminalCommand
- `CommandTextBox`：终端命令输入（LaunchMode=TerminalCommand 时显示）
- 警告提示文本

**动态布局**：
- Executable：显示 Name、ExePath、ArgumentsTemplate，隐藏 CommandText
- TerminalCommand：显示 Name、CommandText，隐藏 ExePath、ArgumentsTemplate

### LauncherAppEditorWindow.xaml.cs

- `PopulateFields()`：回填 LaunchMode、CommandText
- `SaveButton_Click`：`EditedLauncherApp` 包含新字段
- 新增 `LaunchModeComboBox_SelectionChanged` 事件处理动态显示

### LauncherAppManagerWindow

列表 ItemTemplate 根据 `LaunchMode` 显示：
- Executable：`{Name} — {ExePath} {ArgumentsTemplate}`
- TerminalCommand：`{Name} — 终端命令：{CommandText}`

Confirm 时复制 `LaunchMode` 和 `CommandText` 字段。

## Windows Terminal 启动参数

格式：`wt new-tab -d "项目目录" "命令"`

示例：`wt new-tab -d "C:\Projects\MyProject" "codex"`

**注意**：`UseShellExecute = false`（wt.exe 是控制台程序）

## 测试用例

### Executable 回归
- 参数模板为空 → 默认传入带引号的项目路径
- 参数模板包含 `{projectPath}` → 正确替换

### TerminalCommand 新行为
- 命令为空 → 校验失败
- 项目目录不存在 → 校验失败
- wt.exe 不存在 → 校验失败，返回明确错误信息
- 构造参数包含项目目录和命令文本
