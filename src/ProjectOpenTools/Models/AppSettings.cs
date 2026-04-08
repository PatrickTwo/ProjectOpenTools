using System.Collections.Generic;

namespace ProjectOpenTools.Models;

/// <summary>
/// 应用程序本地配置。
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// 最近使用的项目列表。
    /// </summary>
    public List<ProjectEntry> RecentProjects { get; set; } = new List<ProjectEntry>();

    /// <summary>
    /// 可用于打开项目的应用列表。
    /// </summary>
    public List<LauncherAppEntry> LauncherApps { get; set; } = new List<LauncherAppEntry>();
}
