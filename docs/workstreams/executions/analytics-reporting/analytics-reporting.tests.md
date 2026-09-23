---
document_id: WRK-TESTS-ANALYTICS-REPORTING
document_type: workstream-test-plan
status: active
owner: analytics-reporting-team
applies_to: [backend, analytics, reporting, p6, testing]
evidence:
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md
review_on: [p6-requirement-change]
---

# TESTS — P6 Analytics & Reporting

## Source/Metric

- `ANA-TST-SRC-001` projection references approved source contract, not private table contract.
- `ANA-TST-MET-001` Metric definition has stable identity/version.
- `ANA-TST-MET-002` numerator/denominator/unit semantics deterministic.
- `ANA-TST-MET-003` null/zero/unknown/unauthorized remain distinct.
- `ANA-TST-MET-TIME-001` timezone/time-window semantics.

## Projection correctness

- `ANA-TST-PRJ-IDEMP-001` duplicate source event does not double-count.
- `ANA-TST-PRJ-ORDER-001` late/out-of-order source fact converges correctly.
- `ANA-TST-PRJ-REPLAY-001` replay rebuild matches expected derived state.
- `ANA-TST-PRJ-RESTART-001` restart from checkpoint preserves correctness.
- `ANA-TST-PRJ-CORR-001` source correction updates projection correctly.
- `ANA-TST-PRJ-DEL-001` source deletion follows explicit semantics.
- `ANA-TST-PRJ-BF-001` backfill + live traffic does not double-count.

## Cross-context

- `ANA-TST-X-001` each participating projection is D4+.
- `ANA-TST-X-002` temporal consistency/partial result behavior explicit.
- `ANA-TST-X-003` authorization intersection does not broaden access.
- `ANA-TST-X-ARCH-001` no permanent private-table cross-context join contract.

## Security/privacy

- `ANA-TST-SEC-001` cross-tenant report leakage denied.
- `ANA-TST-SEC-002` aggregate visibility does not grant drill-down visibility.
- `ANA-TST-SEC-003` export preserves source authorization.
- `ANA-TST-SEC-004` authorization-sensitive cache keys include required scope/version.
- `ANA-TST-PRIV-001` small-group/privacy rule where applicable.

## Freshness/completeness

- `ANA-TST-FRESH-001` stale projection is distinguishable.
- `ANA-TST-FRESH-002` multi-source partial result is explicit.
- `ANA-TST-FRESH-003` unavailable source is not silently treated as zero.

## Snapshot/export

- `ANA-TST-SNAP-001` snapshot captures semantic/schema version.
- `ANA-TST-SNAP-MIG-001` snapshot schema migration/compatibility.
- `ANA-TST-EXP-001` export values match same Metric contract.
- `ANA-TST-EXP-AUTHZ-001` export cannot bypass hidden data.

## Performance/reliability

- `ANA-TST-PERF-001` representative dashboard avoids full arbitrary JSON scan.
- `ANA-TST-PERF-002` projection throughput/lag within declared target.
- `ANA-TST-REL-001` poison source event is diagnosable and does not silently corrupt checkpoint.
- `ANA-TST-OBS-001` projection lag/replay/correction observable.

## Migration/API

- `ANA-TST-MIG-001` clean DB.
- `ANA-TST-MIG-002` supported upgrade.
- `ANA-TST-MIG-003` no pending model drift.
- `ANA-TST-API-001` report contract deterministic.
- `ANA-TST-OAS-001` OpenAPI drift intentional.

## Certification rule

Each Metric is certified against named source versions. A dashboard containing an uncertified source remains partial/BLOCKED even if other widgets pass.
