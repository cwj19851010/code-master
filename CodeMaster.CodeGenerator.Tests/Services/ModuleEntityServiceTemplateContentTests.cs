using System.Reflection;
using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class ModuleEntityServiceTemplateContentTests
{
    [Fact]
    public void ReplaceTemplateContent_PreservesScriptAfterNestedSlotTemplate()
    {
        var original = """
<template>
  <div>
    <el-table>
      <el-table-column>
        <template #default="scope">
          {{ scope.row.name }}
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>
<script setup>
const ok = true
</script>
""";

        var replacement = """
<div>
  <el-card />
</div>
""";

        var result = InvokePrivateStatic<string>("ReplaceTemplateContent", original, replacement);

        Assert.Contains("<el-card />", result);
        Assert.Contains("<script setup>", result);
        Assert.Contains("const ok = true", result);
        Assert.DoesNotContain("scope.row.name", result);
        Assert.Equal(1, Count(result, "<template>"));
        Assert.Equal(1, Count(result, "</template>"));
    }

    [Fact]
    public void ExtractTemplateContent_ReturnsOuterTemplateWhenNestedSlotTemplateExists()
    {
        var content = """
<template>
  <div>
    <el-table-column>
      <template #default="scope">
        {{ scope.row.name }}
      </template>
    </el-table-column>
  </div>
</template>
<script setup>
const ok = true
</script>
""";

        var result = InvokePrivateStatic<string>("ExtractTemplateContent", content);

        Assert.Contains("scope.row.name", result);
        Assert.Contains("</el-table-column>", result);
        Assert.DoesNotContain("<script setup>", result);
    }

    [Fact]
    public void ReplaceTemplateContent_RemovesStaleTemplateTailFromPreviouslyCorruptVue()
    {
        var corrupt = """
<template>
  <div>new-ish content</div>
</template></el-table-column><el-table-column>
  <template #default="scope">
    {{ stale }}
  </template>
</el-table-column>
</template>
<script setup>
const ok = true
</script>
""";

        var result = InvokePrivateStatic<string>(
            "ReplaceTemplateContent",
            corrupt,
            "<div>fixed</div>");

        Assert.Contains("<div>fixed</div>", result);
        Assert.Contains("<script setup>", result);
        Assert.DoesNotContain("</template></el-table-column>", result);
        Assert.DoesNotContain("{{ stale }}", result);
        Assert.Equal(1, Count(result, "<template>"));
        Assert.Equal(1, Count(result, "</template>"));
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args)
    {
        var method = typeof(ModuleEntityService).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return (T)method!.Invoke(null, args)!;
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
