using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Application.Dtos.CodeGen;
using SqlSugar;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// GenTable服务实现
/// </summary>
public class GenTableService : IGenTableService
{
    private readonly ISqlSugarClient _db;

    public GenTableService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GenTableDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<GenTable>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<GenTableDto>> GetPagedListAsync(GenTableQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<GenTable>()
            .WhereIF(!string.IsNullOrEmpty(query.TableName), x => x.TableName!.Contains(query.TableName!))
            .WhereIF(!string.IsNullOrEmpty(query.EntityName), x => x.EntityName!.Contains(query.EntityName!))
            .WhereIF(!string.IsNullOrEmpty(query.TableComment), x => x.TableComment!.Contains(query.TableComment!))
            .WhereIF(!string.IsNullOrEmpty(query.FunctionName), x => x.FunctionName!.Contains(query.FunctionName!))
            .WhereIF(query.ModuleId.HasValue, x => x.ModuleId == query.ModuleId)
            .WhereIF(query.IsReadOnly.HasValue, x => x.IsReadOnly == query.IsReadOnly)
            .WhereIF(query.OnlyDto.HasValue, x => x.OnlyDto == query.OnlyDto)
            .WhereIF(query.IsTree.HasValue, x => x.IsTree == query.IsTree)
            .WhereIF(query.IsChild.HasValue, x => x.IsChild == query.IsChild)
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(!string.IsNullOrEmpty(query.TreeParentField), x => x.TreeParentField!.Contains(query.TreeParentField!))
            .WhereIF(!string.IsNullOrEmpty(query.TreeNameField), x => x.TreeNameField!.Contains(query.TreeNameField!))
            .WhereIF(!string.IsNullOrEmpty(query.FunctionAuthor), x => x.FunctionAuthor!.Contains(query.FunctionAuthor!))
            .WhereIF(!string.IsNullOrEmpty(query.GenPath), x => x.GenPath!.Contains(query.GenPath!))
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<GenTableDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateGenTableDto dto)
    {
        var entity = new GenTable
        {
            TableName = dto.TableName,
            EntityName = dto.EntityName,
            TableComment = dto.TableComment,
            FunctionName = dto.FunctionName,
            ModuleId = dto.ModuleId,
            IsReadOnly = dto.IsReadOnly,
            OnlyDto = dto.OnlyDto,
            IsTree = dto.IsTree,
            IsChild = dto.IsChild,
            Status = dto.Status,
            TreeParentField = dto.TreeParentField,
            TreeNameField = dto.TreeNameField,
            FunctionAuthor = dto.FunctionAuthor,
            GenPath = dto.GenPath,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateGenTableDto dto)
    {
        var entity = await _db.Queryable<GenTable>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.TableName = dto.TableName;
        entity.EntityName = dto.EntityName;
        entity.TableComment = dto.TableComment;
        entity.FunctionName = dto.FunctionName;
        entity.ModuleId = dto.ModuleId;
        entity.IsReadOnly = dto.IsReadOnly;
        entity.OnlyDto = dto.OnlyDto;
        entity.IsTree = dto.IsTree;
        entity.IsChild = dto.IsChild;
        entity.Status = dto.Status;
        entity.TreeParentField = dto.TreeParentField;
        entity.TreeNameField = dto.TreeNameField;
        entity.FunctionAuthor = dto.FunctionAuthor;
        entity.GenPath = dto.GenPath;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<GenTable>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<GenTable>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private GenTableDto MapToDto(GenTable entity)
    {
        return new GenTableDto
        {
            TableName = entity.TableName,
            EntityName = entity.EntityName,
            TableComment = entity.TableComment,
            FunctionName = entity.FunctionName,
            ModuleId = entity.ModuleId,
            IsReadOnly = entity.IsReadOnly,
            OnlyDto = entity.OnlyDto,
            IsTree = entity.IsTree,
            IsChild = entity.IsChild,
            Status = entity.Status,
            TreeParentField = entity.TreeParentField,
            TreeNameField = entity.TreeNameField,
            FunctionAuthor = entity.FunctionAuthor,
            GenPath = entity.GenPath,
            Id = entity.Id,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            UpdateBy = entity.UpdateBy,
            UpdateTime = entity.UpdateTime,
            Remark = entity.Remark,
        };
    }
}
