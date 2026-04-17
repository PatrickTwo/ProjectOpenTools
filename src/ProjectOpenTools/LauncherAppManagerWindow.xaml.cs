using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectOpenTools.Models;
using ProjectOpenTools.Services;

namespace ProjectOpenTools;

/// <summary>
/// 应用列表管理窗口。
/// </summary>
public partial class LauncherAppManagerWindow : System.Windows.Window
{
    /// <summary>
    /// 注册表应用扫描服务。
    /// </summary>
    private readonly RegistryAppDiscoveryService registryAppDiscoveryService;

    /// <summary>
    /// 应用图标读取服务。
    /// </summary>
    private readonly AppIconService appIconService;

    /// <summary>
    /// 供界面编辑的应用列表。
    /// </summary>
    public ObservableCollection<LauncherAppEntry> EditableApps { get; } = new ObservableCollection<LauncherAppEntry>();

    /// <summary>
    /// 用户确认后的应用列表。
    /// </summary>
    public List<LauncherAppEntry> UpdatedApps { get; private set; } = new List<LauncherAppEntry>();

    #region 初始化与列表同步

    /// <summary>
    /// 初始化应用管理窗口。
    /// </summary>
    public LauncherAppManagerWindow(IReadOnlyCollection<LauncherAppEntry> launcherApps)
    {
        InitializeComponent();
        DataContext = this;
        this.registryAppDiscoveryService = new RegistryAppDiscoveryService();
        this.appIconService = new AppIconService();

        foreach (LauncherAppEntry launcherApp in launcherApps)
        {
            EditableApps.Add(new LauncherAppEntry
            {
                LaunchMode = launcherApp.LaunchMode,
                Name = launcherApp.Name,
                ExePath = launcherApp.ExePath,
                ArgumentsTemplate = launcherApp.ArgumentsTemplate,
                CommandText = launcherApp.CommandText,
                IconImage = this.appIconService.LoadIcon(launcherApp.ExePath)
            });
        }

        RefreshEmptyState();
    }

    /// <summary>
    /// 刷新空列表提示。
    /// </summary>
    private void RefreshEmptyState()
    {
        EmptyAppsBorder.Visibility = EditableApps.Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    #endregion

    #region 列表操作

    /// <summary>
    /// 新增应用配置。
    /// </summary>
    private void AddAppButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LauncherAppEditorWindow editorWindow = new LauncherAppEditorWindow();
        editorWindow.Owner = this;

        bool? dialogResult = editorWindow.ShowDialog();
        if (dialogResult == true)
        {
            editorWindow.EditedLauncherApp.IconImage = this.appIconService.LoadIcon(editorWindow.EditedLauncherApp.ExePath);
            EditableApps.Add(editorWindow.EditedLauncherApp);
            RefreshEmptyState();
        }
    }

    /// <summary>
    /// 编辑选中的应用配置。
    /// </summary>
    private void EditAppButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LauncherAppEntry? selectedApp = AppsListBox.SelectedItem as LauncherAppEntry;
        if (selectedApp == null)
        {
            System.Windows.MessageBox.Show(this, "请先选择要编辑的应用。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        LauncherAppEditorWindow editorWindow = new LauncherAppEditorWindow(selectedApp);
        editorWindow.Owner = this;

        bool? dialogResult = editorWindow.ShowDialog();
        if (dialogResult != true)
        {
            return;
        }

        int selectedIndex = AppsListBox.SelectedIndex;
        editorWindow.EditedLauncherApp.IconImage = this.appIconService.LoadIcon(editorWindow.EditedLauncherApp.ExePath);
        EditableApps[selectedIndex] = editorWindow.EditedLauncherApp;
        AppsListBox.SelectedIndex = selectedIndex;
    }

    /// <summary>
    /// 从系统注册表读取应用并导入。
    /// </summary>
    private void ImportFromRegistryButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        List<DiscoveredAppEntry> discoveredApps = this.registryAppDiscoveryService.DiscoverApplications();
        RegistryAppPickerWindow registryAppPickerWindow = new RegistryAppPickerWindow(discoveredApps);
        registryAppPickerWindow.Owner = this;

        bool? dialogResult = registryAppPickerWindow.ShowDialog();
        if (dialogResult != true || registryAppPickerWindow.SelectedDiscoveredApp == null)
        {
            return;
        }

        DiscoveredAppEntry selectedDiscoveredApp = registryAppPickerWindow.SelectedDiscoveredApp;
        bool exists = EditableApps.Any(item => string.Equals(item.ExePath, selectedDiscoveredApp.ExePath, StringComparison.OrdinalIgnoreCase));
        if (exists)
        {
            System.Windows.MessageBox.Show(this, "该应用已经在列表中了。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        EditableApps.Add(new LauncherAppEntry
        {
            LaunchMode = LaunchMode.Executable,
            Name = selectedDiscoveredApp.Name,
            ExePath = selectedDiscoveredApp.ExePath,
            ArgumentsTemplate = selectedDiscoveredApp.ArgumentsTemplate,
            IconImage = selectedDiscoveredApp.IconImage
        });

        RefreshEmptyState();
    }

    /// <summary>
    /// 删除选中的应用配置。
    /// </summary>
    private void DeleteAppButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LauncherAppEntry? selectedApp = AppsListBox.SelectedItem as LauncherAppEntry;
        if (selectedApp == null)
        {
            System.Windows.MessageBox.Show(this, "请先选择要删除的应用。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        System.Windows.MessageBoxResult deleteResult = System.Windows.MessageBox.Show(
            this,
            $"确定要删除应用“{selectedApp.Name}”吗？",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (deleteResult != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        EditableApps.Remove(selectedApp);
        RefreshEmptyState();
    }

    #endregion

    #region 确认与取消

    /// <summary>
    /// 确认保存应用配置。
    /// </summary>
    private void ConfirmButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        UpdatedApps = EditableApps
            .Select(item => new LauncherAppEntry
            {
                LaunchMode = item.LaunchMode,
                Name = item.Name,
                ExePath = item.ExePath,
                ArgumentsTemplate = item.ArgumentsTemplate,
                CommandText = item.CommandText
            })
            .ToList();

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 取消当前编辑。
    /// </summary>
    private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #endregion
}
