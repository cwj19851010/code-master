using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// GenTable服务接口
/// </summary>
public interface IGenTableService : ICrudService<GenTableDto, CreateGenTableDto, UpdateGenTableDto, GenTableQueryDto>
{
}
