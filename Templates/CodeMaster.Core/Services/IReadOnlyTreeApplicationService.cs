using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

public interface IReadOnlyTreeApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, in TGetListInput>
    : IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, ITree, new()
{
    Task<List<TGetListOutputDto>> GetTreeAsync(TGetListInput input);
    Task<List<TGetListOutputDto>> GetDescendantsAsync(long id);
    Task<List<TGetListOutputDto>> GetAncestorsAsync(long id);
}
