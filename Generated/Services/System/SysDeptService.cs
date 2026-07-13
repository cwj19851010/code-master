using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysDept服务实现
/// </summary>
public class SysDeptService : ISysDeptService
{
    private readonly ISqlSugarClient _db;

    public SysDeptService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<SysDeptDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<SysDept>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<SysDeptDto>> GetPagedListAsync(SysDeptQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<SysDept>()
            .WhereIF(query.ParentId.HasValue, x => x.ParentId == query.ParentId)
            .WhereIF(!string.IsNullOrEmpty(query.Ancestors), x => x.Ancestors!.Contains(query.Ancestors!))
            .WhereIF(!string.IsNullOrEmpty(query.DeptName), x => x.DeptName!.Contains(query.DeptName!))
            .WhereIF(query.OrderNum.HasValue, x => x.OrderNum == query.OrderNum)
            .WhereIF(!string.IsNullOrEmpty(query.Leader), x => x.Leader!.Contains(query.Leader!))
            .WhereIF(!string.IsNullOrEmpty(query.Phone), x => x.Phone!.Contains(query.Phone!))
            .WhereIF(!string.IsNullOrEmpty(query.Email), x => x.Email!.Contains(query.Email!))
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<SysDeptDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateSysDeptDto dto)
    {
        var entity = new SysDept
        {
            ParentId = dto.ParentId,
            Ancestors = dto.Ancestors,
            DeptName = dto.DeptName,
            OrderNum = dto.OrderNum,
            Leader = dto.Leader,
            Phone = dto.Phone,
            Email = dto.Email,
            Status = dto.Status,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateSysDeptDto dto)
    {
        var entity = await _db.Queryable<SysDept>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.ParentId = dto.ParentId;
        entity.Ancestors = dto.Ancestors;
        entity.DeptName = dto.DeptName;
        entity.OrderNum = dto.OrderNum;
        entity.Leader = dto.Leader;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<SysDept>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<SysDept>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private SysDeptDto MapToDto(SysDept entity)
    {
        return new SysDeptDto
        {
            ParentId = entity.ParentId,
            Ancestors = entity.Ancestors,
            DeptName = entity.DeptName,
            OrderNum = entity.OrderNum,
            Leader = entity.Leader,
            Phone = entity.Phone,
            Email = entity.Email,
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
