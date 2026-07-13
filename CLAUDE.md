# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CodeMaster** is a .NET 10.0 enterprise-level rapid development platform, refactored from SmartCoder (ZRAdmin.NET).
- **Backend**: ASP.NET Core Web API with SqlSugar ORM (primary; EF Core used only in Migrator)
- **Frontend**: Vue 3 + Element Plus + Vite + vue-i18n (JavaScript, NOT TypeScript)
- **Database Migration**: EF Core with multi-DB support (SqlServer, MySQL, PostgreSQL, Sqlite)

## Build & Run Commands

```bash
# Build entire solution
dotnet build CodeMaster.sln

# Run Web API (http://localhost:5000, https://localhost:5001)
cd CodeMaster.WebApi
dotnet run

# Run database migration (creates tables + seeds data)
cd CodeMaster.Migrator
dotnet run

# Vue frontend (http://localhost:5173, proxies /api → localhost:5000)
cd CodeMaster.Vue
npm install
npm run dev           # Development server
npm run build         # Production build

# Run tests
dotnet test CodeMaster.CodeGenerator.Tests/CodeMaster.CodeGenerator.Tests.csproj
dotnet test CodeMaster.OpenSpec.Tests/CodeMaster.OpenSpec.Tests.csproj
dotnet test CodeMaster.E2eTests/CodeMaster.E2eTests.csproj
```

Default credentials: `admin` / `admin123`

## Architecture

### Backend Layer Dependency (top-down only; no reverse dependencies)

```
CodeMaster.WebApi          → Controllers, Swagger, Program.cs, middleware pipeline
CodeMaster.Application     → Business logic services, DTOs, ScriptBuilder, VueBuilder, CodeGen engine
CodeMaster.Domain          → Entity models (System/, CodeGen/, Monitor/)
CodeMaster.Infrastructure  → SqlSugar, JWT, SignalR, Redis caching, Quartz, DynamicApi, VueParser
CodeMaster.Core            → Interfaces, base classes, IRepository<>, ITenantContext, IDataFilter
CodeMaster.Migrator        → EF Core migrations + seed data (separate from main runtime)
CodeMaster.CodeGenerator   → Legacy standalone code-gen CLI (Scriban-based; superseded by Application-layer engine)
CodeMaster.SourceGenerator → Roslyn source generators (compile-time)
```

### Supporting Projects

| Project | Purpose |
|---------|---------|
| `CodeMaster.Client` | Shared API client library |
| `CodeMaster.McpServer` | MCP (Model Context Protocol) server for AI integration |
| `CodeMaster.E2eTests` | End-to-end tests with programmatic API client |
| `CodeMaster.CodeGenerator.Tests` | Unit/integration tests for the code generation engine |
| `CodeMaster.OpenSpec.Tests` | OpenSpec workflow tests |

### Key Patterns

- **Service Registration**: Services implement `IApplicationService` marker interface → auto-registered by `AddApplicationServices()` in Program.cs
- **ORM**: SqlSugar with snake_case table/column naming and pluralization. EF Core is used ONLY in the Migrator project for schema migrations, not in the main runtime
- **Authentication**: JWT-based; config section `JwtSettings` (falls back to `Jwt`)
- **Authorization**: `[Permission("system:user:list")]` attribute + `PermissionPolicyProvider` + `PermissionAuthorizationHandler`
- **Dynamic API**: Services annotated with `[HttpMethod]` attributes are auto-exposed as REST endpoints at `api/{service}/{method}` without explicit Controllers. Configured via `.AddDynamicApi()` with lowercase routes, no service suffix
- **Multi-tenancy**: `ITenantContext` (scoped) + `TenantMiddleware` extracts tenant from JWT claims or `X-Tenant-Id` header. Entities inherit `TenantEntityBase` for automatic `TenantId` filtering
- **Data Permissions**: Entities inherit `DataPermissionEntityBase` (implements `IDept`). SqlSugar global filter applied via `DataPermissionMiddleware` reading user's `PostDataScope` from JWT claims. Admin users and scope=3 (全部) bypass filtering
- **Soft Delete**: `ISoftDelete` interface → automatic global filter excludes deleted rows
- **long Serialization**: All `long` values serialized as strings to prevent JavaScript precision loss (`LongToStringConverter`)
- **Snowflake ID**: `Yitter.IdGenerator` configured in Program.cs on startup

### Infrastructure Components

| Component | Registration | Configuration Section |
|-----------|-------------|---------------------|
| **Caching** | `AddCaching(config)` | `Cache.Provider` = Memory / Redis / Hybrid |
| **SignalR** | `AddSignalRNotification()` | Hub at `/hubs/notification` |
| **Quartz** | Manual DI + `StartTaskScheduleAsync()` | Tasks stored in `sys_task` table |
| **File Upload** | `app.UseStaticFiles()` | Serves uploaded files from disk |
| **Request Logging** | `app.UseRequestLogging()` | Custom middleware |

## Code Generation System (CodeGen)

This is the most complex subsystem and is under active development. See [CODEGEN_ARCHITECTURE.md](CODEGEN_ARCHITECTURE.md) for full details.

### Key Components

```
CodeMaster.Application/
├── ScriptBuilder/              ScriptSection structured system
│   ├── ScriptSection.cs        10 types: ImportInfo, ConstInfo, LetInfo, RefInfo,
│   │                           ReactiveInfo, FunctionInfo, HookInfo, ComputedInfo,
│   │                           WatchInfo, DictRefInfo. Merge/FromMarker/ReplaceMarkers
│   └── ScriptRenderer.cs       Renders composable exports, imports, vue <script setup> shell
├── Services/CodeGen/
│   ├── TemplateCodeGenerator.cs     Page generation from DB templates
│   ├── CodeGeneratorService.cs      Full code generation orchestration
│   ├── IncrementalCodeGenerator.cs  Incremental generation (preserves existing files)
│   ├── Marker/                      [gen.xxx], [field.xxx], [relation.xxx] replacement engine
│   │   ├── MarkerReplacer.cs
│   │   └── MarkerContexts.cs
│   └── Project/                     CRUD services for project/module/entity/field mgmt
│       ├── ProjectInitializationService.cs
│       └── ModuleEntityService.cs   Main generation API (GenerateCodeAsync, GenerateTreeOnlyAsync)
└── VueBuilder/                  C# Vue template builder (alternate generation path)
    └── FieldRenderer.cs
```

### Entity Designer (Frontend)

Located in [CodeMaster.Vue/src/views/codegen/entityDesigner/](CodeMaster.Vue/src/views/codegen/entityDesigner/):
- Visual drag-and-drop page designer loading from `.tree.json`
- Component palette (30 Vue/HTML tags), property drawer, ScriptSection editor
- Right-click context menu for copy/paste/move/delete operations
- Dialog visibility toggle for previewing el-dialog components

### Per-Page Output Files

```
{entity}.{pageType}.vue           — Template + script shell (generated; DO NOT hand-edit)
{entity}.{pageType}.auto.js       — Composable logic (generated; DO NOT hand-edit)
{entity}.{pageType}.script.json   — Merged ScriptSections
{entity}.{pageType}.fields.json   — Per-field ScriptSections (user edits preserved across regen)
{entity}.{pageType}.tree.json     — Component tree (designer loads from this, NOT from .vue)
```

### Template Storage

Templates live in the database (`sys_page_templates`, `sys_field_control_templates`, `sys_child_templates`). Seed data is in `CodeMaster.Migrator/SeedData/System/TemplateModule.cs`. **Template edits only take effect after regenerating code.**

### Critical Rules

- **NEVER hand-edit generated `.vue` / `.auto.js` files** — they get overwritten on regeneration. Modify templates or ScriptSections instead
- **`tree.json` is the designer's source of truth**, not the `.vue` file. If they disagree, trust `tree.json`
- **`fields.json` preserves user ScriptSection edits** across regeneration
- **Template changes require re-running code generation** to take effect

## Frontend (CodeMaster.Vue)

- **Language**: JavaScript (NOT TypeScript), Composition API with `<script setup>` syntax
- **State**: Pinia with `pinia-plugin-persistedstate` for persistence
- **Stores**: `user.js`, `settings.js`, `tagsView.js`, `permission.js`
- **HTTP**: Axios with automatic token injection via interceptors ([request.js](CodeMaster.Vue/src/utils/request.js))
- **i18n**: vue-i18n, dynamically loads translations from `GET /api/lang/i18n-map/{langCode}`
- **Realtime**: SignalR client ([signalr.js](CodeMaster.Vue/src/utils/signalr.js)) for notifications and online user tracking
- **API proxy**: Vite dev server proxies `/api` → `http://localhost:5000`

### Key Frontend Directories

```
src/api/          API function definitions (auth.js, system/*.js, codegen/*.js, monitor/*.js)
src/views/        Page components (system/, codegen/, monitor/)
src/stores/       Pinia stores
src/utils/        auth.js, request.js, signalr.js, tree.js, dateFormat.js
src/layout/       App layout components
src/router/       Vue Router config with dynamic menu generation
```

## Database

Supports: SqlServer (default), MySQL, PostgreSQL, Sqlite, Oracle.

Configured via `DbProvider` key and `ConnectionStrings:DefaultConnection` in `appsettings.json`. The WebApi uses this for SqlSugar; the Migrator uses EF Core with conditional compilation symbols (`DB_SQLSERVER`, `DB_MYSQL`, `DB_PGSQL`).

### CodeGen-Specific Tables

`sys_project`, `sys_project_module`, `sys_module_entity`, `sys_entity_field`, `sys_one_to_many_relation` — managed through the codegen UI and seed data.

## Adding a New Feature (Standard Workflow)

1. **Entity**: Create in `CodeMaster.Domain/Entities/{Module}/` inheriting `EntityBase` or `TenantEntityBase` or `DataPermissionEntityBase`
2. **DTO**: Create in `CodeMaster.Application/Dtos/{Module}/`
3. **Service interface + implementation**: Create in `CodeMaster.Application/Services/{Module}/`, implementing `IApplicationService`
4. **Controller** (if needed beyond dynamic API): Create in `CodeMaster.WebApi/Controllers/{Module}/`
5. **Frontend page**: Create in `CodeMaster.Vue/src/views/{module}/`
6. **API functions**: Add to `CodeMaster.Vue/src/api/{module}/`
7. **Menu**: Insert into `sys_menus` table or use menu management UI
8. **Migration**: `cd CodeMaster.Migrator && dotnet ef migrations add MigrationName && dotnet run`

## Troubleshooting Quick Reference

| Problem | Solution |
|---------|----------|
| DLL locked / build fails | `taskkill /F /IM CodeMaster.WebApi.exe` then rebuild |
| Port 5000 occupied | `netstat -ano \| findstr :5000` → `taskkill /PID <pid> /F` |
| Template change not visible | Must re-run code generation after template edit |
| Designer shows stale data | `.tree.json` is the source; regenerate if missing |
| Migration fails | Check `DbProvider` in Migrator's `appsettings.json`; build with `-p:DbProvider=MySql` if needed |
| i18n not loading | Verify `GET /api/lang/i18n-map/zh-CN` returns data; check browser console |
| Data permission not working | Ensure entity inherits `DataPermissionEntityBase`; verify JWT `PostDataScope` claim |
| Git index locked | Delete `.git/index.lock` |

## Current Date

Today's date is 2026-06-22.
