using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ProjectOpenTools.Models;
using ProjectOpenTools.Services;

namespace ProjectOpenTools.Tests;

/// <summary>
/// 核心服务测试。
/// </summary>
public sealed class ProjectOpenToolsServiceTests
{
    #region 最近项目排序

    /// <summary>
    /// 重复添加同一路径时应只保留一条记录，并更新时间。
    /// </summary>
    [Fact]
    public void UpsertRecentProject_ShouldUpdateExistingItemAndKeepSingleRecord()
    {
        ProjectHistoryService projectHistoryService = new ProjectHistoryService();
        List<ProjectEntry> recentProjects = new List<ProjectEntry>
        {
            new ProjectEntry
            {
                Path = @"C:\Work\Demo",
                DisplayName = "Demo",
                LastOpenedAt = new DateTime(2026, 4, 1, 8, 0, 0)
            }
        };

        projectHistoryService.UpsertRecentProject(recentProjects, @"C:\Work\Demo", new DateTime(2026, 4, 2, 9, 30, 0));

        Assert.Single(recentProjects);
        Assert.Equal(new DateTime(2026, 4, 2, 9, 30, 0), recentProjects[0].LastOpenedAt);
    }

    /// <summary>
    /// 新增项目后应按最近时间倒序排列。
    /// </summary>
    [Fact]
    public void UpsertRecentProject_ShouldOrderProjectsByLastOpenedAtDescending()
    {
        ProjectHistoryService projectHistoryService = new ProjectHistoryService();
        List<ProjectEntry> recentProjects = new List<ProjectEntry>
        {
            new ProjectEntry
            {
                Path = @"C:\Work\Old",
                DisplayName = "Old",
                LastOpenedAt = new DateTime(2026, 4, 1, 8, 0, 0)
            }
        };

        projectHistoryService.UpsertRecentProject(recentProjects, @"C:\Work\New", new DateTime(2026, 4, 2, 9, 30, 0));

        Assert.Equal(@"C:\Work\New", recentProjects[0].Path);
        Assert.Equal(@"C:\Work\Old", recentProjects[1].Path);
    }

    #endregion

    #region 启动参数构建

    /// <summary>
    /// 参数模板为空时应默认传入带引号的项目路径。
    /// </summary>
    [Fact]
    public void BuildArguments_ShouldReturnQuotedProjectPath_WhenTemplateIsEmpty()
    {
        LauncherService launcherService = new LauncherService();
        LauncherAppEntry launcherAppEntry = new LauncherAppEntry
        {
            Name = "VS Code",
            ExePath = @"C:\Tools\Code.exe",
            ArgumentsTemplate = string.Empty
        };

        string arguments = launcherService.BuildArguments(launcherAppEntry, @"C:\Work\My Project");

        Assert.Equal("\"C:\\Work\\My Project\"", arguments);
    }

    /// <summary>
    /// 参数模板包含占位符时应替换成带引号的项目路径。
    /// </summary>
    [Fact]
    public void BuildArguments_ShouldReplacePlaceholder_WhenTemplateContainsProjectPath()
    {
        LauncherService launcherService = new LauncherService();
        LauncherAppEntry launcherAppEntry = new LauncherAppEntry
        {
            Name = "Trae",
            ExePath = @"C:\Tools\Trae.exe",
            ArgumentsTemplate = "--folder-uri {projectPath}"
        };

        string arguments = launcherService.BuildArguments(launcherAppEntry, @"C:\Work\My Project");

        Assert.Equal("--folder-uri \"C:\\Work\\My Project\"", arguments);
    }

    /// <summary>
    /// 终端命令模式应构建 Windows Terminal 启动信息。
    /// </summary>
    [Fact]
    public void BuildProcessStartInfo_ShouldCreateWindowsTerminalStartInfo_WhenLaunchModeIsTerminalCommand()
    {
        LauncherService launcherService = new LauncherService();
        LauncherAppEntry launcherAppEntry = new LauncherAppEntry
        {
            LaunchMode = LaunchMode.TerminalCommand,
            Name = "Codex CLI",
            CommandText = "codex"
        };

        ProcessStartInfo processStartInfo = launcherService.BuildProcessStartInfo(launcherAppEntry, @"C:\Work\My Project");
        string expectedEncodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes("codex"));

        Assert.Equal("wt.exe", processStartInfo.FileName);
        Assert.Equal(@"C:\Work\My Project", processStartInfo.WorkingDirectory);
        Assert.Contains("-d \"C:\\Work\\My Project\"", processStartInfo.Arguments, StringComparison.Ordinal);
        Assert.Contains($"-EncodedCommand {expectedEncodedCommand}", processStartInfo.Arguments, StringComparison.Ordinal);
    }

    /// <summary>
    /// 终端命令为空时应阻止启动。
    /// </summary>
    [Fact]
    public void ValidateLaunch_ShouldFail_WhenTerminalCommandIsEmpty()
    {
        LauncherService launcherService = new LauncherService();
        LauncherAppEntry launcherAppEntry = new LauncherAppEntry
        {
            LaunchMode = LaunchMode.TerminalCommand,
            Name = "Codex CLI",
            CommandText = string.Empty
        };

        string tempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;

        try
        {
            LaunchResult launchResult = launcherService.ValidateLaunch(launcherAppEntry, tempDirectory);

            Assert.False(launchResult.IsSuccess);
            Assert.Equal("请先填写终端命令，再执行打开操作。", launchResult.Message);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    #endregion

    #region 配置持久化

    /// <summary>
    /// 保存后再次读取应保留最近项目和应用列表。
    /// </summary>
    [Fact]
    public void SettingsStorageService_ShouldPersistSettingsToJsonFile()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "ProjectOpenToolsTests", Guid.NewGuid().ToString("N"));
        string settingsFilePath = Path.Combine(tempDirectory, "settings.json");
        SettingsStorageService settingsStorageService = new SettingsStorageService(settingsFilePath);

        AppSettings appSettings = new AppSettings
        {
            RecentProjects = new List<ProjectEntry>
            {
                new ProjectEntry
                {
                    Path = @"C:\Work\Alpha",
                    DisplayName = "Alpha",
                    LastOpenedAt = new DateTime(2026, 4, 2, 9, 30, 0)
                }
            },
            LauncherApps = new List<LauncherAppEntry>
            {
                new LauncherAppEntry
                {
                    Name = "VS Code",
                    ExePath = @"C:\Tools\Code.exe",
                    ArgumentsTemplate = "{projectPath}"
                },
                new LauncherAppEntry
                {
                    LaunchMode = LaunchMode.TerminalCommand,
                    Name = "Codex CLI",
                    CommandText = "codex"
                }
            }
        };

        try
        {
            settingsStorageService.SaveSettings(appSettings);
            AppSettings loadedSettings = settingsStorageService.LoadSettings();

            Assert.Single(loadedSettings.RecentProjects);
            Assert.Equal(2, loadedSettings.LauncherApps.Count);
            Assert.Equal("Alpha", loadedSettings.RecentProjects[0].DisplayName);
            Assert.Equal("VS Code", loadedSettings.LauncherApps[0].Name);
            Assert.Equal(LaunchMode.TerminalCommand, loadedSettings.LauncherApps[1].LaunchMode);
            Assert.Equal("codex", loadedSettings.LauncherApps[1].CommandText);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    #endregion
}
