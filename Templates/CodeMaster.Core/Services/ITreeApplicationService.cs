using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

/// <summary>
/// 树形应用服务接口（继承CRUD功能，增加树形操作）
/// </summary>
public interface ITreeApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput, TCreateInput, TUpdateInput>
    : ICrudApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput, TCreateInput, TUpdateInput>
    where TEntity : class, IEntity<long>, new()
    where TGetOutputDto : EntityDto
    where TGetListOutputDto : EntityDto
    where TGetListInput : PagedQueryDto
{
    /// <summary>
    /// 获取树形结构列表
    /// </summary>
    Task<List<TGetListOutputDto>> GetTreeAsync(TGetListInput input);

    /// <summary>
    /// 获取所有子孙节点（包括自己）
    /// </summary>
    Task<List<TGetListOutputDto>> GetDescendantsAsync(long id);

    /// <summary>
    /// 获取所有祖先节点（不包括自己）
    /// </summary>
    Task<List<TGetListOutputDto>> GetAncestorsAsync(long id);

    /// <summary>
    /// 移动节点到新的父节点下
    /// </summary>
    Task MoveNodeAsync(long nodeId, long newParentId);

    /// <summary>
    /// 删除节点及其所有子孙节点
    /// </summary>
    Task DeleteWithDescendantsAsync(long id);
}
