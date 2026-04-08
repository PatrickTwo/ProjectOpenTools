using System;
using System.IO;
using System.Text.Json;
using ProjectOpenTools.Models;

namespace ProjectOpenTools.Services;

/// <summary>
/// 负责将配置读写到本地 JSON 文件。
/// </summary>
public sealed class SettingsStorageService
{
    /// <summary>
    /// JSON 序列化配置。
    /// </summary>
    private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    /// <summary>
    /// 配置文件绝对路径。
    /// </summary>
    private readonly string settingsFilePath;

    /// <summary>
    /// 使用默认 AppData 路径创建服务。
    /// </summary>
    public SettingsStorageService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProjectOpenTools",
            "settings.json"))
    {
    }

    /// <summary>
    /// 使用指定配置文件路径创建服务，便于测试。
    /// </summary>
    public SettingsStorageService(string settingsFilePath)
    {
        this.settingsFilePath = settingsFilePath;
    }

    /// <summary>
    /// 获取配置文件路径。
    /// </summary>
    public string GetSettingsFilePath()
    {
        return this.settingsFilePath;
    }

    /// <summary>
    /// 从磁盘读取配置，首次启动时返回空配置。
    /// </summary>
    public AppSettings LoadSettings()
    {
        if (!File.Exists(this.settingsFilePath))
        {
            return new AppSettings();
        }

        string json = File.ReadAllText(this.settingsFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AppSettings();
        }

        AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, this.serializerOptions);
        return settings ?? new AppSettings();
    }

    /// <summary>
    /// 将配置保存到磁盘。
    /// </summary>
    public void SaveSettings(AppSettings appSettings)
    {
        string? directoryPath = Path.GetDirectoryName(this.settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string json = JsonSerializer.Serialize(appSettings, this.serializerOptions);
        File.WriteAllText(this.settingsFilePath, json);
    }
}
