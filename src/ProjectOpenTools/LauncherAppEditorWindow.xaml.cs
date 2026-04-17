using ProjectOpenTools.Models;

namespace ProjectOpenTools;

/// <summary>
/// 应用配置编辑窗口。
/// </summary>
public partial class LauncherAppEditorWindow : System.Windows.Window
{
    /// <summary>
    /// 编辑完成后的配置结果。
    /// </summary>
    public LauncherAppEntry EditedLauncherApp { get; private set; }

    #region 初始化与回填

    /// <summary>
    /// 初始化编辑窗口。
    /// </summary>
    public LauncherAppEditorWindow(LauncherAppEntry? launcherAppEntry = null)
    {
        InitializeComponent();

        EditedLauncherApp = launcherAppEntry == null
            ? new LauncherAppEntry()
            : new LauncherAppEntry
            {
                LaunchMode = launcherAppEntry.LaunchMode,
                Name = launcherAppEntry.Name,
                ExePath = launcherAppEntry.ExePath,
                ArgumentsTemplate = launcherAppEntry.ArgumentsTemplate,
                CommandText = launcherAppEntry.CommandText
            };

        PopulateFields();
    }

    /// <summary>
    /// 将已有数据回填到输入框。
    /// </summary>
    private void PopulateFields()
    {
        NameTextBox.Text = EditedLauncherApp.Name;
        ExePathTextBox.Text = EditedLauncherApp.ExePath;
        ArgumentsTemplateTextBox.Text = EditedLauncherApp.ArgumentsTemplate;
        CommandTextTextBox.Text = EditedLauncherApp.CommandText;
        LaunchModeComboBox.SelectedIndex = EditedLauncherApp.LaunchMode == LaunchMode.TerminalCommand ? 1 : 0;
        UpdateEditorByLaunchMode(EditedLauncherApp.LaunchMode);
    }

    #endregion

    #region 交互事件

    /// <summary>
    /// 浏览 exe 路径。
    /// </summary>
    private void BrowseExePathButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
        openFileDialog.Title = "请选择应用程序 exe 文件";
        openFileDialog.Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*";

        bool? dialogResult = openFileDialog.ShowDialog(this);
        if (dialogResult == true)
        {
            ExePathTextBox.Text = openFileDialog.FileName;
        }
    }

    /// <summary>
    /// 切换启动方式时同步显示对应输入区域。
    /// </summary>
    private void LaunchModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        LaunchMode launchMode = GetSelectedLaunchMode();
        UpdateEditorByLaunchMode(launchMode);
    }

    /// <summary>
    /// 保存当前输入。
    /// </summary>
    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            System.Windows.MessageBox.Show(this, "请填写应用显示名称。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        LaunchMode launchMode = GetSelectedLaunchMode();
        if (launchMode == LaunchMode.Executable && string.IsNullOrWhiteSpace(ExePathTextBox.Text))
        {
            System.Windows.MessageBox.Show(this, "请填写应用 exe 路径。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (launchMode == LaunchMode.TerminalCommand && string.IsNullOrWhiteSpace(CommandTextTextBox.Text))
        {
            System.Windows.MessageBox.Show(this, "请填写终端命令。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        EditedLauncherApp = new LauncherAppEntry
        {
            LaunchMode = launchMode,
            Name = NameTextBox.Text.Trim(),
            ExePath = launchMode == LaunchMode.Executable ? ExePathTextBox.Text.Trim() : string.Empty,
            ArgumentsTemplate = launchMode == LaunchMode.Executable ? ArgumentsTemplateTextBox.Text.Trim() : string.Empty,
            CommandText = launchMode == LaunchMode.TerminalCommand ? CommandTextTextBox.Text.Trim() : string.Empty
        };

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 取消编辑。
    /// </summary>
    private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #endregion

    #region 编辑器状态同步

    /// <summary>
    /// 根据当前启动方式切换界面输入区域。
    /// </summary>
    private void UpdateEditorByLaunchMode(LaunchMode launchMode)
    {
        // 应用启动时保留 exe 与参数模板输入，终端命令时仅保留命令文本输入。
        System.Windows.Visibility executableVisibility = launchMode == LaunchMode.Executable
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
        System.Windows.Visibility terminalVisibility = launchMode == LaunchMode.TerminalCommand
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;

        ExecutablePathGrid.Visibility = executableVisibility;
        ExecutablePathLabel.Visibility = executableVisibility;
        ExecutableArgumentsPanel.Visibility = executableVisibility;
        TerminalCommandPanel.Visibility = terminalVisibility;
    }

    /// <summary>
    /// 读取当前选择的启动方式。
    /// </summary>
    private LaunchMode GetSelectedLaunchMode()
    {
        if (LaunchModeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem comboBoxItem
            && string.Equals(comboBoxItem.Tag as string, LaunchMode.TerminalCommand.ToString(), System.StringComparison.Ordinal))
        {
            return LaunchMode.TerminalCommand;
        }

        return LaunchMode.Executable;
    }

    #endregion
}
