using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ProjectOpenTools.Models;

/// <summary>
/// 外部应用配置条目。
/// </summary>
public sealed class LauncherAppEntry
{
    /// <summary>
    /// 启动方式，未配置时默认走可执行文件模式，兼容历史配置。
    /// </summary>
    public LaunchMode LaunchMode { get; set; } = LaunchMode.Executable;

    /// <summary>
    /// 应用显示名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 应用可执行文件路径。
    /// </summary>
    public string ExePath { get; set; } = string.Empty;

    /// <summary>
    /// 启动参数模板，支持 {projectPath} 占位符。
    /// </summary>
    public string ArgumentsTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 终端启动时执行的命令。
    /// </summary>
    public string CommandText { get; set; } = string.Empty;

    /// <summary>
    /// 应用图标，仅用于界面展示，不参与持久化。
    /// </summary>
    [JsonIgnore]
    public ImageSource? IconImage { get; set; }

    /// <summary>
    /// 列表中展示的启动摘要。
    /// </summary>
    [JsonIgnore]
    public string SummaryText => this.LaunchMode == LaunchMode.TerminalCommand
        ? $"终端命令：{this.CommandText}"
        : $"参数模板：{this.ArgumentsTemplate}";
}
