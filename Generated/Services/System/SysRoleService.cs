using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysRole服务实现
/// </summary>
public class SysRoleService : ISysRoleService
{
    private readonly ISqlSugarClient _db;

    public SysRoleService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<SysRoleDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<SysRole>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<SysRoleDto>> GetPagedListAsync(SysRoleQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<SysRole>()
            .WhereIF(!string.IsNullOrEmpty(query.RoleName), x => x.RoleName!.Contains(query.RoleName!))
            .WhereIF(!string.IsNullOrEmpty(query.RoleKey), x => x.RoleKey!.Contains(query.RoleKey!))
            .WhereIF(query.RoleSort.HasValue, x => x.RoleSort == query.RoleSort)
            .WhereIF(query.DataScope.HasValue, x => x.DataScope == query.DataScope)
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<SysRoleDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateSysRoleDto dto)
    {
        var entity = new SysRole
        {
            RoleName = dto.RoleName,
            RoleKey = dto.RoleKey,
            RoleSort = dto.RoleSort,
            DataScope = dto.DataScope,
            Status = dto.Status,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateSysRoleDto dto)
    {
        var entity = await _db.Queryable<SysRole>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.RoleName = dto.RoleName;
        entity.RoleKey = dto.RoleKey;
        entity.RoleSort = dto.RoleSort;
        entity.DataScope = dto.DataScope;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<SysRole>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<SysRole>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private SysRoleDto MapToDto(SysRole entity)
    {
        return new SysRoleDto
        {
            RoleName = entity.RoleName,
            RoleKey = entity.RoleKey,
            RoleSort = entity.RoleSort,
            DataScope = entity.DataScope,
            Status = entity.Status,
            DelFlag = entity.DelFlag,
            Id = entity.Id,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            UpdateBy = entity.UpdateBy,
            UpdateTime = entity.UpdateTime,
            Remark = entity.Remark,
        };
    }
}
