# CodeMaster 双模式架构实现总结

## 概述

成功实现了 CodeMaster 的双模式架构，支持浏览器模式和客户端模式两种运行方式。

## 架构设计

### 1. 浏览器模式（服务端初始化）

**流程**：
```
用户点击"初始化"
  ↓
前端调用 POST /api/project/project/initialize
  ↓
后端在服务器上执行：
  - 读取模板 ZIP 文件
  - 解压到服务器目录
  - 重命名文件和目录
  - 替换项目名称
  - 运行 EF 数据库迁移
  - 运行 npm install
  ↓
返回成功/失败结果
```

**适用场景**：
- 服务器端部署
- 多用户共享环境
- 集中管理项目

### 2. 客户端模式（本地初始化）

**流程**：
```
用户点击"初始化"
  ↓
前端检测到运行在 WebView2 中
  ↓
调用 GET /api/project/project/getclientinitializedata/{id}
  ↓
后端返回：
  - 项目信息
  - 模板 ZIP 的 Base64 编码（约 400KB）
  ↓
前端通过 JSBridge 调用客户端方法
  ↓
WPF 客户端执行：
  - Base64 解码为字节数组
  - 解压到本地目录
  - 重命名文件和目录
  - 替换项目名称
  - 运行 EF 数据库迁移
  - 运行 npm install
  ↓
返回成功/失败结果
```

**适用场景**：
- 桌面应用
- 离线开发
- 本地项目管理

## 技术实现

### 后端 API

#### 1. 服务端初始化 API
```csharp
// POST /api/project/project/initialize
public async Task<bool> InitializeAsync(InitializeProjectDto input)
{
    // 1. 获取项目信息
    var project = await Repository.GetByIdAsync(input.Id);

    // 2. 从文件系统读取模板
    var templateZipPath = GetLatestTemplate();

    // 3. 初始化项目
    await _initializationService.InitializeProjectAsync(
        project.ProjectName,
        targetPath,
        project.DatabaseType.ToString(),
        project.ConnectionString,
        templateZipPath);

    // 4. 运行数据库迁移
    await _initializationService.RunDatabaseMigrationAsync(targetPath, project.ProjectName);

    // 5. 运行 npm install
    await _initializationService.RunNpmInstallAsync(targetPath, project.ProjectName);

    // 6. 更新项目状态
    project.Status = ProjectStatus.Initialized;
    await Repository.UpdateAsync(project);

    return true;
}
```

#### 2. 客户端初始化数据 API
```csharp
// GET /api/project/project/getclientinitializedata/{id}
public async Task<ClientInitializeProjectDto> GetClientInitializeDataAsync(long id)
{
    var project = await Repository.GetByIdAsync(id);
    var templateBase64 = await GetTemplateBase64Async();

    return new ClientInitializeProjectDto
    {
        Id = project.Id,
        TemplateBase64 = templateBase64,  // 约 400KB
        ProjectName = project.ProjectName,
        ProjectPath = project.ProjectPath,
        DatabaseType = project.DatabaseType.ToString(),
        ConnectionString = project.ConnectionString
    };
}
```

### 前端实现

#### 1. 环境检测
```javascript
// src/utils/jsbridge.js
export function isInClient() {
  return typeof window.chrome !== 'undefined' &&
         typeof window.chrome.webview !== 'undefined' &&
         typeof window.chrome.webview.hostObjects !== 'undefined' &&
         typeof window.chrome.webview.hostObjects.jsbridge !== 'undefined'
}
```

#### 2. 自动模式选择
```javascript
// src/views/system/project/index.vue
const handleInitialize = async (row) => {
  const inClient = isInClient()

  if (inClient) {
    // 客户端模式
    const data = await getClientInitializeData(row.id)
    await clientInitializeProject(data)
  } else {
    // 服务端模式
    await initializeProject(row.id)
  }
}
```

### WPF 客户端

#### 1. JSBridge 实现
```csharp
[System.Runtime.InteropServices.ComVisible(true)]
public class JsBridge
{
    // 初始化项目
    public string InitializeProject(string jsonData)
    {
        var data = JsonConvert.DeserializeObject<InitializeProjectData>(jsonData);

        // 1. Base64 解码
        var zipBytes = Convert.FromBase64String(data.TemplateBase64);

        // 2. 解压模板
        using (var memoryStream = new MemoryStream(zipBytes))
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
        {
            archive.ExtractToDirectory(data.ProjectPath);
        }

        // 3. 重命名和替换
        RenameFilesAndDirectories(data.ProjectPath, data.ProjectName);
        ReplaceProjectName(data.ProjectPath, data.ProjectName);

        // 4. 数据库迁移
        RunDatabaseMigration(data.ProjectPath, data.ProjectName);

        // 5. npm install
        RunNpmInstall(data.ProjectPath, data.ProjectName);

        return JsonConvert.SerializeObject(new { success = true });
    }

    // 选择文件夹
    public string SelectFolder()
    {
        var dialog = new OpenFolderDialog();
        return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
    }

    // 显示消息
    public void ShowMessage(string message, string title)
    {
        MessageBox.Show(message, title);
    }
}
```

## 核心代码复用

### ProjectInitializationService

通过抽取核心逻辑，实现服务端和客户端代码复用：

```csharp
// 服务端初始化（从 ZIP 文件）
public async Task<bool> InitializeProjectAsync(
    string projectName,
    string projectPath,
    string databaseType,
    string connectionString,
    string templateZipPath)
{
    await ExtractTemplateAsync(templateZipPath, projectPath);
    await InitializeProjectCoreAsync(projectPath, projectName, databaseType, connectionString);
    return true;
}

// 客户端初始化（从 Base64）
public async Task<bool> InitializeProjectFromBase64Async(
    string projectName,
    string projectPath,
    string databaseType,
    string connectionString,
    string templateBase64)
{
    var zipBytes = Convert.FromBase64String(templateBase64);
    await ExtractTemplateFromBytesAsync(zipBytes, projectPath);
    await InitializeProjectCoreAsync(projectPath, projectName, databaseType, connectionString);
    return true;
}

// 核心逻辑（共用）
private async Task InitializeProjectCoreAsync(
    string projectPath,
    string projectName,
    string databaseType,
    string connectionString)
{
    await RenameFilesAndDirectoriesAsync(projectPath, projectName);
    await ReplaceProjectNameAsync(projectPath, projectName);
    await GenerateSolutionFileAsync(projectPath, projectName);
    await UpdateDatabaseConfigAsync(projectPath, projectName, databaseType, connectionString);
}
```

## 文件清单

### 后端文件
```
CodeMaster.Application/
├── Services/Project/
│   ├── ProjectService.cs                    # 添加客户端初始化 API
│   ├── IProjectService.cs                   # 添加接口定义
│   └── ProjectInitializationService.cs      # 核心初始化逻辑
└── Dtos/Project/
    └── ProjectDto.cs                         # 添加 ClientInitializeProjectDto

CodeMaster.Infrastructure/
└── DynamicApi/
    └── DynamicApiControllerConvention.cs     # 自动参数绑定
```

### 前端文件
```
CodeMaster.Vue/
├── src/
│   ├── utils/
│   │   └── jsbridge.js                      # JSBridge 工具类（新建）
│   ├── api/system/
│   │   └── project.js                       # 添加 getClientInitializeData
│   └── views/system/project/
│       └── index.vue                        # 修改初始化逻辑
```

### 客户端文件
```
CodeMaster.Client/                           # 新建项目
├── MainWindow.xaml                          # 主窗口 UI
├── MainWindow.xaml.cs                       # JSBridge 实现
├── CodeMaster.Client.csproj                 # 项目配置
├── README.md                                # 使用文档
└── test-client.ps1                          # 测试脚本
```

## 测试验证

### 服务端模式测试
```bash
# 1. 启动后端服务
cd CodeMaster.WebApi
dotnet run --urls http://localhost:5170

# 2. 登录获取 Token
curl -X POST http://localhost:5170/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# 3. 创建项目
curl -X POST http://localhost:5170/api/project/project/create \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"projectName":"TestProject","displayName":"测试项目",...}'

# 4. 初始化项目
curl -X POST http://localhost:5170/api/project/project/initialize \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"id":123456,"targetPath":"D:/TestProjects/TestProject"}'
```

### 客户端模式测试
```powershell
# 1. 启动后端服务
cd CodeMaster.WebApi
dotnet run --urls http://localhost:5170

# 2. 启动 WPF 客户端
cd CodeMaster.Client
dotnet run

# 3. 在客户端中：
#    - 访问 http://localhost:5170
#    - 登录系统
#    - 创建项目
#    - 点击"初始化"按钮
#    - 自动检测为客户端模式
#    - 选择本地目录
#    - 执行初始化
```

## 性能对比

| 指标 | 服务端模式 | 客户端模式 |
|------|-----------|-----------|
| 模板传输 | 无需传输 | ~400KB Base64 |
| 初始化位置 | 服务器 | 客户端本地 |
| 网络依赖 | 高 | 低（仅获取配置） |
| 并发能力 | 受服务器限制 | 无限制 |
| 适用场景 | 多用户共享 | 单用户桌面 |

## 优势总结

### 1. 代码复用
- 核心初始化逻辑完全复用
- 只有数据源不同（ZIP 文件 vs Base64）

### 2. 灵活部署
- 同一套前端代码
- 自动检测运行环境
- 无需修改即可切换模式

### 3. 用户体验
- 浏览器模式：无需安装客户端
- 客户端模式：更快的响应速度，离线支持

### 4. 可扩展性
- JSBridge 架构易于扩展新功能
- 前后端分离，独立演进

## 后续优化建议

1. **进度反馈**：添加初始化进度条
2. **断点续传**：支持大文件分块传输
3. **缓存机制**：客户端缓存模板，避免重复下载
4. **错误恢复**：初始化失败时自动清理和重试
5. **日志记录**：详细记录初始化过程，便于排查问题

## 总结

成功实现了 CodeMaster 的双模式架构，通过智能环境检测和 JSBridge 通信，实现了浏览器和客户端的无缝切换。核心代码高度复用，架构清晰，易于维护和扩展。
