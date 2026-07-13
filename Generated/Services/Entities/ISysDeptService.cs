using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.Entities;

namespace CodeMaster.Application.Services.Entities;

/// <summary>
/// SysDept服务接口
/// </summary>
public interface ISysDeptService : ICrudService<SysDeptDto, CreateSysDeptDto, UpdateSysDeptDto, SysDeptQueryDto>
{
}
