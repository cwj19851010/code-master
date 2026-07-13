using System.Reflection;
using System.Text.Json;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 组件管理模块种子数据（属于代码管理目录下）
/// </summary>
public class ComponentModule : ISeedModule
{
    public string ModuleName => "组件管理";

    private static readonly HashSet<string> CommonProperties = new()
    {
        "disabled", "size", "model-value", "type", "name", "placeholder",
        "clearable", "label", "value", "title", "icon", "width", "height",
        "maxlength", "minlength", "readonly", "required", "loading", "autofocus",
        "round", "circle", "plain", "text", "border", "stripe",
    };

    private static readonly HashSet<string> CommonEvents = new()
    {
        "change", "blur", "focus", "click", "input", "clear",
        "close", "open", "select", "visible-change",
    };

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<SysMenu>()
            .AnyAsync(m => m.Path == "component" && m.Component == "codegen/component/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找代码管理目录（不是系统管理）
        var codegenMenu = await dbContext.Set<SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "codegen" && m.MenuType == "M");

        if (codegenMenu == null)
        {
            Console.WriteLine("  - 警告：未找到代码管理目录，跳过组件管理菜单");
            return;
        }

        var menuIds = new List<long>();

        var listMenu = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            ParentId = codegenMenu.Id,
            MenuName = "组件管理",
            TitleKey = "component",
            Path = "component",
            Component = "codegen/component/index",
            MenuType = "C",
            Visible = true,
            Status = 0,
            IsCache = true,
            Icon = "ElementPlus",
            OrderNum = 50,
            Perms = "codegen:component:list",
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(listMenu);
        menuIds.Add(listMenu.Id);

        var subPages = new[]
        {
            new { Path = "add", Name = "新增组件", Perms = "codegen:component:create" },
            new { Path = "edit", Name = "编辑组件", Perms = "codegen:component:update" },
            new { Path = "detail", Name = "组件详情", Perms = "codegen:component:view" },
        };

        foreach (var page in subPages)
        {
            var menu = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                ParentId = listMenu.Id,
                MenuName = page.Name,
                Path = page.Path,
                Component = $"codegen/component/{page.Path}",
                MenuType = "C",
                Visible = false,
                Status = 0,
                IsCache = page.Path != "edit" && page.Path != "detail",
                Perms = page.Perms,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);
        Console.WriteLine($"  - 添加组件管理菜单（{menuIds.Count}个，父级：代码管理）");
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        var hasData = await dbContext.Set<SysComponentGroup>().AnyAsync();
        if (hasData)
        {
            Console.WriteLine("  - 组件数据已存在，跳过种子导入");
            return;
        }

        // 1. 创建分组
        var groups = new (string Name, string Code, string Icon, int Sort)[]
        {
            ("Basic 基础组件", "Basic", "Grid", 1),
            ("Form 表单组件", "Form", "Document", 2),
            ("Data 数据展示", "Data", "DataAnalysis", 3),
            ("Navigation 导航", "Navigation", "Guide", 4),
            ("Feedback 反馈组件", "Feedback", "ChatLineSquare", 5),
            ("Others 其他", "Others", "More", 6),
            ("配置组件", "Configuration", "Setting", 7),
        };

        var groupEntities = new Dictionary<string, long>();
        foreach (var (name, code, icon, sort) in groups)
        {
            var group = new SysComponentGroup
            {
                Id = YitIdHelper.NextId(),
                GroupName = name,
                GroupCode = code,
                Icon = icon,
                Sort = sort,
                Status = 0,
                CreateTime = DateTime.UtcNow,
            };
            await dbContext.Set<SysComponentGroup>().AddAsync(group);
            groupEntities[code] = group.Id;
        }
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"  - 创建了 {groups.Length} 个组件分组");

        // 2. 从 JSON 文件导入
        var jsonDir = FindComponentDataDirectory();
        if (jsonDir == null || !Directory.Exists(jsonDir))
        {
            Console.WriteLine("  ⚠ 未找到组件 JSON 数据目录");
            return;
        }

        var jsonFiles = Directory.GetFiles(jsonDir, "*.json");
        Console.WriteLine($"  - 从 {jsonFiles.Length} 个 JSON 文件导入...");

        int totalProps = 0, totalSlots = 0, totalEvents = 0, totalExposes = 0;

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var jsonText = await File.ReadAllTextAsync(jsonFile);
                var import = JsonSerializer.Deserialize<JsonComponent>(jsonText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (import == null || string.IsNullOrEmpty(import.Name))
                    continue;

                var groupCode = MapGroupNameToCode(import.GroupName);
                if (!groupEntities.TryGetValue(groupCode, out var groupId))
                    continue;

                var component = new SysComponent
                {
                    Id = YitIdHelper.NextId(),
                    Name = import.Name,
                    Tag = import.Tag ?? import.Name,
                    Link = import.Link ?? $"https://element-plus.org/zh-CN/component/{import.Name}",
                    GroupId = groupId,
                    Sort = 0,
                    Status = 0,
                    CreateTime = DateTime.UtcNow,
                };
                await dbContext.Set<SysComponent>().AddAsync(component);

                foreach (var p in import.ComponentsProperties)
                {
                    var typeDesc = CleanQuotes(p.TypeDescription);
                    var defaultVal = CleanQuotes(p.DefaultValue);
                    await dbContext.Set<SysComponentProperty>().AddAsync(new SysComponentProperty
                    {
                        Id = YitIdHelper.NextId(),
                        ComponentId = component.Id,
                        PropName = p.Name,
                        PropType = p.Type,
                        TypeDescription = typeDesc,
                        DefaultValue = defaultVal,
                        Description = p.Description,
                        IsCommon = CommonProperties.Contains(p.Name),
                        Sort = 0,
                        CreateTime = DateTime.UtcNow,
                    });
                    totalProps++;
                }

                foreach (var s in import.ComponentSlots)
                {
                    await dbContext.Set<SysComponentSlot>().AddAsync(new SysComponentSlot
                    {
                        Id = YitIdHelper.NextId(),
                        ComponentId = component.Id,
                        SlotName = s.Name,
                        Description = s.Description,
                        SlotType = s.Type,
                        TypeDescription = CleanQuotes(s.TypeDescription),
                        Sort = 0,
                        CreateTime = DateTime.UtcNow,
                    });
                    totalSlots++;
                }

                foreach (var e in import.ComponentEvents)
                {
                    await dbContext.Set<SysComponentEvent>().AddAsync(new SysComponentEvent
                    {
                        Id = YitIdHelper.NextId(),
                        ComponentId = component.Id,
                        EventName = e.Name,
                        Description = e.Description,
                        EventType = e.Type,
                        TypeDescription = CleanQuotes(e.TypeDescription),
                        IsCommon = CommonEvents.Contains(e.Name),
                        Sort = 0,
                        CreateTime = DateTime.UtcNow,
                    });
                    totalEvents++;
                }

                foreach (var x in import.ComponentExposes)
                {
                    await dbContext.Set<SysComponentExpose>().AddAsync(new SysComponentExpose
                    {
                        Id = YitIdHelper.NextId(),
                        ComponentId = component.Id,
                        ExposeName = x.Name,
                        Description = x.Description,
                        ExposeType = x.Type,
                        TypeDescription = CleanQuotes(x.TypeDescription),
                        Sort = 0,
                        CreateTime = DateTime.UtcNow,
                    });
                    totalExposes++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ 导入 {Path.GetFileName(jsonFile)} 失败: {ex.Message}");
            }
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"  ✓ 导入完成: {jsonFiles.Length} 组件, {totalProps} 属性, {totalSlots} 插槽, {totalEvents} 事件, {totalExposes} 暴露方法");

        // 3. 种子 Vue 指令数据
        await SeedDirectivesAsync(dbContext);
    }

    private static async Task SeedDirectivesAsync(CodeMasterDbContext dbContext)
    {
        var hasDirectives = await dbContext.Set<SysDirective>().AnyAsync();
        if (hasDirectives)
        {
            Console.WriteLine("  - 指令数据已存在，跳过");
            return;
        }

        var directives = new (string Name, bool HasValue, string? ValueType, string Description, bool IsCommon)[]
        {
            ("v-if",     true,  "expression", "条件渲染（真时才渲染元素）", true),
            ("v-else-if",true,  "expression", "条件渲染 else-if 分支", false),
            ("v-else",   false, null,          "条件渲染 else 分支（无值）", false),
            ("v-show",   true,  "expression", "条件显示（display 切换）", true),
            ("v-for",    true,  "expression", "列表渲染（item in items）", true),
            ("v-model",  true,  "variable",    "双向绑定", true),
            ("v-model:argument", true, "variable", "带参数的双向绑定", false),
            ("v-bind",   true,  "expression", "动态绑定属性（:prop 简写）", true),
            ("v-on",     true,  "expression", "事件监听（@event 简写）", true),
            ("v-text",   true,  "expression", "更新 textContent", false),
            ("v-html",   true,  "expression", "更新 innerHTML", false),
            ("v-once",   false, null,          "一次性插值（不再响应式）", false),
            ("v-pre",    false, null,          "跳过编译（原样输出）", false),
            ("v-cloak",  false, null,          "编译完成前隐藏（配合 CSS）", false),
            ("v-slot",   true,  "slotName",    "插槽分发（#name 简写）", true),
            ("v-memo",   true,  "expression", "记忆子树（性能优化）", false),
            ("v-permission", true, "expression", "权限指令（自定义）", true),
            ("v-loading",   true, "expression", "加载状态（自定义）", true),
            ("v-hasPermi",  true, "expression", "权限校验（自定义）", true),
            ("v-hasRole",   true, "expression", "角色校验（自定义）", false),
        };

        foreach (var (name, hasValue, valueType, desc, isCommon) in directives)
        {
            await dbContext.Set<SysDirective>().AddAsync(new SysDirective
            {
                Id = YitIdHelper.NextId(),
                DirectiveName = name,
                HasValue = hasValue,
                ValueType = valueType,
                Description = desc,
                IsCommon = isCommon,
                Sort = 0,
                CreateTime = DateTime.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"  ✓ 种子 {directives.Length} 个 Vue 指令");
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "component", new() { { "zh-CN", "组件管理" }, { "en-US", "Component Management" } } },
            { "componentGroup", new() { { "zh-CN", "组件分组" }, { "en-US", "Component Group" } } },
            { "componentName", new() { { "zh-CN", "组件名称" }, { "en-US", "Component Name" } } },
            { "componentTag", new() { { "zh-CN", "HTML标签" }, { "en-US", "HTML Tag" } } },
            { "componentProps", new() { { "zh-CN", "组件属性" }, { "en-US", "Properties" } } },
            { "componentSlots", new() { { "zh-CN", "组件插槽" }, { "en-US", "Slots" } } },
            { "componentEvents", new() { { "zh-CN", "组件事件" }, { "en-US", "Events" } } },
            { "componentExposes", new() { { "zh-CN", "暴露方法" }, { "en-US", "Exposes" } } },
            { "propName", new() { { "zh-CN", "属性名" }, { "en-US", "Property Name" } } },
            { "propType", new() { { "zh-CN", "属性类型" }, { "en-US", "Property Type" } } },
            { "propDefault", new() { { "zh-CN", "默认值" }, { "en-US", "Default Value" } } },
            { "propDescription", new() { { "zh-CN", "属性描述" }, { "en-US", "Description" } } },
            { "isCommon", new() { { "zh-CN", "常用" }, { "en-US", "Common" } } },
            { "eventName", new() { { "zh-CN", "事件名" }, { "en-US", "Event Name" } } },
            { "slotName", new() { { "zh-CN", "插槽名" }, { "en-US", "Slot Name" } } },
            { "exposeName", new() { { "zh-CN", "方法名" }, { "en-US", "Method Name" } } },
            { "addComponent", new() { { "zh-CN", "新增组件" }, { "en-US", "Add Component" } } },
            { "editComponent", new() { { "zh-CN", "编辑组件" }, { "en-US", "Edit Component" } } },
            { "setCommon", new() { { "zh-CN", "设为常用" }, { "en-US", "Set as Common" } } },
            { "cancelCommon", new() { { "zh-CN", "取消常用" }, { "en-US", "Unset Common" } } },
        };
    }

    private static string? CleanQuotes(string? s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("'", "");
    }

    private static string? FindComponentDataDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedData", "System", "ComponentData"),
            Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "System", "ComponentData"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "..", "..", "MyHomeWorks", "ElementUIBuilder", "ElementUIBuilder",
                "bin", "Debug", "net8.0-windows", "components", "Cleaned"),
        };

        foreach (var path in candidates)
        {
            var full = Path.GetFullPath(path);
            if (Directory.Exists(full))
            {
                Console.WriteLine($"  - 找到组件数据目录: {full}");
                return full;
            }
        }
        return null;
    }

    private static string MapGroupNameToCode(string? groupName)
    {
        return groupName switch
        {
            "Basic 基础组件" => "Basic",
            "Form 表单组件" => "Form",
            "Data 数据展示" => "Data",
            "Navigation 导航" => "Navigation",
            "Feedback 反馈组件" => "Feedback",
            "Others 其他" => "Others",
            "配置组件" => "Configuration",
            _ => "Others",
        };
    }

    private class JsonComponent
    {
        public string Name { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public string? Link { get; set; }
        public string? GroupName { get; set; }
        public List<JsonItem> ComponentsProperties { get; set; } = new();
        public List<JsonItem> ComponentSlots { get; set; } = new();
        public List<JsonItem> ComponentEvents { get; set; } = new();
        public List<JsonItem> ComponentExposes { get; set; } = new();
    }

    private class JsonItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? TypeDescription { get; set; }
        public string? DefaultValue { get; set; }
        public string? Description { get; set; }
    }
}
