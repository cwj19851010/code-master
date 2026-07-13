using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.DynamicApi;
using Mapster;
using SqlSugar;
using Yitter.IdGenerator;

namespace CodeMaster.Application.Services;

/// <summary>
/// 树形应用服务基类（继承CRUD功能，增加树形操作）
/// </summary>
public abstract class TreeApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput, TCreateInput, TUpdateInput>
    : CrudApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput, TCreateInput, TUpdateInput>
    where TEntity : TreeEntityBase, new()
    where TGetOutputDto : EntityDto
    where TGetListOutputDto : EntityDto
    where TGetListInput : PagedQueryDto
{
    protected readonly ISqlSugarClient _db;

    protected TreeApplicationService(IRepository<TEntity> repository, ISqlSugarClient db)
        : base(repository)
    {
        _db = db;
    }

    /// <summary>
    /// 获取树形结构列表
    /// </summary>
    [DynamicApi(HttpMethod = "GET")]
    public virtual async Task<List<TGetListOutputDto>> GetTreeAsync(TGetListInput input)
    {
        // 获取所有数据
        var queryable = await CreateFilteredQueryAsync(input);
        var entities = await queryable.ToListAsync();

        // 转换为DTO
        var dtos = entities.Adapt<List<TGetListOutputDto>>();

        // 构建树形结构（子类可以重写此方法自定义树形构建逻辑）
        return BuildTree(dtos);
    }

    /// <summary>
    /// 获取所有子孙节点（包括自己）
    /// </summary>
    public virtual async Task<List<TGetListOutputDto>> GetDescendantsAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null)
            return new List<TGetListOutputDto>();

        // 查询所有子孙节点：ancestors以"父路径,父ID"开头
        var descendants = await _db.Queryable<TEntity>()
            .Where(x => x.Ancestors.StartsWith($"{entity.Ancestors},{entity.Id}"))
            .ToListAsync();

        // 包含自己
        descendants.Insert(0, entity);

        return descendants.Adapt<List<TGetListOutputDto>>();
    }

    /// <summary>
    /// 获取所有祖先节点（不包括自己）
    /// </summary>
    public virtual async Task<List<TGetListOutputDto>> GetAncestorsAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null || string.IsNullOrEmpty(entity.Ancestors))
            return new List<TGetListOutputDto>();

        // 解析ancestors字段，获取所有祖先ID
        var ancestorIds = entity.Ancestors
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x != "0")  // 排除根标记
            .Select(long.Parse)
            .ToList();

        if (ancestorIds.Count == 0)
            return new List<TGetListOutputDto>();

        // 查询所有祖先节点
        var ancestors = await _db.Queryable<TEntity>()
            .Where(x => ancestorIds.Contains(x.Id))
            .OrderBy(x => x.Id)  // 按ID排序，保证层级顺序
            .ToListAsync();

        return ancestors.Adapt<List<TGetListOutputDto>>();
    }

    /// <summary>
    /// 移动节点到新的父节点下
    /// </summary>
    public virtual async Task MoveNodeAsync(long nodeId, long newParentId)
    {
        // 1. 获取要移动的节点
        var node = await Repository.GetByIdAsync(nodeId);
        if (node == null)
            throw new Exception("节点不存在");

        // 2. 获取新父节点
        TEntity? newParent = null;
        string newAncestors;

        if (newParentId == 0)
        {
            // 移动到根节点
            newAncestors = "0";
        }
        else
        {
            newParent = await Repository.GetByIdAsync(newParentId);
            if (newParent == null)
                throw new Exception("新父节点不存在");

            // 3. 检查是否移动到自己的子孙节点（会造成循环）
            if (newParent.Ancestors != null &&
                (newParent.Ancestors.Contains($",{nodeId},") || newParent.Ancestors.EndsWith($",{nodeId}")))
            {
                throw new Exception("不能移动到自己的子孙节点");
            }

            newAncestors = $"{newParent.Ancestors},{newParent.Id}";
        }

        // 4. 保存旧的ancestors路径
        string oldAncestors = node.Ancestors ?? "0";

        // 5. 更新当前节点
        node.ParentId = newParentId == 0 ? null : newParentId;
        node.Ancestors = newAncestors;
        await Repository.UpdateAsync(node);

        // 6. 更新所有子孙节点的ancestors（关键步骤）
        var descendants = await _db.Queryable<TEntity>()
            .Where(x => x.Ancestors.StartsWith($"{oldAncestors},{nodeId}"))
            .ToListAsync();

        if (descendants.Any())
        {
            foreach (var descendant in descendants)
            {
                // 替换路径前缀
                // 例如：oldAncestors="0,1,2", newAncestors="0,1,3"
                // 子节点原路径="0,1,2,5,8" -> 新路径="0,1,3,5,8"
                descendant.Ancestors = descendant.Ancestors?.Replace(
                    $"{oldAncestors},{nodeId}",
                    $"{newAncestors},{nodeId}"
                );
                await Repository.UpdateAsync(descendant);
            }
        }
    }

    /// <summary>
    /// 删除节点及其所有子孙节点
    /// </summary>
    public virtual async Task DeleteWithDescendantsAsync(long id)
    {
        var node = await Repository.GetByIdAsync(id);
        if (node == null)
            return;

        // 1. 查找所有子孙节点
        var descendants = await _db.Queryable<TEntity>()
            .Where(x => x.Ancestors.StartsWith($"{node.Ancestors},{id}"))
            .ToListAsync();

        // 2. 删除所有子孙节点
        foreach (var descendant in descendants)
        {
            await Repository.DeleteAsync(descendant.Id);
        }

        // 3. 删除当前节点
        await Repository.DeleteAsync(id);
    }

    /// <summary>
    /// 创建实体（重写以自动计算ancestors）
    /// </summary>
    public override async Task<long> CreateAsync(TCreateInput input)
    {
        var entity = input.Adapt<TEntity>();

        // 生成雪花ID
        entity.Id = YitIdHelper.NextId();

        // 计算ancestors路径
        if (entity.ParentId == null || entity.ParentId == 0)
        {
            // 根节点
            entity.Ancestors = "0";
        }
        else
        {
            // 获取父节点
            var parent = await Repository.GetByIdAsync(entity.ParentId.Value);
            if (parent == null)
                throw new Exception("父节点不存在");

            // 拼接路径：父节点的ancestors + 父节点ID
            entity.Ancestors = $"{parent.Ancestors},{parent.Id}";
        }

        return await Repository.InsertAsync(entity);
    }

    /// <summary>
    /// 构建树形结构（子类可以重写此方法自定义树形构建逻辑）
    /// </summary>
    protected virtual List<TGetListOutputDto> BuildTree(List<TGetListOutputDto> allNodes)
    {
        // 默认实现：返回扁平列表
        // 子类可以重写此方法，构建真正的树形结构（带children字段）
        return allNodes;
    }
}
