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

    /// <summary>
    /// 使用某个应用打开项目后，是否自动将主窗口隐藏到系统托盘。
    /// </summary>
    public bool AutoHideToTrayAfterLaunch { get; set; }

    /// <summary>
    /// 点击窗口关闭按钮时的行为。
    /// </summary>
    public CloseActionOption CloseAction { get; set; } = CloseActionOption.HideToTray;
}
