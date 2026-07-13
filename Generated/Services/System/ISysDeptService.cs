using CodeMaster.Core.Services;
using CodeMaster.Application.Dtos.System;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// SysDept服务接口
/// </summary>
public interface ISysDeptService : ICrudService<SysDeptDto, CreateSysDeptDto, UpdateSysDeptDto, SysDeptQueryDto>
{
}
