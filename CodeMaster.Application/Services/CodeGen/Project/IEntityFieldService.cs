using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 实体字段服务接口
/// </summary>
public interface IEntityFieldService : ICrudApplicationService<EntityField, EntityFieldDto, EntityFieldDto, EntityFieldQueryDto, CreateEntityFieldDto, UpdateEntityFieldDto>, IApplicationService
{
    /// <summary>
    /// 根据实体ID获取字段列表
    /// </summary>
    Task<List<EntityFieldDto>> GetByEntityIdAsync(long moduleEntityId);

    /// <summary>
    /// 批量创建字段
    /// </summary>
    Task<List<long>> CreateBatchAsync(List<CreateEntityFieldDto> inputs);

    /// <summary>
    /// 批量更新字段
    /// </summary>
    Task<int> UpdateBatchAsync(List<EntityFieldDto> fields);
}
