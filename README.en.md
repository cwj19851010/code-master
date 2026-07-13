# CodeMaster

[简体中文](README.md) | English

CodeMaster is a convention-driven rapid development platform for enterprise applications. It helps teams define projects, modules, entities and fields, then generate maintainable .NET + Vue applications with dynamic APIs, permissions, menus, migrations, i18n data and client-side startup support.

> Current status: early preview. The core framework, code generation, template designer, MCP integration and Tauri client workflow are usable, but APIs and templates may still change before v1.0.

## Highlights

- **Convention over configuration**: define entities and fields once, then generate backend services, DTOs, frontend pages, menus, permissions, language resources and migration data.
- **Dynamic API and permissions**: application services can be exposed as REST endpoints and protected by generated permission codes.
- **Template-driven code generation**: page templates, field controls and child-table templates are stored in the database and can be edited from the UI.
- **Full and incremental generation**: regenerate clean projects or update existing generated code while preserving user-edited script sections.
- **MCP integration**: AI agents can create or edit CodeMaster projects, modules, entities and fields through MCP instead of writing metadata files directly.
- **Tauri + LocalAgent client**: the desktop client can call local capabilities such as project initialization, local code generation and starting generated services.
- **Multi-tenant foundation**: tenant-aware entities, menu scopes, JWT tenant claims and data-permission hooks are built in.
- **Production-oriented stack**: .NET 10, SqlSugar runtime ORM, EF Core Migrator, Vue 3, Element Plus, Vite, SignalR, Quartz and optional Redis cache.

## Architecture

```text
CodeMaster.WebApi          ASP.NET Core API, dynamic API, controllers, middleware
CodeMaster.Application     Services, DTOs, code generation, templates, MCP-facing logic
CodeMaster.Domain          Entity models
CodeMaster.Infrastructure  SqlSugar, repositories, auth, SignalR, Quartz, middleware
CodeMaster.Core            Base entities, interfaces, shared abstractions
CodeMaster.Migrator        EF Core migrations and seed data
CodeMaster.Vue             Vue 3 admin UI and Tauri client source
CodeMaster.LocalAgent      Local sidecar service used by the desktop client
CodeMaster.McpServer       MCP server for AI-assisted code generation workflows
Templates                  Source template package used for generated projects
```

## Requirements

- .NET SDK 10
- Node.js 20+
- PostgreSQL for the current committed EF migration set
- Runtime code supports SQLite, PostgreSQL, MySQL, SQL Server and Oracle, but non-PostgreSQL EF migrations should be regenerated for the selected provider before publishing or deploying.
- Optional: Redis, Tauri prerequisites for desktop client builds

## Quick Start

The committed configuration uses local PostgreSQL example values and placeholder secrets. Update the connection string before running in your own environment.

1. Restore and build:

```bash
dotnet restore CodeMaster.sln
dotnet build CodeMaster.sln
```

2. Create an empty PostgreSQL database named `CodeMasterDB`, then create the tables and seed data:

```bash
createdb -h localhost -U postgres CodeMasterDB
cd CodeMaster.Migrator
dotnet run
```

3. Start the API:

```bash
cd CodeMaster.WebApi
dotnet run
```

4. Start the frontend:

```bash
cd CodeMaster.Vue
npm install
npm run dev
```

Default development account:

```text
admin / admin123
```

## Configuration

Do not commit real production secrets. Use one of these approaches:

- environment variables
- user secrets for local development
- an ignored `appsettings.Production.json`
- server-side secret managers

Important keys:

```text
ConnectionStrings__DefaultConnection
DbProvider
JwtSettings__SecretKey
Authentication__GitHub__ClientId
Authentication__GitHub__ClientSecret
Authentication__GitHub__CallbackUrl
Email__Smtp__UserName
Email__Smtp__Password
Email__CodeSecret
```

For the Vue/Tauri client, copy `CodeMaster.Vue/.env.example` to a local `.env` file when a custom server address is needed.

## Code Generation Notes

- Generated `.vue` and `.auto.js` files should not be hand-edited.
- Template changes should be made in database seed data; one-off local repair or update scripts should not be committed.
- `tree.json` is the entity designer source of truth.
- `fields.json` preserves per-field script sections across regeneration.

## MCP Usage

The MCP server is intended for natural-language workflows such as:

- create or edit projects, modules and entities
- define entity fields and control attributes
- initialize generated projects
- run full or incremental code generation
- start generated frontend/backend services
- inspect generated project entity structure

Agents should use MCP tools for metadata operations instead of directly editing generated project metadata.

## Security

Before publishing a fork or deployment:

- rotate any secrets that may have been committed or shared
- keep production appsettings out of git
- disable Swagger in public production deployments unless explicitly protected
- use HTTPS and a strong JWT secret
- review generated templates before exposing them to untrusted users

See [SECURITY.md](SECURITY.md) for vulnerability reporting.

## Contributing

Issues and pull requests are welcome while the project is stabilizing. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening larger changes.

## License

CodeMaster is released under the [Apache License 2.0](LICENSE).
