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
                Name = launcherAppEntry.Name,
                ExePath = launcherAppEntry.ExePath,
                ArgumentsTemplate = launcherAppEntry.ArgumentsTemplate
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
    /// 保存当前输入。
    /// </summary>
    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            System.Windows.MessageBox.Show(this, "请填写应用显示名称。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ExePathTextBox.Text))
        {
            System.Windows.MessageBox.Show(this, "请填写应用 exe 路径。", "保存失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        EditedLauncherApp = new LauncherAppEntry
        {
            Name = NameTextBox.Text.Trim(),
            ExePath = ExePathTextBox.Text.Trim(),
            ArgumentsTemplate = ArgumentsTemplateTextBox.Text.Trim()
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
}
