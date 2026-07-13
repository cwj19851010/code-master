using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Application.Dtos.CodeGen;
using SqlSugar;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// GenTableColumn服务实现
/// </summary>
public class GenTableColumnService : IGenTableColumnService
{
    private readonly ISqlSugarClient _db;

    public GenTableColumnService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GenTableColumnDto> GetByIdAsync(long id)
    {
        var entity = await _db.Queryable<GenTableColumn>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        return MapToDto(entity);
    }

    public async Task<PagedResultDto<GenTableColumnDto>> GetPagedListAsync(GenTableColumnQueryDto query)
    {
        RefAsync<int> total = 0;
        var entities = await _db.Queryable<GenTableColumn>()
            .WhereIF(query.TableId.HasValue, x => x.TableId == query.TableId)
            .WhereIF(!string.IsNullOrEmpty(query.ColumnName), x => x.ColumnName!.Contains(query.ColumnName!))
            .WhereIF(!string.IsNullOrEmpty(query.PropertyName), x => x.PropertyName!.Contains(query.PropertyName!))
            .WhereIF(!string.IsNullOrEmpty(query.ColumnComment), x => x.ColumnComment!.Contains(query.ColumnComment!))
            .WhereIF(!string.IsNullOrEmpty(query.ColumnType), x => x.ColumnType!.Contains(query.ColumnType!))
            .WhereIF(!string.IsNullOrEmpty(query.CsharpType), x => x.CsharpType!.Contains(query.CsharpType!))
            .WhereIF(query.IsPk.HasValue, x => x.IsPk == query.IsPk)
            .WhereIF(query.IsIncrement.HasValue, x => x.IsIncrement == query.IsIncrement)
            .WhereIF(query.IsRequired.HasValue, x => x.IsRequired == query.IsRequired)
            .WhereIF(query.ShowInList.HasValue, x => x.ShowInList == query.ShowInList)
            .WhereIF(query.ShowInAdd.HasValue, x => x.ShowInAdd == query.ShowInAdd)
            .WhereIF(query.ShowInEdit.HasValue, x => x.ShowInEdit == query.ShowInEdit)
            .WhereIF(query.ShowInDetail.HasValue, x => x.ShowInDetail == query.ShowInDetail)
            .WhereIF(query.IsQuery.HasValue, x => x.IsQuery == query.IsQuery)
            .WhereIF(!string.IsNullOrEmpty(query.QueryType), x => x.QueryType!.Contains(query.QueryType!))
            .WhereIF(!string.IsNullOrEmpty(query.HtmlType), x => x.HtmlType!.Contains(query.HtmlType!))
            .WhereIF(!string.IsNullOrEmpty(query.DictType), x => x.DictType!.Contains(query.DictType!))
            .WhereIF(query.Sort.HasValue, x => x.Sort == query.Sort)
            .WhereIF(query.Status.HasValue, x => x.Status == query.Status)
            .WhereIF(!string.IsNullOrEmpty(query.Remark), x => x.Remark!.Contains(query.Remark!))
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(query.PageIndex, query.PageSize, total);

        return new PagedResultDto<GenTableColumnDto>
        {
            Items = entities.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<long> CreateAsync(CreateGenTableColumnDto dto)
    {
        var entity = new GenTableColumn
        {
            TableId = dto.TableId,
            ColumnName = dto.ColumnName,
            PropertyName = dto.PropertyName,
            ColumnComment = dto.ColumnComment,
            ColumnType = dto.ColumnType,
            CsharpType = dto.CsharpType,
            IsPk = dto.IsPk,
            IsIncrement = dto.IsIncrement,
            IsRequired = dto.IsRequired,
            ShowInList = dto.ShowInList,
            ShowInAdd = dto.ShowInAdd,
            ShowInEdit = dto.ShowInEdit,
            ShowInDetail = dto.ShowInDetail,
            IsQuery = dto.IsQuery,
            QueryType = dto.QueryType,
            HtmlType = dto.HtmlType,
            DictType = dto.DictType,
            Sort = dto.Sort,
            Status = dto.Status,
            Remark = dto.Remark,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(long id, UpdateGenTableColumnDto dto)
    {
        var entity = await _db.Queryable<GenTableColumn>()
            .Where(x => x.Id == id)
            .FirstAsync();

        if (entity == null)
            throw new Exception("数据不存在");

        entity.TableId = dto.TableId;
        entity.ColumnName = dto.ColumnName;
        entity.PropertyName = dto.PropertyName;
        entity.ColumnComment = dto.ColumnComment;
        entity.ColumnType = dto.ColumnType;
        entity.CsharpType = dto.CsharpType;
        entity.IsPk = dto.IsPk;
        entity.IsIncrement = dto.IsIncrement;
        entity.IsRequired = dto.IsRequired;
        entity.ShowInList = dto.ShowInList;
        entity.ShowInAdd = dto.ShowInAdd;
        entity.ShowInEdit = dto.ShowInEdit;
        entity.ShowInDetail = dto.ShowInDetail;
        entity.IsQuery = dto.IsQuery;
        entity.QueryType = dto.QueryType;
        entity.HtmlType = dto.HtmlType;
        entity.DictType = dto.DictType;
        entity.Sort = dto.Sort;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdateTime = DateTime.UtcNow;

        await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await _db.Deleteable<GenTableColumn>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task BatchDeleteAsync(long[] ids)
    {
        await _db.Deleteable<GenTableColumn>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();
    }

    private GenTableColumnDto MapToDto(GenTableColumn entity)
    {
        return new GenTableColumnDto
        {
            TableId = entity.TableId,
            ColumnName = entity.ColumnName,
            PropertyName = entity.PropertyName,
            ColumnComment = entity.ColumnComment,
            ColumnType = entity.ColumnType,
            CsharpType = entity.CsharpType,
            IsPk = entity.IsPk,
            IsIncrement = entity.IsIncrement,
            IsRequired = entity.IsRequired,
            ShowInList = entity.ShowInList,
            ShowInAdd = entity.ShowInAdd,
            ShowInEdit = entity.ShowInEdit,
            ShowInDetail = entity.ShowInDetail,
            IsQuery = entity.IsQuery,
            QueryType = entity.QueryType,
            HtmlType = entity.HtmlType,
            DictType = entity.DictType,
            Sort = entity.Sort,
            Status = entity.Status,
            Id = entity.Id,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            UpdateBy = entity.UpdateBy,
            UpdateTime = entity.UpdateTime,
            Remark = entity.Remark,
        };
    }
}
