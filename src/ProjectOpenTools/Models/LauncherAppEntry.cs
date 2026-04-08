using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ProjectOpenTools.Models;

/// <summary>
/// 外部应用配置条目。
/// </summary>
public sealed class LauncherAppEntry
{
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
    /// 应用图标，仅用于界面展示，不参与持久化。
    /// </summary>
    [JsonIgnore]
    public ImageSource? IconImage { get; set; }
}
