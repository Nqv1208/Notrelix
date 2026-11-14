# Notrelix n8n assets

n8n integration is disabled by default (`N8n:Enabled` is `false` in `backend/src/Notrelix.API/appsettings.json`).

## What exists

- Application handler `HandleN8nCallback`
  (`backend/src/Notrelix.Application/Features/Automation/Executions/Commands/HandleN8nCallback/`), which verifies a
  signed callback.
- Backend configuration keys under `N8n`: `Enabled`, `InternalBaseUrl`, `WebhookBasePath`, `WebhookSecret`,
  `SignatureToleranceSeconds`.
- `N8N_WEBHOOK_SECRET` in `.env.example`.
- `workflows/notrelix-card-assigned-webhook.json`, a sample n8n workflow. Its `card` naming is a legacy sample
  name; the product noun is BoardItem.

## What is not wired

- No n8n service in any Compose file.
- No API route that receives the callback; only the Application handler exists.

See `docs/product/automation.md` for the product contract and `backend/docs/operations/configuration-and-runtime.md`
for configuration.
