using CodeMaster.Application.Services.CodeGen.Marker;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class MarkerReplacerTests
{
    [Fact]
    public void ReplaceField_Expands_DisplayField_VFor_Nodes()
    {
        var template =
            "<el-table-column [v-for=\"displayField in field.displayFields\"] " +
            ":label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_col_[field.id]_[displayField.nameLower]\">" +
            "<template #default=\"scope\">{{ getSelectLabel(scope.row.[field.nameLower], customerOptions, '[displayField.name]') }}</template>" +
            "</el-table-column>";

        var ctx = new FieldContext
        {
            Id = "100",
            NameLower = "customerId",
            DisplayFields =
            {
                new DisplayFieldContext { Name = "Name", NameLower = "name", LabelKey = "name" },
                new DisplayFieldContext { Name = "Phone", NameLower = "phone", LabelKey = "phone" }
            }
        };

        var html = MarkerReplacer.ReplaceField(template, ctx);

        Assert.DoesNotContain("field.displayFields", html);
        Assert.Contains("$t('name')", html);
        Assert.Contains("$t('phone')", html);
        Assert.Contains("gen_col_100_name", html);
        Assert.Contains("gen_col_100_phone", html);
        Assert.Contains("getSelectLabel(scope.row.customerId, customerOptions, 'Name')", html);
        Assert.Contains("getSelectLabel(scope.row.customerId, customerOptions, 'Phone')", html);
    }

    [Fact]
    public void ReplaceField_Expands_DisplayField_VFor_DescriptionItems()
    {
        var template =
            "<el-descriptions-item [v-for=\"displayField in field.displayFields\"] " +
            ":label=\"$t('[displayField.labelKey]')\" data-gen-id=\"gen_field_[field.id]_[displayField.nameLower]\">" +
            "{{ getSelectLabel(detail.[field.nameLower], customerOptions, '[displayField.name]') }}" +
            "</el-descriptions-item>";

        var ctx = new FieldContext
        {
            Id = "100",
            NameLower = "customerId",
            DisplayFields =
            {
                new DisplayFieldContext { Name = "Name", NameLower = "name", LabelKey = "name" },
                new DisplayFieldContext { Name = "Type", NameLower = "type", LabelKey = "type" }
            }
        };

        var html = MarkerReplacer.ReplaceField(template, ctx);

        Assert.DoesNotContain("field.displayFields", html);
        Assert.Contains("$t('name')", html);
        Assert.Contains("$t('type')", html);
        Assert.Contains("gen_field_100_name", html);
        Assert.Contains("gen_field_100_type", html);
        Assert.Contains("getSelectLabel(detail.customerId, customerOptions, 'Name')", html);
        Assert.Contains("getSelectLabel(detail.customerId, customerOptions, 'Type')", html);
    }

    [Fact]
    public void ReplaceField_Does_Not_Expand_Plain_Vue_VFor()
    {
        var template = "<span v-for=\"displayField in field.displayFields\">{{ [displayField.name] }}</span>";
        var ctx = new FieldContext
        {
            DisplayFields =
            {
                new DisplayFieldContext { Name = "Name", NameLower = "name", LabelKey = "name" }
            }
        };

        var html = MarkerReplacer.ReplaceField(template, ctx);

        Assert.Contains("v-for=\"displayField in field.displayFields\"", html);
        Assert.Contains("[displayField.name]", html);
    }
}
