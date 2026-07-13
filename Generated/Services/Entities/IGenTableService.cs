using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.Entities;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// GenTable服务接口
/// </summary>
public interface IGenTableService : ICrudService<GenTableDto, CreateGenTableDto, UpdateGenTableDto, GenTableQueryDto>
{
}
