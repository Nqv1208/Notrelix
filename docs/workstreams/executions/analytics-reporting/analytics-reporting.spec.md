---
document_id: WRK-SPEC-ANALYTICS-REPORTING
document_type: workstream-specification
status: active
owner: analytics-reporting-team
applies_to: [backend, analytics, reporting, p6, metrics, projections, dashboards, exports]
evidence:
  - docs/product/analytics.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/analytics-reporting.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
review_on: [p6-scope-change, metric-semantics-change, projection-contract-change, reporting-authz-change]
---

# SPEC — P6 Analytics & Reporting

## 1. Objective

P6 delivers derived analytical state and reporting from explicit source-owned business facts.

Canonical direction:

```text
source-owned fact/event/reporting contract
→ Analytics projection
→ versioned Metric
→ Dashboard/Report/Export
```

Analytics never becomes owner of the underlying business transaction.

## 2. Entry gate

A metric/report may begin when:

```text
source fact semantics = D5
source event/reporting contract = D4+
authorization scope = D4+
```

Cross-context reports require every participating source projection to reach D4+.

## 3. Source inventory

Every projection declares:

- source bounded context;
- source fact/event/query contract;
- logical identity;
- tenant/workspace/resource scope;
- source semantic version;
- ordering/idempotency assumptions;
- correction/deletion behavior;
- freshness expectation.

Analytics does not start from “which tables can we join?”

## 4. Metric

Metric is versioned business semantics.

Required definition includes:

- stable Metric identity;
- numerator/denominator;
- unit;
- scope;
- time basis/timezone when relevant;
- null/zero/unknown behavior;
- inclusion/exclusion rules;
- freshness;
- compatibility/versioning.

A visually precise number requires semantically precise meaning.

## 5. Projection

Projection is derived state.

Required properties:

- idempotent apply;
- replay;
- rebuild;
- duplicate/out-of-order handling;
- correction handling;
- source deletion semantics;
- progress/checkpoint identity;
- no double-count during backfill + live traffic.

A projection may be discarded/rebuilt where the product contract permits; source facts remain authoritative.

## 6. Cross-context projection

Cross-context reports combine projections/contracts, not private persistence ownership.

No Analytics code may normalize direct joins across source private tables into a stable business contract.

## 7. Dashboard and Widget

Dashboard owns visualization configuration.

Widget configuration is typed and validated against Metric/source semantics.

Dashboard visibility does not bypass source authorization.

Layout/order changes never mutate source context order.

## 8. Authorization and privacy

Aggregation does not erase confidentiality.

Required controls:

- explicit Account/Workspace scope;
- source permission semantics or approved analytical authorization facts;
- drill-down authorization independent of aggregate visibility;
- prevention of cross-tenant leakage;
- small-group/privacy controls where relevant;
- authorization-sensitive cache separation.

## 9. Freshness/completeness

Every user-visible analytical result must expose or internally enforce meaningful freshness semantics.

Multi-source reports must distinguish:

- fresh;
- stale;
- partial;
- unavailable;
- unauthorized.

Unknown/unauthorized is not zero.

## 10. Snapshots

Reporting Snapshot is captured derived truth for a defined purpose/time.

Snapshot schema is versioned and migration-aware.

A historical snapshot may legitimately differ from a rebuilt current projection if source semantics/version changed; this must be explicit.

## 11. Export

Export uses the same Metric, authorization and scope semantics as interactive reporting.

Export does not create a bypass for hidden rows/fields.

## 12. Performance

Enterprise analytical paths must not full-scan arbitrary flexible JSON per dashboard request.

Projection/materialization/index strategy must follow measured workload and source semantics.

## 13. Events/replay

Before P6 hardening:

- event delivery/replay mechanism D5;
- observability D4+.

Replay identity and projection checkpoint semantics must be stable.

## 14. Exit gate

P6 is stable when each released Metric has explicit semantic ownership, projection correctness/rebuild evidence, tenant/privacy controls, freshness semantics and contract-compatible report/export surfaces.
