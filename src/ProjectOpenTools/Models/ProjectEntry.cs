using System;

namespace ProjectOpenTools.Models;

/// <summary>
/// 最近项目条目。
/// </summary>
public sealed class ProjectEntry
{
    /// <summary>
    /// 项目绝对路径。
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 用于界面展示的项目名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 最近一次使用时间。
    /// </summary>
    public DateTime LastOpenedAt { get; set; }
}
