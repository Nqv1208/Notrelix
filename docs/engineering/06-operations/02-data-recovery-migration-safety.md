---
title: "Data Recovery and Migration Safety"
document_class: handbook
normative: true
owner: operations
maturity: FROZEN
conformance: CANONICAL
applies_to: runtime
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Data Recovery and Migration Safety

## OPS-DATA-101 — Recovery protects correctness before availability

Do not restore a database snapshot or replay events without considering tenant scope, schema version, outbox/consumer dedup state and external side effects.

## Migration safety

Pre-deploy: verify backup/restore capability appropriate to environment, migration lock/concurrency behavior, expected duration/table rewrite risk and forward/backward binary compatibility. Large backfills are resumable/idempotent, bounded by key/range and observable.

## Recovery decision

For data corruption: stop the writer, identify first bad change and affected rows/tenants, preserve snapshot/evidence, choose targeted repair vs point-in-time recovery, reconcile outbox/integration effects, validate invariants and audit the repair. Full rollback is not automatically safer than forward repair after external events were emitted.

Recovery verification includes application-level invariants, not only database availability.


## Replay/reconciliation

After restoring data to an earlier point, reconcile outbox messages, consumer dedup records, integration provider side effects and object/search/cache projections. Replaying only the database while external provider operations already happened can duplicate effects. Prefer bounded reconciliation keyed by stable operation/event identity.

## Verification checklist

Check schema/migration history, tenant/RLS policies, critical aggregate invariants/counts, recent writes around the failure window, background backlog and representative read/write flows. Document any permanently lost interval or manually repaired rows with incident/audit evidence rather than silently normalizing counts.
