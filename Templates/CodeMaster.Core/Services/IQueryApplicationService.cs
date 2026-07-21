using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

public interface IQueryApplicationService<TEntity, TGetListOutputDto, in TGetListInput> : IApplicationService
    where TEntity : class, IBaseEntity, new()
{
    Task<PagedResultDto<TGetListOutputDto>> GetPagedListAsync(TGetListInput input);
    Task<List<TGetListOutputDto>> GetListAsync(TGetListInput input);
    Task<byte[]> ExportAsync(TGetListInput input);
}
