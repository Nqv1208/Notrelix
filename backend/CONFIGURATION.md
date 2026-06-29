# Notrelix Enterprise Configuration

## Config Precedence

```
appsettings.json                         ← base defaults (no secrets)
  ↓ override
appsettings.{Environment}.json           ← environment-specific (Development/Staging/Production)
  ↓ override
Environment variables                    ← Docker Compose / `.env` files
  ↓ override
Command-line arguments                   ← CLI (`--migrate`, `--seed`)
```

## Three-Layer Separation

| Layer | Role | Contains | Example |
|-------|------|----------|---------|
| `appsettings*.json` | Application behavior | Feature flags, logging levels, CORS, non-secret defaults | `Database:MigrateOnStartup`, `SeedData:Profile` |
| `.env.*` files | Secrets & credentials | Passwords, API keys, tokens, DB credentials | `POSTGRES_PASSWORD`, `JWT_SECRET`, `RESEND_API_KEY` |
| `docker-compose*.yml` | Topology & env mapping | Service definitions, network, ports, volume mounts, env var mapping | `ConnectionStrings__*`, `JwtSettings__*` |

## Configuration Sections

### Database

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Database:MigrateOnStartup` | `bool` | `false` | Run EF Core migrations on application start. Must be `false` in production. |
| `Database:__` | — | — | Future options. |

### SeedData

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `SeedData:Enabled` | `bool` | `false` | Allow seed pipeline to run (also requires `RunOnStartup` or `--seed`). |
| `SeedData:RunOnStartup` | `bool` | `false` | Execute seed automatically on application startup. |
| `SeedData:Profile` | `enum` | `Small` | Seed size: `Small`, `Medium`, `Large`. |
| `SeedData:ResetBeforeSeed` | `bool` | `false` | WARNING: Deletes all existing seed data before re-seeding. |

**Validation:**
- `Profile` must be `Small`, `Medium`, or `Large`.

### JwtSettings

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `JwtSettings:SecretKey` | `string` | `""` | Signing key, **required**, min 32 characters. Provide via env var. |
| `JwtSettings:Issuer` | `string` | `""` | Token issuer, **required**. |
| `JwtSettings:Audience` | `string` | `""` | Token audience, **required**. |
| `JwtSettings:ExpireMinutes` | `int` | `60` | Access token TTL. |
| `JwtSettings:RefreshTokenExpireDays` | `int` | `7` | Refresh token TTL. |

**Validation:**
- `SecretKey` is required, min 32 characters.
- `Issuer` and `Audience` are required.
- `ExpireMinutes` > 0.
- `RefreshTokenExpireDays` > 0.
- Fails fast on startup via `ValidateOnStart()`.

> Section is named `JwtSettings`. Do not rename to `Jwt`. The JWT service reads `SecretKey`.

### Smtp

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Smtp:Enabled` | `bool` | `false` | Enable SMTP email delivery. |
| `Smtp:Host` | `string` | `""` | SMTP server hostname. |
| `Smtp:Port` | `int` | `587` | SMTP server port. |
| `Smtp:FromEmail` | `string` | `""` | Sender email address. |
| `Smtp:FromName` | `string` | `"Notrelix"` | Sender display name. |
| `Smtp:Username` | `string` | `""` | SMTP auth username (optional). |
| `Smtp:Password` | `string` | `""` | SMTP auth password (optional). |
| `Smtp:EnableSsl` | `bool` | `true` | Enable SSL/TLS. |

**Validation:**
- When `Enabled=false`: No further validation. Startup passes.
- When `Enabled=true`: `Host` required, `FromEmail` required, `Port` must be 1–65535.
- Fails fast on startup via `ValidateOnStart()`.

### Email (Resend)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Email:Enabled` | `bool` | `false` | Enable Resend email delivery. |
| `Email:ApiKey` | `string` | `""` | Resend API key. |
| `Email:FromEmail` | `string` | `"noreply@notrelix.io"` | Sender email address. |
| `Email:FromName` | `string` | `"Notrelix"` | Sender display name. |

**Validation:**
- When `Enabled=false`: No further validation.
- When `Enabled=true`: `ApiKey` required, `FromEmail` required.

### N8n (Automation)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `N8n:Enabled` | `bool` | `false` | Enable n8n workflow automation integration. |
| `N8n:InternalBaseUrl` | `string` | `""` | n8n internal service URL. |
| `N8n:WebhookBasePath` | `string` | `"/webhook"` | Webhook path prefix. |
| `N8n:WebhookSecret` | `string` | `""` | Shared secret for webhook signature verification. |
| `N8n:SignatureToleranceSeconds` | `int` | `300` | Clock skew tolerance for webhook signatures. |

**Validation:**
- When `Enabled=false`: No further validation.
- When `Enabled=true`: `InternalBaseUrl` required, `WebhookSecret` required.
- `SignatureToleranceSeconds` must be > 0 always.
- Fails fast via `ValidateOnStart()`.

### Cors

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Cors:AllowedOrigins` | `string[]` | `[]` | Allowed CORS origins. |

### HttpsRedirection

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `HttpsRedirection:Enabled` | `bool` | `false` | Enable HTTPS redirection. Should be `true` in staging/production. |

### DataProtection

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `DataProtection:ApplicationName` | `string` | `"Notrelix"` | Application name for key isolation. |
| `DataProtection:PersistKeys` | `bool` | `false` | Persist data protection keys to disk. |
| `DataProtection:KeysPath` | `string` | `"/root/.aspnet/DataProtection-Keys"` | Directory for key persistence. |

## Environment Variables (.env files)

### Required for Development

```bash
POSTGRES_DB=notrelix_dev
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

JWT_SECRET=dev-only-at-least-32-characters-long!!
JWT_ISSUER=Notrelix
JWT_AUDIENCE=NotrelixClient
```

### Optional for Development

```bash
N8N_WEBHOOK_SECRET=dev-notrelix-n8n-webhook-secret
RESEND_API_KEY=
SMTP_HOST=
SMTP_PORT=587
SMTP_USERNAME=
SMTP_PASSWORD=
```

### Required for Staging/Production

```bash
POSTGRES_DB=notrelix
POSTGRES_USER=notrelix
POSTGRES_PASSWORD=<secure-password>

JWT_SECRET=<at-least-32-characters>
JWT_ISSUER=https://api.notrelix.io
JWT_AUDIENCE=https://app.notrelix.io

N8N_WEBHOOK_SECRET=<secure-secret>
```

## Docker Compose Env Mapping

The `docker-compose.dev.yml` maps:

| Container env var | Source | Notes |
|-------------------|--------|-------|
| `ConnectionStrings__NotrelixDb` | `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Connection string built from individual vars |
| `JwtSettings__SecretKey` | `JWT_SECRET` | Min 32 chars |
| `JwtSettings__Issuer` | `JWT_ISSUER` | |
| `JwtSettings__Audience` | `JWT_AUDIENCE` | |
| `N8n__WebhookSecret` | `N8N_WEBHOOK_SECRET` | |
| `Email__ApiKey` | `RESEND_API_KEY` | |

## Migrate & Seed Workflow

### Development (auto)

```bash
make dev-up            # Start all services
make dev-reset         # Full reset: clean → up → migrate → seed
```

### Manual (any environment)

```bash
make db-migrate        # dotnet run -- --migrate
make db-seed           # dotnet run -- --seed
make db-init           # dotnet run -- --migrate --seed
```

### Production

- `Database:MigrateOnStartup` must be `false`.
- `SeedData:Enabled` must be `false`.
- Migrate via CI/CD init container: `dotnet Notrelix.API.dll --migrate`
- Seed via CI/CD job: `dotnet Notrelix.API.dll --seed`

## Validation Rules Summary

| Section | Condition | Rule | Startup Impact |
|---------|-----------|------|----------------|
| `JwtSettings` | always | SecretKey min 32 chars, Issuer + Audience required | **Fails fast** |
| `Smtp` | `Enabled=true` | Host + FromEmail required, Port valid | **Fails fast** |
| `Smtp` | `Enabled=false` | No validation | Passes |
| `Email` | `Enabled=true` | ApiKey + FromEmail required | **Fails fast** |
| `Email` | `Enabled=false` | No validation | Passes |
| `N8n` | `Enabled=true` | InternalBaseUrl + WebhookSecret required | **Fails fast** |
| `N8n` | `Enabled=false` | No validation | Passes |
| `SeedData` | always | Profile must be Small/Medium/Large | Passes with safe default |

## Secret Management Rules

1. **Never** put real secrets in `appsettings*.json`.
2. Use `.env.*` files for local development.
3. For staging/production, use environment variables or a secret manager (Vault, AWS Secrets Manager, Kubernetes Secrets).
4. `.env.dev` may contain dev credentials; `.env.staging` and `.env.prod` must never be committed.
5. Rotate secrets regularly.
6. Logging must never expose secrets (passwords, tokens, API keys).
