using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

/// <summary>
/// 只读应用服务接口
/// </summary>
public interface IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, in TGetListInput> : IApplicationService
    where TEntity : class, IEntity<long>, new()
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<TGetOutputDto?> GetByIdAsync(long id);

    /// <summary>
    /// 获取分页列表
    /// </summary>
    Task<PagedResultDto<TGetListOutputDto>> GetPagedListAsync(TGetListInput input);

    /// <summary>
    /// 获取所有列表（不分页，支持查询条件）
    /// </summary>
    Task<List<TGetListOutputDto>> GetListAsync(TGetListInput input);

    /// <summary>
    /// 导出数据到 Excel
    /// </summary>
    Task<byte[]> ExportAsync(TGetListInput input);
}
