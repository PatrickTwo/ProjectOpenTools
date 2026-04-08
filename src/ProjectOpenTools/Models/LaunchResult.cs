namespace ProjectOpenTools.Models;

/// <summary>
/// 启动外部应用后的结果。
/// </summary>
public sealed class LaunchResult
{
    /// <summary>
    /// 是否启动成功。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 给界面展示的状态信息。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 创建启动结果。
    /// </summary>
    public LaunchResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }
}
