# 终端命令启动方式实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 TerminalCommand 启动模式，用户在项目目录通过 Windows Terminal 执行预配置命令。

**Architecture:** 模型层新增 LaunchMode + CommandText 字段；LauncherService 内部分流（Executable 原逻辑，TerminalCommand 新逻辑）；UI 层动态显示/隐藏字段。

**Tech Stack:** WPF + .NET 8, C#, JSON 持久化

---

## 文件变更概览

| 文件 | 变更类型 | 职责 |
|------|---------|------|
| `src/ProjectOpenTools/Models/LauncherAppEntry.cs` | 修改 | 新增 LaunchMode、CommandText 字段 |
| `src/ProjectOpenTools/Services/LauncherService.cs` | 修改 | 新增 ValidateTerminalLaunch、BuildTerminalArguments、LaunchTerminalCommand、GetWindowsTerminalPath |
| `src/ProjectOpenTools/LauncherAppEditorWindow.xaml` | 修改 | 新增 LaunchModeComboBox、CommandTextBox 及动态布局 |
| `src/ProjectOpenTools/LauncherAppEditorWindow.xaml.cs` | 修改 | 新增 LaunchMode 切换逻辑、字段回填 |
| `src/ProjectOpenTools/LauncherAppManagerWindow.xaml.cs` | 修改 | Confirm 时复制新字段 |
| `tests/ProjectOpenTools.Tests/ProjectOpenTools.Tests.csproj` | 修改 | 新增 LauncherService 单元测试 |

---

## Task 1: LauncherAppEntry 模型新增字段

**Files:**
- Modify: `src/ProjectOpenTools/Models/LauncherAppEntry.cs`

- [ ] **Step 1: 添加 LaunchMode 和 CommandText 属性**

```csharp
/// <summary>
/// 启动模式：Executable（默认）或 TerminalCommand
/// </summary>
public string LaunchMode { get; set; } = "Executable";

/// <summary>
/// 终端命令内容，仅 LaunchMode=TerminalCommand 时使用。
/// </summary>
public string CommandText { get; set; } = string.Empty;
```

- [ ] **Step 2: 验证构建通过**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
git add src/ProjectOpenTools/Models/LauncherAppEntry.cs
git commit -m "feat: 新增 LauncherAppEntry.LaunchMode 和 CommandText 字段"
```

---

## Task 2: LauncherService 新增 TerminalCommand 逻辑

**Files:**
- Modify: `src/ProjectOpenTools/Services/LauncherService.cs`

- [ ] **Step 1: 添加 GetWindowsTerminalPath 方法**

```csharp
private static string GetWindowsTerminalPath()
{
    string[] possiblePaths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\WindowsApps\wt.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            @"WindowsApps\wt.exe"),
    };

    foreach (string path in possiblePaths)
    {
        if (File.Exists(path))
            return path;
    }

    return possiblePaths[0]; // 返回第一个路径供错误信息使用
}
```

- [ ] **Step 2: 添加 BuildTerminalArguments 方法**

```csharp
public string BuildTerminalArguments(string commandText, string projectPath)
{
    string quotedProjectPath = QuoteArgument(projectPath);
    string quotedCommand = QuoteArgument(commandText);
    return $"new-tab -d {quotedProjectPath} {quotedCommand}";
}
```

- [ ] **Step 3: 添加 LaunchTerminalCommand 内部方法**

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

- [ ] **Step 4: 修改 ValidateLaunch 按 LaunchMode 分流**

```csharp
public LaunchResult ValidateLaunch(LauncherAppEntry launcherAppEntry, string projectPath)
{
    if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
    {
        return new LaunchResult(false, "当前项目路径不存在，请重新选择项目。");
    }

    if (launcherAppEntry.LaunchMode == "TerminalCommand")
    {
        if (string.IsNullOrWhiteSpace(launcherAppEntry.CommandText))
        {
            return new LaunchResult(false, "终端命令不能为空。");
        }

        string wtPath = GetWindowsTerminalPath();
        if (!File.Exists(wtPath))
        {
            return new LaunchResult(false, $"未找到 Windows Terminal (wt.exe)，请确认已安装。路径：{wtPath}");
        }

        return new LaunchResult(true, "校验通过。");
    }

    // Executable 模式
    if (string.IsNullOrWhiteSpace(launcherAppEntry.ExePath) || !File.Exists(launcherAppEntry.ExePath))
    {
        return new LaunchResult(false, $"应用路径不存在：{launcherAppEntry.ExePath}");
    }

    return new LaunchResult(true, "校验通过。");
}
```

- [ ] **Step 5: 修改 LaunchProject 按 LaunchMode 分流**

```csharp
public LaunchResult LaunchProject(LauncherAppEntry launcherAppEntry, string projectPath)
{
    LaunchResult validationResult = ValidateLaunch(launcherAppEntry, projectPath);
    if (!validationResult.IsSuccess)
    {
        return validationResult;
    }

    if (launcherAppEntry.LaunchMode == "TerminalCommand")
    {
        return LaunchTerminalCommand(launcherAppEntry.CommandText, projectPath);
    }

    // Executable 模式
    string arguments = BuildArguments(launcherAppEntry, projectPath);

    ProcessStartInfo processStartInfo = new ProcessStartInfo
    {
        FileName = launcherAppEntry.ExePath,
        Arguments = arguments,
        UseShellExecute = true,
        WorkingDirectory = projectPath
    };

    Process.Start(processStartInfo);
    return new LaunchResult(true, "启动成功。");
}
```

- [ ] **Step 6: 验证构建通过**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 7: 提交**

```bash
git add src/ProjectOpenTools/Services/LauncherService.cs
git commit -m "feat(LauncherService): 新增 TerminalCommand 启动模式支持"
```

---

## Task 3: LauncherAppEditorWindow XAML 新增 UI 字段

**Files:**
- Modify: `src/ProjectOpenTools/LauncherAppEditorWindow.xaml`

**布局结构变更**：在"显示名称"（NameTextBox）下方新增"启动方式"（LaunchModeComboBox），位于现有"应用路径"和新增"命令内容"之间。TerminalCommand 模式下隐藏"应用路径"和"参数模板"，显示"命令内容"和警告提示。

- [ ] **Step 1: 添加 LaunchModeComboBox（在 NameTextBox 下方）**

在 `NameTextBox` (Row 6) 下方、现有"应用路径" (Row 8) 上方插入：

```xml
<TextBlock Grid.Row="7"
           FontWeight="SemiBold"
           Foreground="{StaticResource StrongTextBrush}"
           Text="启动方式" />
<ComboBox x:Name="LaunchModeComboBox"
          Grid.Row="8"
          Margin="0,12,0,0"
          SelectionChanged="LaunchModeComboBox_SelectionChanged">
    <ComboBoxItem Content="Executable（应用路径）" Tag="Executable" IsSelected="True" />
    <ComboBoxItem Content="TerminalCommand（终端命令）" Tag="TerminalCommand" />
</ComboBox>
```

- [ ] **Step 2: 在"应用路径"区块外包裹 Visibility 控制容器**

将现有的 Row 10-12（应用路径 + 参数模板）包裹在 Border 中：

```xml
<Border x:Name="ExecutablePanel"
        Grid.Row="10"
        Margin="0,18,0,0">
    <StackPanel>
        <TextBlock FontWeight="SemiBold"
                   Foreground="{StaticResource StrongTextBrush}"
                   Text="应用路径" />
        <Grid Margin="0,12,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="12" />
                <ColumnDefinition Width="120" />
            </Grid.ColumnDefinitions>

            <TextBox x:Name="ExePathTextBox"
                     Style="{StaticResource EditorTextBoxStyle}" />
            <Button Grid.Column="2"
                    Click="BrowseExePathButton_Click"
                    Content="浏览 exe"
                    Style="{StaticResource SecondaryButtonStyle}" />
        </Grid>

        <StackPanel Margin="0,18,0,0">
            <TextBlock FontWeight="SemiBold"
                       Foreground="{StaticResource StrongTextBrush}"
                       Text="参数模板" />
            <TextBox x:Name="ArgumentsTemplateTextBox"
                     Margin="0,12,0,0"
                     Height="88"
                     VerticalContentAlignment="Top"
                     TextWrapping="Wrap"
                     AcceptsReturn="True"
                     Style="{StaticResource EditorTextBoxStyle}" />
            <TextBlock Margin="0,10,0,0"
                       Foreground="{StaticResource MutedTextBrush}"
                       Text="示例：--folder-uri {projectPath} 或 --reuse-window {projectPath}" />
        </StackPanel>
    </StackPanel>
</Border>
```

- [ ] **Step 3: 添加 TerminalCommand 面板**

在 ExecutablePanel 下方添加（作为替代面板，用 Visibility 控制显隐）：

```xml
<Border x:Name="TerminalCommandPanel"
        Grid.Row="10"
        Margin="0,18,0,0"
        Visibility="Collapsed">
    <StackPanel>
        <Border Padding="10"
                Background="#FFF3CD"
                BorderBrush="#FFC107"
                BorderThickness="1"
                CornerRadius="6">
            <TextBlock Foreground="#856404"
                       Text="将在当前项目目录的 Windows Terminal 中执行命令" />
        </Border>

        <TextBlock Margin="0,18,0,0"
                   FontWeight="SemiBold"
                   Foreground="{StaticResource StrongTextBrush}"
                   Text="命令内容" />
        <TextBox x:Name="CommandTextBox"
                 Margin="0,12,0,0"
                 Style="{StaticResource EditorTextBoxStyle}" />
        <TextBlock Margin="0,10,0,0"
                   Foreground="{StaticResource MutedTextBrush}"
                   Text="纯命令文本，不支持占位符" />
    </StackPanel>
</Border>
```

- [ ] **Step 4: 调整 RowDefinitions**

原 Row 10 拆分为 ExecutablePanel（Row 10）和 TerminalCommandPanel（同一 Row，叠放）。原有 Row 12（参数模板说明）已包含在 ExecutablePanel 内部。

- [ ] **Step 5: 验证 XAML 构建**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 6: 提交**

```bash
git add src/ProjectOpenTools/LauncherAppEditorWindow.xaml
git commit -m "feat(LauncherAppEditorWindow): 新增 LaunchModeComboBox 和 TerminalCommand 面板"
```

---

## Task 4: LauncherAppEditorWindow.xaml.cs 动态逻辑

**Files:**
- Modify: `src/ProjectOpenTools/LauncherAppEditorWindow.xaml.cs`

- [ ] **Step 1: 修改 PopulateFields 回填新字段**

```csharp
private void PopulateFields()
{
    NameTextBox.Text = EditedLauncherApp.Name;
    ExePathTextBox.Text = EditedLauncherApp.ExePath;
    ArgumentsTemplateTextBox.Text = EditedLauncherApp.ArgumentsTemplate;
    CommandTextBox.Text = EditedLauncherApp.CommandText;

    // 设置 LaunchMode
    foreach (ComboBoxItem item in LaunchModeComboBox.Items)
    {
        if (item.Tag?.ToString() == EditedLauncherApp.LaunchMode)
        {
            LaunchModeComboBox.SelectedItem = item;
            break;
        }
    }

    UpdatePanelVisibility();
}
```

- [ ] **Step 2: 添加 UpdatePanelVisibility 方法**

```csharp
private void UpdatePanelVisibility()
{
    bool isTerminalCommand = LaunchModeComboBox.SelectedItem is ComboBoxItem selectedItem
        && selectedItem.Tag?.ToString() == "TerminalCommand";

    ExecutablePanel.Visibility = isTerminalCommand ? Visibility.Collapsed : Visibility.Visible;
    TerminalCommandPanel.Visibility = isTerminalCommand ? Visibility.Visible : Visibility.Collapsed;
}
```

- [ ] **Step 3: 添加 LaunchModeComboBox_SelectionChanged 事件**

```csharp
private void LaunchModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
{
    UpdatePanelVisibility();
}
```

- [ ] **Step 4: 修改构造函数初始化**

```csharp
public LauncherAppEditorWindow(LauncherAppEntry? launcherAppEntry = null)
{
    InitializeComponent();

    EditedLauncherApp = launcherAppEntry == null
        ? new LauncherAppEntry()
        : new LauncherAppEntry
        {
            Name = launcherAppEntry.Name,
            ExePath = launcherAppEntry.ExePath,
            ArgumentsTemplate = launcherAppEntry.ArgumentsTemplate,
            LaunchMode = launcherAppEntry.LaunchMode,
            CommandText = launcherAppEntry.CommandText
        };

    PopulateFields();
}
```

- [ ] **Step 5: 修改 SaveButton_Click 校验和保存逻辑**

在现有校验后添加 TerminalCommand 校验：

```csharp
// TerminalCommand 模式校验
bool isTerminalCommand = LaunchModeComboBox.SelectedItem is ComboBoxItem selectedItem
    && selectedItem.Tag?.ToString() == "TerminalCommand";

if (isTerminalCommand)
{
    if (string.IsNullOrWhiteSpace(CommandTextBox.Text))
    {
        System.Windows.MessageBox.Show(this, "请填写命令内容。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        return;
    }
}
else
{
    if (string.IsNullOrWhiteSpace(ExePathTextBox.Text))
    {
        System.Windows.MessageBox.Show(this, "请填写应用 exe 路径。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        return;
    }
}
```

保存时包含新字段：

```csharp
EditedLauncherApp = new LauncherAppEntry
{
    Name = NameTextBox.Text.Trim(),
    ExePath = ExePathTextBox.Text.Trim(),
    ArgumentsTemplate = ArgumentsTemplateTextBox.Text.Trim(),
    LaunchMode = (LaunchModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Executable",
    CommandText = CommandTextBox.Text.Trim()
};
```

- [ ] **Step 6: 验证构建通过**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 7: 提交**

```bash
git add src/ProjectOpenTools/LauncherAppEditorWindow.xaml.cs
git commit -m "feat(LauncherAppEditorWindow): 新增 LaunchMode 切换逻辑和动态面板显隐"
```

---

## Task 5: LauncherAppManagerWindow 字段复制

**Files:**
- Modify: `src/ProjectOpenTools/LauncherAppManagerWindow.xaml.cs`

- [ ] **Step 1: 修改 ConfirmButton_Click 包含新字段**

```csharp
UpdatedApps = EditableApps
    .Select(item => new LauncherAppEntry
    {
        Name = item.Name,
        ExePath = item.ExePath,
        ArgumentsTemplate = item.ArgumentsTemplate,
        LaunchMode = item.LaunchMode,
        CommandText = item.CommandText
    })
    .ToList();
```

- [ ] **Step 2: 修改 AddAppButton_Click 和 EditAppButton_Click 图标加载逻辑**

AddApp 时 TerminalCommand 没有 ExePath，跳过图标加载：

```csharp
if (dialogResult == true)
{
    var newApp = editorWindow.EditedLauncherApp;
    if (newApp.LaunchMode != "TerminalCommand" && !string.IsNullOrWhiteSpace(newApp.ExePath))
    {
        newApp.IconImage = this.appIconService.LoadIcon(newApp.ExePath);
    }
    EditableApps.Add(newApp);
    RefreshEmptyState();
}
```

EditApp 同样处理：

```csharp
int selectedIndex = AppsListBox.SelectedIndex;
var editedApp = editorWindow.EditedLauncherApp;
if (editedApp.LaunchMode != "TerminalCommand" && !string.IsNullOrWhiteSpace(editedApp.ExePath))
{
    editedApp.IconImage = this.appIconService.LoadIcon(editedApp.ExePath);
}
EditableApps[selectedIndex] = editedApp;
```

- [ ] **Step 3: 验证构建通过**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: 提交**

```bash
git add src/ProjectOpenTools/LauncherAppManagerWindow.xaml.cs
git commit -m "feat(LauncherAppManagerWindow): 支持 LaunchMode 和 CommandText 字段"
```

---

## Task 6: 列表项显示（TerminalCommand 特殊展示）

**Files:**
- Modify: `src/ProjectOpenTools/LauncherAppManagerWindow.xaml` (ItemTemplate)

- [ ] **Step 1: 查看现有 ItemTemplate**

找到 AppsListBox 的 ItemsPanel / ItemTemplate 定义。

- [ ] **Step 2: 修改列表项模板，根据 LaunchMode 显示不同描述**

将现有的显示描述（exe 路径）改为根据 LaunchMode 条件显示：

```xml
<!-- 在现有 TextBlock 附近，将简单描述替换为条件显示 -->
<TextBlock Text="{Binding LaunchMode}"
          Visibility="Collapsed" /> <!-- 保留用于调试 -->

<!-- 替换原来的直接显示 ExePath，用 Converter 或 DataTrigger -->
```

**注意**：WPF XAML 中根据 ViewModel 条件显示不同文本需要 ValueConverter 或 DataTrigger。建议在 LauncherAppEntry 中添加一个计算属性 DisplayDescription：

```csharp
[JsonIgnore]
public string DisplayDescription => LaunchMode == "TerminalCommand"
    ? $"终端命令：{CommandText}"
    : string.IsNullOrEmpty(ArgumentsTemplate)
        ? ExePath
        : $"{ExePath} {ArgumentsTemplate}";
```

然后在 XAML 中绑定到 DisplayDescription。

- [ ] **Step 3: 如使用 DisplayDescription，修改 XAML ItemTemplate**

```xml
<TextBlock Text="{Binding DisplayDescription}"
           Foreground="{StaticResource MutedTextBrush}"
           TextTrimming="CharacterEllipsis" />
```

- [ ] **Step 4: 验证构建通过**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: 提交**

```bash
git add src/ProjectOpenTools/LauncherAppManagerWindow.xaml src/ProjectOpenTools/Models/LauncherAppEntry.cs
git commit -m "feat(LauncherAppManagerWindow): DisplayDescription 根据 LaunchMode 显示不同描述"
```

---

## Task 7: 单元测试

**Files:**
- Create: `tests/ProjectOpenTools.Tests/LauncherServiceTests.cs`
- Verify: `tests/ProjectOpenTools.Tests/ProjectOpenTools.Tests.csproj` 存在

- [ ] **Step 1: 创建测试文件**

```csharp
using ProjectOpenTools.Models;
using ProjectOpenTools.Services;
using Xunit;

namespace ProjectOpenTools.Tests;

public class LauncherServiceTests
{
    private readonly LauncherService _service = new LauncherService();

    [Fact]
    public void BuildArguments_WithEmptyTemplate_ReturnsQuotedProjectPath()
    {
        var app = new LauncherAppEntry { ArgumentsTemplate = "" };
        var result = _service.BuildArguments(app, @"C:\Projects\Test");
        Assert.Equal("\"C:\\Projects\\Test\"", result);
    }

    [Fact]
    public void BuildArguments_WithProjectPathPlaceholder_ReplacesCorrectly()
    {
        var app = new LauncherAppEntry { ArgumentsTemplate = "--folder {projectPath}" };
        var result = _service.BuildArguments(app, @"C:\Projects\Test");
        Assert.Equal("--folder \"C:\\Projects\\Test\"", result);
    }

    [Fact]
    public void BuildTerminalArguments_FormatsCorrectly()
    {
        var result = _service.BuildTerminalArguments("codex", @"C:\Projects\Test");
        Assert.Equal("new-tab -d \"C:\\Projects\\Test\" \"codex\"", result);
    }

    [Fact]
    public void ValidateLaunch_TerminalCommand_EmptyCommand_ReturnsFalse()
    {
        var app = new LauncherAppEntry { LaunchMode = "TerminalCommand", CommandText = "" };
        var result = _service.ValidateLaunch(app, @"C:\Projects\Test");
        Assert.False(result.IsSuccess);
        Assert.Contains("终端命令不能为空", result.Message);
    }

    [Fact]
    public void ValidateLaunch_TerminalCommand_ValidCommand_ChecksWtExe()
    {
        var app = new LauncherAppEntry { LaunchMode = "TerminalCommand", CommandText = "codex" };
        var result = _service.ValidateLaunch(app, @"C:\Projects\Test");
        // wt.exe 可能存在也可能不存在，取决于环境
        // 如果存在则成功，如果不存在则包含 "未找到 Windows Terminal"
        if (!result.IsSuccess)
        {
            Assert.Contains("Windows Terminal", result.Message);
        }
    }

    [Fact]
    public void ValidateLaunch_Executable_MissingExePath_ReturnsFalse()
    {
        var app = new LauncherAppEntry { LaunchMode = "Executable", ExePath = "" };
        var result = _service.ValidateLaunch(app, @"C:\Projects\Test");
        Assert.False(result.IsSuccess);
        Assert.Contains("应用路径不存在", result.Message);
    }

    [Fact]
    public void ValidateLaunch_BackwardCompatible_DefaultLaunchMode_IsExecutable()
    {
        // 老配置没有 LaunchMode 字段，默认为 Executable
        var app = new LauncherAppEntry { ExePath = "", ArgumentsTemplate = "" };
        Assert.Equal("Executable", app.LaunchMode);
    }
}
```

- [ ] **Step 2: 运行测试验证**

Run: `dotnet build .\tests\ProjectOpenTools.Tests\ProjectOpenTools.Tests.csproj -p:UseSharedCompilation=false`
Expected: BUILD SUCCEEDED

Run: `dotnet vstest .\tests\ProjectOpenTools.Tests\bin\Debug\net8.0-windows\ProjectOpenTools.Tests.dll`
Expected: 所有测试通过

- [ ] **Step 3: 提交**

```bash
git add tests/ProjectOpenTools.Tests/LauncherServiceTests.cs
git commit -m "test: 新增 LauncherService TerminalCommand 相关测试"
```

---

## Task 8: 集成验证

- [ ] **Step 1: 完整构建**

Run: `dotnet build .\src\ProjectOpenTools\ProjectOpenTools.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 2: 运行测试**

Run: `dotnet vstest .\tests\ProjectOpenTools.Tests\bin\Debug\net8.0-windows\ProjectOpenTools.Tests.dll`
Expected: 全部测试 PASS

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat: 完成终端命令启动方式全部功能"
```

---

## 实施检查清单

- [ ] Task 1: LauncherAppEntry 新增字段
- [ ] Task 2: LauncherService TerminalCommand 逻辑
- [ ] Task 3: EditorWindow XAML UI
- [ ] Task 4: EditorWindow 代码隐藏
- [ ] Task 5: ManagerWindow 字段复制
- [ ] Task 6: 列表显示 DisplayDescription
- [ ] Task 7: 单元测试
- [ ] Task 8: 集成验证和最终提交
