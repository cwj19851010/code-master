using CodeMaster.Core.Dtos;

namespace CodeMaster.Core.Services;

/// <summary>
/// CRUD服务接口
/// </summary>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
public interface ICrudService<in TCreateDto, in TUpdateDto, TDto> : IReadOnlyService<TDto>, IApplicationService
    where TCreateDto : CreateDtoBase
    where TUpdateDto : UpdateDtoBase<long>
    where TDto : DtoBase<long>
{
    /// <summary>
    /// 创建
    /// </summary>
    long Create(TCreateDto dto);

    /// <summary>
    /// 创建（异步）
    /// </summary>
    Task<long> CreateAsync(TCreateDto dto);

    /// <summary>
    /// 更新
    /// </summary>
    int Update(TUpdateDto dto);

    /// <summary>
    /// 更新（异步）
    /// </summary>
    Task<int> UpdateAsync(TUpdateDto dto);

    /// <summary>
    /// 删除
    /// </summary>
    int Delete(long id);

    /// <summary>
    /// 删除（异步）
    /// </summary>
    Task<int> DeleteAsync(long id);

    /// <summary>
    /// 批量删除
    /// </summary>
    int DeleteBatch(long[] ids);

    /// <summary>
    /// 批量删除（异步）
    /// </summary>
    Task<int> DeleteBatchAsync(long[] ids);
}
