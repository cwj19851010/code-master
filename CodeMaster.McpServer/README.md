# CodeMaster MCP Server

`CodeMaster.McpServer` exposes CodeMaster project and code-generation workflows through the official .NET MCP SDK over stdio.

## Architecture

The MCP process never connects to the CodeMaster metadata database.

- Project, module, entity, field, and structure operations call the authenticated CodeMaster WebApi.
- MCP tokens are validated by CodeMaster and saved under `~/.codemaster/mcp-auth.json`.
- Initialization, generation, migration, build, and process operations reuse `CodeMaster.LocalAgent` and run on the user's machine.
- LocalAgent downloads a tenant-filtered generation bundle and templates from WebApi, uses a temporary local SQLite metadata snapshot, and reports completion to WebApi.

## Tools

- `codemaster_login`
- `codemaster_logout`
- `codemaster_whoami`
- `resolve_project_context`
- `query_project`
- `analyze_requirements`
- `save_project`
- `save_module`
- `create_or_update_entity`
- `run_project_operation`
- `generate_code`
- `get_project_structure`

`create_or_update_entity` supports the same relation metadata used by the Web UI:

- `relations`: legacy one-to-many child-table relations.
- `ownedOneRelations`: owned one-to-one composition relations, resolved across modules in the same project.
- `fields[].resultMappings`: `select-table` source-to-local field assignments.

Entity capability rules are shared by Web UI, Agent, MCP, generated services, and migration scanning:

- Every generated entity starts from `IBaseEntity`.
- `hasPrimaryKey=true` adds the standard `long Id`, `IEntity<long>`, migration scanning, and `getById` support.
- `isReadOnly=true` disables create/update/delete. A keyed read-only entity keeps `getById`; a keyless read-only entity exposes list/query/export only.
- Writable and tree entities require a primary key.
- Audit fields are `CreateUserId`, `CreateBy`, `CreateTime`, `UpdateUserId`, `UpdateBy`, and `UpdateTime`.

Field controls include `input`, `textarea`, `number`, `select`, `select-table`, `date`, `datetime`, `switch`, `checkbox`, `radio-group`, `checkbox-group`, `file`, `image`, `editor`, and `cascader`.

- Computed fields use `fieldCategory=Computed` and arithmetic formulas with `[FieldName]` references.
- Aggregate fields use `fieldCategory=Aggregate`, `aggregateType=Sum|Avg|Concat`, and a child field from an existing one-to-many relation.

All metadata writes still go through the authenticated CodeMaster WebApi. The MCP process does not receive or require a database connection string.

## Authentication And Project Context

1. Generate an MCP Token in CodeMaster personal center.
2. Open a generated project directory containing `.codemaster/project-context.json`.
3. Call `codemaster_login` once and paste the token.
4. Future tools resolve `serverBaseUrl` and `projectId` from project context and the saved token.

Tokens are never written into generated project directories.

## Build

```powershell
dotnet build CodeMaster.McpServer/CodeMaster.McpServer.csproj
```

## Recommended MCP Configuration

```json
{
  "mcpServers": {
    "codemaster": {
      "command": "dotnet",
      "args": [
        "D:/MyHomeWorks/CodeMaster/CodeMaster.McpServer/bin/Debug/net10.0/CodeMaster.McpServer.dll"
      ],
      "cwd": "D:/your-generated-project"
    }
  }
}
```

Use a prebuilt DLL. Running `dotnet run` can write restore/build output before the MCP stdio transport starts.
