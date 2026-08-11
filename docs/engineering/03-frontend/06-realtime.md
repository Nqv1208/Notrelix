---
title: "Frontend Realtime Contract"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Realtime Contract

Realtime accelerates convergence and collaboration; it does not replace authoritative server APIs, query ownership or permission checks.

## FE-RT-101 — Realtime is scoped and authenticated

Subscriptions are created only after account/workspace/session scope is established. Topic/resource identifiers are produced through owned contracts. A client MUST NOT subscribe to guessed cross-tenant identifiers or retain old-workspace subscriptions after a scope transition.

## FE-RT-102 — Events complement query state

An incoming event may:
- invalidate an owned query;
- apply a deterministic patch when payload/version are sufficient;
- update ephemeral collaboration presence state;
- trigger refetch/reconciliation.

It MUST NOT create a parallel permanent entity store with different ownership from the query/state package.

## FE-RT-103 — Duplicate/out-of-order delivery is expected

Handlers must be safe for duplicate delivery and tolerate reconnect/out-of-order behavior according to event contract. Prefer version/identity-aware reconciliation to assuming each event is unique and strictly ordered globally.

## FE-RT-104 — Authorization can change while connected

A connection established under valid permission does not make that permission permanent. Server-side subscription authorization remains authoritative. Client logic must handle revoked access, resource deletion and resubscription failures without leaking stale protected data.

## FE-RT-105 — Lifecycle ownership is explicit

The owner that creates a subscription disposes it. Scope change, logout, host unmount and reconnect replace prior consumers deterministically. Listener duplication is a correctness/performance bug.

## Collaboration vs domain events

Presence/cursor/typing signals may be ephemeral and need not enter authoritative query state. Durable business changes should converge through server state plus durable event/realtime projection semantics. UI code must distinguish the two.

## Proof

Test duplicate event handling, reconnect/resubscribe, workspace transition, stale-event rejection/version reconciliation and cache invalidation. Integration/e2e tests should cover at least one real critical resource path rather than only mocking callbacks.
