using System.Text.RegularExpressions;
using CodeMaster.Infrastructure.VueParser.Model;

namespace CodeMaster.Infrastructure.VueParser;

/// <summary>
/// Vue 单文件组件 (.vue) 的 Template 部分解析器
/// 将 HTML 模板解析为 Component 树，支持 data-gen-id 标记识别
/// Port from SmartCoder.ProjectCore.VueTemplateGenerator
/// </summary>
public class VueTemplateParser
{
    /// <summary>
    /// 解析 Vue 文件中的 template 部分为 Component 树
    /// </summary>
    public List<Component> Parse(string vueContent)
    {
        // 1. 提取 template 内部内容
        string templateContent = ExtractTemplateContent(vueContent);

        // 2. 去除注释
        string cleanContent = RemoveComments(templateContent);

        // 2.5 转义引号内的 > (避免箭头函数 => 被误判为标签结束)
        string escapedContent = EscapeGtInQuotes(cleanContent);

        // 3. 修复未闭合标签
        string fixedContent = FixUnclosedTags(escapedContent);

        // 4. 解析为 Component 树
        var list = ParseComponents(fixedContent);
        FixTemplateSlots(list);
        return list;
    }

    /// <summary>
    /// 仅解析 template 内容（不含外层 <template> 标签）
    /// </summary>
    public List<Component> ParseTemplateContent(string templateInner)
    {
        string clean = RemoveComments(templateInner);
        string escaped = EscapeGtInQuotes(clean);
        string fixed_ = FixUnclosedTags(escaped);
        var list = ParseComponents(fixed_);
        FixTemplateSlots(list);
        return list;
    }

    #region Private helpers

    private string ExtractTemplateContent(string vueContent)
    {
        // 找第一个 <template> 和最后一个 </template>（跳过嵌套的 #default 等）
        var startMatch = Regex.Match(vueContent, @"<template[^>]*>", RegexOptions.IgnoreCase);
        var endMatches = Regex.Matches(vueContent, @"</template>", RegexOptions.IgnoreCase);

        if (startMatch.Success && endMatches.Count > 0)
        {
            int start = startMatch.Index + startMatch.Length;
            int end = endMatches[endMatches.Count - 1].Index; // 最后一个
            if (end > start)
                return vueContent.Substring(start, end - start);
        }
        return vueContent;
    }

    private string RemoveComments(string content)
    {
        content = Regex.Replace(content, @"<!--[\s\S]*?-->", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"//.*?$", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "", RegexOptions.Multiline);
        return content;
    }

    /// <summary>
    /// 将双引号属性值内的 &gt; 替换为占位符 \u00FF，避免正则误判为标签结束
    /// 例如 :on-success="(res) => { ... }" 中的 =&gt; 会破坏 [^&gt;]* 匹配
    /// </summary>
    private static string EscapeGtInQuotes(string content)
    {
        var sb = new System.Text.StringBuilder(content);
        bool inQuote = false;
        for (int i = 0; i < sb.Length; i++)
        {
            if (sb[i] == '"')
                inQuote = !inQuote;
            else if (inQuote && sb[i] == '>')
                sb[i] = '\u00FF'; // 占位符
        }
        return sb.ToString();
    }

    private static string UnescapeGtInQuotes(string content)
    {
        return content.Replace('\u00FF', '>');
    }

    private string FixUnclosedTags(string content)
    {
        var tagStack = new Stack<string>();
        var tagPattern = @"<(/?)([\w-]+)(\s[^>]*?)*(/?)>";
        var matches = Regex.Matches(content, tagPattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            bool isClosing = match.Groups[1].Value == "/";
            string tagName = match.Groups[2].Value;
            bool isSelfClosing = match.Groups[4].Value == "/" || IsSelfClosingTag(tagName);

            if (isSelfClosing) continue;

            if (isClosing)
            {
                if (tagStack.Count > 0 && tagStack.Peek() == tagName)
                    tagStack.Pop();
            }
            else
            {
                tagStack.Push(tagName);
            }
        }

        var result = new System.Text.StringBuilder(content);
        var unclosed = tagStack.Reverse().ToList();
        foreach (var tag in unclosed)
            result.Append($"</{tag}>\n");

        return result.ToString();
    }

    private List<Component> ParseComponents(string content)
    {
        var components = new List<Component>();
        var stack = new Stack<Component>();

        var tagPattern = @"<(/?)(\w+[\w-]*)(\s[^>]*)?(\/?)>|([^<]+)";
        var matches = Regex.Matches(content, tagPattern, RegexOptions.Singleline);

        Component? currentParent = null;

        foreach (Match match in matches)
        {
            string fullMatch = match.Value.Trim();
            if (string.IsNullOrEmpty(fullMatch)) continue;

            // 文本内容
            if (!fullMatch.StartsWith("<"))
            {
                var textComp = CreateTextComponent(fullMatch);
                if (currentParent != null)
                {
                    currentParent.Children ??= new List<Component>();
                    currentParent.Children.Add(textComp);
                }
                else
                {
                    components.Add(textComp);
                }
                continue;
            }

            bool isClosing = match.Groups[1].Value == "/";
            string tagName = match.Groups[2].Value;
            string attributes = match.Groups[3].Value;
            bool isSelfClosing = fullMatch.EndsWith("/>") || IsSelfClosingTag(tagName);

            if (isClosing)
            {
                if (stack.Count > 0 && stack.Peek().Tag == tagName)
                {
                    stack.Pop();
                    currentParent = stack.Count > 0 ? stack.Peek() : null;
                }
            }
            else
            {
                var component = ParseSingleComponent(tagName, attributes);

                if (currentParent != null)
                {
                    currentParent.Children ??= new List<Component>();
                    currentParent.Children.Add(component);
                }
                else
                {
                    components.Add(component);
                }

                if (!isSelfClosing)
                {
                    stack.Push(component);
                    currentParent = component;
                }
            }
        }

        return components;
    }

    private Component CreateTextComponent(string content)
    {
        return new Component
        {
            Tag = "text",
            Content = content.Trim(),
            Props = new List<ComponentProp> { new() { Key = "content", Value = content.Trim(), Type = "string" } },
            Instructions = new List<ComponentInstruction>(),
            Events = new List<ComponentEvent>(),
            UseSlots = new List<ComponentSlot>()
        };
    }

    private bool IsSelfClosingTag(string tagName)
    {
        var selfClosing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "img", "br", "hr", "input", "meta", "link", "area", "base", "col",
            "embed", "source", "track", "wbr", "el-icon"
        };
        return selfClosing.Contains(tagName);
    }

    private Component ParseSingleComponent(string tagName, string attributes)
    {
        var component = new Component
        {
            Tag = tagName,
            Props = new List<ComponentProp>(),
            Instructions = new List<ComponentInstruction>(),
            Events = new List<ComponentEvent>(),
            UseSlots = new List<ComponentSlot>()
        };

        if (!string.IsNullOrEmpty(attributes))
        {
            ParseAttributes(attributes.Trim(), component);
        }

        return component;
    }

    private void ParseAttributes(string attributes, Component component)
    {
        // 还原被转义的 >
        attributes = UnescapeGtInQuotes(attributes);

        // Vue 指令关键字
        var vueDirectives = new HashSet<string>
        {
            "v-for", "v-if", "v-else", "v-else-if", "v-show", "v-model",
            "v-bind", "v-on", "v-slot", "v-text", "v-html", "v-once",
            "v-pre", "v-cloak", "v-hasPermi", "v-permission", "v-loading"
        };

        // 匹配属性：key="value" 或 key（无值）
        var attrPattern = @"([\w\-:.#@]+)(?:=""([^""]*)"")?";
        var matches = Regex.Matches(attributes, attrPattern);

        foreach (Match match in matches)
        {
            string attrName = match.Groups[1].Value;
            string attrValue = match.Groups[2].Value;
            bool hasValue = !string.IsNullOrEmpty(attrValue);

            // data-gen-id → 存储到 GenId
            if (attrName == "data-gen-id")
            {
                component.GenId = hasValue ? attrValue : null;
                continue;
            }

            // data-entity-table / data-entity-field
            if (attrName == "data-entity-table")
            {
                component.EntityTable = hasValue ? attrValue : null;
                continue;
            }
            if (attrName == "data-entity-field")
            {
                component.EntityField = hasValue ? attrValue : null;
                continue;
            }

            // <template #slotName> 或 <template v-slot:slotName> → Slot
            if (component.Tag == "template")
            {
                string? slotName = null;
                if (attrName.StartsWith("#"))
                    slotName = attrName[1..];
                else if (attrName.StartsWith("v-slot:"))
                    slotName = attrName[7..];

                if (slotName != null)
                {
                    component.UseSlots!.Add(new ComponentSlot
                    {
                        Name = slotName,
                        Parameter = hasValue ? attrValue : null,
                        Components = new List<Component>()
                    });
                }
            }
            // @event → Event
            else if (attrName.StartsWith("@"))
            {
                component.Events!.Add(new ComponentEvent
                {
                    Name = attrName[1..],
                    Body = hasValue ? attrValue : null,
                    IsSingle = !hasValue
                });
            }
            // :prop → Bind prop
            else if (attrName.StartsWith(":"))
            {
                component.Props!.Add(new ComponentProp
                {
                    Key = attrName[1..],
                    Value = hasValue ? attrValue : null,
                    IsBind = true,
                    IsSingle = !hasValue
                });
            }
            // v-xxx → Instruction
            else if (vueDirectives.Contains(attrName) || attrName.StartsWith("v-"))
            {
                component.Instructions!.Add(new ComponentInstruction
                {
                    Name = attrName,
                    Value = hasValue ? attrValue : null,
                    IsSingle = !hasValue
                });
            }
            // ref → Ref
            else if (attrName == "ref")
            {
                component.Ref = hasValue ? attrValue : null;
            }
            // 普通属性
            else
            {
                component.Props!.Add(new ComponentProp
                {
                    Key = attrName,
                    Value = hasValue ? attrValue : null,
                    IsBind = false,
                    IsSingle = !hasValue
                });
            }
        }
    }

    /// <summary>
    /// 将 <template #slotName> 转为父组件的 UseSlots
    /// </summary>
    private void FixTemplateSlots(List<Component> parents)
    {
        foreach (var component in parents.ToList())
        {
            var templateSlots = component.Children?.Where(t =>
                t.Tag == "template" && t.UseSlots?.Count > 0);

            if (templateSlots != null)
            {
                foreach (var templateSlot in templateSlots.ToList())
                {
                    if (templateSlot?.UseSlots?.Count > 0)
                    {
                        component.UseSlots ??= new List<ComponentSlot>();
                        var useSlot = templateSlot.UseSlots[0];
                        useSlot.Components = templateSlot.Children;
                        component.UseSlots.Add(useSlot);
                        component.Children!.Remove(templateSlot);

                        if (useSlot.Components != null)
                            FixTemplateSlots(useSlot.Components);
                    }
                }
            }

            if (component.Children != null)
                FixTemplateSlots(component.Children);
        }
    }

    #endregion
}
