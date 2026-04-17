namespace ProjectOpenTools.Models;

/// <summary>
/// 启动项目时使用的方式。
/// </summary>
public enum LaunchMode
{
    /// <summary>
    /// 通过可执行文件和参数模板启动。
    /// </summary>
    Executable = 0,

    /// <summary>
    /// 在终端中执行预设命令。
    /// </summary>
    TerminalCommand = 1
}
