using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen.Marker;

/// <summary>
/// 将 ScriptSection 渲染后的 JS 中的 [gen.*] / [relation.*] / [field.*] 标记替换为实际值
/// </summary>
public static class ScriptMarkerReplacer
{
    public static string ReplaceGenInScript(string script, GenContext ctx)
    {
        return Regex.Replace(script, @"\[gen\.(\w+)\]", m =>
        {
            var key = m.Groups[1].Value;
            return key switch
            {
                "entityName" => ctx.EntityName,
                "entityNameLower" => ctx.EntityNameLower,
                "entityNameAllLower" => ctx.EntityNameAllLower,
                "entityDescription" or "description" => ctx.EntityDescription,
                "moduleName" => ctx.ModuleName,
                "moduleNameLower" => ctx.ModuleNameLower,
                _ => m.Value
            };
        });
    }

    public static string ReplaceRelationInScript(string script, RelationContext ctx)
    {
        return Regex.Replace(script, @"\[relation\.(\w+)\]", m =>
        {
            var key = m.Groups[1].Value;
            return key switch
            {
                "entityName" => ctx.ChildEntityName,
                "entityNameLower" => ctx.ChildEntityNameLower,
                "entityNameAllLower" => ctx.ChildEntityNameAllLower,
                "description" => ctx.ChildEntityDescription,
                "foreignKey" => ctx.ChildForeignKey,
                _ => m.Value
            };
        });
    }

    public static string ReplaceFieldInScript(string script, FieldContext ctx)
    {
        return Regex.Replace(script, @"\[field\.(\w+)\]", m =>
        {
            var key = m.Groups[1].Value;
            return key switch
            {
                "name" => ctx.Name,
                "nameLower" => ctx.NameLower,
                "description" => ctx.Description,
                "dataType" => ctx.DataType,
                _ => m.Value
            };
        });
    }
}
