using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Services;
using CodeMaster.Core.Authorization;
using CodeMaster.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 部门服务接口
/// </summary>
public interface ISysDeptService : ITreeApplicationService<SysDept, SysDeptDto, SysDeptDto, SysDeptQueryDto, CreateSysDeptDto, UpdateSysDeptDto>
{
    Task<List<SysDeptDto>> GetDeptListAsync();
}

/// <summary>
/// 部门服务实现
/// </summary>
[DynamicApiPermission(requirePermission: true)]
public class SysDeptService : TreeApplicationService<SysDept, SysDeptDto, SysDeptDto, SysDeptQueryDto, CreateSysDeptDto, UpdateSysDeptDto>, ISysDeptService
{
    public SysDeptService(IRepository<SysDept> repository, ISqlSugarClient db)
        : base(repository, db)
    {
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    public override async Task<long> CreateAsync(CreateSysDeptDto input)
    {
        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    public override async Task<int> UpdateAsync(long id, UpdateSysDeptDto input)
    {
        // 获取当前部门
        var dept = await Repository.GetByIdAsync(id);
        if (dept == null)
        {
            throw new Exception($"部门 {id} 不存在");
        }

        // 检查父级是否变更
        if (input.NewParentId != dept.ParentId)
        {
            // 父级变更，需要移动节点并更新所有子孙节点的 ancestors
            await MoveNodeAsync(id, input.NewParentId ?? 0);
        }

        // 更新部门名称
        dept.Name = input.Name;
        return await Repository.UpdateAsync(dept);
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    public override async Task<int> DeleteAsync(long id)
    {
        return await base.DeleteAsync(id);
    }

    /// <summary>
    /// 创建过滤查询
    /// </summary>
    protected override Task<ISugarQueryable<SysDept>> CreateFilteredQueryAsync(SysDeptQueryDto input)
    {
        var queryable = (ISugarQueryable<SysDept>)Repository.GetQueryable();

        queryable = queryable.WhereIF(!string.IsNullOrEmpty(input.Name), d => d.Name.Contains(input.Name));

        return Task.FromResult(queryable);
    }

    /// <summary>
    /// 获取部门列表（扁平结构，由前端构建树形）
    /// </summary>
    [AllowAnonymous]
    public async Task<List<SysDeptDto>> GetDeptListAsync()
    {
        return await GetListAsync(new SysDeptQueryDto());
    }
}
