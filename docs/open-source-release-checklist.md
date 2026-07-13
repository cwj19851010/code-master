# Open Source Release Checklist

Use this checklist before publishing CodeMaster to a public repository.

## 1. Rotate Secrets

Rotate every secret that has ever appeared in source files, logs, screenshots, chats or build artifacts:

- database passwords
- SMTP authorization codes
- GitHub OAuth client secrets
- JWT signing keys
- Redis passwords
- production server credentials

Do this even if the current working tree no longer contains the secret. Git history and local artifacts may still contain old values.

## 2. Check the Working Tree

Run:

```bash
git status --short
rg -n --hidden --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/node_modules/**' --glob '!**/.git/**' --glob '!artifacts/**' "password|secret|clientsecret|authorization|host=|username=|smtp|bearer|token|124.221"
```

Review every match manually. Many matches are legitimate code identifiers, but real credentials must be removed.

## 3. Remove Binary and Local Artifacts

Do not publish:

- local SQLite databases
- database backups
- generated installers
- publish output
- user-specific IDE settings
- production `appsettings.Production.json`
- `.env` files

## 4. Clean Git History If Needed

If secrets were committed before, removing them from the latest commit is not enough. Use a history rewrite tool such as `git filter-repo` or BFG Repo-Cleaner on a private copy, then force-push the cleaned repository only after validating it.

After rewriting history, rotate the secrets anyway.

## 5. Verify Legal and Community Files

Required:

- `LICENSE`
- `NOTICE`
- `README.md`
- `CONTRIBUTING.md`
- `SECURITY.md`
- `CODE_OF_CONDUCT.md`

Recommended:

- issue templates
- pull request template
- roadmap
- screenshots or demo video
- architecture notes

## 6. Build Before Publishing

```bash
dotnet build CodeMaster.Migrator/CodeMaster.Migrator.csproj
dotnet build CodeMaster.WebApi/CodeMaster.WebApi.csproj
dotnet test CodeMaster.CodeGenerator.Tests/CodeMaster.CodeGenerator.Tests.csproj
cd CodeMaster.Vue
npm install
npm run build
```

Also run a dependency audit:

```bash
dotnet list CodeMaster.sln package --vulnerable --include-transitive
```

The current committed EF migration set is PostgreSQL-oriented. If you want SQLite, MySQL, SQL Server or Oracle as the first public quick-start path, regenerate and verify a clean migration set for that provider before tagging a release.

## 7. Publish Positioning

Suggested first release label:

```text
v0.1.0-preview
```

Suggested positioning:

```text
CodeMaster is a convention-driven rapid development platform for .NET + Vue enterprise applications, with dynamic APIs, generated permissions, template-driven code generation, MCP integration and local client support.
```
