using System.Text.Json.Serialization;

namespace CodeMaster.Application.ScriptBuilder;

/// <summary>
/// 统一的 ScriptSection —— 表达一段 Vue &lt;script setup&gt; 代码的结构化描述。
/// 每个控件/模板都可以携带自己的 ScriptSection，生成时 Merge 去重后统一输出。
/// </summary>
public class ScriptSection
{
    [JsonPropertyName("imports")]
    public List<ImportInfo> Imports { get; set; } = new();

    [JsonPropertyName("consts")]
    public List<ConstInfo> Consts { get; set; } = new();

    [JsonPropertyName("lets")]
    public List<LetInfo> Lets { get; set; } = new();

    [JsonPropertyName("refs")]
    public List<RefInfo> Refs { get; set; } = new();

    [JsonPropertyName("reactives")]
    public List<ReactiveInfo> Reactives { get; set; } = new();

    [JsonPropertyName("functions")]
    public List<FunctionInfo> Functions { get; set; } = new();

    [JsonPropertyName("hooks")]
    public List<HookInfo> Hooks { get; set; } = new();

    [JsonPropertyName("computed")]
    public List<ComputedInfo> Computed { get; set; } = new();

    [JsonPropertyName("watches")]
    public List<WatchInfo> Watches { get; set; } = new();

    [JsonPropertyName("dictRefs")]
    public List<DictRefInfo> DictRefs { get; set; } = new();

    // ====== Merge ======

    /// <summary>将 other 合并到当前，按规则去重</summary>
    public void Merge(ScriptSection other)
    {
        MergeImports(other.Imports);
        MergeByKey(Consts, other.Consts, c => c.Name);
        MergeByKey(Lets, other.Lets, l => l.Name);
        MergeByKey(Refs, other.Refs, r => r.Name);
        MergeReactives(other.Reactives);
        MergeByKey(Functions, other.Functions, f => f.Name);
        MergeHooks(other.Hooks);
        MergeByKey(Computed, other.Computed, c => c.Name);
        MergeByKey(Watches, other.Watches, w => w.Source);
        MergeByKey(DictRefs, other.DictRefs, d => d.VarName);
    }

    /// <summary>检查是否为空（没有任何脚本内容）</summary>
    public bool IsEmpty =>
        Imports.Count == 0 && Consts.Count == 0 && Lets.Count == 0 &&
        Refs.Count == 0 && Reactives.Count == 0 && Functions.Count == 0 &&
        Hooks.Count == 0 && Computed.Count == 0 && Watches.Count == 0 &&
        DictRefs.Count == 0;

    // ====== 去重合并细节 ======

    private void MergeImports(List<ImportInfo> others)
    {
        foreach (var o in others)
        {
            var existing = Imports.FirstOrDefault(i =>
                i.From == o.From && i.Mode == o.Mode);

            if (existing == null)
            {
                Imports.Add(o);
                continue;
            }

            // named 模式：合并 Names 里的名字
            if (o.Mode == "named")
            {
                var existingNames = SplitNames(existing.Names);
                var newNames = SplitNames(o.Names);
                foreach (var n in newNames)
                {
                    if (!existingNames.Contains(n))
                        existingNames.Add(n);
                }
                existing.Names = string.Join(", ", existingNames);
            }
            // default / namespace / sideEffect：已存在就跳过
        }
    }

    private void MergeReactives(List<ReactiveInfo> others)
    {
        foreach (var o in others)
        {
            var existing = Reactives.FirstOrDefault(r => r.Name == o.Name);
            if (existing == null)
            {
                Reactives.Add(o);
                continue;
            }
            // 合并 Fields
            foreach (var kv in o.Fields)
            {
                if (!existing.Fields.ContainsKey(kv.Key))
                    existing.Fields[kv.Key] = kv.Value;
            }
        }
    }

    private void MergeHooks(List<HookInfo> others)
    {
        // Vue 3 允许多个同名 hook；只去掉完全相同的 hook，避免同一控件脚本被多处合并时重复请求。
        foreach (var o in others)
        {
            if (!Hooks.Any(h => h.Name == o.Name && h.Body == o.Body))
                Hooks.Add(o);
        }
    }

    private static void MergeByKey<T>(List<T> target, List<T> source, Func<T, string> keySelector)
    {
        var existing = new HashSet<string>(target.Select(keySelector));
        foreach (var item in source)
        {
            if (existing.Add(keySelector(item)))
                target.Add(item);
        }
    }

    private static List<string> SplitNames(string names) =>
        names.Split(',', StringSplitOptions.RemoveEmptyEntries)
             .Select(n => n.Trim())
             .Where(n => n.Length > 0)
             .ToList();

    // ====== 从旧 Marker 格式转换 ======

    /// <summary>从旧 Marker.ScriptSection (JSON 格式) 转换到新的 ScriptSection</summary>
    public static ScriptSection FromMarker(CodeMaster.Application.Services.CodeGen.Marker.ScriptSection marker)
    {
        var s = new ScriptSection();

        foreach (var imp in marker.Imports)
        {
            if (imp.Destructured != null)
                s.Imports.Add(new ImportInfo { From = imp.Path, Mode = "named", Names = imp.Destructured });
            else if (imp.Default != null)
                s.Imports.Add(new ImportInfo { From = imp.Path, Mode = "default", Names = imp.Default });
            else
                s.Imports.Add(new ImportInfo { From = imp.Path, Mode = "sideEffect" });
        }

        foreach (var rf in marker.Refs)
            s.Refs.Add(new RefInfo { Name = rf.Name, Value = rf.InitialValue ?? "null" });

        foreach (var rc in marker.Reactives)
            s.Reactives.Add(new ReactiveInfo { Name = rc.Name, Fields = new Dictionary<string, string>(rc.Fields) });

        foreach (var fn in marker.Functions)
            s.Functions.Add(new FunctionInfo
            {
                Name = fn.Name,
                Params = fn.Parameters,
                IsAsync = fn.IsAsync,
                Body = string.Join("\n", fn.Body)
            });

        foreach (var hk in marker.Hooks)
            s.Hooks.Add(new HookInfo { Name = hk.Name, Body = string.Join("\n", hk.Body) });

        foreach (var cb in marker.Computed)
            s.Computed.Add(new ComputedInfo { Name = cb.Name, Body = string.Join("\n", cb.Body) });

        foreach (var wb in marker.Watches)
            s.Watches.Add(new WatchInfo { Source = wb.Source, Body = string.Join("\n", wb.Body), Deep = wb.Deep, Immediate = wb.Immediate });

        return s;
    }

    // ====== 标记替换 ======

    /// <summary>对所有名称和 body 字符串应用标记替换</summary>
    public void ReplaceMarkers(Func<string, string> replacer)
    {
        foreach (var fn in Functions) { fn.Name = replacer(fn.Name); if (fn.Params != null) fn.Params = replacer(fn.Params); fn.Body = replacer(fn.Body); }
        foreach (var hk in Hooks) { hk.Name = replacer(hk.Name); hk.Body = replacer(hk.Body); }
        foreach (var cb in Computed) { cb.Name = replacer(cb.Name); cb.Body = replacer(cb.Body); }
        foreach (var wb in Watches) { wb.Source = replacer(wb.Source); wb.Body = replacer(wb.Body); }
        foreach (var c in Consts) { c.Name = replacer(c.Name); c.Value = replacer(c.Value); }
        foreach (var l in Lets) { l.Name = replacer(l.Name); l.Value = replacer(l.Value); }
        foreach (var r in Refs) { r.Name = replacer(r.Name); r.Value = replacer(r.Value); }
        foreach (var r in Reactives)
        {
            r.Name = replacer(r.Name);
            var newFields = new Dictionary<string, string>();
            foreach (var kv in r.Fields)
                newFields[replacer(kv.Key)] = replacer(kv.Value);
            r.Fields = newFields;
        }
        foreach (var imp in Imports)
        {
            imp.From = replacer(imp.From);
            imp.Names = replacer(imp.Names);
        }
        foreach (var d in DictRefs)
        {
            d.VarName = replacer(d.VarName);
            d.Source = replacer(d.Source);
        }
    }

    // ====== 导出名收集 ======

    /// <summary>收集所有需要从 composable 导出的名称</summary>
    public List<string> GetExportNames()
    {
        var names = new List<string>();
        names.AddRange(Consts.Select(c => c.Name));
        names.AddRange(Lets.Select(l => l.Name));
        names.AddRange(Refs.Select(r => r.Name));
        names.AddRange(Reactives.Select(r => r.Name));
        names.AddRange(Functions.Select(f => f.Name));
        names.AddRange(Computed.Select(c => c.Name));
        names.AddRange(DictRefs.Select(d => d.VarName));
        return names;
    }
}

// ====== 具体类型 ======

/// <summary>
/// Import —— 四种导入模式
/// </summary>
public class ImportInfo
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>named | default | namespace | sideEffect</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "named";

    /// <summary>
    /// named:    "ref, reactive"
    /// default:  "orderApi"
    /// namespace:"orderApi"
    /// sideEffect: ""
    /// </summary>
    [JsonPropertyName("names")]
    public string Names { get; set; } = string.Empty;
}

/// <summary>顶层常量 —— const STATUS_MAP = { ... }</summary>
public class ConstInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>顶层可变变量 —— let editingIndex = -1</summary>
public class LetInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>ref —— const loading = ref(false)</summary>
public class RefInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = "null";
}

/// <summary>reactive —— const form = reactive({ name: '', age: 0 })</summary>
public class ReactiveInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = new();
}

/// <summary>函数 —— const getList = async (id) => { ... }</summary>
public class FunctionInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public string? Params { get; set; }

    [JsonPropertyName("async")]
    public bool IsAsync { get; set; }

    /// <summary>完整函数体，一段文本。支持 [gen.xxx] 标记</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

/// <summary>生命周期 hook —— onMounted(() => { ... })</summary>
public class HookInfo
{
    /// <summary>onMounted / onUnmounted / onBeforeMount 等</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Hook 内的完整代码，一段文本</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

/// <summary>computed —— const doubled = computed(() => ...)</summary>
public class ComputedInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>computed 箭头函数体</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

/// <summary>watch —— watch(source, (newVal, oldVal) => { ... }, { deep, immediate })</summary>
public class WatchInfo
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("deep")]
    public bool Deep { get; set; }

    [JsonPropertyName("immediate")]
    public bool Immediate { get; set; }
}

/// <summary>
/// 字典/关联表选项引用 —— const statusOptions = ref([])
/// 便利封装：等价于一个 Ref + Import(getDataListByType) + Hook body 拼接
/// </summary>
public class DictRefInfo
{
    /// <summary>变量名，如 statusOptions。去重 key</summary>
    [JsonPropertyName("varName")]
    public string VarName { get; set; } = string.Empty;

    /// <summary>
    /// 加载方式：
    /// "dict:status"        → getDataListByType('status')
    /// "table:Product"      → getProductList()
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}
