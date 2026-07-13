using System.Text;

namespace CodeMaster.Application.ScriptBuilder;

/// <summary>
/// 将 ScriptSection 渲染为 JavaScript 代码
/// </summary>
public class ScriptRenderer
{
    /// <summary>
    /// 渲染完整的 composable 函数：export function useXxx() { ... return { ... }; }
    /// </summary>
    public string RenderComposable(ScriptSection section, string entityName, string? formParam = null)
    {
        var sb = new StringBuilder();
        var entityLower = ToCamelCase(entityName);

        // imports
        foreach (var imp in section.Imports)
            sb.AppendLine(RenderImport(imp));

        if (section.Imports.Count > 0) sb.AppendLine();

        // export function
        var funcParams = formParam != null ? formParam : "";
        sb.AppendLine($"export function use{entityName}({funcParams}) {{");
        sb.AppendLine();

        var exportedNames = new List<string>();

        // consts
        foreach (var c in section.Consts)
        {
            sb.AppendLine($"  const {c.Name} = {c.Value};");
            exportedNames.Add(c.Name);
        }
        if (section.Consts.Count > 0) sb.AppendLine();

        // lets
        foreach (var l in section.Lets)
        {
            sb.AppendLine($"  let {l.Name} = {l.Value};");
            exportedNames.Add(l.Name);
        }
        if (section.Lets.Count > 0) sb.AppendLine();

        // refs
        foreach (var r in section.Refs)
        {
            // 检测 composable 调用（如 useRouter(), useI18n()）——不应包裹 ref()
            var isComposable = r.Value != null && r.Value.StartsWith("use") && r.Value.Contains('(') && r.Value.Contains(')');
            if (isComposable)
                sb.AppendLine($"  const {r.Name} = {r.Value};");
            else
                sb.AppendLine($"  const {r.Name} = ref({r.Value});");
            exportedNames.Add(r.Name);
        }
        if (section.Refs.Count > 0) sb.AppendLine();

        // reactives
        foreach (var r in section.Reactives)
        {
            var fields = string.Join(", ", r.Fields.Select(kv => $"{kv.Key}: {kv.Value}"));
            sb.AppendLine($"  const {r.Name} = reactive({{ {fields} }});");
            exportedNames.Add(r.Name);
        }
        if (section.Reactives.Count > 0) sb.AppendLine();

        // dictRefs
        foreach (var d in section.DictRefs)
        {
            sb.AppendLine($"  const {d.VarName} = ref([]);");
            exportedNames.Add(d.VarName);
        }
        if (section.DictRefs.Count > 0) sb.AppendLine();

        // functions
        foreach (var fn in section.Functions)
        {
            var prefix = fn.IsAsync ? "async " : "";
            var pars = fn.Params ?? "";
            sb.AppendLine($"  const {fn.Name} = {prefix}({pars}) => {{");
            if (!string.IsNullOrWhiteSpace(fn.Body))
            {
                foreach (var line in fn.Body.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(trimmed))
                        sb.AppendLine();
                    else
                        sb.AppendLine($"    {trimmed}");
                }
            }
            sb.AppendLine($"  }};");
            sb.AppendLine();
            exportedNames.Add(fn.Name);
        }

        // computed
        foreach (var c in section.Computed)
        {
            sb.AppendLine($"  const {c.Name} = computed(() => {{");
            if (!string.IsNullOrWhiteSpace(c.Body))
            {
                foreach (var line in c.Body.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(trimmed))
                        sb.AppendLine();
                    else
                        sb.AppendLine($"    {trimmed}");
                }
            }
            sb.AppendLine($"  }});");
            sb.AppendLine();
            exportedNames.Add(c.Name);
        }

        // watches
        foreach (var w in section.Watches)
        {
            var opts = new List<string>();
            if (w.Deep) opts.Add("deep: true");
            if (w.Immediate) opts.Add("immediate: true");
            var optStr = opts.Count > 0 ? $", {{ {string.Join(", ", opts)} }}" : "";
            sb.AppendLine($"  watch(() => {w.Source}, (newVal, oldVal) => {{");
            if (!string.IsNullOrWhiteSpace(w.Body))
            {
                foreach (var line in w.Body.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(trimmed))
                        sb.AppendLine();
                    else
                        sb.AppendLine($"    {trimmed}");
                }
            }
            sb.AppendLine($"  }}{optStr});");
            sb.AppendLine();
        }

        // hooks
        foreach (var h in section.Hooks)
        {
            sb.AppendLine($"  {h.Name}(() => {{");
            if (!string.IsNullOrWhiteSpace(h.Body))
            {
                foreach (var line in h.Body.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(trimmed))
                        sb.AppendLine();
                    else
                        sb.AppendLine($"    {trimmed}");
                }
            }
            sb.AppendLine($"  }});");
            sb.AppendLine();
        }

        // return
        sb.AppendLine($"  return {{ {string.Join(", ", exportedNames)} }};");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    /// <summary>
    /// 渲染单个 import 行
    /// </summary>
    public static string RenderImport(ImportInfo imp)
    {
        return imp.Mode switch
        {
            "named" => string.IsNullOrEmpty(imp.Names)
                ? $"import {{ }} from '{imp.From}';"
                : $"import {{ {imp.Names} }} from '{imp.From}';",
            "default" => $"import {imp.Names} from '{imp.From}';",
            "namespace" => $"import * as {imp.Names} from '{imp.From}';",
            "sideEffect" => $"import '{imp.From}';",
            _ => $"// unknown import mode: {imp.Mode}"
        };
    }

    /// <summary>
    /// 渲染 .vue 文件需要的壳：import composable + destructure + onMounted init
    /// </summary>
    public string RenderVueShell(string entityName, string pageType, List<string> exportNames)
    {
        var entityLower = ToCamelCase(entityName);
        var names = string.Join(", ", exportNames);

        return $@"import {{ onMounted }} from 'vue'
import {{ use{entityName} }} from './{entityLower}.{pageType}.auto.js'
const {{ {names} }} = use{entityName}()
onMounted(() => {{ init() }})";
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
