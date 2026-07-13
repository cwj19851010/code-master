---
name: codemaster-codegen-workflow
description: Use when changing CodeMaster code generation, templates, generated project output, MCP codegen tools, or release checks. It keeps template seed data, current database scripts, generated files, and validation steps aligned.
---

# CodeMaster Codegen Workflow

Use this workflow for CodeMaster code generation work, especially when MCP tools or page templates are involved.

## First Reads

1. Read `AGENTS.md`.
2. Read `CODEGEN_ARCHITECTURE.md` when changing generator behavior, template markers, ScriptSection merge logic, or designer output.
3. Inspect existing generated output before changing templates. Treat `.tree.json` as the designer source of truth.

## Generated Files

Do not hand-edit generated `.vue` or `.auto.js` files as a fix. Change the generator, templates, or ScriptSection data and regenerate.

Per-page generated files must stay consistent:

- `{entity}.{page}.vue`
- `{entity}.{page}.auto.js`
- `{entity}.{page}.script.json`
- `{entity}.{page}.fields.json`
- `{entity}.{page}.tree.json`

Field keys in `.fields.json` should use field IDs, not field names.

## Template Changes

Every template content or marker contract change must update all of these:

1. `CodeMaster.Migrator/SeedData/System/TemplateModule.cs`
2. A runnable current-database update script under `scripts/`
3. The current configured database by running that script
4. Regenerated output for the affected entity/page

Do not claim a template change is complete until fresh seed data and the current local database are both updated.

## MCP Usage

Prefer MCP tools for AI-driven codegen workflows:

- `query_project`: inspect projects, modules, entities, fields, and relations
- `get_project_structure`: inspect full project/module/entity/field/relation metadata plus generated project file state
- `analyze_requirements`: turn a user request into a create/update checklist
- `save_project`: create or update project metadata; include frontend and backend ports for new projects
- `save_module`: create or update module metadata
- `create_or_update_entity`: create or update entity, field, control, and relation metadata without deleting existing metadata
- `run_project_operation`: initialize projects, run initialization steps, start/stop frontend/backend/all, query status, migrate, and build
- `generate_code`: generate code for an entity/module/project in full or incremental mode and validate the generated project build

For natural-language project/module/entity generation tasks, use the personal `codemaster-mcp-codegen` skill when available. It defines the MCP-first boundary and field-control conventions.

Pass long IDs as strings. Return long IDs as strings.

For MCP client configuration, run a prebuilt server with `dotnet run --no-build` or `dotnet <CodeMaster.McpServer.dll>`. Plain `dotnet run` can emit restore/build output to stdout before the MCP server starts.

For one-to-many relations, default the child foreign key from the parent entity name, for example `OrderId` for `Order -> OrderItem`.

For `select-table`, preserve:

- related entity name
- related ID field
- display field list
- multi-select flag

## Required Validation

Choose the smallest validation set that covers the change. For release-facing codegen work, run:

1. `dotnet build CodeMaster.sln`
2. `dotnet test CodeMaster.CodeGenerator.Tests/CodeMaster.CodeGenerator.Tests.csproj`
3. Regenerate the OrderManager sample project
4. Build generated `OrderManager.sln`
5. Run `npm run build` in generated `OrderManager.Vue`

For MCP-only changes, at minimum run:

1. `dotnet build CodeMaster.McpServer/CodeMaster.McpServer.csproj`
2. A stdio JSON-RPC smoke test for `initialize`, `tools/list`, and one safe tool call

## Review Checklist

Before finishing, check:

- no business-specific hardcoded names in generic templates
- generated i18n keys follow the frontend naming convention
- `select-table` list/detail display handles multiple display fields
- multi-select dictionary and select-table save and query behavior still match generated APIs
- template marker syntax is consistent and documented in seed data and scripts
