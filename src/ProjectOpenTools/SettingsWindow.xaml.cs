using System.Windows;
using ProjectOpenTools.Models;

namespace ProjectOpenTools;

/// <summary>
/// 偏好设置窗口，负责调整程序行为类选项。
/// </summary>
public partial class SettingsWindow : Window
{
    #region 初始化与数据回填

    /// <summary>
    /// 使用当前配置初始化设置窗口。
    /// </summary>
    /// <param name="appSettings">当前配置对象。</param>
    public SettingsWindow(AppSettings appSettings)
    {
        InitializeComponent();

        // 将当前设置回填到界面控件，保持交互简单直接。
        AutoHideToTrayAfterLaunchCheckBox.IsChecked = appSettings.AutoHideToTrayAfterLaunch;
        HideToTrayRadioButton.IsChecked = appSettings.CloseAction == CloseActionOption.HideToTray;
        ExitApplicationRadioButton.IsChecked = appSettings.CloseAction == CloseActionOption.ExitApplication;
    }

    /// <summary>
    /// 保存后的自动隐藏设置。
    /// </summary>
    public bool AutoHideToTrayAfterLaunch { get; private set; }

    /// <summary>
    /// 保存后的关闭按钮行为。
    /// </summary>
    public CloseActionOption CloseAction { get; private set; } = CloseActionOption.HideToTray;

    #endregion

    #region 设置保存与取消

    /// <summary>
    /// 取消修改并关闭窗口。
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    /// <summary>
    /// 保存设置并返回主窗口。
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 读取控件状态并输出为简单值，避免为这一页引入额外绑定层。
        this.AutoHideToTrayAfterLaunch = AutoHideToTrayAfterLaunchCheckBox.IsChecked == true;
        this.CloseAction = ExitApplicationRadioButton.IsChecked == true
            ? CloseActionOption.ExitApplication
            : CloseActionOption.HideToTray;

        DialogResult = true;
    }

    #endregion
}
