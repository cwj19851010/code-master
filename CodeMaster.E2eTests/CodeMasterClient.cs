using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeMaster.E2eTests;

/// <summary>
/// CodeMaster API 客户端助手
/// </summary>
public class CodeMasterClient : IDisposable
{
    private readonly HttpClient _http;
    private static string? _authToken;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CodeMasterClient()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(E2eConfig.BaseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };

        if (!string.IsNullOrEmpty(_authToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        }
    }

    /// <summary>
    /// 登录获取 Token（admin/admin123）
    /// </summary>
    public async Task LoginAsync()
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "admin123",
            tenantId = 0
        });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var token = json?["accessToken"]?.GetValue<string>()
                 ?? json?["token"]?.GetValue<string>()
                 ?? json?["data"]?["accessToken"]?.GetValue<string>()
                 ?? json?["data"]?["token"]?.GetValue<string>();

        if (!string.IsNullOrEmpty(token))
        {
            _authToken = token;
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            throw new Exception($"登录失败，无法获取 Token。响应：{content}");
        }
    }

    /// <summary>
    /// POST /api/codegen/project/create
    /// </summary>
    public async Task<long> CreateProjectAsync()
    {
        var resp = await _http.PostAsJsonAsync("/api/codegen/project/create", new
        {
            projectName = E2eConfig.TestProjectName,
            displayName = "E2E端到端测试项目",
            projectPath = E2eConfig.TestProjectParentPath,
            databaseType = 4, // SQLite
            connectionString = E2eConfig.ConnectionString,
            backendPort = E2eConfig.BackendPort,
            frontendPort = E2eConfig.FrontendPort
        });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var id = ReadLong(json)
               ?? ReadLong(json?["data"])
               ?? ReadLong(json?["id"])
               ?? 0;

        if (id == 0)
            throw new Exception($"创建项目失败，响应：{content}");

        return id;
    }

    /// <summary>
    /// 调用分步初始化步骤 Step1~Step9
    /// </summary>
    public async Task InitializeProjectStepsAsync(long projectId)
    {
        var stepApis = new[]
        {
            ("/api/codegen/project/initialize/step1", "Step1_解压模板"),
            ("/api/codegen/project/initialize/step2", "Step2_生成解决方案"),
            ("/api/codegen/project/initialize/step3", "Step3_更新数据库配置"),
            ("/api/codegen/project/initialize/step4", "Step4_更新端口配置"),
            ("/api/codegen/project/initialize/step7", "Step7_还原依赖"),
            ("/api/codegen/project/initialize/step8", "Step8_写入翻译"),
            ("/api/codegen/project/initialize/step9", "Step9_安装前端依赖"),
        };

        foreach (var (url, stepName) in stepApis)
        {
            var resp = await _http.PostAsJsonAsync(url, new { projectId });
            var content = await resp.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(content);

            var success = json?["success"]?.GetValue<bool>()
                       ?? json?["data"]?["success"]?.GetValue<bool>()
                       ?? (json?["code"]?.GetValue<int>() == 200);

            if (!success)
            {
                var error = json?["error"]?.GetValue<string>()
                         ?? json?["message"]?.GetValue<string>()
                         ?? content;
                throw new Exception($"{stepName} 失败：{error}");
            }

            Console.WriteLine($"✅ {stepName} 完成");
        }
    }

    /// <summary>
    /// 创建项目模块
    /// </summary>
    public async Task<long> CreateModuleAsync(long projectId)
    {
        var resp = await _http.PostAsJsonAsync("/api/codegen/projectmodule/create", new
        {
            projectId,
            moduleName = "System",
            moduleDescription = "系统模块",
            orderNum = 1
        });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var id = ReadLong(json)
               ?? ReadLong(json?["data"])
               ?? 0;

        if (id == 0)
            throw new Exception($"创建模块失败，响应：{content}");

        return id;
    }

    /// <summary>
    /// 同步模块到菜单
    /// </summary>
    public async Task SyncModuleToMenuAsync(long moduleId)
    {
        var resp = await _http.PostAsJsonAsync($"/api/codegen/projectmodule/syncmoduletomenu/{moduleId}", new { });

        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"同步菜单响应：{content}");
    }

    /// <summary>
    /// 创建 ModuleEntity（实体）
    /// </summary>
    public async Task<long> CreateEntityAsync(long moduleId, string name, string description,
        bool hasPrimaryKey = true, bool hasToMany = false, bool hasTenant = false,
        bool generateFrontend = true)
    {
        var resp = await _http.PostAsJsonAsync("/api/codegen/moduleentity/create", new
        {
            moduleId,
            name,
            description,
            hasPrimaryKey,
            hasToMany,
            hasTenant,
            generateFrontend,
            isReadOnly = false,
            isTree = false,
            hasDataPermission = false,
            frontendRoute = $"/system/{name.ToLower()}",
            menuIcon = "Document"
        });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var id = ReadLong(json)
               ?? ReadLong(json?["data"])
               ?? 0;

        if (id == 0)
            throw new Exception($"创建实体 {name} 失败，响应：{content}");

        return id;
    }

    /// <summary>
    /// 为实体添加字段
    /// </summary>
    public async Task AddFieldAsync(long entityId, string name, string dataType,
        string description = "", string formControlType = "input",
        bool isPrimaryKey = false, bool isSystemField = false,
        bool showInList = true, bool showInAddForm = true,
        bool showInEditForm = true, bool showInDetail = true)
    {
        // 先获取实体再更新（含新增字段）
        var resp = await _http.PostAsJsonAsync("/api/codegen/entityfield/create", new
        {
            moduleEntityId = entityId,
            name,
            description = string.IsNullOrEmpty(description) ? name : description,
            dataType,
            formControlType,
            isPrimaryKey,
            isSystemField,
            isNullable = !isPrimaryKey,
            isRequired = isPrimaryKey,
            showInList,
            showInAddForm,
            showInEditForm,
            showInDetail,
            showInSearch = false,
            orderNum = 1
        });

        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"  添加字段 {name}：{content.Substring(0, Math.Min(100, content.Length))}");
    }

    /// <summary>
    /// 添加1对多关系
    /// </summary>
    public async Task AddOneToManyRelationAsync(long mainEntityId, string mainField,
        long subEntityId, string subForeignKey)
    {
        var mainEntity = await GetEntityDataAsync(mainEntityId);
        var childEntity = await GetEntityDataAsync(subEntityId);

        var resp = await _http.PutAsJsonAsync($"/api/codegen/moduleentity/update/{mainEntityId}", new
        {
            name = mainEntity["name"]?.GetValue<string>() ?? string.Empty,
            description = mainEntity["description"]?.GetValue<string>() ?? string.Empty,
            hasPrimaryKey = mainEntity["hasPrimaryKey"]?.GetValue<bool>() ?? true,
            tableName = mainEntity["tableName"]?.GetValue<string>(),
            isTree = mainEntity["isTree"]?.GetValue<bool>() ?? false,
            isReadOnly = mainEntity["isReadOnly"]?.GetValue<bool>() ?? false,
            hasTenant = mainEntity["hasTenant"]?.GetValue<bool>() ?? false,
            hasDataPermission = mainEntity["hasDataPermission"]?.GetValue<bool>() ?? false,
            generateFrontend = mainEntity["generateFrontend"]?.GetValue<bool>() ?? true,
            isChildTable = mainEntity["isChildTable"]?.GetValue<bool>() ?? false,
            frontendRoute = mainEntity["frontendRoute"]?.GetValue<string>(),
            menuIcon = mainEntity["menuIcon"]?.GetValue<string>(),
            orderNum = mainEntity["orderNum"]?.GetValue<int>() ?? 0,
            remark = mainEntity["remark"]?.GetValue<string>(),
            newRelations = new[]
            {
                new
                {
                    masterField = mainField,
                    childEntityId = subEntityId,
                    childEntityName = childEntity["name"]?.GetValue<string>() ?? string.Empty,
                    childForeignKey = subForeignKey,
                    orderNum = 1
                }
            },
            updatedRelations = Array.Empty<object>(),
            deletedRelationIds = Array.Empty<long>(),
            newFields = Array.Empty<object>(),
            updatedFields = Array.Empty<object>(),
            deletedFieldIds = Array.Empty<long>()
        });

        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"添加1对多关系响应：{content.Substring(0, Math.Min(100, content.Length))}");
    }

    /// <summary>
    /// 生成代码
    /// </summary>
    private async Task<JsonNode> GetEntityDataAsync(long entityId)
    {
        var resp = await _http.GetAsync($"/api/codegen/moduleentity/getbyid/{entityId}");
        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        return json?["data"] ?? json
            ?? throw new Exception($"Failed to get entity {entityId}: {content}");
    }

    public async Task<bool> GenerateCodeAsync(long entityId)
    {
        var resp = await _http.PostAsJsonAsync($"/api/codegen/moduleentity/generatecode/{entityId}", new { });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        return json?.GetValue<bool>()
             ?? json?["data"]?.GetValue<bool>()
             ?? resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// 执行数据库迁移
    /// </summary>
    public async Task<(bool success, string message, string? output)> MigrateDatabaseAsync(long projectId)
    {
        var resp = await _http.PostAsJsonAsync("/api/codegen/project/migratedatabase", new
        {
            projectId
        });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var success = json?["success"]?.GetValue<bool>()
                   ?? json?["data"]?["success"]?.GetValue<bool>()
                   ?? ((json?["code"]?.GetValue<int>() == 200) || resp.IsSuccessStatusCode);
        var message = json?["message"]?.GetValue<string>()
                   ?? json?["data"]?["message"]?.GetValue<string>()
                   ?? content;
        var output = json?["output"]?.GetValue<string>()
                  ?? json?["data"]?["output"]?.GetValue<string>();
        return (success, message, output);
    }

    /// <summary>
    /// 编译项目
    /// </summary>
    public async Task<(bool success, string message, string? output)> BuildProjectAsync(long projectId)
    {
        var resp = await _http.PostAsJsonAsync("/api/codegen/project/build", new
        {
            projectId
        });

        var content = await resp.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var success = json?["success"]?.GetValue<bool>()
                   ?? json?["data"]?["success"]?.GetValue<bool>()
                   ?? ((json?["code"]?.GetValue<int>() == 200) || resp.IsSuccessStatusCode);
        var message = json?["message"]?.GetValue<string>()
                   ?? json?["data"]?["message"]?.GetValue<string>()
                   ?? content;
        var output = json?["output"]?.GetValue<string>()
                  ?? json?["data"]?["output"]?.GetValue<string>();
        return (success, message, output);
    }

    private static long? ReadLong(JsonNode? node)
    {
        if (node == null)
            return null;

        try
        {
            return node.GetValue<long>();
        }
        catch
        {
            try
            {
                var text = node.GetValue<string>();
                return long.TryParse(text, out var value) ? value : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public void Dispose() => _http.Dispose();
}
