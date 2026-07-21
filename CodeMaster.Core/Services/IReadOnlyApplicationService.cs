using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

/// <summary>
/// 只读应用服务接口
/// </summary>
public interface IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, in TGetListInput>
    : IQueryApplicationService<TEntity, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, new()
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<TGetOutputDto?> GetByIdAsync(long id);
}
