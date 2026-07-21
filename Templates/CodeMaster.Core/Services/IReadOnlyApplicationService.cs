using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

public interface IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, in TGetListInput>
    : IQueryApplicationService<TEntity, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, new()
{
    Task<TGetOutputDto?> GetByIdAsync(long id);
}
