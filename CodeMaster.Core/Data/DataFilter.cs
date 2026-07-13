using System.Collections.Concurrent;

namespace CodeMaster.Core.Data;

/// <summary>
/// 数据过滤器实现
/// 使用 AsyncLocal 存储过滤器状态，确保线程安全
/// </summary>
public class DataFilter : IDataFilter
{
    private static readonly ConcurrentDictionary<Type, AsyncLocal<FilterState>> _filters = new();

    public IDisposable Disable<TFilter>() where TFilter : class
    {
        return SetFilterState<TFilter>(false);
    }

    public IDisposable Enable<TFilter>() where TFilter : class
    {
        return SetFilterState<TFilter>(true);
    }

    public bool IsEnabled<TFilter>() where TFilter : class
    {
        var filterType = typeof(TFilter);
        if (!_filters.TryGetValue(filterType, out var asyncLocal))
        {
            return true; // 默认启用
        }

        return asyncLocal.Value?.IsEnabled ?? true;
    }

    private IDisposable SetFilterState<TFilter>(bool isEnabled) where TFilter : class
    {
        var filterType = typeof(TFilter);
        var asyncLocal = _filters.GetOrAdd(filterType, _ => new AsyncLocal<FilterState>());

        var previousState = asyncLocal.Value?.IsEnabled ?? true;
        asyncLocal.Value = new FilterState { IsEnabled = isEnabled };

        return new DisposeAction(() =>
        {
            asyncLocal.Value = new FilterState { IsEnabled = previousState };
        });
    }

    private class FilterState
    {
        public bool IsEnabled { get; set; }
    }

    private class DisposeAction : IDisposable
    {
        private readonly Action _action;

        public DisposeAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}
