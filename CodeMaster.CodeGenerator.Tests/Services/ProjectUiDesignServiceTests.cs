using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Infrastructure.VueParser.Model;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class ProjectUiDesignServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ApplyDesignDocument_ReplaysByGenIdAndPreservesExistingNodeIds()
    {
        var document = new ProjectUiDesignDocumentDto
        {
            Operations =
            {
                new ProjectUiNodeOperationDto
                {
                    Type = "SetTag",
                    TargetGenId = "gen_field_101",
                    TargetScope = "Control",
                    Tag = "el-input-number"
                },
                new ProjectUiNodeOperationDto
                {
                    Type = "SetGrid",
                    TargetGenId = "gen_field_101",
                    Md = 8,
                    Lg = 6
                },
                new ProjectUiNodeOperationDto
                {
                    Type = "Move",
                    TargetGenId = "gen_field_102",
                    TargetScope = "FieldUnit",
                    BeforeGenId = "gen_field_101"
                }
            }
        };

        var firstResult = Apply(CreateFormTree("first"), document, out var firstApplied);
        var secondResult = Apply(CreateFormTree("second"), document, out var secondApplied);

        Assert.Equal(3, firstApplied);
        Assert.Equal(3, secondApplied);
        Assert.Equal("first-field-101", FindByGenId(firstResult, "gen_field_101").Id);
        Assert.Equal("second-field-101", FindByGenId(secondResult, "gen_field_101").Id);
        Assert.Equal("el-input-number", Assert.Single(FindByGenId(firstResult, "gen_field_101").Children!).Tag);

        var firstRow = FindByGenId(firstResult, "gen_form_area").Children![0];
        Assert.Equal("gen_field_102", firstRow.Children![0].Children![0].GenId);
        Assert.Equal("gen_field_101", firstRow.Children[1].Children![0].GenId);

        var firstColumn = firstRow.Children[1];
        Assert.Equal("8", firstColumn.Props!.Single(prop => prop.Key == "md").Value);
        Assert.Equal("6", firstColumn.Props.Single(prop => prop.Key == "lg").Value);
        Assert.All(Flatten(firstResult), node => Assert.False(string.IsNullOrWhiteSpace(node.Id)));
    }

    [Fact]
    public void ApplyDesignDocument_GroupsFieldsWithoutChangingTheirGenIds()
    {
        var document = new ProjectUiDesignDocumentDto
        {
            Operations =
            {
                new ProjectUiNodeOperationDto
                {
                    Type = "Group",
                    GroupId = "base-info",
                    GroupTitle = "Base information",
                    MemberGenIds = { "gen_field_101", "gen_field_102" }
                }
            }
        };

        var result = Apply(CreateFormTree("group"), document, out var appliedCount);

        Assert.Equal(1, appliedCount);
        var groupColumn = FindById(result, "design_group_base-info_column");
        var groupCard = Assert.Single(groupColumn.Children!);
        Assert.Equal("design_group_base-info", groupCard.Id);
        var groupRow = Assert.Single(groupCard.Children!);
        Assert.Equal(new[] { "gen_field_101", "gen_field_102" },
            groupRow.Children!.Select(column => Assert.Single(column.Children!).GenId));
    }

    [Fact]
    public void ApplyDesignDocument_RejectsDataBackedControlConversion()
    {
        var document = new ProjectUiDesignDocumentDto
        {
            Operations =
            {
                new ProjectUiNodeOperationDto
                {
                    Type = "SetTag",
                    TargetGenId = "gen_field_101",
                    TargetScope = "Control",
                    Tag = "el-select"
                }
            }
        };

        Assert.Throws<InvalidOperationException>(() => Apply(CreateFormTree("invalid"), document, out _));
    }

    [Fact]
    public async Task ApplyEntityPageAsync_ReplacesTheOuterTemplateAndKeepsScriptBlock()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codemaster-ui-{Guid.NewGuid():N}");
        try
        {
            var viewRoot = Path.Combine(root, "Demo.Vue", "src", "views", "sales", "order");
            Directory.CreateDirectory(viewRoot);
            var treePath = Path.Combine(viewRoot, "order.edit.tree.json");
            var vuePath = Path.Combine(viewRoot, "edit.vue");
            await File.WriteAllTextAsync(treePath, JsonSerializer.Serialize(CreateFormTree("file")));
            await File.WriteAllTextAsync(vuePath, """
                <template>
                  <el-table>
                    <el-table-column>
                      <template #default>old nested template</template>
                    </el-table-column>
                  </el-table>
                </template>
                <script setup>
                const scriptMarker = 'keep-me'
                </script>
                """);

            var result = await ProjectUiDesignService.ApplyEntityPageAsync(
                new Project { Id = 1, ProjectName = "Demo", ProjectPath = root },
                new ProjectModule { Id = 2, ProjectId = 1, ModuleName = "Sales" },
                new ModuleEntity { Id = 3, ProjectId = 1, ModuleId = 2, Name = "Order" },
                new ProjectUiEnhancementDto
                {
                    ProjectId = 1,
                    TargetKind = "EntityPage",
                    EntityId = 3,
                    PageType = "edit",
                    Operations =
                    {
                        new ProjectUiNodeOperationDto
                        {
                            Type = "SetProp",
                            TargetGenId = "gen_form_area",
                            PropName = "class",
                            PropValue = "enhanced-form"
                        }
                    }
                },
                root);

            var vue = await File.ReadAllTextAsync(vuePath);
            Assert.True(result.Success);
            Assert.Contains("enhanced-form", vue, StringComparison.Ordinal);
            Assert.Contains("const scriptMarker = 'keep-me'", vue, StringComparison.Ordinal);
            Assert.DoesNotContain("old nested template", vue, StringComparison.Ordinal);
            Assert.Equal(1, CountOccurrences(vue, "<script setup>"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static List<Component> Apply(
        List<Component> tree,
        ProjectUiDesignDocumentDto document,
        out int appliedCount)
    {
        var json = JsonSerializer.Serialize(tree);
        var result = ProjectUiDesignService.ApplyDesignDocument(json, document, out appliedCount);
        return JsonSerializer.Deserialize<List<Component>>(result, JsonOptions)!;
    }

    private static List<Component> CreateFormTree(string idPrefix)
    {
        return new List<Component>
        {
            new()
            {
                Id = $"{idPrefix}-form",
                Tag = "el-form",
                GenId = "gen_form_area",
                Props = new List<ComponentProp>(),
                Children = new List<Component>
                {
                    new()
                    {
                        Id = $"{idPrefix}-row",
                        Tag = "el-row",
                        Props = new List<ComponentProp>(),
                        Children = new List<Component>
                        {
                            CreateFieldColumn(idPrefix, 101),
                            CreateFieldColumn(idPrefix, 102)
                        }
                    }
                }
            }
        };
    }

    private static Component CreateFieldColumn(string idPrefix, long fieldId)
    {
        return new Component
        {
            Id = $"{idPrefix}-column-{fieldId}",
            Tag = "el-col",
            Props = new List<ComponentProp>
            {
                new() { Key = "xs", Value = "24", IsBind = true },
                new() { Key = "sm", Value = "12", IsBind = true }
            },
            Children = new List<Component>
            {
                new()
                {
                    Id = $"{idPrefix}-field-{fieldId}",
                    Tag = "el-form-item",
                    GenId = $"gen_field_{fieldId}",
                    EntityField = $"Field{fieldId}",
                    Props = new List<ComponentProp>(),
                    Children = new List<Component>
                    {
                        new()
                        {
                            Id = $"{idPrefix}-control-{fieldId}",
                            Tag = "el-input",
                            Props = new List<ComponentProp>()
                        }
                    }
                }
            }
        };
    }

    private static Component FindByGenId(IEnumerable<Component> tree, string genId)
    {
        return Flatten(tree).Single(node => node.GenId == genId);
    }

    private static Component FindById(IEnumerable<Component> tree, string id)
    {
        return Flatten(tree).Single(node => node.Id == id);
    }

    private static IEnumerable<Component> Flatten(IEnumerable<Component> tree)
    {
        foreach (var node in tree)
        {
            yield return node;
            if (node.Children != null)
            {
                foreach (var child in Flatten(node.Children)) yield return child;
            }

            if (node.UseSlots == null) continue;
            foreach (var slot in node.UseSlots)
            {
                if (slot.Components == null) continue;
                foreach (var child in Flatten(slot.Components)) yield return child;
            }
        }
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}
