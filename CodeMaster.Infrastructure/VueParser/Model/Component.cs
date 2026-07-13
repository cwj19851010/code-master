namespace CodeMaster.Infrastructure.VueParser.Model;

/// <summary>
/// Vue 模板解析后的组件节点（DOM 树节点）
/// 支持按 data-gen-id 属性做增量查找和替换
/// </summary>
public class Component
{
    public string? Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Ref { get; set; }
    public string? GenId { get; set; }
    /// <summary>所属表名: 空=主表, "OrderItem"=子表</summary>
    public string? EntityTable { get; set; }
    /// <summary>所属字段名</summary>
    public string? EntityField { get; set; }
    public List<Component>? Children { get; set; }
    public string? Content { get; set; }
    public List<ComponentProp>? Props { get; set; }
    public List<ComponentSlot>? UseSlots { get; set; }
    public List<ComponentEvent>? Events { get; set; }
    public List<ComponentInstruction>? Instructions { get; set; }
    /// <summary>标记为已删除，序列化时跳过</summary>
    public bool IsDeleted { get; set; }

    /// <summary>该节点关联的 ScriptSection JSON（生成时嵌入）</summary>
    public string? ScriptSection { get; set; }

    /// <summary>
    /// 递归查找具有指定 data-gen-id 属性的第一个节点
    /// </summary>
    public Component? FindByGenId(string genId)
    {
        if (GenId == genId) return this;

        if (Children != null)
        {
            foreach (var child in Children)
            {
                var found = child.FindByGenId(genId);
                if (found != null) return found;
            }
        }

        return null;
    }

    /// <summary>
    /// 递归查找所有具有指定 data-gen-id 属性的节点
    /// </summary>
    public List<Component> FindAllByGenId(string genId)
    {
        var result = new List<Component>();
        if (GenId == genId) result.Add(this);

        if (Children != null)
        {
            foreach (var child in Children)
                result.AddRange(child.FindAllByGenId(genId));
        }

        return result;
    }

    /// <summary>
    /// 查找直接子节点中匹配 data-gen-id 的第一个，找不到返回 null
    /// </summary>
    public Component? FindChildByGenId(string genId)
    {
        if (Children == null) return null;
        return Children.FirstOrDefault(c => c.GenId == genId);
    }

    /// <summary>
    /// 替换直接子节点中匹配 data-gen-id 的节点，返回是否替换成功
    /// </summary>
    public bool ReplaceChildByGenId(string genId, Component newChild)
    {
        if (Children == null) return false;
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i].GenId == genId)
            {
                Children[i] = newChild;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 在末尾区域插入新子节点（用于新增字段的插入点查找）
    /// </summary>
    public void InsertBeforeLastNonGenChild(Component newChild)
    {
        Children ??= new List<Component>();

        // 找到最后一个非 gen 标记的文本节点位置，插在它前面
        int insertAt = Children.Count;
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].Tag == "text" && Children[i].GenId == null)
                insertAt = i;
            else if (Children[i].GenId != null)
                break;
        }
        Children.Insert(insertAt, newChild);
    }
}
