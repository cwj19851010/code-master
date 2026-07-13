using System.Text;
using CodeMaster.Infrastructure.VueParser.Model;

namespace CodeMaster.Infrastructure.VueParser;

/// <summary>
/// 将 Component 树序列化回 Vue HTML 字符串
/// 自动跳过 IsDeleted=true 的节点
/// 保留 data-gen-id 属性
/// Port from SmartCoder.ProjectCore.VueTemplateGenerator.GenerateVueTemplate
/// </summary>
public class VueTemplateSerializer
{
    /// <summary>
    /// 将 Component 树序列化为 Vue template 内容（不含外层 <template> 标签）
    /// </summary>
    public string Serialize(List<Component> components)
    {
        var sb = new StringBuilder();
        foreach (var component in components)
        {
            SerializeComponent(sb, component, 0);
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 序列化为完整 Vue template（含 <template> 标签）
    /// </summary>
    public string SerializeWithWrapper(List<Component> components)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<template>");
        foreach (var component in components)
        {
            SerializeComponent(sb, component, 1);
        }
        sb.AppendLine("</template>");
        return sb.ToString();
    }

    private void SerializeComponent(StringBuilder sb, Component component, int indentLevel)
    {
        // 跳过已删除节点
        if (component.IsDeleted) return;

        var indent = new string(' ', indentLevel * 2);

        // 文本节点
        if (component.Tag == "text")
        {
            if (!string.IsNullOrEmpty(component.Content))
                sb.Append(component.Content);
            return;
        }

        // 开始标签
        sb.Append($"{indent}<{component.Tag}");

        // data-gen-id
        if (!string.IsNullOrEmpty(component.GenId))
            sb.Append($" data-gen-id=\"{component.GenId}\"");

        // ref
        if (!string.IsNullOrEmpty(component.Ref))
            sb.Append($" ref=\"{component.Ref}\"");

        // Props
        if (component.Props != null)
        {
            foreach (var prop in component.Props)
            {
                sb.Append(' ');
                if (prop.IsBind) sb.Append(':');
                if (prop.IsSingle)
                    sb.Append(prop.Key);
                else
                    sb.Append($"{prop.Key}=\"{prop.Value}\"");
            }
        }

        // Instructions (v-if, v-for, v-model...)
        if (component.Instructions != null)
        {
            foreach (var inst in component.Instructions)
            {
                sb.Append(' ');
                if (inst.IsSingle)
                    sb.Append(inst.Name);
                else
                    sb.Append($"{inst.Name}=\"{inst.Value}\"");
            }
        }

        // Events (@click, @change...)
        if (component.Events != null)
        {
            foreach (var evt in component.Events)
            {
                sb.Append($" @{evt.Name}");
                if (!string.IsNullOrEmpty(evt.Expression))
                    sb.Append($"=\"{evt.Expression}\"");
                else if (!string.IsNullOrEmpty(evt.Body))
                    sb.Append($"=\"{evt.Body}\"");
            }
        }

        // Children / Content / Slots
        bool hasChildren = (component.Children != null && component.Children.Any(c => !c.IsDeleted))
                        || (component.UseSlots != null && component.UseSlots.Count > 0)
                        || !string.IsNullOrEmpty(component.Content);

        if (hasChildren)
        {
            sb.AppendLine(">");

            // Content
            if (!string.IsNullOrEmpty(component.Content))
            {
                var contentIndent = new string(' ', (indentLevel + 1) * 2);
                sb.AppendLine($"{contentIndent}{component.Content}");
            }

            // Slots
            if (component.UseSlots != null)
            {
                foreach (var slot in component.UseSlots)
                {
                    SerializeSlot(sb, slot, indentLevel + 1);
                }
            }

            // Children
            if (component.Children != null)
            {
                foreach (var child in component.Children)
                {
                    SerializeComponent(sb, child, indentLevel + 1);
                }
            }

            sb.AppendLine($"{indent}</{component.Tag}>");
        }
        else
        {
            sb.AppendLine(" />");
        }
    }

    private void SerializeSlot(StringBuilder sb, ComponentSlot slot, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);

        if (string.IsNullOrEmpty(slot.Parameter))
            sb.AppendLine($"{indent}<template #{slot.Name}>");
        else
            sb.AppendLine($"{indent}<template #{slot.Name}=\"{slot.Parameter}\">");

        if (slot.Components != null)
        {
            foreach (var c in slot.Components)
                SerializeComponent(sb, c, indentLevel + 1);
        }

        sb.AppendLine($"{indent}</template>");
    }
}
