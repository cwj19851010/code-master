# Public Portal, Registration, GitHub Login, and Community

This document records the first implementation pass for the public CodeMaster product portal.

## Pages

- `/` renders the MVC product homepage with SEO metadata, OpenGraph metadata, SoftwareApplication JSON-LD, feature sections, architecture copy, FAQ, and screenshot placeholders.
- `/download` is the public download page for future client/source/demo release links.
- `/register` is the public email registration page.
- `/community` is the lightweight forum list page with server-rendered categories, topic search, topic creation, and topic links.
- `/community/topic/{id}` is the server-rendered topic detail page with replies and a reply form.
- `/robots.txt`, `/sitemap.xml`, and `/llms.txt` are served by MVC for search engines and AI search/coding tools.
- `/login` redirects to the Vue admin login URL.
- `/app` redirects to the Vue admin frontend entry.

Configure the admin login redirect when deployed:

```json
{
  "Portal": {
    "AdminLoginUrl": "https://app.example.com/login",
    "AdminAppUrl": "https://app.example.com"
  }
}
```

## Email Verification

Registration uses `IPublicAccountService` and `IEmailSender`.

Default behavior is safe for local development: if no email provider is configured, verification codes are written to application logs with provider `Console`.

Resend example:

```json
{
  "Email": {
    "Provider": "Resend",
    "From": "CodeMaster <noreply@example.com>",
    "CodeSecret": "replace-with-a-long-random-secret",
    "Resend": {
      "ApiKey": "re_xxx"
    }
  }
}
```

SMTP example:

```json
{
  "Email": {
    "Provider": "Smtp",
    "From": "noreply@example.com",
    "CodeSecret": "replace-with-a-long-random-secret",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "EnableSsl": true,
      "UserName": "smtp-user",
      "Password": "smtp-password"
    }
  }
}
```

## GitHub Login

GitHub OAuth is bound into the normal `SysUser` account system through `SysExternalLogin`.

```json
{
  "Authentication": {
    "GitHub": {
      "ClientId": "github-oauth-client-id",
      "ClientSecret": "github-oauth-client-secret",
      "CallbackUrl": "https://api.example.com/api/account/github/callback"
    }
  }
}
```

GitHub users are matched by existing external login first. If the GitHub account exposes a verified email and a verified CodeMaster user exists with the same email, it is bound to that user. Otherwise, a new `member` user is created.

## Community

The first community version is intentionally small and uses the same CodeMaster account. The MVC pages are server-rendered for SEO and AI search readability, while create/reply actions call the JSON API with the browser's CodeMaster JWT token.

- `CommunityCategory`
- `CommunityTopic`
- `CommunityReply`
- `CommunityTopicLike`

Public pages:

- `GET /community`
- `GET /community/topic/{id}`

API endpoints:

- `GET /api/community/categories`
- `GET /api/community/topics`
- `GET /api/community/topics/{id}`
- `POST /api/community/topics`
- `GET /api/community/topics/{topicId}/replies`
- `POST /api/community/replies`

Default categories are seeded by the Migrator:

- 产品建议
- 问题反馈
- 模板与生成
- 展示区

## Migration

The migration `PublicPortalAuthCommunity` adds:

- `sys_users.email_confirmed`
- `sys_external_logins`
- `sys_email_verifications`
- `community_categories`
- `community_topics`
- `community_replies`
- `community_topic_likes`

Run the Migrator after pulling these changes:

```bash
cd CodeMaster.Migrator
dotnet run
```
