using Mapster;
using SqlSugar;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services;

public abstract class ReadOnlyTreeApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    : ReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>,
      IReadOnlyTreeApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, ITree, new()
    where TGetOutputDto : class
    where TGetListOutputDto : class
    where TGetListInput : PagedQueryDto
{
    private readonly ISqlSugarClient _db;

    protected ReadOnlyTreeApplicationService(IReadOnlyRepository<TEntity> repository, ISqlSugarClient db, IExcelService? excelService = null, Core.Services.ICacheService? cacheService = null)
        : base(repository, excelService, cacheService)
    {
        _db = db;
    }

    public virtual async Task<List<TGetListOutputDto>> GetTreeAsync(TGetListInput input) =>
        BuildTree((await (await CreateFilteredQueryAsync(input)).ToListAsync()).Adapt<List<TGetListOutputDto>>());

    public virtual async Task<List<TGetListOutputDto>> GetDescendantsAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null) return new List<TGetListOutputDto>();
        var descendants = await _db.Queryable<TEntity>()
            .Where(item => item.Ancestors != null && item.Ancestors.StartsWith($"{entity.Ancestors},{entity.Id}"))
            .ToListAsync();
        descendants.Insert(0, entity);
        return descendants.Adapt<List<TGetListOutputDto>>();
    }

    public virtual async Task<List<TGetListOutputDto>> GetAncestorsAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null || string.IsNullOrWhiteSpace(entity.Ancestors)) return new List<TGetListOutputDto>();
        var ids = entity.Ancestors.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(value => value != "0").Select(long.Parse).ToList();
        return (await _db.Queryable<TEntity>().Where(item => ids.Contains(item.Id)).OrderBy(item => item.Id).ToListAsync()).Adapt<List<TGetListOutputDto>>();
    }

    protected virtual List<TGetListOutputDto> BuildTree(List<TGetListOutputDto> allNodes) => allNodes;
}
