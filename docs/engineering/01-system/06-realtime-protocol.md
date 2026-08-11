---
title: "Realtime Protocol"
document_class: handbook
normative: true
owner: platform
maturity: FROZEN
conformance: CANONICAL
applies_to: realtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Realtime Protocol

Realtime is a **freshness/interaction channel**, not the primary source of truth.

## SYS-RT-001 — Logical event identity is stable

Realtime event names/payload versions are contracts independent of CLR/TypeScript class names. Cosmetic refactors do not silently rename logical identity.

## SYS-RT-002 — Subscription scope is explicit

Channels/subscriptions are scoped by the narrowest meaningful tenant/resource contract. A client must not receive unrelated workspace/resource events and filter them only in UI.

## SYS-RT-003 — Realtime is replay-safe at the client boundary

Clients assume duplicate/out-of-order delivery may occur unless a stronger stream contract is documented. Cache patches require enough version/order identity to prove safety; otherwise invalidate/refetch authoritative state.

## Event envelope minimum

A logical realtime contract normally needs:

```text
event id
logical name/version
scope (account/workspace/resource as applicable)
resource identity
occurred/produced metadata as approved
payload
```

Do not include raw secrets or provider transport internals.

## Connection lifecycle

Runtime owns reconnect/backoff/auth transport. Product code owns relevance mapping. Workspace/resource transition disposes previous subscriptions before the new context becomes authoritative.
