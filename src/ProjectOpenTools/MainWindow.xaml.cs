using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProjectOpenTools.Models;
using ProjectOpenTools.Services;

namespace ProjectOpenTools;

/// <summary>
/// 主窗口，负责项目选择、最近项目展示与应用启动。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 本地配置持久化服务。
    /// </summary>
    private readonly SettingsStorageService settingsStorageService;

    /// <summary>
    /// 最近项目列表整理服务。
    /// </summary>
    private readonly ProjectHistoryService projectHistoryService;

    /// <summary>
    /// 外部应用启动服务。
    /// </summary>
    private readonly LauncherService launcherService;

    /// <summary>
    /// 应用图标读取服务。
    /// </summary>
    private readonly AppIconService appIconService;

    /// <summary>
    /// 当前正在使用的应用配置。
    /// </summary>
    private AppSettings appSettings;

    /// <summary>
    /// 当前选中的项目路径。
    /// </summary>
    private string? currentProjectPath;

    #region 初始化与数据加载

    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // 使用代码隐藏维持轻量实现，避免为当前规模引入完整 MVVM 基础设施。
        this.settingsStorageService = new SettingsStorageService();
        this.projectHistoryService = new ProjectHistoryService();
        this.launcherService = new LauncherService();
        this.appIconService = new AppIconService();
        this.appSettings = new AppSettings();

        DataContext = this;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    /// <summary>
    /// 提供最近项目集合给界面绑定。
    /// </summary>
    public ObservableCollection<ProjectEntry> RecentProjects { get; } = new ObservableCollection<ProjectEntry>();

    /// <summary>
    /// 提供应用集合给界面绑定。
    /// </summary>
    public ObservableCollection<LauncherAppEntry> LauncherApps { get; } = new ObservableCollection<LauncherAppEntry>();

    /// <summary>
    /// 窗口加载时初始化配置。
    /// </summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettingsIntoWindow();
    }

    /// <summary>
    /// 关闭窗口前保存一次配置，避免最后状态丢失。
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveSettings();
    }

    /// <summary>
    /// 从磁盘读取配置并刷新界面。
    /// </summary>
    private void LoadSettingsIntoWindow()
    {
        this.appSettings = this.settingsStorageService.LoadSettings();
        RefreshCollections();
        UpdateCurrentProject(this.appSettings.RecentProjects.FirstOrDefault()?.Path);
        UpdateEmptyStateVisibility();
        UpdateStatus("准备就绪。");
    }

    /// <summary>
    /// 将内存中的配置同步到界面集合。
    /// </summary>
    private void RefreshCollections()
    {
        RecentProjects.Clear();
        LauncherApps.Clear();

        foreach (ProjectEntry project in this.appSettings.RecentProjects.OrderByDescending(item => item.LastOpenedAt))
        {
            RecentProjects.Add(project);
        }

        foreach (LauncherAppEntry launcherApp in this.appSettings.LauncherApps)
        {
            launcherApp.IconImage = this.appIconService.LoadIcon(launcherApp.ExePath);
            LauncherApps.Add(launcherApp);
        }
    }

    /// <summary>
    /// 根据当前状态刷新空列表提示。
    /// </summary>
    private void UpdateEmptyStateVisibility()
    {
        EmptyProjectsHintBorder.Visibility = RecentProjects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyAppsHintBorder.Visibility = LauncherApps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 更新当前项目并同步界面文字。
    /// </summary>
    private void UpdateCurrentProject(string? projectPath)
    {
        this.currentProjectPath = projectPath;
        CurrentProjectPathTextBlock.Text = string.IsNullOrWhiteSpace(projectPath) ? "尚未选择项目文件夹" : projectPath;

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            RecentProjectsListBox.SelectedItem = null;
            return;
        }

        ProjectEntry? targetProject = RecentProjects.FirstOrDefault(item => string.Equals(item.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        RecentProjectsListBox.SelectedItem = targetProject;
    }

    /// <summary>
    /// 保存当前配置到本地 JSON。
    /// </summary>
    private void SaveSettings()
    {
        this.settingsStorageService.SaveSettings(this.appSettings);
    }

    #endregion

    #region 项目选择与最近项目

    /// <summary>
    /// 选择项目文件夹。
    /// </summary>
    private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
    {
        using System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
        folderBrowserDialog.Description = "请选择要快速打开的项目文件夹";
        folderBrowserDialog.UseDescriptionForTitle = true;
        folderBrowserDialog.ShowNewFolderButton = false;

        if (!string.IsNullOrWhiteSpace(this.currentProjectPath) && Directory.Exists(this.currentProjectPath))
        {
            folderBrowserDialog.InitialDirectory = this.currentProjectPath;
        }

        System.Windows.Forms.DialogResult dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        ApplySelectedProject(folderBrowserDialog.SelectedPath, true);
    }

    /// <summary>
    /// 点击最近项目列表时切换当前项目。
    /// </summary>
    private void RecentProjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ProjectEntry? selectedProject = RecentProjectsListBox.SelectedItem as ProjectEntry;
        if (selectedProject == null)
        {
            return;
        }

        UpdateCurrentProject(selectedProject.Path);
        UpdateStatus($"已切换到项目：{selectedProject.DisplayName}");
    }

    /// <summary>
    /// 选择项目后更新最近项目列表。
    /// </summary>
    private void ApplySelectedProject(string projectPath, bool shouldUpdateHistory)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            UpdateStatus("项目路径无效，请重新选择。");
            return;
        }

        if (shouldUpdateHistory)
        {
            this.projectHistoryService.UpsertRecentProject(this.appSettings.RecentProjects, projectPath, DateTime.Now);
            SaveSettings();
            RefreshCollections();
            UpdateEmptyStateVisibility();
        }

        UpdateCurrentProject(projectPath);
        UpdateStatus($"已选中项目：{Path.GetFileName(projectPath)}");
    }

    #endregion

    #region 应用配置与启动

    /// <summary>
    /// 打开应用配置窗口。
    /// </summary>
    private void ManageAppsButton_Click(object sender, RoutedEventArgs e)
    {
        LauncherAppManagerWindow launcherAppManagerWindow = new LauncherAppManagerWindow(this.appSettings.LauncherApps);
        launcherAppManagerWindow.Owner = this;

        bool? dialogResult = launcherAppManagerWindow.ShowDialog();
        if (dialogResult != true)
        {
            return;
        }

        this.appSettings.LauncherApps = launcherAppManagerWindow.UpdatedApps;
        SaveSettings();
        RefreshCollections();
        UpdateEmptyStateVisibility();
        UpdateStatus("应用配置已更新。");
    }

    /// <summary>
    /// 使用选中的应用打开当前项目。
    /// </summary>
    private void LauncherAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not LauncherAppEntry launcherAppEntry)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(this.currentProjectPath))
        {
            UpdateStatus("请先选择项目文件夹，再执行打开操作。");
            return;
        }

        LaunchResult launchResult = this.launcherService.LaunchProject(launcherAppEntry, this.currentProjectPath);
        if (!launchResult.IsSuccess)
        {
            UpdateStatus(launchResult.Message);
            return;
        }

        ApplySelectedProject(this.currentProjectPath, true);
        UpdateStatus($"已用 {launcherAppEntry.Name} 打开项目：{Path.GetFileName(this.currentProjectPath)}");
    }

    #endregion

    #region 轻量状态反馈

    /// <summary>
    /// 更新底部状态栏文字。
    /// </summary>
    private void UpdateStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    #endregion
}
