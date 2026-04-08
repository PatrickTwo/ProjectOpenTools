using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectOpenTools.Services;

/// <summary>
/// 负责从 exe 提取应用图标。
/// </summary>
public sealed class AppIconService
{
    /// <summary>
    /// 根据可执行文件路径读取图标。
    /// </summary>
    public ImageSource? LoadIcon(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return null;
        }

        try
        {
            Icon? icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon == null)
            {
                return null;
            }

            using Icon clonedIcon = (Icon)icon.Clone();
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                clonedIcon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));

            bitmapSource.Freeze();
            return bitmapSource;
        }
        catch
        {
            // 图标读取失败时保持静默回退，避免影响主流程。
            return null;
        }
    }
}
