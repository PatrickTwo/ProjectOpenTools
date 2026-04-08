using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectOpenTools.Models;

namespace ProjectOpenTools;

/// <summary>
/// 系统注册表应用选择窗口。
/// </summary>
public partial class RegistryAppPickerWindow : System.Windows.Window
{
    /// <summary>
    /// 供界面展示的已发现应用列表。
    /// </summary>
    public ObservableCollection<DiscoveredAppEntry> DiscoveredApps { get; } = new ObservableCollection<DiscoveredAppEntry>();

    /// <summary>
    /// 用户最终选中的系统应用。
    /// </summary>
    public DiscoveredAppEntry? SelectedDiscoveredApp { get; private set; }

    /// <summary>
    /// 初始化窗口。
    /// </summary>
    public RegistryAppPickerWindow(IReadOnlyCollection<DiscoveredAppEntry> discoveredApps)
    {
        InitializeComponent();
        DataContext = this;

        foreach (DiscoveredAppEntry discoveredApp in discoveredApps)
        {
            DiscoveredApps.Add(discoveredApp);
        }

        EmptyRegistryAppsBorder.Visibility = DiscoveredApps.Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    /// <summary>
    /// 导入当前选中的应用。
    /// </summary>
    private void ImportButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DiscoveredAppEntry? selectedDiscoveredApp = RegistryAppsListBox.SelectedItem as DiscoveredAppEntry;
        if (selectedDiscoveredApp == null)
        {
            System.Windows.MessageBox.Show(this, "请先选择要导入的应用。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        SelectedDiscoveredApp = selectedDiscoveredApp;
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 取消导入。
    /// </summary>
    private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
