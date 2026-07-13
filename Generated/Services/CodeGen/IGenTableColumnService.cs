using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// GenTableColumn服务接口
/// </summary>
public interface IGenTableColumnService : ICrudService<GenTableColumnDto, CreateGenTableColumnDto, UpdateGenTableColumnDto, GenTableColumnQueryDto>
{
}
