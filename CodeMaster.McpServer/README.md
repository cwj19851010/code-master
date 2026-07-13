# CodeMaster MCP Server

CodeMaster.McpServer exposes CodeMaster project metadata and code generation workflows through MCP over stdio.

## Tools

- `query_project`: inspect projects, modules, entities, fields, and relations.
- `analyze_requirements`: return a CodeMaster-oriented schema checklist and example payload.
- `create_or_update_entity`: create or add fields/relations without deleting existing metadata.
- `generate_code`: generate code for an entity and optionally validate generated builds.

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
