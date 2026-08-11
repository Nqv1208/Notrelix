---
title: "Tenancy and Resource Scope"
document_class: constitution
normative: true
owner: security
maturity: FROZEN
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Tenancy and Resource Scope

Tenant isolation is both a security property and a data-correctness property.

## Scope classes

Every persisted/business resource and externally visible event is classified as one of:

```text
global
account-scoped
workspace-scoped
hybrid (explicit contract only)
```

Global is a real product classification, not “workspace ID = empty”.

## SYS-TEN-001 — Tenant identity is explicit and immutable

Required account/workspace IDs MUST be non-empty, stable for the resource lifecycle, and validated at trust boundaries. Nullable tenant IDs reject empty IDs when present.

## SYS-TEN-002 — Cross-tenant references are invalid

A workspace-scoped object may only reference another workspace-scoped object when scope compatibility is established by owned facts. UI selection or URL co-location is not proof.

## SYS-TEN-003 — Scope survives derived systems

Scope must be preserved in:

- database columns/queries and RLS session;
- cache keys and permission-version keys;
- search index documents and filters;
- outbox/integration messages;
- realtime subscriptions/channels;
- background jobs and consumer context;
- client query keys and workspace transition.

## Trust boundaries

Do not trust caller-supplied workspace/account IDs merely because they match a route. Backend authorization resolves the resource and verifies scope. Client-side scope keeps UX/cache correct but is not security authority.

## Workspace switch

A client workspace transition is a boundary event:

1. stop/dispose old-workspace realtime subscriptions;
2. cancel or make stale old-workspace requests where possible;
3. remove/invalidate old-workspace cache/state that must not bleed;
4. establish new workspace context;
5. only then render/load new child scope.

## Proof

Backend RLS/authorization integration tests and frontend query/realtime transition tests are required for high-risk tenant paths.
