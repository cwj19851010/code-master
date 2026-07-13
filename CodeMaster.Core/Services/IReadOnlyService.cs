using CodeMaster.Core.Dtos;

namespace CodeMaster.Core.Services;

/// <summary>
/// 只读服务接口
/// </summary>
/// <typeparam name="TDto">DTO类型</typeparam>
public interface IReadOnlyService<TDto> where TDto : DtoBase<long>
{
    /// <summary>
    /// 根据ID获取
    /// </summary>
    TDto? GetById(long id);

    /// <summary>
    /// 根据ID获取（异步）
    /// </summary>
    Task<TDto?> GetByIdAsync(long id);

    /// <summary>
    /// 获取分页列表
    /// </summary>
    PagedResultDto<TDto> GetPagedList(PagedQueryDto query);

    /// <summary>
    /// 获取分页列表（异步）
    /// </summary>
    Task<PagedResultDto<TDto>> GetPagedListAsync(PagedQueryDto query);
}
