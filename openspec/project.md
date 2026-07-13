# CodeMaster Project Specification

## Project Overview

CodeMaster is a .NET 10.0 enterprise-level rapid development platform for backend management systems.

**Tech Stack:**
- **Backend**: ASP.NET Core Web API + SqlSugar ORM
- **Frontend**: Vue 3 + JavaScript + Element Plus + Vite + vue-i18n
- **Database Migration**: EF Core (separate console app)
- **Multi-database Support**: SQL Server, MySQL, PostgreSQL

## Implemented Features

### System Management (系统管理)
- User Management (用户管理) — 完整 CRUD + 导出 + 密码修改
- Role Management (角色管理) — 完整 CRUD + 角色菜单分配
- Department Management (部门管理) — 树形结构 CRUD
- Menu Management (菜单管理) — 树形结构 CRUD + 权限标识
- Multi-tenancy (多租户) — 物理/逻辑隔离 + 全局过滤器
- Position Management (岗位管理) — 完整 CRUD + DataScope 数据权限配置
- Dictionary Management (字典管理) — 字典类型 + 字典数据，统一树形表格页面
- Multi-language Management (多语言管理) — 语言 + 翻译文本，数据库驱动 i18n
- File Management (文件管理) — 上传下载 + 列表查看

### Monitoring Management (监控管理)
- Online User Management (在线用户管理) — SignalR 实时追踪 + 强制下线
- Operation Logs (操作日志) — 全局过滤器自动记录（请求参数/响应/耗时）
- Login Logs (登录日志) — 自动记录登录行为
- Scheduled Tasks (定时任务) — Quartz.NET 集成，支持 HTTP/SQL/Assembly 三种任务类型
- Task Logs (任务日志) — 任务执行历史记录

### Real-time Communication (实时通信)
- SignalR Notification Hub (`/hubs/notification`)
- 项目初始化进度推送（`ProjectInitializationHub`）
- 通知推送服务（`NotificationService`）

### Project Management (项目管理)

#### 1. Project Management (项目管理)
**1.1 Project CRUD**
- Create, Read, Update, Delete projects

**1.2 Project Initialization API**
- Extracts project template zip file
- Unpacks to configured project path
- Replaces "CodeMaster" text with new project name throughout files
- Renames paths containing "CodeMaster" to new project name
- Updates database type, connection string, backend port, frontend port
  - **SQLite Connection String Rule**: If the generated project uses SQLite and the configured connection string contains a relative path (e.g., `Data Source=../CodeMaster.db`), it MUST be converted to an **absolute path** based on the target project's root directory.
  - The connection strings written to both `CodeMaster.Migrator` and `CodeMaster.WebApi` in the target project MUST remain consistent. This ensures that CodeMaster can reliably connect to the target project's database via SqlSugar later explicitly.
- Executes via .NET Process:
  - `dotnet restore` for NuGet packages
  - Runs Migrator EF migrations and initialization
  - Starts WebApi project
  - Runs `npm install`
  - Runs `npm run dev` to start frontend
- Result: A fully initialized new project

#### 2. Module Management (模块管理)
**2.1 Module CRUD**
- Create, Read, Update, Delete modules

**2.2 Update to Menu (Target Project DB)**
- Converts modules to directory menus in the target project.
- Uses `ISqlSugarClient` with a new connection configuration to connect to the **target project's database** (using the connection string assigned during project creation).
- Writes or updates the generated menu structures directly into the target project's database, ensuring CodeMaster's self database is not polluted.

#### 3. Entity Management (实体管理)
**3.1 Entity Structure**
- Entities consist of Entity + Fields (1-to-many relationship)
- Entity CRUD operations fully implemented

**3.2 Code Generation Target**
- Code generation connects tracking the `Project` associated with the `ModuleEntity`.
- Finds the correct physical directory of the target project on disk.
- Generates the `.cs` and `.vue` files directly into the target project's directory structure.
- Uses SqlSugar (connecting to the target project's DB) for executing CodeFirst schema migrations for that entity.

#### 4. Code Generation Engine (代码生成引擎)

**4.1 Scriban 模板引擎**
- 后端模板：`EntityAutoTemplate.scriban`（.auto.cs）、`EntityTemplate.scriban`（.cs 首次生成）、`DtoTemplate.scriban`、`ServiceInterfaceTemplate.scriban`、`ServiceTemplate.scriban`
- 前端模板：`ApiTemplate.scriban`、`IndexTemplate.scriban`、`AddTemplate.scriban`、`EditTemplate.scriban`、`DetailTemplate.scriban`
- Partial 类分离：`.auto.cs` 每次覆盖 / `.cs` 仅首次生成不覆盖
- 接口继承计算：根据 `HasPrimaryKey`/`IsTree`/`HasTenant`/`HasDataPermission` 自动计算继承链

**4.2 VueBuilder（Vue 页面自动生成器）**
- Model 层：VueProp / VueComponent / VueSlot / VuePage / ScriptSection / PageBuildContext
- Renderer：VueRenderer（Template/Script/Style）、AutoJsRenderer（composable 拆分）、IncrementalRenderer（增量 @gen:id 标记合并）
- Builders：PageBuilderBase（表单控件 14 种）、IndexPageBuilder、AddPageBuilder、EditPageBuilder、DetailPageBuilder
- 支持 Split Script 模式：自动生成 useXxx() composable + wrapper .vue

**4.3 一对多关系（OneToManyRelation）**
- 主表可关联多个子表，字段不限主键
- 生成的实体包含 SqlSugar 导航属性 `[Navigate]`
- 主表新增/编辑页包含子表行内编辑
- 详情页展示子表数据列表
- 后端 Create/Update 同时处理子表增删改

**4.4 表单控件类型（14 种）**
`input` / `textarea` / `number` / `select` / `switch` / `checkbox` / `checkbox-group` / `radio-group` / `date` / `datetime` / `upload` / `image` / `editor` / `select-table` / `cascader`

**4.5 跨项目执行规则**
- 生成代码精准写入目标项目物理目录
- 菜单同步写入目标项目数据库（非 CodeMaster 自身 DB）
- SQLite 连接字符串自动转换为绝对路径
- EntityField 页面为单页内联弹框 CRUD（非独立 add/edit 页）

## Architecture

### Backend Architecture

**Layer Structure:**
```
CodeMaster.WebApi          → Controllers, API endpoints, Swagger, Program.cs
CodeMaster.Application     → Business logic services
CodeMaster.Domain          → Entity models
CodeMaster.Infrastructure  → Cross-cutting concerns (SqlSugar, JWT, Middleware, DynamicApi)
CodeMaster.Core            → Core interfaces and base classes
CodeMaster.Migrator        → EF Core database migrations (console app)
CodeMaster.CodeGenerator   → Code generation engine
CodeMaster.SourceGenerator → Roslyn source generators
```

### Key Architecture Principles

#### 1. Dynamic API
- **No Controllers needed** - Services automatically exposed as REST APIs
- **Default Route**: `api/{namespace-lowercase}/{classname-lowercase}/{method-lowercase}`
- Example: `SystemService.GetUsersAsync()` → `GET /api/system/systemservice/getusers`

#### 2. Service Base Classes

**ReadOnlyApplicationService** (只读基类):
- `GetPagedList` - Pagination
- `GetById` - Get detail by ID
- `GetList` - Get all data (for tree structures)

**CrudApplicationService** (增删改查基类):
- Inherits from `ReadOnlyApplicationService`
- `Create` - Create new entity
- `Update(id, updateDto)` - Update entity (2 parameters: id + DTO)
- `Delete` - Delete entity

#### 3. Primary Key Strategy
- **Snowflake ID** (雪花Id) for all primary keys

#### 4. Permission System
- **Auto-generated from Dynamic API**
- **Format**: `{namespace-lowercase}:{classname-lowercase}:{method-flag}`
- **Method Flags**:
  - List/Pagination → `list`
  - Create → `create`
  - Update → `update`
  - Detail → `detail`
- Example: `system:user:list`, `system:user:create`

#### 5. Dual ORM Strategy
- **SqlSugar**: Business logic and data operations
- **EF Core**: Database migrations only (separate console app)
  - Domain entities auto-generate DbContext
  - No manual DbContext editing required
  - Used exclusively for migrations

**SqlSugar Global Naming Conventions (configured in `SqlSugarSetup.cs`):**

Entities **do not need** explicit `[SugarTable]` or `[SugarColumn]` attributes. SqlSugar is globally configured with `ConfigureExternalServices` to auto-convert names, ensuring consistency with EF Core migrations:

| Rule | Convention | Example |
|------|-----------|---------|
| Table Name | PascalCase → pluralize → snake_case | `SysUser` → `sys_users` |
| Column Name | PascalCase → snake_case | `UserName` → `user_name` |

- If an entity has an explicit `[SugarTable("xxx")]` attribute, it overrides the auto-convention
- Pluralization rule: append `s` if not already ending in `s`
- Database type mapping is also auto-configured per `DbType` (SqlServer/MySQL/PostgreSQL)

#### 6. SqlSugar Global Query Filters

SqlSugar configures 3 global query filters via `db.QueryFilter.AddTableFilter<T>()`. These filters are automatically applied to all queries on entities that implement the corresponding interface.

**Filter 1: Tenant Filter (`ITenant`)**
- **Interface**: `ITenant` — requires `long TenantId { get; set; }`
- **Logic**: `currentTenantId == 0 || entity.TenantId == currentTenantId`
- **Behavior**: When `TenantId=0` (Host), sees all tenant data; otherwise only sees data belonging to current tenant
- **Condition**: Only added when `ITenantContext` is injected

**Filter 2: Soft Delete Filter (`ISoftDelete`)**
- **Interface**: `ISoftDelete` — requires `bool IsDeleted` and `DateTime? DeleteTime`
- **Logic**: `entity.IsDeleted == false`
- **Behavior**: Always active, automatically excludes soft-deleted records from all queries
- **Condition**: Always added (unconditional)

**Filter 3: Data Permission Filter (`IDept`)**
- **Interface**: `IDept : IDataPermission` — requires `long? DeptId`, `string? DeptAncestors`, `long? CreateUserId`
- **Behavior**: Filters data visibility based on the user's `PostDataScope` setting (configured per Position/岗位)
- **Condition**: Only added when `IDataPermissionContext` is enabled and user is not Admin

**PostDataScope Enum:**

| Value | Name | Filter Logic |
|-------|------|-------------|
| 1 | `All` (全部数据) | No filter added — sees everything |
| 2 | `Dept` (本部门) | `entity.DeptAncestors == currentUserDeptAncestors` |
| 3 | `DeptAndBelow` (本部门及以下) | `entity.DeptAncestors.StartsWith(currentUserDeptAncestors)` |
| 4 | `Self` (仅本人) | `entity.CreateUserId == currentUserId` |

**Important Notes:**
- Admin users bypass data permission filters entirely
- Filters can be temporarily disabled via `db.QueryFilter.ClearAndBackup<T>()` and restored via `db.QueryFilter.Restore()` (used in AuthService for login)
- Entity base class `DataPermissionEntityBase` inherits `EntityBaseWithTenant` and implements `IDept`, automatically enabling both tenant and data permission filters

### Task Scheduling (Quartz.NET)

- **Job Types**: HTTP (`HttpJob`), SQL (`SqlJob`), Assembly (`AssemblyJob`)
- **JobFactory**: DI-integrated `JobFactory` for constructor injection
- **TaskSchedulerServer**: Singleton service managing all scheduled tasks
- **Auto-load**: On application start, loads all enabled tasks from `SysTask` table
- **CRUD Management**: Full API + frontend for managing tasks and viewing logs

### SignalR Integration

- **NotificationHub** (`/hubs/notification`): Real-time notification center
- **NotificationService**: Push notifications to specific users or all clients
- **Online User Tracking**: `OnlineUserService` tracks connected users, supports force logout
- **Project Initialization Progress**: `ProjectInitializationHub` streams project setup progress to frontend

## Frontend Architecture

### Key Principles

#### 1. Page Navigation (Not Dialogs)
- **Add/Edit/Detail pages are separate routes**, not modal dialogs
- Navigate to independent pages for CRUD operations

#### 2. Detail Page is Mandatory
- Every entity must have a detail/view page
- Cannot skip detail page implementation

#### 3. Tree Structure Lists
- **No pagination** for tree structures
- Backend returns flat data via `GetList`
- Frontend transforms flat data into tree structure for display

### Frontend Tech Stack
- Vue 3 + JavaScript (not TypeScript)
- Element Plus UI components
- Vite build tool
- vue-i18n for internationalization (dynamic loading from backend API)
- Pinia for state management (with persistence plugin)
- Vue Router for routing (dynamic route generation from backend menus)
- Axios for HTTP requests (auto token injection)
- SignalR client for real-time communication

### Frontend Features
- **Dynamic Routing**: Routes generated from backend menu API, components loaded via `import.meta.glob`
- **Permission Directive**: `v-permission` directive for button-level access control
- **i18n**: Database-driven translations loaded from `GET /api/lang/i18n-map/{langCode}`
- **Theme Switching**: Light / Dark / Business Gray / Tech Blue
- **Language Switcher**: Dynamic locale switching with Element Plus locale
- **Tab View**: Multi-tab page navigation with keep-alive caching
- **Breadcrumb**: Auto-generated from route hierarchy
- **Layout**: Sidebar + Navbar + Main content + Tab view

## Development Conventions

### Backend Conventions
1. Services implement `IApplicationService` marker interface
2. Auto-registration via `AddApplicationServices()` in Program.cs
3. Update methods always have 2 parameters: `id` and `updateDto`
4. Use Snowflake ID for all primary keys
5. SqlSugar for business logic, EF Core for migrations only

### Frontend Conventions
1. Separate pages for Add/Edit/Detail (no dialogs)
2. Detail page is mandatory
3. Tree lists use `GetList` + client-side tree transformation
4. No pagination for tree structures

## Database Support (5 databases)
- **SQL Server** (default) - `DbProvider: "SqlServer"`
- **MySQL** - `DbProvider: "MySql"`
- **PostgreSQL** - `DbProvider: "PostgreSQL"`
- **SQLite** - `DbProvider: "Sqlite"`
- **Oracle** - `DbProvider: "Oracle"`

Configuration in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "DbProvider": "SqlServer"
}
```

Build Migrator with specific database:
```bash
# SQL Server (default)
dotnet build

# MySQL
dotnet build -p:DbProvider=MySql

# PostgreSQL
dotnet build -p:DbProvider=PostgreSQL

# SQLite
dotnet build -p:DbProvider=Sqlite

# Oracle
dotnet build -p:DbProvider=Oracle
```
