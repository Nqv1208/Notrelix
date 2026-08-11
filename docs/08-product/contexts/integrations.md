---
title: "Integrations Context"
document_class: constitution
normative: true
owner: integrations
maturity: FROZEN
conformance: CANONICAL
applies_to: integrations
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Integrations Context

## Mission

Integrations owns external provider connections, credential references, inbound webhooks and outbound synchronization/provider operations. It translates provider concepts into stable Notrelix contracts without leaking provider SDK/models into product domains.

## INT-101 — Provider secret material is isolated

OAuth tokens/API keys/webhook secrets are encrypted/stored through approved secret/provider infrastructure and referenced by connection identity. They never appear in Domain events, frontend contracts, logs or ordinary configuration payloads.

## INT-102 — Connection lifecycle is explicit

Connection states cover creation/authorization, active, expired/refresh-needed, disconnected/revoked and error/reconnect as provider semantics require. Reauthorization/secret rotation does not create duplicate logical connections accidentally. Disconnect defines webhook/subscription cleanup and queued-work behavior.

## INT-103 — Webhooks are authenticated before business processing

Verify provider signature/secret/timestamp/replay policy from raw request as required, bound payload size, resolve connection/tenant safely, then persist/deduplicate an inbound identity before asynchronous business processing. Do not trust workspace/account identifiers supplied only in webhook JSON.

## INT-104 — Inbound/outbound processing is idempotent

Store/provider event IDs or stable derived identities per connection. Duplicate webhook delivery does not duplicate product mutations. Outbound sync actions track local operation and provider correlation/idempotency so retry can distinguish unknown, failed and completed effects.

## INT-105 — Provider models are anti-corruption boundaries

Map provider projects/tasks/calendar events/users into explicit integration DTOs and product commands. Product Domain does not import provider SDK types or branch on provider brand. Provider-specific limitations remain inside adapter/capability configuration.

## Sync conflicts

Two-way sync defines source-of-truth per field/change, external revision/version mapping, deletion behavior and conflict resolution. “Last write wins” is allowed only if explicitly accepted for that mapping; otherwise preserve conflict/authoritative direction.

## Authorization and scope

Connecting/disconnecting providers and selecting resources require Governance authorization. Background sync executes under a service/connection principal with bounded capability, not unrestricted system authority. Provider data cannot route to a tenant from an unverified external field.

## Events/operations

Provider failures are observable with connection/operation identity, redacted error and retry classification. Poison events are quarantined by concrete identity. Rate limits use provider-aware retry/backoff rather than hot-loop retry.

## Deletion/retention

Disconnect/revoke destroys or disables secret access and future sync. Decide retention of provider mapping/history and whether Notrelix-created business data remains. GDPR/privacy deletion considers provider-held copies separately.

## Forbidden designs

- secret tokens in Domain/client/event/log;
- product aggregates calling Google/Slack/etc. SDKs;
- webhook tenant routing from untrusted payload alone;
- non-idempotent retry of external creates;
- assuming provider deletion equals Notrelix deletion without mapped policy;
- generic integration table with unvalidated arbitrary JSON and no discriminator/version.

## Tests/change impact

Test webhook signature/replay/tenant resolution, connection lifecycle/rotation, mapping, duplicate/outbound retry, provider rate-limit/error handling and disconnect. New provider adapters require threat/privacy review, config/secret/runbook and product contract compatibility.
