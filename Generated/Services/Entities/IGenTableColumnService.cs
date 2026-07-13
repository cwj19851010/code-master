using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.Entities;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// GenTableColumn服务接口
/// </summary>
public interface IGenTableColumnService : ICrudService<GenTableColumnDto, CreateGenTableColumnDto, UpdateGenTableColumnDto, GenTableColumnQueryDto>
{
}
