using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

/// <summary>
/// CRUD应用服务接口
/// </summary>
public interface ICrudApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, in TGetListInput, in TCreateInput, in TUpdateInput>
    : IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, new()
{
    /// <summary>
    /// 创建实体
    /// </summary>
    Task<long> CreateAsync(TCreateInput input);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<int> UpdateAsync(long id, TUpdateInput input);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task<int> DeleteAsync(long id);

    /// <summary>
    /// 批量删除
    /// </summary>
    Task<int> DeleteBatchAsync(List<long> ids);

    /// <summary>
    /// 从 Excel 导入数据
    /// </summary>
    Task<int> ImportAsync(byte[] fileBytes);
}
