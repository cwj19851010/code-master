using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Services;
using Mapster;
using SqlSugar;
using CodeMaster.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CodeMaster.Core.Authorization;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 用户服务接口
/// </summary>
public interface ISysUserService : IApplicationService
{
    // 使用默认权限规则：system:user:view
    Task<SysUserDto?> GetByIdAsync(long id);

    // 使用默认权限规则：system:user:list
    Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    // 使用默认权限规则：system:user:create
    Task<long> CreateAsync([FromBody] CreateSysUserDto dto);

    // 使用默认权限规则：system:user:update
    Task<int> UpdateAsync(long id, [FromBody] UpdateSysUserDto dto);

    // 使用默认权限规则：system:user:delete
    Task<int> DeleteAsync(long id);

    // 显式指定权限：system:user:view
    [Permission("system:user:view")]
    Task<SysUserDto?> GetByUserNameAsync(string userName);

    // 显式指定权限：system:user:view
    [Permission("system:user:view")]
    Task<SysUserDto?> GetUserById(long id);
}

/// <summary>
/// 用户服务实现
/// </summary>
public class SysUserService : ISysUserService
{
    private readonly IRepository<SysUser> _userRepository;
    private readonly IReadOnlyRepository<SysDept> _deptRepository;
    private readonly IReadOnlyRepository<SysPost> _postRepository;
    private readonly ISqlSugarClient _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SysUserService(
        IRepository<SysUser> userRepository,
        IReadOnlyRepository<SysDept> deptRepository,
        IReadOnlyRepository<SysPost> postRepository,
        ISqlSugarClient db,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _deptRepository = deptRepository;
        _postRepository = postRepository;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SysUserDto?> GetByIdAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        // 使用 Mapster 自动映射
        var userDto = user.Adapt<SysUserDto>();

        // 关联部门信息
        if (user.DeptId.HasValue)
        {
            var dept = await _deptRepository.GetByIdAsync(user.DeptId.Value);
            userDto.DeptName = dept?.Name;
        }

        // 关联职位信息
        if (user.PostId.HasValue)
        {
            var post = await _postRepository.GetByIdAsync(user.PostId.Value);
            userDto.PostName = post?.PostName;
        }

        return userDto;
    }

    public async Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query)
    {
        var result = await _userRepository.GetPagedListAsync(
            where: u =>
                (string.IsNullOrEmpty(query.UserName) || u.UserName.Contains(query.UserName)) &&
                (string.IsNullOrEmpty(query.PhoneNumber) || u.PhoneNumber.Contains(query.PhoneNumber)) &&
                (query.Status == null || u.Status == query.Status.Value) &&
                (query.DeptId == null || u.DeptId == query.DeptId.Value) &&
                (query.BeginTime == null || u.CreateTime >= query.BeginTime.Value) &&
                (query.EndTime == null || u.CreateTime <= query.EndTime.Value),
            pageNum: query.PageNum,
            pageSize: query.PageSize
        );

        var items = result.Item1;
        var total = result.Item2;

        // 使用 Mapster 批量映射
        var dtoList = items.Adapt<List<SysUserDto>>();

        // 批量关联部门信息
        var deptIds = items.Where(u => u.DeptId.HasValue).Select(u => u.DeptId!.Value).Distinct().ToList();
        if (deptIds.Any())
        {
            var depts = await _deptRepository.GetListAsync(d => deptIds.Contains(d.Id));
            var deptDict = depts.ToDictionary(d => d.Id, d => d.Name);

            foreach (var dto in dtoList)
            {
                if (dto.DeptId.HasValue && deptDict.ContainsKey(dto.DeptId.Value))
                {
                    dto.DeptName = deptDict[dto.DeptId.Value];
                }
            }
        }

        // 批量关联职位信息
        var postIds = items.Where(u => u.PostId.HasValue).Select(u => u.PostId!.Value).Distinct().ToList();
        if (postIds.Any())
        {
            var posts = await _postRepository.GetListAsync(p => postIds.Contains(p.Id));
            var postDict = posts.ToDictionary(p => p.Id, p => p.PostName);

            foreach (var dto in dtoList)
            {
                if (dto.PostId.HasValue && postDict.ContainsKey(dto.PostId.Value))
                {
                    dto.PostName = postDict[dto.PostId.Value];
                }
            }
        }

        return new PagedResultDto<SysUserDto>
        {
            Items = dtoList,
            Total = total,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CreateAsync(CreateSysUserDto dto)
    {
        // 1. 验证用户名不能以 admin 结尾（保留给租户管理员）
        if (dto.UserName.EndsWith("admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("用户名不能以 'admin' 结尾，该命名规则保留给租户管理员使用");
        }

        // 2. 禁用全局租户过滤，检查用户名在所有租户中是否唯一
        var existUser = await _db.Queryable<SysUser>()
            .ClearFilter() // 禁用全局过滤器
            .Where(u => u.UserName == dto.UserName)
            .AnyAsync();

        if (existUser)
        {
            throw new Exception($"用户名 {dto.UserName} 已存在");
        }

        // 使用 Mapster 映射
        var user = dto.Adapt<SysUser>();

        // 设置创建人ID
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdClaim?.Value, out var currentUserId))
        {
            user.CreateUserId = currentUserId;
        }

        // 密码加密
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        return await _userRepository.InsertAsync(user);
    }

    public async Task<int> UpdateAsync(long id, UpdateSysUserDto dto)
    {
        Console.WriteLine($"[UpdateAsync] 开始更新用户 ID: {id}");
        Console.WriteLine($"[UpdateAsync] DTO 类型: {dto.GetType().FullName}");
        Console.WriteLine($"[UpdateAsync] NickName: '{dto.NickName}' (IsNull: {dto.NickName == null})");
        Console.WriteLine($"[UpdateAsync] Email: '{dto.Email}' (IsNull: {dto.Email == null})");
        Console.WriteLine($"[UpdateAsync] PhoneNumber: '{dto.PhoneNumber}' (IsNull: {dto.PhoneNumber == null})");
        Console.WriteLine($"[UpdateAsync] Sex: {dto.Sex} (HasValue: {dto.Sex.HasValue})");
        Console.WriteLine($"[UpdateAsync] Status: {dto.Status} (HasValue: {dto.Status.HasValue})");
        Console.WriteLine($"[UpdateAsync] Remark: '{dto.Remark}' (IsNull: {dto.Remark == null})");

        // 1. 获取当前用户信息
        var currentUser = await _db.Queryable<SysUser>()
            .Where(u => u.Id == id)
            .FirstAsync();

        if (currentUser == null)
        {
            throw new Exception("用户不存在");
        }

        // 2. 检查是否为管理员账号（admin 或以 admin 结尾）
        bool isAdminAccount = currentUser.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase)
                           || currentUser.UserName.EndsWith("admin", StringComparison.OrdinalIgnoreCase);

        if (isAdminAccount)
        {
            throw new Exception($"管理员账号 '{currentUser.UserName}' 不允许修改");
        }

        // 使用 SqlSugar 的 UpdateColumns 方法，只更新指定的列
        var columns = new List<string>();

        if (dto.NickName != null) columns.Add(nameof(SysUser.NickName));
        if (dto.Email != null) columns.Add(nameof(SysUser.Email));
        if (dto.PhoneNumber != null) columns.Add(nameof(SysUser.PhoneNumber));
        if (dto.Sex.HasValue) columns.Add(nameof(SysUser.Sex));
        if (dto.Avatar != null) columns.Add(nameof(SysUser.Avatar));
        if (dto.Status.HasValue) columns.Add(nameof(SysUser.Status));
        if (dto.DeptId.HasValue) columns.Add(nameof(SysUser.DeptId));
        if (dto.PostId.HasValue) columns.Add(nameof(SysUser.PostId));
        if (dto.Remark != null) columns.Add(nameof(SysUser.Remark));

        Console.WriteLine($"[UpdateAsync] 需要更新的列: {string.Join(", ", columns)}");

        if (columns.Count == 0)
        {
            Console.WriteLine($"[UpdateAsync] 没有需要更新的列，返回 0");
            return 0;
        }

        // 创建更新对象
        var updateEntity = new SysUser { Id = id };
        if (dto.NickName != null) updateEntity.NickName = dto.NickName;
        if (dto.Email != null) updateEntity.Email = dto.Email;
        if (dto.PhoneNumber != null) updateEntity.PhoneNumber = dto.PhoneNumber;
        if (dto.Sex.HasValue) updateEntity.Sex = dto.Sex.Value;
        if (dto.Avatar != null) updateEntity.Avatar = dto.Avatar;
        if (dto.Status.HasValue) updateEntity.Status = dto.Status.Value;
        if (dto.DeptId.HasValue) updateEntity.DeptId = dto.DeptId;
        if (dto.PostId.HasValue) updateEntity.PostId = dto.PostId;
        if (dto.Remark != null) updateEntity.Remark = dto.Remark;

        var result = await _db.Updateable(updateEntity)
            .UpdateColumns(columns.ToArray())
            .ExecuteCommandAsync();

        Console.WriteLine($"[UpdateAsync] 更新结果: {result}");
        return result;
    }

    public async Task<int> DeleteAsync(long id)
    {
        // 1. 获取用户信息
        var user = await _db.Queryable<SysUser>()
            .Where(u => u.Id == id)
            .FirstAsync();

        if (user == null)
        {
            throw new Exception("用户不存在");
        }

        // 2. 检查是否为管理员账号
        bool isAdminAccount = user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase)
                           || user.UserName.EndsWith("admin", StringComparison.OrdinalIgnoreCase);

        if (isAdminAccount)
        {
            throw new Exception($"管理员账号 '{user.UserName}' 不允许删除");
        }

        return await _db.SoftDeleteAsync<SysUser>(id);
    }

    public async Task<SysUserDto?> GetByUserNameAsync(string userName)
    {
        var users = await _userRepository.GetListAsync(u => u.UserName == userName);
        var user = users.FirstOrDefault();
        if (user == null) return null;

        // 使用 Mapster 映射
        var userDto = user.Adapt<SysUserDto>();

        // 关联部门信息
        if (user.DeptId.HasValue)
        {
            var dept = await _deptRepository.GetByIdAsync(user.DeptId.Value);
            userDto.DeptName = dept?.Name;
        }

        // 关联职位信息
        if (user.PostId.HasValue)
        {
            var post = await _postRepository.GetByIdAsync(user.PostId.Value);
            userDto.PostName = post?.PostName;
        }

        return userDto;
    }

    public async Task<SysUserDto?> GetUserById(long id)
    {
        return await GetByIdAsync(id);
    }
}
