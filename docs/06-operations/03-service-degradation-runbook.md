---
title: "Service Degradation Runbook"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Service Degradation Runbook

Use for database, Redis/cache, object storage, realtime gateway or frontend delivery degradation.

## Trigger
Elevated errors/latency, health dependency failure, connection exhaustion, cache outage, asset/realtime failures or customer-visible stale/unavailable behavior.

## Contain
- stop recent rollout/config if correlated and rollback is safe;
- shed optional load/background work before correctness-critical writes;
- disable optional cache/realtime acceleration only if authoritative API semantics remain correct;
- do not bypass authorization/RLS/provider validation to restore traffic.

## Diagnose
Separate app saturation from dependency saturation. Check recent deploy/migration, DB locks/pool/query latency, Redis connectivity/eviction, storage provider error, realtime connection/reconnect rate, CDN/assets and per-endpoint/tenant patterns. Use correlation/traces to locate dominant failing operation.

## Decide
Cache outage may degrade to direct authoritative reads only when load can tolerate it and scope correctness is preserved. Realtime outage should normally fall back to refetch/poll/reconnect behavior rather than block authoritative writes. DB degradation may require write throttling/maintenance rather than retry storms.

## Recover/verify
Restore dependency/capacity/config, gradually reopen workload, verify error/latency and product flows, inspect stale caches/subscriptions and confirm no cross-tenant leakage or lost writes. Keep watch until queues/connections return to steady state.
