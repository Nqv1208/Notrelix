---
title: "Platform Messaging, Outbox and Idempotency"
document_class: handbook
normative: true
owner: backend-platform
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/platform
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Platform Messaging, Outbox and Idempotency

This document protects order-sensitive reliability semantics. Small changes here can create duplicate side effects, skipped messages or cross-tenant execution.

## Outbox

### BE-MSG-101 — Durable side-effect intent commits with business state

When a business change must result in a durable cross-boundary action, record the outbox/integration intent in the same database transaction as the state change. Dispatch occurs after commit.

Domain does not persist outbox rows and does not know broker envelopes.

## Consumer idempotency

### BE-MSG-201 — Dedup identity = message/event identity + consumer identity

Different consumers may legitimately process the same event. One consumer retry must not suppress another consumer. Use the approved claim-before-execute dedup mechanism; do not hand-roll check-then-mark logic in handlers.

Current established pattern uses a key equivalent to `event_id + consumer_name` and claim state such as Processing/Succeeded/Failed.

### BE-MSG-202 — Claim, RLS and consumer work share one transaction

Conceptual order:

```text
set in-memory tenant context
→ begin consumer transaction
→ apply RLS/session inside transaction
→ claim dedup identity
→ execute consumer
→ persist business/outbox changes
→ mark/commit success
```

If consumer work fails, transaction rollback allows safe retry according to the delivery contract.

## Ordering

### BE-MSG-301 — Ordering advances only after handler success

Do not record sequence progress before the protected handler succeeds. Otherwise a crash/failure can make a later retry appear stale and permanently skip work.

### BE-MSG-302 — Scope ordering to the contracted stream

Order by message/resource/aggregate stream identity as required. Do not impose global event-type ordering across unrelated tenants/resources.

## Poison detection

### BE-MSG-401 — Poison identity is concrete enough not to blacklist healthy messages

Repeated failure of one concrete message/consumer must not mark an entire logical event type as poison. Poison/dead-letter identity includes the message identity and consumer/stream dimensions required by the runtime contract.

## Retry

Classify transient vs terminal errors. Retries are bounded/backed off; non-retryable validation/compatibility failures surface operationally. Never transform “retry until it passes” into hidden infinite work.

## Proof

Platform tests cover identity/order/poison/retry state machine; integration tests cover outbox/dedup/RLS transaction behavior with real persistence graph.
