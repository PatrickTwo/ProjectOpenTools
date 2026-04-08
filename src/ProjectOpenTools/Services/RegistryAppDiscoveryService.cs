using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using ProjectOpenTools.Models;

namespace ProjectOpenTools.Services;

/// <summary>
/// 负责从注册表扫描可用应用。
/// </summary>
public sealed class RegistryAppDiscoveryService
{
    /// <summary>
    /// 需要扫描的 App Paths 根键。
    /// </summary>
    private static readonly string[] AppPathsRegistryLocations =
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths"
    };

    /// <summary>
    /// 需要扫描的卸载信息根键。
    /// </summary>
    private static readonly string[] UninstallRegistryLocations =
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

    /// <summary>
    /// 图标读取服务。
    /// </summary>
    private readonly AppIconService appIconService;

    /// <summary>
    /// 初始化注册表应用扫描服务。
    /// </summary>
    public RegistryAppDiscoveryService()
    {
        this.appIconService = new AppIconService();
    }

    /// <summary>
    /// 扫描并返回系统中的候选应用。
    /// </summary>
    public List<DiscoveredAppEntry> DiscoverApplications()
    {
        Dictionary<string, DiscoveredAppEntry> discoveredApps = new Dictionary<string, DiscoveredAppEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (RegistryHive registryHive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (RegistryView registryView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                CollectFromAppPaths(discoveredApps, registryHive, registryView);
                CollectFromUninstall(discoveredApps, registryHive, registryView);
            }
        }

        return discoveredApps.Values
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    #region 注册表扫描

    /// <summary>
    /// 从 App Paths 节点收集应用。
    /// </summary>
    private void CollectFromAppPaths(Dictionary<string, DiscoveredAppEntry> discoveredApps, RegistryHive registryHive, RegistryView registryView)
    {
        foreach (string registryLocation in AppPathsRegistryLocations)
        {
            using RegistryKey? baseKey = RegistryKey.OpenBaseKey(registryHive, registryView);
            using RegistryKey? appPathsKey = baseKey.OpenSubKey(registryLocation);
            if (appPathsKey == null)
            {
                continue;
            }

            foreach (string subKeyName in appPathsKey.GetSubKeyNames())
            {
                using RegistryKey? appKey = appPathsKey.OpenSubKey(subKeyName);
                if (appKey == null)
                {
                    continue;
                }

                string? exePath = NormalizeExecutablePath(appKey.GetValue(string.Empty)?.ToString());
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    continue;
                }

                string appName = ResolveApplicationName(subKeyName, exePath);
                AddDiscoveredApp(discoveredApps, appName, exePath);
            }
        }
    }

    /// <summary>
    /// 从卸载信息节点收集应用。
    /// </summary>
    private void CollectFromUninstall(Dictionary<string, DiscoveredAppEntry> discoveredApps, RegistryHive registryHive, RegistryView registryView)
    {
        foreach (string registryLocation in UninstallRegistryLocations)
        {
            using RegistryKey? baseKey = RegistryKey.OpenBaseKey(registryHive, registryView);
            using RegistryKey? uninstallKey = baseKey.OpenSubKey(registryLocation);
            if (uninstallKey == null)
            {
                continue;
            }

            foreach (string subKeyName in uninstallKey.GetSubKeyNames())
            {
                using RegistryKey? appKey = uninstallKey.OpenSubKey(subKeyName);
                if (appKey == null)
                {
                    continue;
                }

                string? displayName = appKey.GetValue("DisplayName")?.ToString();
                string? displayIcon = appKey.GetValue("DisplayIcon")?.ToString();
                string? installLocation = appKey.GetValue("InstallLocation")?.ToString();
                string? iconCandidatePath = NormalizeExecutablePath(displayIcon);
                string? installExecutablePath = TryFindExecutableFromInstallLocation(installLocation);
                string? exePath = iconCandidatePath ?? installExecutablePath;

                if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(exePath))
                {
                    continue;
                }

                AddDiscoveredApp(discoveredApps, displayName, exePath);
            }
        }
    }

    #endregion

    #region 数据清洗

    /// <summary>
    /// 向结果集中写入发现的应用。
    /// </summary>
    private void AddDiscoveredApp(Dictionary<string, DiscoveredAppEntry> discoveredApps, string appName, string exePath)
    {
        if (!File.Exists(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (discoveredApps.ContainsKey(exePath))
        {
            return;
        }

        discoveredApps.Add(exePath, new DiscoveredAppEntry
        {
            Name = appName,
            ExePath = exePath,
            ArgumentsTemplate = "{projectPath}",
            IconImage = this.appIconService.LoadIcon(exePath)
        });
    }

    /// <summary>
    /// 规范化 exe 路径。
    /// </summary>
    private static string? NormalizeExecutablePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        string sanitizedPath = rawPath.Trim().Trim('"');
        int commaIndex = sanitizedPath.IndexOf(',');
        if (commaIndex >= 0)
        {
            sanitizedPath = sanitizedPath.Substring(0, commaIndex).Trim().Trim('"');
        }

        if (File.Exists(sanitizedPath) && sanitizedPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return sanitizedPath;
        }

        return null;
    }

    /// <summary>
    /// 基于文件描述或文件名推断应用名称。
    /// </summary>
    private static string ResolveApplicationName(string registryName, string exePath)
    {
        try
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
            {
                return versionInfo.FileDescription;
            }

            if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
            {
                return versionInfo.ProductName;
            }
        }
        catch
        {
            // 文件版本读取失败时回退到文件名即可。
        }

        string fileName = Path.GetFileNameWithoutExtension(registryName);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        return Path.GetFileNameWithoutExtension(exePath);
    }

    /// <summary>
    /// 从安装目录中尝试定位一个 exe。
    /// </summary>
    private static string? TryFindExecutableFromInstallLocation(string? installLocation)
    {
        if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
        {
            return null;
        }

        try
        {
            string[] exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
            if (exeFiles.Length == 0)
            {
                return null;
            }

            return exeFiles[0];
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
