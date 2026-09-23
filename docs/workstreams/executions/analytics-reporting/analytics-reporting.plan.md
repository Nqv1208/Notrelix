---
document_id: WRK-PLAN-ANALYTICS-REPORTING
document_type: workstream-plan
status: active
owner: analytics-reporting-team
applies_to: [backend, analytics, reporting, p6]
evidence:
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/analytics-reporting.md
review_on: [p6-sequence-change, source-readiness-change]
---

# PLAN — P6 Analytics & Reporting

## 1. Execution order

```text
Phase 0 source/readiness inventory
Phase 1 Metric definitions
Phase 2 projection foundation
Phase 3 idempotency/replay/checkpoint
Phase 4 first domain projection
Phase 5 additional domain projections
Phase 6 cross-context projection
Phase 7 Report/Dashboard API
Phase 8 snapshot/export
Phase 9 privacy/performance/rebuild hardening
Phase 10 certification
```

## 2. Phase 0 — Source inventory

For every intended report list:

- source context;
- D5 semantic fact;
- D4+ event/reporting contract;
- authorization scope;
- historical/correction behavior;
- replay availability.

Sources that do not meet gate remain BLOCKED, not guessed from private tables.

## 3. Phase 1 — Metrics

Define Metric registry/vocabulary before building dashboards.

Preferred PR: `PR-ANA-01 metric definitions`.

## 4. Phase 2 — Projection foundation

Build generic mechanism only for demonstrated needs:

- projection identity/version;
- checkpoint;
- idempotent apply;
- rebuild lifecycle;
- lag/freshness observation.

Do not create a generic analytics platform larger than current source requirements.

Preferred PR: `PR-ANA-02 projection foundation`.

## 5. Phase 3 — Replay/idempotency

Prove duplicate, late, out-of-order, restart and replay behavior before broad source onboarding.

## 6. Phase 4 — First source projection

Choose one source with D5 semantics and D4+ reporting contract.

Implement end-to-end projection → metric → API.

Preferred PR: `PR-ANA-03 first source`.

## 7. Phase 5 — Additional projections

Onboard sources independently. One stable source does not certify another.

## 8. Phase 6 — Cross-context

Only after all participating source projections are D4+.

Define:

- join key semantics;
- temporal consistency;
- partial/freshness behavior;
- authorization intersection;
- correction propagation.

Preferred PR: `PR-ANA-04 cross-context`.

## 9. Phase 7 — Report/Dashboard API

Expose typed metric/query contracts, pagination and privacy-safe error semantics.

## 10. Phase 8 — Snapshot/export

Define snapshot version/retention and asynchronous export where payload size requires it.

Export reuses exact authorization/metric semantics.

## 11. Phase 9 — Hardening

Run:

- full replay/rebuild;
- backfill + live overlap;
- tenant-isolation/security;
- small-group/privacy cases;
- large-volume query/load;
- stale/partial source simulation;
- migration/version compatibility.

## 12. Stop conditions

Stop if:

- metric meaning is inferred from table shape;
- cross-context direct join becomes permanent semantic contract;
- source event lacks stable identity/semantics;
- replay cannot avoid double counting;
- authorization is weaker for aggregate/export;
- stale/partial data is presented as exact current truth.
