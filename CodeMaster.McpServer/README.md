# CodeMaster MCP Server

CodeMaster.McpServer exposes CodeMaster project metadata and code generation workflows through MCP over stdio.

## Tools

- `codemaster_login`: validate and save a CodeMaster MCP token in `~/.codemaster/mcp-auth.json`.
- `codemaster_logout`: remove saved MCP tokens.
- `codemaster_whoami`: validate and show the saved MCP identity.
- `resolve_project_context`: read `.codemaster/project-context.json` from a generated project directory.
- `query_project`: inspect projects, modules, entities, fields, and relations.
- `analyze_requirements`: return a CodeMaster-oriented schema checklist and example payload.
- `create_or_update_entity`: create or add fields/relations without deleting existing metadata.
- `generate_code`: generate code for an entity and optionally validate generated builds.

## Auth And Project Context

Generated projects contain `.codemaster/project-context.json` with the CodeMaster project id, project name, ports, database type, and optional server base URL. It does not contain user tokens.

For production-style MCP usage:

1. Generate an MCP Token in CodeMaster personal center.
2. Open the generated project directory in the AI client.
3. Call `resolve_project_context` to confirm the current project.
4. Call `codemaster_login` with the token. If the context has no `serverBaseUrl`, pass it explicitly.
5. Future MCP calls can omit `projectId`; tools resolve it from `.codemaster/project-context.json`.

Saved tokens live in `~/.codemaster/mcp-auth.json`, outside the generated project.

## Build

```powershell
dotnet build CodeMaster.McpServer/CodeMaster.McpServer.csproj
```

## Recommended MCP Config

Use a prebuilt server so dotnet restore/build output cannot pollute MCP stdout.

```json
{
  "mcpServers": {
    "codemaster": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "D:/MyHomeWorks/CodeMaster/CodeMaster.McpServer/CodeMaster.McpServer.csproj",
        "--no-build"
      ],
      "cwd": "D:/MyHomeWorks/CodeMaster"
    }
  }
}
```

Direct DLL launch is also valid after building:

```json
{
  "mcpServers": {
    "codemaster": {
      "command": "dotnet",
      "args": [
        "D:/MyHomeWorks/CodeMaster/CodeMaster.McpServer/bin/Debug/net10.0/CodeMaster.McpServer.dll"
      ],
      "cwd": "D:/MyHomeWorks/CodeMaster"
    }
  }
}
```

The server writes MCP JSON-RPC responses only to stdout. Diagnostics and application logs go to stderr.
