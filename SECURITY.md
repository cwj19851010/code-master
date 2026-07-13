# Security Policy

## Supported Versions

CodeMaster is currently in preview. Security fixes are applied to the main development branch until stable releases are published.

## Reporting a Vulnerability

Please do not open a public issue for a suspected security vulnerability.

Send a private report to the project maintainer through the preferred private channel listed on the project page. Include:

- affected version or commit
- reproduction steps
- impact description
- relevant logs or screenshots with secrets removed
- suggested mitigation, if known

## Secret Handling

Never commit production secrets. This includes:

- database connection strings
- SMTP authorization codes
- OAuth client secrets
- JWT signing keys
- private deployment scripts
- `.env` files and production appsettings files

Use environment variables, user secrets, ignored production config files or a managed secret store.

If a secret has ever been committed or shared in a chat/log, rotate it before publishing the repository.

## Production Defaults

- Disable Swagger in public production deployments unless access is protected.
- Use HTTPS.
- Set a strong `JwtSettings__SecretKey`.
- Review generated templates before allowing untrusted users to upload or execute templates.
- Keep LocalAgent endpoints bound to local/private interfaces.
