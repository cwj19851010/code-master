using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;
using Mapster;
using SqlSugar;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 实体字段服务
/// </summary>
public class EntityFieldService : CrudApplicationService<EntityField, EntityFieldDto, EntityFieldDto, EntityFieldQueryDto, CreateEntityFieldDto, UpdateEntityFieldDto>, IEntityFieldService
{
    private readonly ISqlSugarClient _db;

    public EntityFieldService(
        IRepository<EntityField> repository,
        IExcelService excelService,
        ISqlSugarClient db,
        CodeMaster.Core.Services.ICacheService? cacheService = null) : base(repository, excelService, cacheService)
    {
        _db = db;
    }

    /// <summary>
    /// 根据实体ID获取字段列表
    /// </summary>
    public async Task<List<EntityFieldDto>> GetByEntityIdAsync(long moduleEntityId)
    {
        var fields = await _db.Queryable<EntityField>()
            .Where(f => f.ModuleEntityId == moduleEntityId)
            .OrderBy(f => f.OrderNum)
            .ToListAsync();

        return fields.Adapt<List<EntityFieldDto>>();
    }

    /// <summary>
    /// 批量创建字段
    /// </summary>
    public async Task<List<long>> CreateBatchAsync(List<CreateEntityFieldDto> inputs)
    {
        var entities = inputs.Adapt<List<EntityField>>();
        var ids = new List<long>();

        foreach (var entity in entities)
        {
            entity.CreateTime = DateTime.UtcNow;
            var id = await Repository.InsertAsync(entity);
            ids.Add(id);
        }

        return ids;
    }

    public override async Task<int> UpdateAsync(long id, UpdateEntityFieldDto input)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception($"Entity with id {id} not found");
        }

        input.Adapt(entity);
        entity.UpdateTime = DateTime.UtcNow;
        return await Repository.UpdateAsync(entity);
    }

    public override async Task<int> DeleteAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null)
        {
            return 0;
        }

        entity.IsDeleted = true;
        entity.DeleteTime = DateTime.UtcNow;
        return await Repository.UpdateAsync(entity);
    }

    public override async Task<int> DeleteBatchAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0) return 0;

        var count = 0;
        foreach (var id in ids)
        {
            count += await DeleteAsync(id);
        }

        return count;
    }

    /// <summary>
    /// 批量更新字段
    /// </summary>
    public async Task<int> UpdateBatchAsync(List<EntityFieldDto> fields)
    {
        var count = 0;
        foreach (var field in fields)
        {
            var entity = await Repository.GetByIdAsync(field.Id);
            if (entity != null)
            {
                field.Adapt(entity);
                entity.UpdateTime = DateTime.UtcNow;
                await Repository.UpdateAsync(entity);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 创建过滤查询
    /// </summary>
    protected override async Task<ISugarQueryable<EntityField>> CreateFilteredQueryAsync(EntityFieldQueryDto input)
    {
        var query = (ISugarQueryable<EntityField>)Repository.GetQueryable();

        if (input.ModuleEntityId.HasValue)
        {
            query = query.Where(f => f.ModuleEntityId == input.ModuleEntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            query = query.Where(f => f.Name.Contains(input.Name));
        }

        if (!string.IsNullOrWhiteSpace(input.Description))
        {
            query = query.Where(f => f.Description.Contains(input.Description));
        }

        if (!string.IsNullOrWhiteSpace(input.DataType))
        {
            query = query.Where(f => f.DataType == input.DataType);
        }

        if (input.IsSystemField.HasValue)
        {
            query = query.Where(f => f.IsSystemField == input.IsSystemField.Value);
        }

        return await Task.FromResult(query);
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    protected override ISugarQueryable<EntityField> ApplySorting(ISugarQueryable<EntityField> queryable, EntityFieldQueryDto input)
    {
        return queryable.OrderBy(f => f.OrderNum);
    }
}
