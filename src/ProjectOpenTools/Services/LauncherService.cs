using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using ProjectOpenTools.Models;

namespace ProjectOpenTools.Services;

/// <summary>
/// 负责校验启动参数并调用外部进程。
/// </summary>
public sealed class LauncherService
{
    /// <summary>
    /// Windows Terminal 的命令名。
    /// </summary>
    private const string WindowsTerminalExecutable = "wt.exe";

    /// <summary>
    /// 使用指定应用打开项目。
    /// </summary>
    public LaunchResult LaunchProject(LauncherAppEntry launcherAppEntry, string projectPath)
    {
        LaunchResult validationResult = ValidateLaunch(launcherAppEntry, projectPath);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        ProcessStartInfo processStartInfo = BuildProcessStartInfo(launcherAppEntry, projectPath);
        try
        {
            Process.Start(processStartInfo);
            return new LaunchResult(true, "启动成功。");
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            if (launcherAppEntry.LaunchMode == LaunchMode.TerminalCommand)
            {
                return new LaunchResult(false, "未找到 Windows Terminal（wt.exe），请先安装或检查系统终端配置。");
            }

            return new LaunchResult(false, $"启动失败：{exception.Message}");
        }
    }

    /// <summary>
    /// 仅校验启动前的必要条件。
    /// </summary>
    public LaunchResult ValidateLaunch(LauncherAppEntry launcherAppEntry, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return new LaunchResult(false, "当前项目路径不存在，请重新选择项目。");
        }

        if (launcherAppEntry.LaunchMode == LaunchMode.TerminalCommand)
        {
            if (string.IsNullOrWhiteSpace(launcherAppEntry.CommandText))
            {
                return new LaunchResult(false, "请先填写终端命令，再执行打开操作。");
            }

            return new LaunchResult(true, "校验通过。");
        }

        if (string.IsNullOrWhiteSpace(launcherAppEntry.ExePath) || !File.Exists(launcherAppEntry.ExePath))
        {
            return new LaunchResult(false, $"应用路径不存在：{launcherAppEntry.ExePath}");
        }

        return new LaunchResult(true, "校验通过。");
    }

    /// <summary>
    /// 根据参数模板构建实际启动参数。
    /// </summary>
    public string BuildArguments(LauncherAppEntry launcherAppEntry, string projectPath)
    {
        string quotedProjectPath = QuoteArgument(projectPath);
        if (string.IsNullOrWhiteSpace(launcherAppEntry.ArgumentsTemplate))
        {
            return quotedProjectPath;
        }

        if (launcherAppEntry.ArgumentsTemplate.Contains("{projectPath}", StringComparison.Ordinal))
        {
            return launcherAppEntry.ArgumentsTemplate.Replace("{projectPath}", quotedProjectPath, StringComparison.Ordinal);
        }

        return launcherAppEntry.ArgumentsTemplate;
    }

    /// <summary>
    /// 构建实际启动进程信息，便于复用和测试。
    /// </summary>
    public ProcessStartInfo BuildProcessStartInfo(LauncherAppEntry launcherAppEntry, string projectPath)
    {
        if (launcherAppEntry.LaunchMode == LaunchMode.TerminalCommand)
        {
            return new ProcessStartInfo
            {
                FileName = WindowsTerminalExecutable,
                Arguments = BuildTerminalArguments(launcherAppEntry.CommandText, projectPath),
                UseShellExecute = true,
                WorkingDirectory = projectPath
            };
        }

        return new ProcessStartInfo
        {
            FileName = launcherAppEntry.ExePath,
            Arguments = BuildArguments(launcherAppEntry, projectPath),
            UseShellExecute = true,
            WorkingDirectory = projectPath
        };
    }

    /// <summary>
    /// 构建 Windows Terminal 的启动参数。
    /// </summary>
    public string BuildTerminalArguments(string commandText, string projectPath)
    {
        string encodedCommand = EncodePowerShellCommand(commandText);
        return $"-d {QuoteArgument(projectPath)} powershell.exe -NoExit -EncodedCommand {encodedCommand}";
    }

    /// <summary>
    /// 为带空格的参数补充命令行引号。
    /// </summary>
    private static string QuoteArgument(string value)
    {
        return $"\"{value}\"";
    }

    /// <summary>
    /// 将 PowerShell 命令编码成 Base64，避免命令中的引号干扰终端启动参数。
    /// </summary>
    private static string EncodePowerShellCommand(string commandText)
    {
        byte[] commandBytes = Encoding.Unicode.GetBytes(commandText);
        return Convert.ToBase64String(commandBytes);
    }
}
