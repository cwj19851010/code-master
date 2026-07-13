using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.Entities;
using CodeMaster.Application.Dtos.Entities;
using SqlSugar;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// SysMenu服务实现
/// </summary>
public class SysMenuService : ISysMenuService
{
    private readonly ISqlSugarClient _db;

    public SysMenuService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<SysMenuDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<SysMenu>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<SysMenuDto>> GetPagedListAsync(SysMenuQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<SysMenu>()
            .WhereIF(!string.IsNullOrEmpty(query.MenuName), x => x.MenuName!.Contains(query.MenuName!))
            .WhereIF(query.ParentId.HasValue, x => x.ParentId == query.ParentId)
            .WhereIF(query.OrderNum.HasValue, x => x.OrderNum == query.OrderNum)
            .WhereIF(!string.IsNullOrEmpty(query.Path), x => x.Path!.Contains(query.Path!))
            .WhereIF(!string.IsNullOrEmpty(query.Component), x => x.Component!.Contains(query.Component!))
            .WhereIF(!string.IsNullOrEmpty(query.Query), x => x.Query!.Contains(query.Query!))
            .WhereIF(query.IsFrame.HasValue, x => x.IsFrame == query.IsFrame)
            .WhereIF(query.IsCache.HasValue, x => x.IsCache == query.IsCache)
            .WhereIF(!string.IsNullOrEmpty(query.MenuType), x => x.MenuType!.Contains(query.MenuType!))
            .WhereIF(query.Visible.HasValue, x => x.Visible == query.Visible)
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(!string.IsNullOrEmpty(query.Perms), x => x.Perms!.Contains(query.Perms!))
            .WhereIF(!string.IsNullOrEmpty(query.Icon), x => x.Icon!.Contains(query.Icon!))
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<SysMenuDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateSysMenuDto dto)
    {
        var entity = new SysMenu
        {
            MenuName = dto.MenuName,
            ParentId = dto.ParentId,
            OrderNum = dto.OrderNum,
            Path = dto.Path,
            Component = dto.Component,
            Query = dto.Query,
            IsFrame = dto.IsFrame,
            IsCache = dto.IsCache,
            MenuType = dto.MenuType,
            Visible = dto.Visible,
            Status = dto.Status,
            Perms = dto.Perms,
            Icon = dto.Icon,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateSysMenuDto dto)
    {
        var entity = await _db.Queryable<SysMenu>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.MenuName = dto.MenuName;
        entity.ParentId = dto.ParentId;
        entity.OrderNum = dto.OrderNum;
        entity.Path = dto.Path;
        entity.Component = dto.Component;
        entity.Query = dto.Query;
        entity.IsFrame = dto.IsFrame;
        entity.IsCache = dto.IsCache;
        entity.MenuType = dto.MenuType;
        entity.Visible = dto.Visible;
        entity.Status = dto.Status;
        entity.Perms = dto.Perms;
        entity.Icon = dto.Icon;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<SysMenu>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<SysMenu>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private SysMenuDto MapToDto(SysMenu entity)
    {
        return new SysMenuDto
        {
            MenuName = entity.MenuName,
            ParentId = entity.ParentId,
            OrderNum = entity.OrderNum,
            Path = entity.Path,
            Component = entity.Component,
            Query = entity.Query,
            IsFrame = entity.IsFrame,
            IsCache = entity.IsCache,
            MenuType = entity.MenuType,
            Visible = entity.Visible,
            Status = entity.Status,
            Perms = entity.Perms,
            Icon = entity.Icon,
            Id = entity.Id,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            UpdateBy = entity.UpdateBy,
            UpdateTime = entity.UpdateTime,
            Remark = entity.Remark,
        };
    }
}
