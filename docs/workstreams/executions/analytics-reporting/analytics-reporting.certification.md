---
document_id: WRK-CERT-ANALYTICS-REPORTING
document_type: workstream-certification
status: active
owner: analytics-reporting-team
applies_to: [backend, analytics, reporting, p6, certification]
evidence:
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.tests.md
review_on: [p6-candidate-change, certification-evidence-change]
---

# CERTIFICATION — P6 Analytics & Reporting

## Candidate

```text
Branch:
Candidate SHA:
Source contracts:
Replay mechanism D5:
Observability D4+:
Migration head:
CI:
Reviewer:
Decision date:
```

Initial status: NOT_EVALUATED.

## Foundation matrix

| Capability | Required | Actual | Evidence |
|---|---|---|---|
| source inventory | verified | NOT_EVALUATED | |
| Metric definitions | D5 per released metric | NOT_EVALUATED | |
| projection foundation | D5 | NOT_EVALUATED | |
| idempotency/replay | D5 | NOT_EVALUATED | |
| authorization/privacy | D5 | NOT_EVALUATED | |
| freshness/completeness | D4+ | NOT_EVALUATED | |

## Source projections

Populate independently:

| Source | Semantic readiness | Reporting/event contract | Projection | Evidence |
|---|---|---|---|---|
| WorkManagement | NOT_EVALUATED | NOT_EVALUATED | NOT_EVALUATED | |
| Documents | NOT_EVALUATED | NOT_EVALUATED | NOT_EVALUATED | |
| Collaboration | NOT_EVALUATED | NOT_EVALUATED | NOT_EVALUATED | |
| Automation | NOT_EVALUATED | NOT_EVALUATED | NOT_EVALUATED | |
| Integrations | NOT_EVALUATED | NOT_EVALUATED | NOT_EVALUATED | |
| Billing | NOT_EVALUATED | NOT_EVALUATED | NOT_EVALUATED | |

## Reporting matrix

| Capability | Actual | Evidence |
|---|---|---|
| domain reports | NOT_EVALUATED | |
| cross-context reports | NOT_EVALUATED | |
| dashboard/widgets | NOT_EVALUATED | |
| snapshots | NOT_EVALUATED | |
| export | NOT_EVALUATED | |

## Critical assertions

- Analytics remains derived state: NOT_EVALUATED
- no private-table semantic coupling: NOT_EVALUATED
- replay/rebuild correctness: NOT_EVALUATED
- no double count: NOT_EVALUATED
- tenant/privacy isolation: NOT_EVALUATED
- stale/partial semantics explicit: NOT_EVALUATED
- export auth parity: NOT_EVALUATED

## Final decision

```text
P6:
Certified metrics:
Certified source projections:
Cross-context readiness:
Critical blockers:
Known debt:
Freshness/performance risks:
Reviewer:
Decision date:
```

No metric reaches VERIFIED/STABLE from implementation presence alone.
