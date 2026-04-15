namespace ProjectOpenTools.Models;

/// <summary>
/// 定义点击窗口关闭按钮时的行为。
/// </summary>
public enum CloseActionOption
{
    /// <summary>
    /// 隐藏到系统托盘。
    /// </summary>
    HideToTray = 0,

    /// <summary>
    /// 直接退出程序。
    /// </summary>
    ExitApplication = 1
}
