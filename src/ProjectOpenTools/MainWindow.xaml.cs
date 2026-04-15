using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

    /// <summary>
    /// 系统托盘图标，用于隐藏后继续驻留。
    /// </summary>
    private readonly System.Windows.Forms.NotifyIcon notifyIcon;

    /// <summary>
    /// 标记当前是否由托盘菜单主动触发退出。
    /// </summary>
    private bool isExitRequested;

    /// <summary>
    /// 标记是否已提示过“最小化到托盘”的引导信息。
    /// </summary>
    private bool hasShownTrayHint;

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
        this.notifyIcon = CreateNotifyIcon();

        DataContext = this;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
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
        // 根据用户偏好决定点击关闭按钮时是隐藏到托盘还是直接退出。
        if (!this.isExitRequested && this.appSettings.CloseAction == CloseActionOption.HideToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        SaveSettings();
    }

    /// <summary>
    /// 窗口真正关闭后释放托盘资源。
    /// </summary>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // 释放系统托盘对象，避免程序退出后仍残留空白图标。
        this.notifyIcon.Visible = false;
        this.notifyIcon.Dispose();
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

    #region 托盘驻留与窗口恢复

    /// <summary>
    /// 创建系统托盘图标与快捷菜单。
    /// </summary>
    /// <returns>配置完成的托盘图标实例。</returns>
    private System.Windows.Forms.NotifyIcon CreateNotifyIcon()
    {
        System.Windows.Forms.NotifyIcon trayNotifyIcon = new System.Windows.Forms.NotifyIcon();
        System.Windows.Forms.ContextMenuStrip contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
        System.Windows.Forms.ToolStripMenuItem showMainWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem("显示主窗口");
        System.Windows.Forms.ToolStripMenuItem exitApplicationMenuItem = new System.Windows.Forms.ToolStripMenuItem("退出程序");
        System.Drawing.Icon? processIcon = LoadTrayIcon();

        showMainWindowMenuItem.Click += ShowMainWindowMenuItem_Click;
        exitApplicationMenuItem.Click += ExitApplicationMenuItem_Click;

        contextMenuStrip.Items.Add(showMainWindowMenuItem);
        contextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenuStrip.Items.Add(exitApplicationMenuItem);

        trayNotifyIcon.Text = "项目启动器";
        trayNotifyIcon.Visible = true;
        trayNotifyIcon.ContextMenuStrip = contextMenuStrip;
        trayNotifyIcon.DoubleClick += NotifyIcon_DoubleClick;

        if (processIcon != null)
        {
            // 托盘图标直接复用当前可执行文件图标，避免再维护第二套路径。
            trayNotifyIcon.Icon = processIcon;
        }

        return trayNotifyIcon;
    }

    /// <summary>
    /// 读取当前进程图标，供系统托盘使用。
    /// </summary>
    /// <returns>可用于托盘显示的图标对象。</returns>
    private static System.Drawing.Icon? LoadTrayIcon()
    {
        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return null;
        }

        // 从当前程序文件中提取图标，确保托盘与 exe 保持一致。
        return System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
    }

    /// <summary>
    /// 将窗口隐藏到系统托盘。
    /// </summary>
    private void HideToTray()
    {
        // 隐藏主窗口并从任务栏移除，只保留右下角托盘入口。
        Hide();
        ShowInTaskbar = false;
        WindowState = WindowState.Minimized;
        UpdateStatus("项目启动器已隐藏到系统托盘。");

        if (!this.hasShownTrayHint)
        {
            // 首次隐藏时给出提示，帮助用户理解新的关闭行为。
            this.notifyIcon.ShowBalloonTip(2000, "项目启动器", "程序已隐藏到右下角托盘，双击图标可恢复窗口。", System.Windows.Forms.ToolTipIcon.Info);
            this.hasShownTrayHint = true;
        }
    }

    /// <summary>
    /// 从系统托盘恢复主窗口。
    /// </summary>
    private void RestoreFromTray()
    {
        // 先恢复到普通状态，再激活窗口，避免窗口被系统留在后台。
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
        UpdateStatus("已从系统托盘恢复窗口。");
    }

    /// <summary>
    /// 双击托盘图标时恢复主窗口。
    /// </summary>
    private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
    {
        RestoreFromTray();
    }

    /// <summary>
    /// 点击“显示主窗口”菜单时恢复窗口。
    /// </summary>
    private void ShowMainWindowMenuItem_Click(object? sender, EventArgs e)
    {
        RestoreFromTray();
    }

    /// <summary>
    /// 点击“退出程序”菜单时真正关闭应用。
    /// </summary>
    private void ExitApplicationMenuItem_Click(object? sender, EventArgs e)
    {
        // 标记为真实退出后再关闭窗口，避免再次被 Closing 事件拦截。
        this.isExitRequested = true;
        Close();
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
    /// 打开偏好设置窗口。
    /// </summary>
    private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsWindow settingsWindow = new SettingsWindow(this.appSettings);
        settingsWindow.Owner = this;

        bool? dialogResult = settingsWindow.ShowDialog();
        if (dialogResult != true)
        {
            return;
        }

        // 将窗口返回的设置直接写回当前配置并持久化。
        this.appSettings.AutoHideToTrayAfterLaunch = settingsWindow.AutoHideToTrayAfterLaunch;
        this.appSettings.CloseAction = settingsWindow.CloseAction;
        SaveSettings();
        UpdateStatus("偏好设置已保存。");
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

        if (this.appSettings.AutoHideToTrayAfterLaunch)
        {
            // 按用户偏好，在成功启动项目后立即隐藏主窗口。
            HideToTray();
        }
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
