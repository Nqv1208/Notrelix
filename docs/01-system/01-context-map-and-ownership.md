---
title: "Context Map and Ownership"
document_class: constitution
normative: true
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: system
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Context Map and Ownership

## Business bounded contexts

| Context | Owns | Does not own |
|---|---|---|
| Accounts | account/customer administrative boundary, account-level ownership | user identity lifecycle, workspace resource state |
| Identity | user identity, sessions/credentials/MFA/OAuth/security lifecycle | workspace membership/authorization policy |
| Workspaces | workspace/space/team/membership/invitation organization | board/document internals |
| Governance | reusable resource permission/policy/audit governance semantics | business aggregate state of protected resource |
| Work Management | boards, dynamic fields, items, groups, views and work engines | document block state, account billing state |
| Documents | pages/blocks/document hierarchy/content semantics | generic comments/notifications, board item state |
| Collaboration | comments, mentions, notifications/activity collaboration | immutable audit governance facts |
| Automation | automation definitions/executions product semantics | provider transports/schedulers/retry mechanism |
| Integrations | external connection/product integration state | generic provider SDK/runtime implementation |
| Billing | plan catalog, subscription/entitlement/commercial lifecycle | authorization policy implementation unrelated to entitlement |
| Analytics | analytical facts/definitions/insight semantics where source-owned | arbitrary reporting projection mistaken for source of truth |

## SYS-CTX-001 — Context ownership follows lifecycle, not foreign keys

A table referencing `workspace_id` is not automatically owned by Workspaces. Ownership is determined by which capability defines lifecycle/invariants and is authorized to mutate the state.

## SYS-CTX-002 — Cross-context references are contracts

Contexts reference other roots by stable IDs/immutable facts. They do not navigate another context's mutable aggregate or frontend private package state.

## Cross-context patterns

### Read

Application may obtain a read-only fact through an explicit read service/query contract when local use-case behavior requires it. Cache/projection is acceptable when freshness semantics are explicit.

### Write

A context must not directly mutate another context's state. Use:

- synchronous orchestrated use case only when one transaction/host owns both and coupling is explicitly accepted;
- integration event/outbox consumer for durable decoupled change;
- saga/process manager for multi-step stateful coordination.

### Shared concept

Promote to shared kernel/foundation only when meaning and lifecycle are genuinely identical across contexts. Similar field names are insufficient.
