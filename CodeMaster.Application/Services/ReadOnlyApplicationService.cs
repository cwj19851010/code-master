using Mapster;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services;

/// <summary>
/// Read-only application service for entities that have a long primary key.
/// </summary>
public abstract class ReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    : QueryApplicationService<TEntity, TGetListOutputDto, TGetListInput>,
      IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, new()
    where TGetOutputDto : class
    where TGetListOutputDto : class
    where TGetListInput : PagedQueryDto
{
    protected ReadOnlyApplicationService(
        IReadOnlyRepository<TEntity> repository,
        IExcelService? excelService = null,
        Core.Services.ICacheService? cacheService = null)
        : base(repository, excelService, cacheService)
    {
    }

    public virtual async Task<TGetOutputDto?> GetByIdAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        return entity == null ? default : entity.Adapt<TGetOutputDto>();
    }
}
