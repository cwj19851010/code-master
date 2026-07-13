using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using CodeMaster.Core.Authorization;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 语言服务接口
/// </summary>
public interface ISysLangService : IApplicationService
{
    Task<SysLangDto?> GetByIdAsync(long id);

    Task<PagedResultDto<SysLangDto>> GetPagedListAsync(SysLangQueryDto query);

    Task<long> CreateAsync(CreateSysLangDto dto);

    Task<bool> UpdateAsync(long id, UpdateSysLangDto dto);

    Task<bool> DeleteAsync(long id);

    Task<List<SysLangDto>> GetEnabledListAsync();

    Task<SysLangDto?> GetDefaultAsync();
}

/// <summary>
/// 语言服务实现
/// </summary>
public class SysLangService : ISysLangService
{
    private readonly IRepository<SysLang> _langRepository;
    private readonly ISqlSugarClient _db;

    public SysLangService(IRepository<SysLang> langRepository, ISqlSugarClient db)
    {
        _langRepository = langRepository;
        _db = db;
    }

    [Permission("system:lang:view")]
    public async Task<SysLangDto?> GetByIdAsync(long id)
    {
        var entity = await _langRepository.GetByIdAsync(id);
        return entity?.Adapt<SysLangDto>();
    }

    [Permission("system:lang:list")]
    public async Task<PagedResultDto<SysLangDto>> GetPagedListAsync(SysLangQueryDto query)
    {
        var queryable = _db.Queryable<SysLang>()
            .WhereIF(!string.IsNullOrWhiteSpace(query.LangCode), l => l.LangCode.Contains(query.LangCode!))
            .WhereIF(!string.IsNullOrWhiteSpace(query.LangName), l => l.LangName.Contains(query.LangName!))
            .WhereIF(query.IsEnabled.HasValue, l => l.IsEnabled == query.IsEnabled!.Value);

        RefAsync<int> total = 0;
        var items = await queryable.ToPageListAsync(query.PageNum, query.PageSize, total);

        return new PagedResultDto<SysLangDto>
        {
            Items = items.Adapt<List<SysLangDto>>(),
            Total = total.Value,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    [Permission("system:lang:create")]
    public async Task<long> CreateAsync(CreateSysLangDto dto)
    {
        var exists = await _db.Queryable<SysLang>()
            .Where(l => l.LangCode == dto.LangCode)
            .AnyAsync();

        if (exists)
        {
            throw new Exception($"语言代码 {dto.LangCode} 已存在");
        }

        var entity = dto.Adapt<SysLang>();

        if (entity.IsDefault == 1)
        {
            await _db.Updateable<SysLang>()
                .SetColumns(l => new SysLang { IsDefault = 0 })
                .Where(l => l.IsDefault == 1)
                .ExecuteCommandAsync();
        }

        return await _langRepository.InsertAsync(entity);
    }

    [Permission("system:lang:update")]
    [HttpPut("update/{id}")]
    public async Task<bool> UpdateAsync([FromRoute] long id, [FromBody] UpdateSysLangDto dto)
    {
        if (dto == null)
        {
            throw new Exception("请求数据不能为空");
        }

        dto.Id = id;
        var entity = await _langRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            throw new Exception("语言不存在");
        }

        if (!string.IsNullOrEmpty(dto.LangCode))
        {
            var exists = await _db.Queryable<SysLang>()
                .Where(l => l.LangCode == dto.LangCode && l.Id != dto.Id)
                .AnyAsync();
            if (exists)
            {
                throw new Exception($"语言代码 {dto.LangCode} 已存在");
            }
        }

        dto.Adapt(entity);

        if (entity.IsDefault == 1)
        {
            await _db.Updateable<SysLang>()
                .SetColumns(l => new SysLang { IsDefault = 0 })
                .Where(l => l.Id != entity.Id && l.IsDefault == 1)
                .ExecuteCommandAsync();
        }

        return await _langRepository.UpdateAsync(entity) > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _langRepository.DeleteAsync(id) > 0;
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<List<SysLangDto>> GetEnabledListAsync()
    {
        var list = await _langRepository.GetListAsync(l => l.IsEnabled == 1);
        return list.Adapt<List<SysLangDto>>();
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<SysLangDto?> GetDefaultAsync()
    {
        var entity = await _db.Queryable<SysLang>()
            .Where(l => l.IsDefault == 1 && l.IsEnabled == 1)
            .FirstAsync();

        return entity?.Adapt<SysLangDto>();
    }
}

/// <summary>
/// 语言文本服务接口
/// </summary>
public interface ISysLangTextService : IApplicationService
{
    Task<SysLangTextDto?> GetByIdAsync(long id);

    Task<PagedResultDto<SysLangTextDto>> GetPagedListAsync(SysLangTextQueryDto query);

    Task<long> CreateAsync(CreateSysLangTextDto dto);

    Task<bool> UpdateAsync(long id, UpdateSysLangTextDto dto);

    Task<bool> DeleteAsync(long id);
    
    Task<Dictionary<string, string>> GetI18nMapAsync(string langCode);

    Task<string> ExportAllTranslationsToJsonAsync();
}

/// <summary>
/// 语言文本服务实现
/// </summary>
public class SysLangTextService : ISysLangTextService
{
    private readonly IRepository<SysLangText> _langTextRepository;
    private readonly ISqlSugarClient _db;

    public SysLangTextService(IRepository<SysLangText> langTextRepository, ISqlSugarClient db)
    {
        _langTextRepository = langTextRepository;
        _db = db;
    }

    [Permission("system:lang:text:view")]
    public async Task<SysLangTextDto?> GetByIdAsync(long id)
    {
        var entity = await _langTextRepository.GetByIdAsync(id);
        return entity?.Adapt<SysLangTextDto>();
    }

    [Permission("system:lang:text:list")]
    public async Task<PagedResultDto<SysLangTextDto>> GetPagedListAsync(SysLangTextQueryDto query)
    {
        var result = await _langTextRepository.GetPagedListAsync(
            where: t =>
                (string.IsNullOrEmpty(query.LangCode) || t.LangCode.Contains(query.LangCode)) &&
                (string.IsNullOrEmpty(query.LangKey) || t.LangKey.Contains(query.LangKey)) &&
                (string.IsNullOrEmpty(query.Category) || t.Category!.Contains(query.Category)),
            pageNum: query.PageNum,
            pageSize: query.PageSize);

        return new PagedResultDto<SysLangTextDto>
        {
            Items = result.Items.Adapt<List<SysLangTextDto>>(),
            Total = result.Total,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    [Permission("system:lang:text:create")]
    public async Task<long> CreateAsync(CreateSysLangTextDto dto)
    {
        var exists = await _db.Queryable<SysLangText>()
            .Where(t => t.LangCode == dto.LangCode && t.LangKey == dto.LangKey)
            .AnyAsync();
        if (exists)
        {
            throw new Exception($"语言键 {dto.LangKey} 已存在");
        }

        var entity = dto.Adapt<SysLangText>();
        return await _langTextRepository.InsertAsync(entity);
    }

    [Permission("system:lang:text:update")]
    [HttpPut("update/{id}")]
    public async Task<bool> UpdateAsync([FromRoute] long id, [FromBody] UpdateSysLangTextDto dto)
    {
        if (dto == null)
        {
            throw new Exception("请求数据不能为空");
        }

        dto.Id = id;
        var entity = await _langTextRepository.GetByIdAsync(dto.Id);
        if (entity == null)
        {
            throw new Exception("语言文本不存在");
        }

        if (!string.IsNullOrEmpty(dto.LangCode) && !string.IsNullOrEmpty(dto.LangKey))
        {
            var exists = await _db.Queryable<SysLangText>()
                .Where(t => t.LangCode == dto.LangCode && t.LangKey == dto.LangKey && t.Id != dto.Id)
                .AnyAsync();
            if (exists)
            {
                throw new Exception($"语言键 {dto.LangKey} 已存在");
            }
        }

        dto.Adapt(entity);
        return await _langTextRepository.UpdateAsync(entity) > 0;
    }

    [Permission("system:lang:text:delete")]
    public async Task<bool> DeleteAsync(long id)
    {
        return await _langTextRepository.DeleteAsync(id) > 0;
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<Dictionary<string, string>> GetI18nMapAsync(string langCode)
    {
        var list = await _db.Queryable<SysLangText>()
            .Where(t => t.LangCode == langCode)
            .ToListAsync();

        var result = new Dictionary<string, string>();

        foreach (var item in list)
        {
            // 数据库中的键（标准格式：下划线命名）
            var dbKey = item.LangKey;
            result[dbKey] = item.LangValue;

            // 同时支持驼峰命名（batchDelete）
            var camelCaseKey = ConvertToCamelCase(dbKey);
            if (camelCaseKey != dbKey)
            {
                result[camelCaseKey] = item.LangValue;
            }

            // 同时支持帕斯卡命名（BatchDelete）
            var pascalCaseKey = ConvertToPascalCase(dbKey);
            if (pascalCaseKey != dbKey && pascalCaseKey != camelCaseKey)
            {
                result[pascalCaseKey] = item.LangValue;
            }
        }

        return result;
    }

    /// <summary>
    /// 将下划线命名转换为驼峰命名
    /// 例如：batch_delete -> batchDelete
    /// </summary>
    private string ConvertToCamelCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase) || !snakeCase.Contains('_'))
            return snakeCase;

        var parts = snakeCase.Split('_');
        var result = parts[0].ToLower();

        for (int i = 1; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
            }
        }

        return result;
    }

    /// <summary>
    /// 将下划线命名转换为帕斯卡命名
    /// 例如：batch_delete -> BatchDelete
    /// </summary>
    private string ConvertToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase) || !snakeCase.Contains('_'))
        {
            // 如果没有下划线，首字母大写
            return string.IsNullOrEmpty(snakeCase) ? snakeCase : char.ToUpper(snakeCase[0]) + snakeCase.Substring(1);
        }

        var parts = snakeCase.Split('_');
        var result = "";

        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part))
            {
                result += char.ToUpper(part[0]) + part.Substring(1).ToLower();
            }
        }

        return result;
    }

    [Permission("system:lang:text:export")]
    public async Task<string> ExportAllTranslationsToJsonAsync()
    {
        // 获取所有翻译数据
        var allTexts = await _db.Queryable<SysLangText>()
            .OrderBy(t => t.LangKey)
            .OrderBy(t => t.LangCode)
            .ToListAsync();

        // 按 LangKey 分组
        var grouped = allTexts
            .GroupBy(t => t.LangKey)
            .Select(g => new
            {
                key = g.Key,
                category = g.First().Category ?? "",
                translations = g.GroupBy(t => t.LangCode)
                    .ToDictionary(lg => lg.Key, lg => lg.First().LangValue)
            })
            .ToList();

        var result = new
        {
            exportTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            totalKeys = grouped.Count,
            translations = grouped
        };

        // 序列化为 JSON，确保中文不被转义
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        return JsonSerializer.Serialize(result, options);
    }
}
