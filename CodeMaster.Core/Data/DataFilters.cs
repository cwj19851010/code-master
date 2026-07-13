using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Data;

/// <summary>
/// 软删除过滤器
/// 用于标识软删除的全局过滤器
/// </summary>
public interface ISoftDeleteFilter
{
}

/// <summary>
/// 多租户过滤器
/// 用于标识多租户的全局过滤器
/// </summary>
public interface ITenantFilter
{
}
