---
title: "Dependency Services"
document_class: handbook
normative: true
owner: infrastructure
maturity: STABILIZING
conformance: CANONICAL
applies_to: infrastructure
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Dependency Services

Runtime dependencies have explicit protocol semantics; “try again” is not a universal resilience strategy.

## INFRA-DEP-101 — PostgreSQL is authoritative durable state

Connections use bounded pools/timeouts/cancellation. Transactions align with Application/consumer commit boundaries. RLS/session scope is established in the transaction required by the persistence contract. Migration/DDL is managed separately from steady-state queries. Connection exhaustion/lock pressure is observable.

## INFRA-DEP-102 — Redis/cache is scoped acceleration

Cache keys include tenant/resource/version dimensions required by the owning contract. Cache loss can cause misses/degradation but cannot authorize access or silently change business truth. Permission-sensitive cache uses authoritative permission version/invalidation. Eviction/TTL is not a substitute for explicit correctness invalidation where stale data is unsafe.

## INFRA-DEP-103 — Messaging/outbox assumes at-least-once delivery

Broker/dispatcher/consumer configuration preserves logical message/event and consumer identity. Acknowledgement/commit occurs only after handler/dedup/transaction semantics succeed as defined by Platform docs. Retry/backoff and dead-letter/poison handling are bounded and observable.

## INFRA-DEP-104 — Object/external storage is referenced, not embedded

Business state stores object metadata/reference rather than large binary payloads in Domain/events. Upload/download access is authorized and time-bounded as needed. Delete/retention aligns with owning product/privacy policy and handles orphan cleanup explicitly.

## INFRA-DEP-105 — External providers have operation semantics

Adapters set cancellation/timeout, classify transient vs permanent failure, obey provider rate limits and use idempotency/correlation where possible. Retrying a create/payment/webhook side effect without a stable operation identity is forbidden.

## Capacity and backpressure

Monitor database pool/query latency/locks, queue backlog age, Redis memory/evictions, storage/provider failure and worker throughput. Scale/backpressure decisions avoid retry storms. Optional background work may be paused before correctness-critical transactional work.

## Proof

Integration/provider-contract tests for failure behavior, operational readiness/runbooks and production telemetry. Dependency substitutions require an architecture review when semantics (transactionality/order/consistency/security) differ, even if API surface looks similar.
