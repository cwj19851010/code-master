using System.Linq.Expressions;
using System.Security.Claims;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using Mapster;
using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace CodeMaster.Application.Services;

/// <summary>
/// CRUD服务基类
/// 提供通用的增删改查功能，自动集成数据权限过滤
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
/// <typeparam name="TQueryDto">查询DTO类型</typeparam>
public abstract class CrudServiceBase<TEntity, TCreateDto, TUpdateDto, TDto, TQueryDto>
    : ICrudService<TCreateDto, TUpdateDto, TDto>
    where TEntity : EntityBase, new()
    where TCreateDto : CreateDtoBase
    where TUpdateDto : UpdateDtoBase<long>
    where TDto : DtoBase<long>
    where TQueryDto : PagedQueryDto
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly ISqlSugarClient _db;
    protected readonly IDataPermissionService? _dataPermissionService;
    protected readonly IHttpContextAccessor? _httpContextAccessor;

    protected CrudServiceBase(
        IRepository<TEntity> repository,
        ISqlSugarClient db,
        IDataPermissionService? dataPermissionService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _repository = repository;
        _db = db;
        _dataPermissionService = dataPermissionService;
        _httpContextAccessor = httpContextAccessor;
    }

    #region 查询

    /// <summary>
    /// 根据ID获取
    /// </summary>
    public virtual TDto? GetById(long id)
    {
        return GetByIdAsync(id).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 根据ID获取（异步）
    /// </summary>
    public virtual async Task<TDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity?.Adapt<TDto>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public virtual PagedResultDto<TDto> GetPagedList(PagedQueryDto query)
    {
        return GetPagedListAsync(query).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取分页列表（异步）
    /// </summary>
    public virtual async Task<PagedResultDto<TDto>> GetPagedListAsync(PagedQueryDto query)
    {
        var queryable = _db.Queryable<TEntity>();

        // 应用查询条件（子类重写）
        queryable = BuildQueryConditions(queryable, (TQueryDto)query);

        // 应用数据权限过滤
        queryable = ApplyDataPermission(queryable);

        // 获取总数
        var total = await queryable.CountAsync();

        // 分页查询
        var items = await queryable
            .Skip((query.PageNum - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResultDto<TDto>
        {
            Items = items.Adapt<List<TDto>>(),
            Total = total,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// 构建查询条件（子类重写此方法）
    /// </summary>
    protected virtual ISugarQueryable<TEntity> BuildQueryConditions(
        ISugarQueryable<TEntity> query,
        TQueryDto queryDto)
    {
        return query;
    }

    /// <summary>
    /// 应用数据权限过滤
    /// </summary>
    protected virtual ISugarQueryable<TEntity> ApplyDataPermission(ISugarQueryable<TEntity> query)
    {
        // 检查实体是否实现了 IDataPermission 接口
        if (typeof(IDataPermission).IsAssignableFrom(typeof(TEntity)) &&
            _dataPermissionService != null &&
            _httpContextAccessor?.HttpContext != null)
        {
            var userId = GetCurrentUserId();
            var dataScope = GetCurrentUserDataScope();
            var deptId = GetCurrentUserDeptId();

            // 如果是全部数据权限，直接返回
            if (dataScope == 1)
            {
                return query;
            }

            // 如果是本部门及以下，需要获取子部门列表
            if (dataScope == 4 && deptId.HasValue)
            {
                var deptIds = _dataPermissionService.GetChildDeptIdsAsync(deptId.Value)
                    .GetAwaiter().GetResult();

                return query.Where($"dept_id IN ({string.Join(",", deptIds)})");
            }

            // 其他权限范围使用表达式过滤
            var permissionExpr = _dataPermissionService
                .BuildDataPermissionExpression<IDataPermission>(userId, dataScope, deptId);

            // 转换为 TEntity 的表达式
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var body = Expression.Invoke(permissionExpr, parameter);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);

            return query.Where(lambda);
        }

        return query;
    }

    #endregion

    #region 创建

    /// <summary>
    /// 创建
    /// </summary>
    public virtual long Create(TCreateDto dto)
    {
        return CreateAsync(dto).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 创建（异步）
    /// </summary>
    public virtual async Task<long> CreateAsync(TCreateDto dto)
    {
        var entity = dto.Adapt<TEntity>();

        // 设置创建人ID（如果实体支持数据权限）
        if (entity is IDataPermission dataPermissionEntity)
        {
            dataPermissionEntity.CreateUserId = GetCurrentUserId();
        }

        return await _repository.InsertAsync(entity);
    }

    #endregion

    #region 更新

    /// <summary>
    /// 更新
    /// </summary>
    public virtual int Update(TUpdateDto dto)
    {
        return UpdateAsync(dto).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 更新（异步）
    /// </summary>
    public virtual async Task<int> UpdateAsync(TUpdateDto dto)
    {
        var entity = await _repository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            throw new Exception($"实体ID {dto.Id} 不存在");
        }

        // 使用 Mapster 更新现有对象
        dto.Adapt(entity);

        return await _repository.UpdateAsync(entity);
    }

    #endregion

    #region 删除

    /// <summary>
    /// 删除
    /// </summary>
    public virtual int Delete(long id)
    {
        return DeleteAsync(id).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 删除（异步）
    /// </summary>
    public virtual async Task<int> DeleteAsync(long id)
    {
        return await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    public virtual int DeleteBatch(long[] ids)
    {
        return DeleteBatchAsync(ids).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 批量删除（异步）
    /// </summary>
    public virtual async Task<int> DeleteBatchAsync(long[] ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            count += await _repository.DeleteAsync(id);
        }
        return count;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    protected virtual long GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor?.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier);
        return long.Parse(userIdClaim?.Value ?? "0");
    }

    /// <summary>
    /// 获取当前用户数据权限范围
    /// </summary>
    protected virtual int GetCurrentUserDataScope()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        var postScopeClaim = user?.FindFirst("PostDataScope");
        if (!string.IsNullOrEmpty(postScopeClaim?.Value))
        {
            return int.Parse(postScopeClaim.Value);
        }

        var dataScopeClaim = user?.FindFirst("DataScope");
        return int.Parse(dataScopeClaim?.Value ?? "1");
    }

    /// <summary>
    /// 获取当前用户部门ID
    /// </summary>
    protected virtual long? GetCurrentUserDeptId()
    {
        var deptIdClaim = _httpContextAccessor?.HttpContext?.User
            .FindFirst("DeptId");
        return string.IsNullOrEmpty(deptIdClaim?.Value)
            ? null
            : long.Parse(deptIdClaim.Value);
    }

    #endregion
}
