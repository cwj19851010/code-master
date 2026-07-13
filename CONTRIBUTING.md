# Contributing to CodeMaster

Thanks for helping improve CodeMaster. The project is still in preview, so the most valuable contributions are clear bug reports, reproducible cases, template fixes and focused pull requests.

## Before You Start

- Search existing issues first.
- For larger features, open an issue or discussion before writing a large PR.
- Keep changes focused. Avoid mixing unrelated refactors with feature work.
- Do not commit secrets, production configuration, generated installers or local database files.

## Development Setup

```bash
dotnet restore CodeMaster.sln
dotnet build CodeMaster.sln

cd CodeMaster.Migrator
dotnet run

cd ../CodeMaster.WebApi
dotnet run

cd ../CodeMaster.Vue
npm install
npm run dev
```

Default account:

```text
admin / admin123
```

## Code Generation Rules

- Do not hand-edit generated `.vue` or `.auto.js` files when the fix belongs in templates.
- Template behavior changes should update seed data. One-off local database or template repair scripts should not be committed.
- Keep Web and Tauri client behavior shared where possible.
- MCP metadata operations should go through MCP tools instead of direct metadata file edits.

## Pull Request Checklist

- The change has a clear reason and limited scope.
- Backend builds with `dotnet build CodeMaster.WebApi/CodeMaster.WebApi.csproj`.
- Frontend changes build with `npm run build` under `CodeMaster.Vue` when relevant.
- Template changes include seed data alignment when relevant.
- Public files contain no private credentials, tokens, IP-only production URLs or local machine paths.

## Commit Style

Use short, imperative commit messages, for example:

```text
Fix PostgreSQL nullable date filters
Add template download endpoint
Improve entity designer dark theme
```

## License

By contributing to CodeMaster, you agree that your contributions are licensed under the Apache License 2.0.
