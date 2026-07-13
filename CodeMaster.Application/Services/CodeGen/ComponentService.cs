using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;
using Mapster;
using SqlSugar;

namespace CodeMaster.Application.Services.CodeGen;

// ================================================================
// ComponentGroup Service
// ================================================================

public interface IComponentGroupService : IApplicationService
{
    Task<SysComponentGroupDto?> GetByIdAsync(long id);
    Task<List<SysComponentGroupDto>> GetListAsync();
    Task<long> CreateAsync(CreateSysComponentGroupDto dto);
    Task<int> UpdateAsync(long id, UpdateSysComponentGroupDto dto);
    Task<int> DeleteAsync(long id);
}

public class ComponentGroupService : IComponentGroupService
{
    private readonly IRepository<SysComponentGroup> _repository;
    private readonly ISqlSugarClient _db;

    public ComponentGroupService(IRepository<SysComponentGroup> repository, ISqlSugarClient db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<SysComponentGroupDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<SysComponentGroupDto>();
    }

    public async Task<List<SysComponentGroupDto>> GetListAsync()
    {
        var groups = await _db.Queryable<SysComponentGroup>()
            .Where(g => g.Status == 0)
            .OrderBy(g => g.Sort)
            .ToListAsync();

        var result = groups.Select(g =>
        {
            var dto = g.Adapt<SysComponentGroupDto>();
            dto.ComponentCount = _db.Queryable<SysComponent>()
                .Count(c => c.GroupId == g.Id && c.Status == 0);
            return dto;
        }).ToList();

        return result;
    }

    public async Task<long> CreateAsync(CreateSysComponentGroupDto dto)
    {
        var exists = await _db.Queryable<SysComponentGroup>()
            .Where(g => g.GroupCode == dto.GroupCode)
            .AnyAsync();
        if (exists)
            throw new Exception($"分组编码 {dto.GroupCode} 已存在");

        var entity = dto.Adapt<SysComponentGroup>();
        return await _repository.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysComponentGroupDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new Exception($"分组ID {id} 不存在");

        if (!string.IsNullOrEmpty(dto.GroupCode) && dto.GroupCode != entity.GroupCode)
        {
            var exists = await _db.Queryable<SysComponentGroup>()
                .Where(g => g.GroupCode == dto.GroupCode && g.Id != id)
                .AnyAsync();
            if (exists)
                throw new Exception($"分组编码 {dto.GroupCode} 已存在");
        }

        dto.Adapt(entity);
        return await _repository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        var hasChildren = await _db.Queryable<SysComponent>()
            .Where(c => c.GroupId == id)
            .AnyAsync();
        if (hasChildren)
            throw new Exception("该分组下还有组件，无法删除");

        return await _repository.DeleteAsync(id);
    }
}

// ================================================================
// Component Service
// ================================================================

public interface IComponentService : IApplicationService
{
    Task<SysComponentDetailDto?> GetByIdAsync(long id);
    Task<SysComponentDetailDto?> GetDetailAsync(long id);
    Task<List<SysComponentDto>> GetListByGroupAsync(long groupId);
    Task<List<SysComponentDto>> GetPagedListAsync(SysComponentQueryDto query);
    Task<long> CreateAsync(CreateSysComponentDto dto);
    Task<int> UpdateAsync(long id, UpdateSysComponentDto dto);
    Task<int> DeleteAsync(long id);
}

public class ComponentService : IComponentService
{
    private readonly IRepository<SysComponent> _repository;
    private readonly ISqlSugarClient _db;

    public ComponentService(IRepository<SysComponent> repository, ISqlSugarClient db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<SysComponentDetailDto?> GetByIdAsync(long id)
    {
        return await GetDetailAsync(id);
    }

    public async Task<SysComponentDetailDto?> GetDetailAsync(long id)
    {
        var component = await _db.Queryable<SysComponent>()
            .Includes(c => c.Properties)
            .Includes(c => c.Slots)
            .Includes(c => c.Events)
            .Includes(c => c.Exposes)
            .InSingleAsync(id);

        if (component == null) return null;
        return BuildDetailDto(component);
    }

    public async Task<List<SysComponentDto>> GetListByGroupAsync(long groupId)
    {
        var components = await _db.Queryable<SysComponent>()
            .Where(c => c.GroupId == groupId && c.Status == 0)
            .OrderBy(c => c.Sort)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return components.Select(BuildDto).ToList();
    }

    public async Task<List<SysComponentDto>> GetPagedListAsync(SysComponentQueryDto query)
    {
        var queryable = _db.Queryable<SysComponent>()
            .WhereIF(!string.IsNullOrEmpty(query.Name), c => c.Name!.Contains(query.Name!))
            .WhereIF(!string.IsNullOrEmpty(query.Tag), c => c.Tag!.Contains(query.Tag!))
            .WhereIF(query.GroupId.HasValue, c => c.GroupId == query.GroupId!.Value)
            .WhereIF(query.Status.HasValue, c => c.Status == query.Status!.Value)
            .OrderBy(c => c.Sort)
            .OrderBy(c => c.Name);

        RefAsync<int> total = 0;
        var items = await queryable.ToPageListAsync(query.PageNum, query.PageSize, total);
        return items.Select(BuildDto).ToList();
    }

    public async Task<long> CreateAsync(CreateSysComponentDto dto)
    {
        var exists = await _db.Queryable<SysComponent>()
            .Where(c => c.Name == dto.Name)
            .AnyAsync();
        if (exists)
            throw new Exception($"组件 {dto.Name} 已存在");

        var entity = dto.Adapt<SysComponent>();
        return await _repository.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysComponentDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new Exception($"组件ID {id} 不存在");

        if (!string.IsNullOrEmpty(dto.Name) && dto.Name != entity.Name)
        {
            var exists = await _db.Queryable<SysComponent>()
                .Where(c => c.Name == dto.Name && c.Id != id)
                .AnyAsync();
            if (exists)
                throw new Exception($"组件 {dto.Name} 已存在");
        }

        dto.Adapt(entity);
        return await _repository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        await _db.Deleteable<SysComponentProperty>().Where(p => p.ComponentId == id).ExecuteCommandAsync();
        await _db.Deleteable<SysComponentSlot>().Where(s => s.ComponentId == id).ExecuteCommandAsync();
        await _db.Deleteable<SysComponentEvent>().Where(e => e.ComponentId == id).ExecuteCommandAsync();
        await _db.Deleteable<SysComponentExpose>().Where(e => e.ComponentId == id).ExecuteCommandAsync();
        return await _repository.DeleteAsync(id);
    }

    private SysComponentDto BuildDto(SysComponent entity)
    {
        var dto = entity.Adapt<SysComponentDto>();
        dto.GroupName = entity.Group?.GroupName;
        dto.PropertyCount = entity.Properties?.Count ?? 0;
        dto.SlotCount = entity.Slots?.Count ?? 0;
        dto.EventCount = entity.Events?.Count ?? 0;
        dto.ExposeCount = entity.Exposes?.Count ?? 0;
        return dto;
    }

    private SysComponentDetailDto BuildDetailDto(SysComponent entity)
    {
        var dto = entity.Adapt<SysComponentDetailDto>();
        dto.GroupName = entity.Group?.GroupName;
        dto.PropertyCount = entity.Properties?.Count ?? 0;
        dto.SlotCount = entity.Slots?.Count ?? 0;
        dto.EventCount = entity.Events?.Count ?? 0;
        dto.ExposeCount = entity.Exposes?.Count ?? 0;
        dto.Properties = entity.Properties?
            .OrderByDescending(p => p.IsCommon)
            .ThenBy(p => p.Sort)
            .Select(p => p.Adapt<SysComponentPropertyDto>())
            .ToList() ?? new();
        dto.Slots = entity.Slots?
            .OrderBy(s => s.Sort)
            .Select(s => s.Adapt<SysComponentSlotDto>())
            .ToList() ?? new();
        dto.Events = entity.Events?
            .OrderByDescending(e => e.IsCommon)
            .ThenBy(e => e.Sort)
            .Select(e => e.Adapt<SysComponentEventDto>())
            .ToList() ?? new();
        dto.Exposes = entity.Exposes?
            .OrderBy(x => x.Sort)
            .Select(x => x.Adapt<SysComponentExposeDto>())
            .ToList() ?? new();
        return dto;
    }
}

// ================================================================
// ComponentProperty Service
// ================================================================

public interface IComponentPropertyService : IApplicationService
{
    Task<SysComponentPropertyDto?> GetByIdAsync(long id);
    Task<List<SysComponentPropertyDto>> GetListByComponentAsync(long componentId);
    Task<long> CreateAsync(CreateSysComponentPropertyDto dto);
    Task<int> UpdateAsync(long id, UpdateSysComponentPropertyDto dto);
    Task<int> DeleteAsync(long id);
    Task<int> SetCommonAsync(SetCommonDto dto);
}

public class ComponentPropertyService : IComponentPropertyService
{
    private readonly IRepository<SysComponentProperty> _repository;
    private readonly ISqlSugarClient _db;

    public ComponentPropertyService(IRepository<SysComponentProperty> repository, ISqlSugarClient db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<SysComponentPropertyDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<SysComponentPropertyDto>();
    }

    public async Task<List<SysComponentPropertyDto>> GetListByComponentAsync(long componentId)
    {
        var list = await _db.Queryable<SysComponentProperty>()
            .Where(p => p.ComponentId == componentId)
            .OrderByDescending(p => p.IsCommon)
            .OrderBy(p => p.Sort)
            .ToListAsync();
        return list.Adapt<List<SysComponentPropertyDto>>();
    }

    public async Task<long> CreateAsync(CreateSysComponentPropertyDto dto)
    {
        var exists = await _db.Queryable<SysComponentProperty>()
            .Where(p => p.ComponentId == dto.ComponentId && p.PropName == dto.PropName)
            .AnyAsync();
        if (exists)
            throw new Exception($"该组件已有同名属性 {dto.PropName}");

        // Clean single quotes from enum type descriptions
        dto.TypeDescription = CleanTypeDescription(dto.TypeDescription);
        dto.DefaultValue = CleanDefaultValue(dto.DefaultValue);

        var entity = dto.Adapt<SysComponentProperty>();
        return await _repository.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysComponentPropertyDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new Exception($"属性ID {id} 不存在");

        dto.TypeDescription = CleanTypeDescription(dto.TypeDescription);
        dto.DefaultValue = CleanDefaultValue(dto.DefaultValue);
        dto.Adapt(entity);
        return await _repository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<int> SetCommonAsync(SetCommonDto dto)
    {
        return await _db.Updateable<SysComponentProperty>()
            .SetColumns(p => p.IsCommon == dto.IsCommon)
            .Where(p => p.Id == dto.Id)
            .ExecuteCommandAsync();
    }

    /// <summary>Remove single quotes from enum type descriptions like 'large' | 'default' → large | default</summary>
    private static string? CleanTypeDescription(string? td)
    {
        if (string.IsNullOrEmpty(td)) return td;
        return td.Replace("'", "");
    }

    /// <summary>Remove single quotes from default values like 'button' → button</summary>
    private static string? CleanDefaultValue(string? dv)
    {
        if (string.IsNullOrEmpty(dv)) return dv;
        return dv.Trim('\'');
    }
}

// ================================================================
// ComponentSlot Service
// ================================================================

public interface IComponentSlotService : IApplicationService
{
    Task<SysComponentSlotDto?> GetByIdAsync(long id);
    Task<List<SysComponentSlotDto>> GetListByComponentAsync(long componentId);
    Task<long> CreateAsync(CreateSysComponentSlotDto dto);
    Task<int> UpdateAsync(long id, UpdateSysComponentSlotDto dto);
    Task<int> DeleteAsync(long id);
}

public class ComponentSlotService : IComponentSlotService
{
    private readonly IRepository<SysComponentSlot> _repository;
    private readonly ISqlSugarClient _db;

    public ComponentSlotService(IRepository<SysComponentSlot> repository, ISqlSugarClient db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<SysComponentSlotDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<SysComponentSlotDto>();
    }

    public async Task<List<SysComponentSlotDto>> GetListByComponentAsync(long componentId)
    {
        var list = await _db.Queryable<SysComponentSlot>()
            .Where(s => s.ComponentId == componentId)
            .OrderBy(s => s.Sort)
            .ToListAsync();
        return list.Adapt<List<SysComponentSlotDto>>();
    }

    public async Task<long> CreateAsync(CreateSysComponentSlotDto dto)
    {
        var entity = dto.Adapt<SysComponentSlot>();
        return await _repository.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysComponentSlotDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new Exception($"插槽ID {id} 不存在");
        dto.Adapt(entity);
        return await _repository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        return await _repository.DeleteAsync(id);
    }
}

// ================================================================
// ComponentEvent Service
// ================================================================

public interface IComponentEventService : IApplicationService
{
    Task<SysComponentEventDto?> GetByIdAsync(long id);
    Task<List<SysComponentEventDto>> GetListByComponentAsync(long componentId);
    Task<long> CreateAsync(CreateSysComponentEventDto dto);
    Task<int> UpdateAsync(long id, UpdateSysComponentEventDto dto);
    Task<int> DeleteAsync(long id);
    Task<int> SetCommonAsync(SetCommonDto dto);
}

public class ComponentEventService : IComponentEventService
{
    private readonly IRepository<SysComponentEvent> _repository;
    private readonly ISqlSugarClient _db;

    public ComponentEventService(IRepository<SysComponentEvent> repository, ISqlSugarClient db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<SysComponentEventDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<SysComponentEventDto>();
    }

    public async Task<List<SysComponentEventDto>> GetListByComponentAsync(long componentId)
    {
        var list = await _db.Queryable<SysComponentEvent>()
            .Where(e => e.ComponentId == componentId)
            .OrderByDescending(e => e.IsCommon)
            .OrderBy(e => e.Sort)
            .ToListAsync();
        return list.Adapt<List<SysComponentEventDto>>();
    }

    public async Task<long> CreateAsync(CreateSysComponentEventDto dto)
    {
        var exists = await _db.Queryable<SysComponentEvent>()
            .Where(e => e.ComponentId == dto.ComponentId && e.EventName == dto.EventName)
            .AnyAsync();
        if (exists)
            throw new Exception($"该组件已有同名事件 {dto.EventName}");

        var entity = dto.Adapt<SysComponentEvent>();
        return await _repository.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysComponentEventDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new Exception($"事件ID {id} 不存在");
        dto.Adapt(entity);
        return await _repository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<int> SetCommonAsync(SetCommonDto dto)
    {
        return await _db.Updateable<SysComponentEvent>()
            .SetColumns(e => e.IsCommon == dto.IsCommon)
            .Where(e => e.Id == dto.Id)
            .ExecuteCommandAsync();
    }
}

// ================================================================
// ComponentExpose Service
// ================================================================

public interface IComponentExposeService : IApplicationService
{
    Task<SysComponentExposeDto?> GetByIdAsync(long id);
    Task<List<SysComponentExposeDto>> GetListByComponentAsync(long componentId);
    Task<long> CreateAsync(CreateSysComponentExposeDto dto);
    Task<int> UpdateAsync(long id, UpdateSysComponentExposeDto dto);
    Task<int> DeleteAsync(long id);
}

public class ComponentExposeService : IComponentExposeService
{
    private readonly IRepository<SysComponentExpose> _repository;
    private readonly ISqlSugarClient _db;

    public ComponentExposeService(IRepository<SysComponentExpose> repository, ISqlSugarClient db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<SysComponentExposeDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<SysComponentExposeDto>();
    }

    public async Task<List<SysComponentExposeDto>> GetListByComponentAsync(long componentId)
    {
        var list = await _db.Queryable<SysComponentExpose>()
            .Where(e => e.ComponentId == componentId)
            .OrderBy(e => e.Sort)
            .ToListAsync();
        return list.Adapt<List<SysComponentExposeDto>>();
    }

    public async Task<long> CreateAsync(CreateSysComponentExposeDto dto)
    {
        var entity = dto.Adapt<SysComponentExpose>();
        return await _repository.InsertAsync(entity);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysComponentExposeDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new Exception($"暴露方法ID {id} 不存在");
        dto.Adapt(entity);
        return await _repository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        return await _repository.DeleteAsync(id);
    }
}
