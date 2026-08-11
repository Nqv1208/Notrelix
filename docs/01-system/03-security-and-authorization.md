---
title: "Security and Authorization Architecture"
document_class: constitution
normative: true
owner: security
maturity: FROZEN
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Security and Authorization Architecture

## Security boundary model

```text
identity/authentication
→ request actor/context
→ tenant/resource resolution
→ Application authorization policy
→ domain/business invariant
→ persistence/RLS defense-in-depth
```

No single lower layer replaces the others.

## SYS-AUTH-001 — Backend authorizes commands and queries

Every protected use case, including reads, must be authorized server-side. “Query does not mutate” is not a security exemption.

## SYS-AUTH-002 — Authorization is resource/capability based

Role names are policy inputs. Handlers/endpoints must not scatter `if role == Admin` logic when the permission/resource model owns the decision.

## SYS-AUTH-003 — Frontend permission checks are UX only

Frontend may hide/disable/redirect, but must handle backend denial as authoritative and cannot assume UI visibility grants access.

## SYS-AUTH-004 — Secrets/tokens do not enter business events/logs

Domain/integration/realtime payloads, analytics and normal application logs must not carry raw authentication tokens, provider secrets, password hashes or sensitive credentials.

## Defense in depth

RLS protects data access even when an application query is wrong, but it is not a replacement for use-case authorization. Likewise API authentication is not sufficient without resource authorization.

## Review triggers

Security review is mandatory for:

- new authentication/session/token storage;
- new permission/resource kind;
- cross-tenant data access/admin operations;
- provider credential storage;
- public link/share semantics;
- webhooks/inbound integrations;
- data export/import or bulk operations.
