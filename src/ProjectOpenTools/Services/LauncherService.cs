using System;
using System.Diagnostics;
using System.IO;
using ProjectOpenTools.Models;

namespace ProjectOpenTools.Services;

/// <summary>
/// 负责校验启动参数并调用外部进程。
/// </summary>
public sealed class LauncherService
{
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

    /// <summary>
    /// 仅校验启动前的必要条件。
    /// </summary>
    public LaunchResult ValidateLaunch(LauncherAppEntry launcherAppEntry, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return new LaunchResult(false, "当前项目路径不存在，请重新选择项目。");
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
    /// 为带空格的参数补充命令行引号。
    /// </summary>
    private static string QuoteArgument(string value)
    {
        return $"\"{value}\"";
    }
}
