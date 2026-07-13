using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysTenant服务实现
/// </summary>
public class SysTenantService : ISysTenantService
{
    private readonly ISqlSugarClient _db;

    public SysTenantService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<SysTenantDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<SysTenant>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<SysTenantDto>> GetPagedListAsync(SysTenantQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<SysTenant>()
            .WhereIF(!string.IsNullOrEmpty(query.TenantCode), x => x.TenantCode!.Contains(query.TenantCode!))
            .WhereIF(!string.IsNullOrEmpty(query.TenantName), x => x.TenantName!.Contains(query.TenantName!))
            .WhereIF(query.IsolationType.HasValue, x => x.IsolationType == query.IsolationType)
            .WhereIF(!string.IsNullOrEmpty(query.ConfigId), x => x.ConfigId!.Contains(query.ConfigId!))
            .WhereIF(!string.IsNullOrEmpty(query.ConnectionString), x => x.ConnectionString!.Contains(query.ConnectionString!))
            .WhereIF(query.DbType.HasValue, x => x.DbType == query.DbType)
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(query.ExpireTime.HasValue, x => x.ExpireTime == query.ExpireTime)
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<SysTenantDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateSysTenantDto dto)
    {
        var entity = new SysTenant
        {
            TenantCode = dto.TenantCode,
            TenantName = dto.TenantName,
            IsolationType = dto.IsolationType,
            ConfigId = dto.ConfigId,
            ConnectionString = dto.ConnectionString,
            DbType = dto.DbType,
            Status = dto.Status,
            ExpireTime = dto.ExpireTime,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateSysTenantDto dto)
    {
        var entity = await _db.Queryable<SysTenant>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.TenantCode = dto.TenantCode;
        entity.TenantName = dto.TenantName;
        entity.IsolationType = dto.IsolationType;
        entity.ConfigId = dto.ConfigId;
        entity.ConnectionString = dto.ConnectionString;
        entity.DbType = dto.DbType;
        entity.Status = dto.Status;
        entity.ExpireTime = dto.ExpireTime;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<SysTenant>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<SysTenant>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private SysTenantDto MapToDto(SysTenant entity)
    {
        return new SysTenantDto
        {
            TenantCode = entity.TenantCode,
            TenantName = entity.TenantName,
            IsolationType = entity.IsolationType,
            ConfigId = entity.ConfigId,
            ConnectionString = entity.ConnectionString,
            DbType = entity.DbType,
            Status = entity.Status,
            ExpireTime = entity.ExpireTime,
            Id = entity.Id,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            UpdateBy = entity.UpdateBy,
            UpdateTime = entity.UpdateTime,
            Remark = entity.Remark,
        };
    }
}
