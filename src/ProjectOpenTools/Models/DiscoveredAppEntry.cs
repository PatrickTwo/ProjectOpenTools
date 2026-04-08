using System.Windows.Media;

namespace ProjectOpenTools.Models;

/// <summary>
/// 从系统注册表中发现的应用信息。
/// </summary>
public sealed class DiscoveredAppEntry
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
    /// 默认启动参数模板。
    /// </summary>
    public string ArgumentsTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 应用图标。
    /// </summary>
    public ImageSource? IconImage { get; set; }
}
