using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

/// <summary>
/// Query-only application service contract for entities without a primary key.
/// </summary>
public interface IQueryApplicationService<TEntity, TGetListOutputDto, in TGetListInput> : IApplicationService
    where TEntity : class, IBaseEntity, new()
{
    Task<PagedResultDto<TGetListOutputDto>> GetPagedListAsync(TGetListInput input);

    Task<List<TGetListOutputDto>> GetListAsync(TGetListInput input);

    Task<byte[]> ExportAsync(TGetListInput input);
}
