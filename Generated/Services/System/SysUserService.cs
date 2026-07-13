using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysUser服务实现
/// </summary>
public class SysUserService : ISysUserService
{
    private readonly ISqlSugarClient _db;

    public SysUserService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<SysUserDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<SysUser>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<SysUser>()
            .WhereIF(!string.IsNullOrEmpty(query.UserName), x => x.UserName!.Contains(query.UserName!))
            .WhereIF(!string.IsNullOrEmpty(query.NickName), x => x.NickName!.Contains(query.NickName!))
            .WhereIF(!string.IsNullOrEmpty(query.UserType), x => x.UserType!.Contains(query.UserType!))
            .WhereIF(!string.IsNullOrEmpty(query.Email), x => x.Email!.Contains(query.Email!))
            .WhereIF(!string.IsNullOrEmpty(query.PhoneNumber), x => x.PhoneNumber!.Contains(query.PhoneNumber!))
            .WhereIF(query.Sex.HasValue, x => x.Sex == query.Sex)
            .WhereIF(!string.IsNullOrEmpty(query.Avatar), x => x.Avatar!.Contains(query.Avatar!))
            .WhereIF(!string.IsNullOrEmpty(query.Password), x => x.Password!.Contains(query.Password!))
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(!string.IsNullOrEmpty(query.LoginIp), x => x.LoginIp!.Contains(query.LoginIp!))
            .WhereIF(query.LoginDate.HasValue, x => x.LoginDate == query.LoginDate)
            .WhereIF(query.DeptId.HasValue, x => x.DeptId == query.DeptId)
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<SysUserDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateSysUserDto dto)
    {
        var entity = new SysUser
        {
            UserName = dto.UserName,
            NickName = dto.NickName,
            UserType = dto.UserType,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Sex = dto.Sex,
            Avatar = dto.Avatar,
            Password = dto.Password,
            Status = dto.Status,
            LoginIp = dto.LoginIp,
            LoginDate = dto.LoginDate,
            DeptId = dto.DeptId,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateSysUserDto dto)
    {
        var entity = await _db.Queryable<SysUser>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.UserName = dto.UserName;
        entity.NickName = dto.NickName;
        entity.UserType = dto.UserType;
        entity.Email = dto.Email;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.Sex = dto.Sex;
        entity.Avatar = dto.Avatar;
        entity.Password = dto.Password;
        entity.Status = dto.Status;
        entity.LoginIp = dto.LoginIp;
        entity.LoginDate = dto.LoginDate;
        entity.DeptId = dto.DeptId;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<SysUser>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<SysUser>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private SysUserDto MapToDto(SysUser entity)
    {
        return new SysUserDto
        {
            UserName = entity.UserName,
            NickName = entity.NickName,
            UserType = entity.UserType,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Sex = entity.Sex,
            Avatar = entity.Avatar,
            Password = entity.Password,
            Status = entity.Status,
            DelFlag = entity.DelFlag,
            LoginIp = entity.LoginIp,
            LoginDate = entity.LoginDate,
            DeptId = entity.DeptId,
            Id = entity.Id,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            UpdateBy = entity.UpdateBy,
            UpdateTime = entity.UpdateTime,
            Remark = entity.Remark,
        };
    }
}
