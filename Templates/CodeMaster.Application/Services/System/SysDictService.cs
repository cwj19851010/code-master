using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using CodeMaster.Core.Authorization;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 字典类型服务接口
/// </summary>
public interface ISysDictTypeService : IApplicationService
{
    Task<SysDictTypeDto?> GetByIdAsync(long id);

    Task<PagedResultDto<SysDictTypeDto>> GetPagedListAsync(SysDictTypeQueryDto query);

    Task<long> CreateAsync(CreateSysDictTypeDto dto);

    Task<int> UpdateAsync(long id, UpdateSysDictTypeDto dto);

    Task<int> DeleteAsync(long id);
}

/// <summary>
/// 字典类型服务实现
/// </summary>
public class SysDictTypeService : ISysDictTypeService
{
    private readonly IRepository<SysDictType> _dictTypeRepository;
    private readonly ISqlSugarClient _db;

    public SysDictTypeService(IRepository<SysDictType> dictTypeRepository, ISqlSugarClient db)
    {
        _dictTypeRepository = dictTypeRepository;
        _db = db;
    }

    [Permission("system:dict:type:view")]
    public async Task<SysDictTypeDto?> GetByIdAsync(long id)
    {
        var entity = await _dictTypeRepository.GetByIdAsync(id);
        return entity?.Adapt<SysDictTypeDto>();
    }

    [Permission("system:dict:type:list")]
    public async Task<PagedResultDto<SysDictTypeDto>> GetPagedListAsync(SysDictTypeQueryDto query)
    {
        // 构建查询条件
        var queryable = _db.Queryable<SysDictType>()
            .WhereIF(!string.IsNullOrEmpty(query.DictName), t => t.DictName.Contains(query.DictName!))
            .WhereIF(!string.IsNullOrEmpty(query.DictType), t => t.DictType.Contains(query.DictType!))
            .WhereIF(query.Status != null, t => t.Status == query.Status!.Value);

        // 如果有子表查询条件，使用子查询过滤
        if (!string.IsNullOrEmpty(query.Label) || !string.IsNullOrEmpty(query.Value) || !string.IsNullOrEmpty(query.LangKey))
        {
            queryable = queryable.Where(t => SqlFunc.Subqueryable<SysDictData>()
                .Where(d => d.DictType == t.DictType)
                .WhereIF(!string.IsNullOrEmpty(query.Label), d => d.Label.Contains(query.Label!))
                .WhereIF(!string.IsNullOrEmpty(query.Value), d => d.Value.Contains(query.Value!))
                .WhereIF(!string.IsNullOrEmpty(query.LangKey), d => d.LangKey!.Contains(query.LangKey!))
                .Any());
        }

        // 分页查询
        RefAsync<int> totalCount = 0;
        var items = await queryable
            .OrderBy(t => t.Sort)
            .OrderBy(t => t.CreateTime, OrderByType.Desc)
            .ToPageListAsync(query.PageNum, query.PageSize, totalCount);

        // 加载子节点（字典数据）
        var dictTypes = items.Select(t => t.DictType).ToList();
        var dictDataList = await _db.Queryable<SysDictData>()
            .Where(d => dictTypes.Contains(d.DictType))
            .OrderBy(d => d.Sort)
            .ToListAsync();

        // 映射为DTO并组装树形结构
        var result = items.Select(t =>
        {
            var dto = t.Adapt<SysDictTypeDto>();
            dto.Children = dictDataList
                .Where(d => d.DictType == t.DictType)
                .Select(d => d.Adapt<SysDictDataDto>())
                .ToList();
            return dto;
        }).ToList();

        return new PagedResultDto<SysDictTypeDto>
        {
            Items = result,
            Total = totalCount.Value,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    [Permission("system:dict:type:create")]
    public async Task<long> CreateAsync(CreateSysDictTypeDto dto)
    {
        var exists = await _db.Queryable<SysDictType>()
            .Where(t => t.DictType == dto.DictType)
            .AnyAsync();

        if (exists)
        {
            throw new Exception($"字典类型 {dto.DictType} 已存在");
        }

        var entity = dto.Adapt<SysDictType>();
        return await _dictTypeRepository.InsertAsync(entity);
    }

    [Permission("system:dict:type:update")]
    public async Task<int> UpdateAsync(long id, UpdateSysDictTypeDto dto)
    {
        var entity = await _dictTypeRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception($"字典类型ID {id} 不存在");
        }

        if (!string.IsNullOrEmpty(dto.DictType))
        {
            var exists = await _db.Queryable<SysDictType>()
                .Where(t => t.DictType == dto.DictType && t.Id != id)
                .AnyAsync();
            if (exists)
            {
                throw new Exception($"字典类型 {dto.DictType} 已存在");
            }
        }

        dto.Adapt(entity);
        return await _dictTypeRepository.UpdateAsync(entity);
    }

    public async Task<int> DeleteAsync(long id)
    {
        var entity = await _dictTypeRepository.GetByIdAsync(id);
        if (entity == null)
        {
            return 0;
        }

        // 删除字典数据
        await _db.Deleteable<SysDictData>()
            .Where(d => d.DictType == entity.DictType)
            .ExecuteCommandAsync();

        return await _dictTypeRepository.DeleteAsync(id);
    }
}

/// <summary>
/// 字典数据服务接口
/// </summary>
public interface ISysDictDataService : IApplicationService
{
    Task<SysDictDataDto?> GetByIdAsync(long id);

    Task<PagedResultDto<SysDictDataDto>> GetPagedListAsync(SysDictDataQueryDto query);

    Task<List<SysDictDataDto>> GetListByTypeAsync(string dictType);

    Task<long> CreateAsync(CreateSysDictDataDto dto);

    Task<int> UpdateAsync(long id, UpdateSysDictDataDto dto);

    Task<int> DeleteAsync(long id);
}

/// <summary>
/// 字典数据服务实现
/// </summary>
public class SysDictDataService : ISysDictDataService
{
    private readonly IRepository<SysDictData> _dictDataRepository;
    private readonly ISqlSugarClient _db;

    public SysDictDataService(IRepository<SysDictData> dictDataRepository, ISqlSugarClient db)
    {
        _dictDataRepository = dictDataRepository;
        _db = db;
    }

    [Permission("system:dict:data:view")]
    public async Task<SysDictDataDto?> GetByIdAsync(long id)
    {
        var entity = await _dictDataRepository.GetByIdAsync(id);
        return entity?.Adapt<SysDictDataDto>();
    }

    [Permission("system:dict:data:list")]
    public async Task<PagedResultDto<SysDictDataDto>> GetPagedListAsync(SysDictDataQueryDto query)
    {
        var result = await _dictDataRepository.GetPagedListAsync(
            where: d =>
                (string.IsNullOrEmpty(query.DictType) || d.DictType.Contains(query.DictType)) &&
                (string.IsNullOrEmpty(query.Label) || d.Label.Contains(query.Label)) &&
                (query.Status == null || d.Status == query.Status.Value),
            pageNum: query.PageNum,
            pageSize: query.PageSize);

        return new PagedResultDto<SysDictDataDto>
        {
            Items = result.Items.Adapt<List<SysDictDataDto>>(),
            Total = result.Total,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    public async Task<List<SysDictDataDto>> GetListByTypeAsync(string dictType)
    {
        var list = await _db.Queryable<SysDictData>()
            .Where(d => d.DictType == dictType && d.Status == 0)
            .OrderBy(d => d.Sort)
            .ToListAsync();

        return list.Adapt<List<SysDictDataDto>>();
    }

    [Permission("system:dict:data:create")]
    public async Task<long> CreateAsync(CreateSysDictDataDto dto)
    {
        // 确保字典类型存在
        var exists = await _db.Queryable<SysDictType>()
            .Where(t => t.DictType == dto.DictType)
            .AnyAsync();
        if (!exists)
        {
            throw new Exception($"字典类型 {dto.DictType} 不存在");
        }

        var entity = dto.Adapt<SysDictData>();
        return await _dictDataRepository.InsertAsync(entity);
    }

    [Permission("system:dict:data:update")]
    public async Task<int> UpdateAsync(long id, UpdateSysDictDataDto dto)
    {
        var entity = await _dictDataRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception($"字典数据ID {id} 不存在");
        }

        if (!string.IsNullOrEmpty(dto.DictType))
        {
            var exists = await _db.Queryable<SysDictType>()
                .Where(t => t.DictType == dto.DictType)
                .AnyAsync();
            if (!exists)
            {
                throw new Exception($"字典类型 {dto.DictType} 不存在");
            }
        }

        dto.Adapt(entity);
        return await _dictDataRepository.UpdateAsync(entity);
    }

    [Permission("system:dict:data:delete")]
    public async Task<int> DeleteAsync(long id)
    {
        return await _dictDataRepository.DeleteAsync(id);
    }
}
