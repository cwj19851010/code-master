namespace CodeMaster.Core.Data;

/// <summary>
/// 数据过滤器接口
/// 用于控制全局过滤器的启用和禁用
/// </summary>
public interface IDataFilter
{
    /// <summary>
    /// 禁用指定类型的过滤器
    /// </summary>
    /// <typeparam name="TFilter">过滤器类型</typeparam>
    /// <returns>返回一个 IDisposable 对象，Dispose 时会恢复过滤器</returns>
    IDisposable Disable<TFilter>() where TFilter : class;

    /// <summary>
    /// 启用指定类型的过滤器
    /// </summary>
    /// <typeparam name="TFilter">过滤器类型</typeparam>
    /// <returns>返回一个 IDisposable 对象，Dispose 时会恢复过滤器</returns>
    IDisposable Enable<TFilter>() where TFilter : class;

    /// <summary>
    /// 检查指定类型的过滤器是否启用
    /// </summary>
    /// <typeparam name="TFilter">过滤器类型</typeparam>
    /// <returns>如果启用返回 true，否则返回 false</returns>
    bool IsEnabled<TFilter>() where TFilter : class;
}
