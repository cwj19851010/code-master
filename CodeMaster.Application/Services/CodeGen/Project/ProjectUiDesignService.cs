using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Infrastructure.VueParser;
using CodeMaster.Infrastructure.VueParser.Model;

namespace CodeMaster.Application.Services.CodeGen;

public static class ProjectUiDesignService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly HashSet<string> PageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "index", "add", "edit", "detail"
    };

    private static readonly HashSet<string> ContainerTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "section", "main", "aside", "el-card", "el-row", "el-col", "el-space",
        "el-container", "el-header", "el-aside", "el-main", "el-footer", "el-tabs", "el-tab-pane"
    };

    private static readonly HashSet<string> ControlTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "el-input", "el-input-number", "el-input-tag", "el-autocomplete", "el-mention",
        "el-select", "el-tree-select", "el-cascader", "el-date-picker", "el-time-picker",
        "el-time-select", "el-switch", "el-slider", "el-rate", "el-color-picker",
        "el-checkbox", "el-checkbox-group", "el-radio", "el-radio-group", "el-upload"
    };

    private static readonly HashSet<string> OptionContainerTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "el-select", "el-checkbox-group", "el-radio-group", "el-tree-select", "el-cascader"
    };

    public static async Task<ProjectUiEnhancementResultDto> ApplyEntityPageAsync(
        Project project,
        ProjectModule module,
        ModuleEntity entity,
        ProjectUiEnhancementDto input,
        string projectRoot)
    {
        var pageType = NormalizePageType(input.PageType ?? input.Page);
        var entityLower = char.ToLowerInvariant(entity.Name[0]) + entity.Name[1..];
        var viewRoot = Path.Combine(
            projectRoot,
            $"{project.ProjectName}.Vue",
            "src",
            "views",
            module.ModuleName.ToLowerInvariant(),
            entity.Name.ToLowerInvariant());
        var treePath = Path.Combine(viewRoot, $"{entityLower}.{pageType}.tree.json");
        var vuePath = Path.Combine(viewRoot, $"{pageType}.vue");
        var designPath = Path.Combine(viewRoot, $"{entityLower}.{pageType}.design.json");

        EnsureInsideDirectory(projectRoot, treePath);
        EnsureInsideDirectory(projectRoot, vuePath);
        if (!File.Exists(treePath))
            throw new FileNotFoundException($"Component tree not found: {treePath}", treePath);
        if (!File.Exists(vuePath))
            throw new FileNotFoundException($"Vue page not found: {vuePath}", vuePath);

        var document = input.ReplaceExistingDesign
            ? new ProjectUiDesignDocumentDto()
            : await ReadDesignDocumentAsync(designPath);
        document.Operations = MergeOperations(document.Operations, input.Operations);
        document.UpdatedAt = DateTime.UtcNow;

        var originalTreeJson = await File.ReadAllTextAsync(treePath);
        var transformedTreeJson = ApplyDesignDocument(originalTreeJson, document, out var appliedCount);
        var components = DeserializeTree(transformedTreeJson);
        var templateHtml = new VueTemplateSerializer().Serialize(components);
        var originalVue = await File.ReadAllTextAsync(vuePath);
        var transformedVue = ReplaceTemplateContent(originalVue, templateHtml);

        var backupRoot = Path.Combine(projectRoot, ".codemaster", "backups", "ui", DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        Directory.CreateDirectory(backupRoot);
        var backupTreePath = Path.Combine(backupRoot, Path.GetFileName(treePath));
        var backupVuePath = Path.Combine(backupRoot, $"{entityLower}.{pageType}.vue");
        File.Copy(treePath, backupTreePath, overwrite: true);
        File.Copy(vuePath, backupVuePath, overwrite: true);

        await WriteAtomicAsync(treePath, transformedTreeJson);
        await WriteAtomicAsync(vuePath, transformedVue);
        await WriteAtomicAsync(designPath, JsonSerializer.Serialize(document, JsonOptions));

        return new ProjectUiEnhancementResultDto
        {
            Success = true,
            Message = $"{entity.Name} {pageType} page design updated",
            Page = pageType,
            FilePath = vuePath,
            BackupPath = backupVuePath,
            DesignPath = designPath,
            AppliedOperationCount = appliedCount
        };
    }

    public static async Task<string> ReplaySavedDesignAsync(string treeJson, string designPath)
    {
        if (!File.Exists(designPath))
            return treeJson;

        var document = await ReadDesignDocumentAsync(designPath);
        return ApplyDesignDocument(treeJson, document, out _);
    }

    public static string ApplyDesignDocument(
        string treeJson,
        ProjectUiDesignDocumentDto document,
        out int appliedCount)
    {
        var tree = DeserializeTree(treeJson);
        EnsureStableNodeIds(tree);
        appliedCount = 0;

        foreach (var operation in document.Operations)
        {
            if (ApplyOperation(tree, operation))
                appliedCount++;
        }

        return JsonSerializer.Serialize(tree, JsonOptions);
    }

    public static void EnsureStableNodeIds(List<Component> tree)
    {
        AssignStableIds(tree, "root");
    }

    private static bool ApplyOperation(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        return operation.Type.Trim().ToLowerInvariant() switch
        {
            "settag" => ApplySetTag(tree, operation),
            "setprop" => ApplySetProp(tree, operation),
            "removeprop" => ApplyRemoveProp(tree, operation),
            "setgrid" => ApplySetGrid(tree, operation),
            "move" => ApplyMove(tree, operation),
            "group" => ApplyGroup(tree, operation),
            _ => throw new ArgumentException($"Unsupported UI design operation: {operation.Type}")
        };
    }

    private static bool ApplySetTag(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        if (string.IsNullOrWhiteSpace(operation.Tag))
            throw new ArgumentException("SetTag requires Tag.");

        var location = ResolveScopedLocation(tree, operation);
        if (location == null) return false;
        var newTag = operation.Tag.Trim();
        ValidateTagChange(location.Node.Tag, newTag, operation.TargetScope);

        if (string.Equals(location.Node.Tag, "el-card", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(newTag, "el-card", StringComparison.OrdinalIgnoreCase) &&
            location.Node.UseSlots is { Count: > 0 })
        {
            location.Node.Children ??= new List<Component>();
            var slotChildren = location.Node.UseSlots
                .SelectMany(slot => slot.Components ?? Enumerable.Empty<Component>())
                .ToList();
            location.Node.Children.InsertRange(0, slotChildren);
            location.Node.UseSlots = new List<ComponentSlot>();
        }

        if (ControlTags.Contains(location.Node.Tag) &&
            !OptionContainerTags.Contains(newTag))
        {
            location.Node.Children = null;
            location.Node.UseSlots = new List<ComponentSlot>();
        }

        location.Node.Tag = newTag;
        return true;
    }

    private static bool ApplySetProp(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        if (string.IsNullOrWhiteSpace(operation.PropName))
            throw new ArgumentException("SetProp requires PropName.");

        var location = ResolveScopedLocation(tree, operation);
        if (location == null) return false;
        UpsertProp(location.Node, operation.PropName.Trim(), operation.PropValue, operation.IsBind, operation.IsSingle);
        return true;
    }

    private static bool ApplyRemoveProp(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        if (string.IsNullOrWhiteSpace(operation.PropName))
            throw new ArgumentException("RemoveProp requires PropName.");

        var location = ResolveScopedLocation(tree, operation);
        if (location == null) return false;
        var removed = location.Node.Props?.RemoveAll(prop =>
            string.Equals(prop.Key, operation.PropName, StringComparison.OrdinalIgnoreCase)) ?? 0;
        return removed > 0;
    }

    private static bool ApplySetGrid(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        var target = FindLocation(tree, operation.TargetGenId, operation.TargetNodeId);
        if (target == null) return false;
        var column = FindNearest(target, node => string.Equals(node.Tag, "el-col", StringComparison.OrdinalIgnoreCase));
        if (column == null) return false;

        SetGridProp(column.Node, "xs", operation.Xs);
        SetGridProp(column.Node, "sm", operation.Sm);
        SetGridProp(column.Node, "md", operation.Md);
        SetGridProp(column.Node, "lg", operation.Lg);
        return true;
    }

    private static bool ApplyMove(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        var source = ResolveScopedLocation(tree, operation);
        if (source == null) return false;

        if (!string.IsNullOrWhiteSpace(operation.BeforeGenId) || !string.IsNullOrWhiteSpace(operation.AfterGenId))
        {
            var anchor = ResolveScope(
                FindLocation(tree, operation.BeforeGenId ?? operation.AfterGenId, null),
                operation.TargetScope);
            if (anchor == null || ReferenceEquals(anchor.Node, source.Node)) return false;
            source.Container.RemoveAt(source.Index);
            var refreshedAnchor = ResolveScope(
                FindLocation(tree, operation.BeforeGenId ?? operation.AfterGenId, null),
                operation.TargetScope);
            if (refreshedAnchor == null) return false;
            var insertIndex = refreshedAnchor.Index + (string.IsNullOrWhiteSpace(operation.AfterGenId) ? 0 : 1);
            refreshedAnchor.Container.Insert(insertIndex, source.Node);
            return true;
        }

        if (string.IsNullOrWhiteSpace(operation.ParentGenId))
            throw new ArgumentException("Move requires ParentGenId, BeforeGenId, or AfterGenId.");

        var parentOperation = new ProjectUiNodeOperationDto
        {
            TargetGenId = operation.ParentGenId,
            TargetScope = operation.ParentScope
        };
        var targetParent = ResolveScopedLocation(tree, parentOperation);
        if (targetParent == null || IsDescendant(source.Node, targetParent.Node)) return false;

        source.Container.RemoveAt(source.Index);
        targetParent.Node.Children ??= new List<Component>();
        targetParent.Node.Children.Add(source.Node);
        return true;
    }

    private static bool ApplyGroup(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        if (operation.MemberGenIds.Count == 0)
            throw new ArgumentException("Group requires MemberGenIds.");

        var groupId = NormalizeDesignId(operation.GroupId);
        var existingGroup = FindLocation(tree, null, $"design_group_{groupId}");
        if (existingGroup != null)
            RemoveLocation(existingGroup);

        var members = operation.MemberGenIds
            .Select(genId => FindLocation(tree, genId, null))
            .Where(location => location != null)
            .Select(location => FindNearest(location!, node => string.Equals(node.Tag, "el-col", StringComparison.OrdinalIgnoreCase)) ?? location!)
            .DistinctBy(location => location.Node.Id)
            .ToList();
        if (members.Count == 0) return false;

        var first = members[0];
        var targetContainer = first.Container;
        var insertIndex = first.Index - members.Count(location =>
            ReferenceEquals(location.Container, targetContainer) && location.Index < first.Index);
        var movedNodes = members.Select(location => location.Node).ToList();
        foreach (var containerGroup in members.GroupBy(location => location.Container))
        {
            foreach (var member in containerGroup.OrderByDescending(location => location.Index))
                member.Container.RemoveAt(member.Index);
        }

        var groupRow = new Component
        {
            Id = $"design_group_{groupId}_row",
            Tag = "el-row",
            Props = new List<ComponentProp>
            {
                new() { Key = "gutter", Value = "20", IsBind = true }
            },
            Children = movedNodes,
            UseSlots = new List<ComponentSlot>(),
            Events = new List<ComponentEvent>(),
            Instructions = new List<ComponentInstruction>()
        };
        var groupCard = new Component
        {
            Id = $"design_group_{groupId}",
            Tag = string.IsNullOrWhiteSpace(operation.Tag) ? "el-card" : operation.Tag.Trim(),
            Props = new List<ComponentProp>
            {
                new() { Key = "shadow", Value = "never" },
                new() { Key = "class", Value = "design-field-group" }
            },
            Children = new List<Component> { groupRow },
            UseSlots = BuildGroupHeader(operation.GroupTitle),
            Events = new List<ComponentEvent>(),
            Instructions = new List<ComponentInstruction>()
        };
        var groupColumn = new Component
        {
            Id = $"design_group_{groupId}_column",
            Tag = "el-col",
            EntityField = string.Join(',', operation.MemberGenIds),
            Props = new List<ComponentProp>
            {
                new() { Key = "xs", Value = "24", IsBind = true },
                new() { Key = "sm", Value = "24", IsBind = true }
            },
            Children = new List<Component> { groupCard },
            UseSlots = new List<ComponentSlot>(),
            Events = new List<ComponentEvent>(),
            Instructions = new List<ComponentInstruction>()
        };

        targetContainer.Insert(Math.Min(insertIndex, targetContainer.Count), groupColumn);
        return true;
    }

    private static List<ComponentSlot> BuildGroupHeader(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return new List<ComponentSlot>();

        return new List<ComponentSlot>
        {
            new()
            {
                Name = "header",
                Components = new List<Component>
                {
                    new()
                    {
                        Id = $"design_text_{StableHash(title)}",
                        Tag = "span",
                        Props = new List<ComponentProp>
                        {
                            new() { Key = "class", Value = "design-field-group__title" }
                        },
                        Children = new List<Component>
                        {
                            new()
                            {
                                Id = $"design_text_{StableHash(title)}_content",
                                Tag = "text",
                                Content = WebUtility.HtmlEncode(title.Trim()),
                                Props = new List<ComponentProp>(),
                                Children = new List<Component>(),
                                UseSlots = new List<ComponentSlot>(),
                                Events = new List<ComponentEvent>(),
                                Instructions = new List<ComponentInstruction>()
                            }
                        },
                        UseSlots = new List<ComponentSlot>(),
                        Events = new List<ComponentEvent>(),
                        Instructions = new List<ComponentInstruction>()
                    }
                }
            }
        };
    }

    private static NodeLocation? ResolveScopedLocation(List<Component> tree, ProjectUiNodeOperationDto operation)
    {
        var location = FindLocation(tree, operation.TargetGenId, operation.TargetNodeId);
        return ResolveScope(location, operation.TargetScope);
    }

    private static NodeLocation? ResolveScope(NodeLocation? location, string? scope)
    {
        if (location == null) return null;
        return (scope ?? "Self").Trim().ToLowerInvariant() switch
        {
            "control" => FindDescendant(location, node =>
                ControlTags.Contains(node.Tag) ||
                node.Instructions?.Any(item => string.Equals(item.Name, "v-model", StringComparison.OrdinalIgnoreCase)) == true),
            "fieldunit" or "column" => FindNearest(location, node =>
                string.Equals(node.Tag, "el-col", StringComparison.OrdinalIgnoreCase)),
            "container" => FindNearest(location, node => ContainerTags.Contains(node.Tag)),
            _ => location
        } ?? location;
    }

    private static NodeLocation? FindLocation(
        List<Component> tree,
        string? genId,
        string? nodeId)
    {
        foreach (var location in EnumerateLocations(tree, null))
        {
            if (!string.IsNullOrWhiteSpace(genId) &&
                string.Equals(location.Node.GenId, genId, StringComparison.OrdinalIgnoreCase))
                return location;
            if (!string.IsNullOrWhiteSpace(nodeId) &&
                string.Equals(location.Node.Id, nodeId, StringComparison.OrdinalIgnoreCase))
                return location;
        }

        return null;
    }

    private static NodeLocation? FindNearest(NodeLocation start, Func<Component, bool> predicate)
    {
        for (var current = start; current != null; current = current.Parent)
        {
            if (predicate(current.Node)) return current;
        }

        return null;
    }

    private static NodeLocation? FindDescendant(NodeLocation start, Func<Component, bool> predicate)
    {
        if (predicate(start.Node)) return start;
        foreach (var location in EnumerateChildLocations(start))
        {
            if (predicate(location.Node)) return location;
        }

        return null;
    }

    private static IEnumerable<NodeLocation> EnumerateLocations(List<Component> nodes, NodeLocation? parent)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            var location = new NodeLocation(nodes[index], nodes, index, parent);
            yield return location;
            foreach (var descendant in EnumerateChildLocations(location))
                yield return descendant;
        }
    }

    private static IEnumerable<NodeLocation> EnumerateChildLocations(NodeLocation parent)
    {
        if (parent.Node.Children != null)
        {
            foreach (var location in EnumerateLocations(parent.Node.Children, parent))
                yield return location;
        }

        if (parent.Node.UseSlots == null) yield break;
        foreach (var slot in parent.Node.UseSlots)
        {
            if (slot.Components == null) continue;
            foreach (var location in EnumerateLocations(slot.Components, parent))
                yield return location;
        }
    }

    private static void AssignStableIds(List<Component>? nodes, string parentSeed)
    {
        if (nodes == null) return;
        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            var identity = !string.IsNullOrWhiteSpace(node.GenId)
                ? $"gen:{node.GenId}"
                : !string.IsNullOrWhiteSpace(node.EntityField)
                    ? $"field:{node.EntityTable}:{node.EntityField}:{node.Tag}"
                    : $"{node.Tag}:{index}";
            node.Id ??= $"node_{StableHash($"{parentSeed}/{identity}")}";
            AssignStableIds(node.Children, node.Id);
            if (node.UseSlots == null) continue;
            foreach (var slot in node.UseSlots)
                AssignStableIds(slot.Components, $"{node.Id}/slot:{slot.Name}");
        }
    }

    private static void ValidateTagChange(string oldTag, string newTag, string scope)
    {
        if (string.Equals(oldTag, newTag, StringComparison.OrdinalIgnoreCase)) return;
        if (scope.Equals("Control", StringComparison.OrdinalIgnoreCase))
        {
            if (!ControlTags.Contains(oldTag) || !ControlTags.Contains(newTag))
                throw new InvalidOperationException($"Control tag cannot change from {oldTag} to {newTag}.");
            if (OptionContainerTags.Contains(newTag) && !OptionContainerTags.Contains(oldTag))
            {
                throw new InvalidOperationException(
                    $"Changing {oldTag} to {newTag} requires field metadata and control-template generation, not a visual-only operation.");
            }
            return;
        }

        if (ContainerTags.Contains(oldTag) && ContainerTags.Contains(newTag)) return;
        if (ControlTags.Contains(oldTag) && ControlTags.Contains(newTag)) return;
        if (string.Equals(oldTag, "el-button", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(newTag, "el-link", StringComparison.OrdinalIgnoreCase)) return;
        if (string.Equals(oldTag, "el-link", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(newTag, "el-button", StringComparison.OrdinalIgnoreCase)) return;

        throw new InvalidOperationException($"Incompatible semantic tag change: {oldTag} -> {newTag}.");
    }

    private static List<ProjectUiNodeOperationDto> MergeOperations(
        IEnumerable<ProjectUiNodeOperationDto> current,
        IEnumerable<ProjectUiNodeOperationDto> incoming)
    {
        var merged = current.ToList();
        foreach (var operation in incoming)
        {
            NormalizeOperation(operation);
            var key = GetOperationKey(operation);
            merged.RemoveAll(item => string.Equals(GetOperationKey(item), key, StringComparison.OrdinalIgnoreCase));
            merged.Add(operation);
        }

        return merged;
    }

    private static void NormalizeOperation(ProjectUiNodeOperationDto operation)
    {
        operation.Type = operation.Type.Trim();
        operation.TargetScope = string.IsNullOrWhiteSpace(operation.TargetScope) ? "Self" : operation.TargetScope.Trim();
        operation.ParentScope = string.IsNullOrWhiteSpace(operation.ParentScope) ? "Self" : operation.ParentScope.Trim();
        operation.OperationId = string.IsNullOrWhiteSpace(operation.OperationId)
            ? $"op_{StableHash(GetOperationKey(operation))}"
            : operation.OperationId.Trim();
    }

    private static string GetOperationKey(ProjectUiNodeOperationDto operation)
    {
        var target = operation.TargetGenId ?? operation.TargetNodeId ?? operation.GroupId ?? string.Empty;
        return operation.Type.Trim().ToLowerInvariant() switch
        {
            "setprop" or "removeprop" => $"prop:{target}:{operation.TargetScope}:{operation.PropName}",
            "settag" => $"tag:{target}:{operation.TargetScope}",
            "setgrid" => $"grid:{target}",
            "move" => $"move:{target}:{operation.TargetScope}",
            "group" => $"group:{operation.GroupId}",
            _ => $"{operation.Type}:{operation.OperationId}:{target}"
        };
    }

    private static async Task<ProjectUiDesignDocumentDto> ReadDesignDocumentAsync(string designPath)
    {
        if (!File.Exists(designPath))
            return new ProjectUiDesignDocumentDto();

        var json = await File.ReadAllTextAsync(designPath);
        return JsonSerializer.Deserialize<ProjectUiDesignDocumentDto>(json, JsonOptions)
            ?? new ProjectUiDesignDocumentDto();
    }

    private static List<Component> DeserializeTree(string treeJson)
    {
        return JsonSerializer.Deserialize<List<Component>>(treeJson, JsonOptions)
            ?? throw new InvalidOperationException("Component tree JSON is invalid.");
    }

    private static string ReplaceTemplateContent(string vueContent, string templateHtml)
    {
        var replacement = $"<template>\n{templateHtml}\n</template>";
        var templateMatch = Regex.Match(vueContent, "<template(?:\\s[^>]*)?>", RegexOptions.IgnoreCase);
        if (!templateMatch.Success)
            throw new InvalidOperationException("Vue page does not contain a template block.");

        var scriptIndex = vueContent.IndexOf("<script", templateMatch.Index + templateMatch.Length, StringComparison.OrdinalIgnoreCase);
        var searchEnd = scriptIndex >= 0 ? scriptIndex : vueContent.Length;
        var templateEnd = vueContent.LastIndexOf("</template>", searchEnd - 1, StringComparison.OrdinalIgnoreCase);
        if (templateEnd < templateMatch.Index)
            throw new InvalidOperationException("Vue page template block is not closed.");

        var blockEnd = templateEnd + "</template>".Length;
        return string.Concat(vueContent.AsSpan(0, templateMatch.Index), replacement, vueContent.AsSpan(blockEnd));
    }

    private static void SetGridProp(Component node, string name, int? value)
    {
        if (!value.HasValue) return;
        if (value.Value is < 1 or > 24)
            throw new ArgumentOutOfRangeException(name, "Grid span must be between 1 and 24.");
        UpsertProp(node, name, value.Value.ToString(), isBind: true, isSingle: false);
    }

    private static void UpsertProp(Component node, string key, string? value, bool isBind, bool isSingle)
    {
        node.Props ??= new List<ComponentProp>();
        var prop = node.Props.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
        if (prop == null)
        {
            node.Props.Add(new ComponentProp { Key = key, Value = value, IsBind = isBind, IsSingle = isSingle });
            return;
        }

        prop.Value = value;
        prop.IsBind = isBind;
        prop.IsSingle = isSingle;
    }

    private static bool IsDescendant(Component source, Component target)
    {
        if (ReferenceEquals(source, target)) return true;
        if (source.Children?.Any(child => IsDescendant(child, target)) == true) return true;
        return source.UseSlots?.Any(slot => slot.Components?.Any(child => IsDescendant(child, target)) == true) == true;
    }

    private static void RemoveLocation(NodeLocation location)
    {
        if (location.Index >= 0 && location.Index < location.Container.Count)
            location.Container.RemoveAt(location.Index);
    }

    private static string NormalizePageType(string value)
    {
        var pageType = value.Trim().ToLowerInvariant();
        if (!PageTypes.Contains(pageType))
            throw new ArgumentException("Entity page type must be index, add, edit, or detail.");
        return pageType;
    }

    private static string NormalizeDesignId(string? value)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim();
        var normalized = Regex.Replace(candidate, "[^a-zA-Z0-9_-]", "_");
        return normalized.Length <= 64 ? normalized : normalized[..64];
    }

    private static string StableHash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
    }

    private static async Task WriteAtomicAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".codemaster.tmp";
        await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(false));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static void EnsureInsideDirectory(string root, string path)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);
        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved UI design path is outside the generated project.");
    }

    private sealed record NodeLocation(
        Component Node,
        List<Component> Container,
        int Index,
        NodeLocation? Parent);
}
