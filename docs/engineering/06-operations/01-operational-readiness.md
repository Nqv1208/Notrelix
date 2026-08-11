---
title: "Operational Readiness"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Operational Readiness

Before a new capability/failure mode is production-ready, define health/dependency signals, structured logs/correlation, critical metrics, alert ownership, safe config/feature flag, migration/backfill behavior, expected external dependencies, capacity risk and a recovery/disable path.

## OPS-READY-101 — Observability follows semantic identifiers

Logs/metrics/traces include stable operation/context/resource/correlation identifiers needed to debug without recording sensitive payloads. Background consumers include event/consumer identity and retry outcome. Tenant IDs may be included according to data classification/logging policy; secrets/content are redacted.

## Readiness review

Ask: Can operators distinguish dependency outage from authorization/product bug? Can a stuck consumer be identified without replaying everything? Can rollout be stopped? Is schema compatible with previous binary where rollback is claimed? Is there a bounded backfill? Is capacity based on realistic cardinality?


## Dependency/failure table

For each critical dependency record whether the capability can degrade, what user-visible behavior results, retry/backoff policy, timeout/cancellation propagation, capacity signal and runbook. A dependency marked degradable still needs a correctness story—for example Redis loss may bypass cache only through tenant-safe authoritative reads, while database loss cannot be “degraded” by serving guessed writable state.

## Launch evidence

Readiness review captures owner, dashboard/log queries, feature flag/disable mechanism, migration/backfill progress signal, queue/backlog expectations and recovery test. A change that introduces a new durable consumer without retry/poison/backlog observability is not operationally complete.
