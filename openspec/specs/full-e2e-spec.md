# 全流程端到端需求规范（E2E Full Spec）

**版本**：1.0  
**日期**：2026-03-26  
**优先级**：P0（阻塞性，需在下次迭代前全部完成）

---

## 一、模板修复：补充 IBaseEntity（已执行）

### 背景

SourceGenerator 已升级为按 `IBaseEntity` 扫描实体（兼容有主键/无主键两种场景），但旧的项目模板 zip 包中 `{ProjectName}.Core/Entities/` 目录缺少 `IBaseEntity.cs`，导致从模板生成的新项目编译失败。

### 修复项

1. 在 `Templates/CodeMaster.Core/Entities/` 下新增 `IBaseEntity.cs`
2. 修改同目录 `IEntity.cs`，使 `IEntity<TKey>` 继承 `IBaseEntity`
3. 重新打包 zip 模板（由后端 `/api/project/export-template` 接口触发）

### 验收标准

- 新导出的 zip 包中 `{ProjectName}.Core/Entities/IBaseEntity.cs` 文件存在
- 新建项目 `dotnet build` 零错误

---

## 二、项目列表页操作按钮升级

### 2.1 启动按钮 → 下拉按钮（前端 / 后端）

当前项目列表有单个「启动」按钮，需改为 **`el-dropdown`** 下拉：

```
启动 ▼
  ├── 启动前端
  └── 启动后端
```

- 点击「启动前端」：调用 `POST /api/project/start-frontend/{id}`
- 点击「启动后端」：调用 `POST /api/project/start-backend/{id}`
- 启动成功后在该行显示绿色运行状态角标

### 2.2 新增「停止」下拉按钮

```
停止 ▼
  ├── 停止前端
  └── 停止后端
```

- 点击「停止前端」：调用 `POST /api/project/stop-frontend/{id}`
- 点击「停止后端」：调用 `POST /api/project/stop-backend/{id}`
- 停止成功后清除该行的绿色运行状态角标

### 2.3 新增「迁移数据库」按钮

独立按钮（非下拉），点击后：

1. 前端展示确认弹框，显示要迁移的项目名称
2. 确认后调用 `POST /api/project/migrate/{id}`
3. 后端自动：
   - 拼接迁移名称（格式：`Migration_{yyyyMMddHHmmss}`）
   - 在目标项目的 `{ProjectName}.Migrator` 目录下执行：
     ```
     dotnet ef migrations add Migration_{timestamp} --project {ProjectName}.Migrator
     dotnet ef database update --project {ProjectName}.Migrator
     ```
   - 以 SSE 或 WebSocket 实时流式返回执行日志到前端
4. 前端以日志弹窗展示迁移过程

### 2.4 后端接口清单

| 接口 | 方法 | 描述 |
|------|------|------|
| `/api/project/start-frontend/{id}` | POST | 启动目标项目前端（npm run dev） |
| `/api/project/start-backend/{id}` | POST | 启动目标项目后端（dotnet run） |
| `/api/project/stop-frontend/{id}` | POST | 停止目标项目前端进程 |
| `/api/project/stop-backend/{id}` | POST | 停止目标项目后端进程 |
| `/api/project/status/{id}` | GET | 查询目标项目前后端运行状态 |
| `/api/project/migrate/{id}` | POST | 执行 EF Core 迁移 |

**进程管理规则：**
- 后端维护一个进程字典 `Dictionary<long, Process>`（按项目ID索引），前后端分开存储（`_frontendProcesses`、`_backendProcesses`）
- 进程退出时自动从字典移除，状态更新为已停止
- 防止重复启动：启动前检查进程是否仍在运行

---

## 三、全流程自动化测试

### 3.1 测试覆盖范围

```
测试 #1: 模板导出完整性验证
测试 #2: 项目初始化流程（API 调用）
测试 #3: ModuleEntity 创建 + 菜单同步
测试 #4: 实体字段添加（含1对多）
测试 #5: 代码生成验证（文件存在性）
测试 #6: 前后端文件结构验证
测试 #7: 前后端启动验证
测试 #8: 数据 CRUD 验证（通过生成的API新增一条记录并验证入库）
```

### 3.2 测试 #1：模板导出完整性

**断言：**
- 导出 zip 中包含 `{Name}.Core/Entities/IBaseEntity.cs`
- 导出 zip 中 `IEntity.cs` 的 `IEntity<TKey>` 继承自 `IBaseEntity`
- 导出 zip 中包含 `.SourceGenerator/{Name}.SourceGenerator.csproj`

### 3.3 测试 #2：项目初始化

**步骤：**
1. 调用 `POST /api/project` 创建项目记录（DatabaseType=SQLite）
2. 调用 `POST /api/project/init/{id}` 解压模板、重命名命名空间、写入连接字符串
3. 调用 `POST /api/project/migrate/{id}` 建库建表（初始迁移）

**断言：**
- 项目物理目录已创建
- `{Name}.sln` 存在
- `appsettings.json` 中连接字符串已更新
- `dotnet build` 零错误
- SQLite 数据库文件已生成

### 3.4 测试 #3：ModuleEntity 创建 + 菜单同步

**步骤：**
1. 调用 `POST /api/moduleentity` 创建主实体（如 `Article`，含 `HasPrimaryKey=true`、`HasTenant=true`）
2. 调用 `POST /api/project/sync-menu/{entityId}` 同步菜单

**断言：**
- 目标项目数据库 `sys_menu` 表中存在对应目录菜单 + 页面菜单 + 操作按钮（至少5条）
- 权限标识格式正确（如 `system:article:list`）

### 3.5 测试 #4：实体字段（单表 + 1对多）

**主表（Article）字段：**
| 字段 | 类型 | 控件 |
|------|------|------|
| Id | long | （系统字段） |
| Title | string | input |
| Content | string | editor |
| ArticleType | int | select（dict） |
| PublishDate | DateTime | datetime |

**子表（ArticleComment）字段：**
| 字段 | 类型 | 控件 |
|------|------|------|
| Id | long | （系统字段） |
| ArticleId | long | （外键，系统字段） |
| CommentContent | string | textarea |
| CommentBy | string | input |

**1对多关系：**
- 主表字段：`Id` → 子表外键：`ArticleId`

**断言：**
- `EntityField` 表中 Article 有5条记录
- `EntityField` 表中 ArticleComment 有4条记录
- `OneToManyRelation` 表中有1条关联记录

### 3.6 测试 #5，#6：代码生成 + 文件结构验证

**调用：** `POST /api/moduleentity/generate-code/{articleId}`

**断言（后端文件）：**
- `{ProjectPath}/{Name}.Domain/Entities/System/Article.cs` 存在
- `{ProjectPath}/{Name}.Domain/Entities/System/Article.auto.cs` 存在
- `{ProjectPath}/{Name}.Application/Dtos/System/ArticleDto.cs` 存在
- `{ProjectPath}/{Name}.Application/Services/System/IArticleService.cs` 存在
- `{ProjectPath}/{Name}.Application/Services/System/ArticleService.cs` 存在
- 同样验证 ArticleComment 的相关文件

**断言（前端文件）：**
- `{ProjectPath}/{Name}.Vue/src/api/system/article.js` 存在
- `{ProjectPath}/{Name}.Vue/src/views/system/article/index.vue` 存在
- `{ProjectPath}/{Name}.Vue/src/views/system/article/add.vue` 存在
- `{ProjectPath}/{Name}.Vue/src/views/system/article/edit.vue` 存在
- `{ProjectPath}/{Name}.Vue/src/views/system/article/detail.vue` 存在

**断言（生成代码质量）：**
- `.auto.cs` 包含 `public partial class Article`
- `.auto.cs` 包含 `IEntity` 或 `IBaseEntity` 继承声明
- `index.vue` 包含 `getPagedList` 调用

### 3.7 测试 #7：前后端启动验证

**步骤：**
1. 调用 `POST /api/project/migrate/{id}`（再次迁移，包含新增表）
2. 调用 `POST /api/project/start-backend/{id}`
3. 等待5秒
4. 调用 `POST /api/project/start-frontend/{id}`
5. 等待5秒

**断言：**
- `GET /api/project/status/{id}` 返回 `{ "backendRunning": true, "frontendRunning": true }`
- 后端 HTTP 健康检查 `GET http://localhost:{port}/health` 返回 200
- 前端 HTTP `GET http://localhost:{vitePort}/` 返回 200

### 3.8 测试 #8：CRUD 数据验证

**前提：** 测试 #7 中后端已启动

**步骤：**
1. `POST http://localhost:{port}/api/system/article` 新增一篇文章（含ArticleComment子记录）
2. `GET http://localhost:{port}/api/system/article/{id}` 查询详情

**断言：**
- 新增接口返回 `200 OK`，响应包含 `id` 字段
- 详情查询返回主表 + 子表数据

---

## 四、执行计划（有序）

| 步骤 | 工作 | 负责 |
|------|------|------|
| Step 1 | 修复模板：新增 IBaseEntity.cs，更新 IEntity.cs | AI 立即执行 |
| Step 2 | 调用后端接口重新打包导出模板 zip | AI 通过浏览器执行 |
| Step 3 | 通过 CodeMaster UI 创建新项目并初始化 | AI 通过浏览器执行 |
| Step 4 | 通过 UI 添加 ModuleEntity（Article + ArticleComment）及字段 | AI 通过浏览器执行 |
| Step 5 | 添加1对多关系，生成代码 | AI 通过浏览器执行 |
| Step 6 | 开发项目列表页按钮升级（启动/停止下拉 + 迁移数据库按钮） | AI 写代码 |
| Step 7 | 开发后端进程管理接口（start/stop/status/migrate） | AI 写代码 |
| Step 8 | 编写并运行完整自动化测试套件 | AI 写代码 + 运行 |

---

## 五、技术约束

1. 进程管理需使用 `System.Diagnostics.Process`，注册到 DI 容器中的单例 `ProcessManagerService`
2. EF 迁移必须在目标项目的 `{Name}.Migrator` 目录下执行（`WorkingDirectory` 设置正确）
3. 前端启动检测通过 HTTP 轮询 `http://localhost:{port}/` 实现（最多重试10次，间隔1s）
4. 后端启动检测通过检查进程是否存活 + HTTP 健康检查接口实现
5. 所有生成代码必须能 `dotnet build` 零错误

---

**状态：** 🔄 执行中（Step 1 已完成，Step 2 开始）
