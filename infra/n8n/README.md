# Notrelix n8n integration

This folder contains local n8n assets for the Notrelix automation runtime.

## Runtime

- Dev UI: `http://localhost:5678`
- Internal n8n base URL for backend: `http://n8n:5678`
- Default Notrelix-to-n8n webhook path base: `/webhook`
- n8n data volume: `/home/node/.n8n`
- n8n database: `${N8N_POSTGRES_DB:-notrelix_n8n}`

## Automation rule example

Create a workspace automation rule with:

```json
{
  "name": "n8n card assigned workflow",
  "triggerEvent": "card.assigned",
  "actionType": "n8n.webhook",
  "configuration": "{\"webhookPath\":\"notrelix-card-assigned\"}"
}
```

The backend dispatch worker will call:

```text
POST http://n8n:5678/webhook/notrelix-card-assigned
```

## Signed callback contract

n8n callbacks to Notrelix must be signed.

```text
POST /api/v1/integrations/n8n/callback
X-Notrelix-Timestamp: <unix seconds>
X-Notrelix-Signature: sha256=<hmac_sha256_hex(timestamp + "." + rawBody)>
```

The shared secret is configured by `NOTRELIX_N8N_WEBHOOK_SECRET` in Docker and `N8n:WebhookSecret` in the backend.

