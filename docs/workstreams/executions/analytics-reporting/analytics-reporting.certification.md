---
document_id: WRK-CERT-ANALYTICS-REPORTING
document_type: workstream-certification
status: active
owner: analytics-reporting-team
applies_to: [backend, frontend, analytics, reporting, metrics, projections, dashboards, snapshots, exports, migrations, certification]
evidence:
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.tests.md
  - docs/product/analytics.md
  - docs/workstreams/teams/analytics-reporting.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/executions/backend-team-architecture-closure/
review_on: [accepted-candidate-sha-change, source-contract-change, metric-semantic-version-change, projection-runtime-change, dashboard-widget-contract-change, analytics-security-or-privacy-change, freshness-or-realtime-change, snapshot-or-export-change, migration-or-rls-change, runtime-owner-or-ci-change]
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# CERTIFICATION — P6 Analytics & Reporting

## 1. Certification model

P6 is certified by **released slice**, never by implementation presence or one global maturity score.

```text
approved source contract
+ Metric/report semantics
+ projection/query correctness
+ authorization/privacy
+ freshness/completeness
+ migration/compatibility
+ required API/frontend/export surface
+ production-runtime proof
+ exact candidate SHA
```

The Work placement projection is the canonical reference mechanism. It does not certify generic Metrics, Dashboards, cross-context reports, exports, frontend Analytics, Snapshots or another source context.

## 2. Status vocabulary

```text
Preparation/source posture:
  IMPLEMENTED_REFERENCE
  IMPLEMENTED_INTERNAL
  PARTIAL
  DOMAIN_PERSISTENCE_ONLY
  INFRASTRUCTURE_ONLY
  MISSING_TARGET
  LEGACY_GAP
  SOURCE_CONTRACT_PARTIAL
  BLOCKED_BY_SOURCE_CONTRACT
  DEFER_UNLESS_RELEASED

Certification:
  NOT_EVALUATED
  BLOCKED
  VERIFIED (D4)
  STABLE (D5)
  DEFERRED
  NOT_APPLICABLE
  STALE
```

Preparation status is not certification.

## 3. Exact-candidate evidence

```text
Preparation baseline SHA: 902bc9c5b39a003df3dce6673edc124984d2b398
Accepted certification SHA: NOT_RECORDED
Final P6 certification: NOT_EVALUATED
```

Every final record must include non-zero execution where runnable, actual runtime owner where claimed, failure/negative evidence, tenant/privacy evidence, migration/compatibility evidence, durable post-condition and exact CI/job/artifact locators.

Evidence is invalid when it is zero-execution, from another SHA, fixture-only for a missing runtime, unit-only for a production composition claim, source-specific proof borrowed from another source, or a stale/partial result presented as exact current truth.

## 4. Capability readiness matrix

| Capability | Preparation posture | Required target | Certification record |
|---|---|---|---|
| Authority, brownfield posture and release governance | `PREPARATION_STRONG / NOT_EVALUATED` | `D5` | `AR-CERT-GOV-001` |
| Source contracts and producer-owned inputs | `PARTIAL / SOURCE_SPECIFIC` | `D5 per released source contract` | `AR-CERT-SRC-001` |
| Metric registry and semantic definitions | `MISSING_TARGET` | `D5 per released Metric` | `AR-CERT-MET-001` |
| Projection foundation and Analytics persistence abstraction | `PARTIAL / IMPLEMENTATION_DEBT` | `D5` | `AR-CERT-PRJ-001` |
| WorkManagement placement reference projection | `IMPLEMENTED_REFERENCE / NOT_EVALUATED` | `D5` | `AR-CERT-WORK-001` |
| Dashboard, Source and Widget lifecycle | `DOMAIN_PERSISTENCE_ONLY / GAP_CONFIRMED` | `D5 if released` | `AR-CERT-DASH-001` |
| Authorization, tenancy, RLS and privacy | `MECHANISM_PARTIAL / NOT_EVALUATED` | `D5` | `AR-CERT-SEC-001` |
| Freshness, completeness and realtime posture | `PARTIAL / QUERY_REFETCH_INITIAL` | `D4+; D5 when interpretation depends on freshness` | `AR-CERT-FRESH-001` |
| ReportingSnapshot artifact, history and retention | `LEGACY_GAP / PARTIAL` | `D4+; D5 if historical reporting is released` | `AR-CERT-SNAP-001` |
| Transactional source-context onboarding | `SOURCE_SPECIFIC / PARTIAL` | `D4+ or D5 per source criticality` | `AR-CERT-ONBOARD-001` |
| Cross-context report composition | `MISSING_TARGET` | `D4+ per released report` | `AR-CERT-XREP-001` |
| Report/Dashboard API and frontend consumer | `MISSING_TARGET / FRONTEND_ABSENT` | `D5 producer public contract + D4+ frontend` | `AR-CERT-API-001` |
| Export and generated report artifacts | `MISSING_TARGET / DEFER_UNLESS_RELEASED` | `D4+ per released format/job` | `AR-CERT-EXP-001` |
| Performance, cache, data quality and observability | `PARTIAL` | `D4+; critical reconciliation D5` | `AR-CERT-OPS-001` |
| Migration, RLS and compatibility | `MECHANISM_EXISTS / CANDIDATE_SPECIFIC` | `D5` | `AR-CERT-MIG-001` |
| Architecture, production runtime, CI and final release | `NOT_EVALUATED` | `D5` | `AR-CERT-FINAL-001` |

## 5. Source-specific readiness ledger

| Source | Preparation state | Current/target contract | Primary proof | Target | Current certification |
|---|---|---|---|---|---|
| WorkManagement | `REFERENCE_READY / NOT_EVALUATED` | BoardItemMoved/Created/Archived V2 + producer-owned projection source | `AR-TST-REQ-033..040`, `073`, `080` | D5 | `NOT_EVALUATED` |
| Workspace / Account / Identity | `SOURCE_CONTRACT_PARTIAL` | AccountCreated + WorkspaceCreated + WorkspaceMemberAdded/Removed or approved producer reporting contract; Analytics never owns membership | `AR-TST-REQ-079`, `080` | D4+ per admitted Metric; Identity-sensitive use D5 security | `NOT_EVALUATED` |
| Documents | `SOURCE_CONTRACT_PARTIAL` | metadata/security-safe Documents facts only unless richer content is admitted | `AR-TST-REQ-074`, `080` | D4+ per admitted Metric | `NOT_EVALUATED` |
| Collaboration | `SOURCE_CONTRACT_PARTIAL` | Collaboration facts; Activity/Audit remain separate | `AR-TST-REQ-075`, `080` | D4+ per admitted Metric | `NOT_EVALUATED` |
| Automation | `BLOCKED_BY_P5_PUBLIC_FACTS` | P5 public execution semantics required before product analytics | `AR-TST-REQ-076`, `080` | D4+ after P5 dependency | `BLOCKED` |
| Integrations | `SOURCE_CONTRACT_INCOMPLETE` | provider-safe connection/sync/health facts without credentials/raw payload | `AR-TST-REQ-077`, `080` | D4+ per admitted Metric | `NOT_EVALUATED` |
| Billing | `SOURCE_CONTRACT_PARTIAL` | Billing-owned reporting facts; no commercial authority transfer | `AR-TST-REQ-078`, `080` | D4+ per admitted Metric | `NOT_EVALUATED` |

Source readiness is independent. Work D5 cannot promote any other source.

### 5.1. Per-source exact-candidate execution record

Each row is completed independently:

```text
Source:
Candidate SHA:
Source→Analytics handoff:
Metric handoff:
Producer contract/version:
Current readiness:
Required readiness:
Primary AR-TST:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Security/privacy evidence:
Replay/backfill/rebuild evidence:
Compatibility evidence:
Evidence locator:
Known blocker:
Invalidated by:
Reviewer:
Decision timestamp:
Certification:
```

The frozen initial Metric/report uses WorkManagement only. Every other source remains non-released until its own record reaches target readiness.

## 6. PLAN gate routing

| PLAN gate | Scope | Required certification records | Gate rule |
|---|---|---|---|
| `AR-GATE-001` | `ARREQ001–ARREQ040` | `AR-CERT-GOV-001`, `AR-CERT-SRC-001`, `AR-CERT-MET-001`, `AR-CERT-PRJ-001`, `AR-CERT-WORK-001` | authority/source/Metric/projection foundation and Work reference satisfy their targets |
| `AR-GATE-002` | `ARREQ041–ARREQ072` | `AR-CERT-DASH-001`, `AR-CERT-SEC-001`, `AR-CERT-FRESH-001`, `AR-CERT-SNAP-001` | released Dashboard/security/freshness/history are safe and honest |
| `AR-GATE-003` | `ARREQ073–ARREQ104` | `AR-CERT-ONBOARD-001`, `AR-CERT-XREP-001`, `AR-CERT-API-001`, `AR-CERT-EXP-001` | source onboarding and frozen API/frontend slice pass; cross-context/export/scheduled delivery remain deferred unless admitted |
| `AR-GATE-004` | `ARREQ105–ARREQ128` | `AR-CERT-OPS-001`, `AR-CERT-MIG-001`, `AR-CERT-FINAL-001` | performance/data-quality, migration/RLS/compatibility and final production evidence pass |

No aggregate gate may be greener than a required child record.

## 7. TAC AR-FLOW certification routing

| Flow | Meaning | ARREQ | Certification records | Final proof rule |
|---|---|---|---|---|
| `AR-FLOW-01` | Live Work placement projection | ARREQ026–ARREQ029, ARREQ033–ARREQ035, ARREQ038 | `AR-CERT-PRJ-001`, `AR-CERT-WORK-001` | `IMPLEMENTED_REFERENCE / exact-candidate rerun` is preparation evidence; rerun when event/revision/adapter/persistence/RLS/messaging/rebuild boundary changes and never use it to certify unrelated P6 product surfaces. |
| `AR-FLOW-02` | Analytics local read | ARREQ030, ARREQ036, ARREQ039 | `AR-CERT-PRJ-001`, `AR-CERT-WORK-001` | `IMPLEMENTED_INTERNAL / exact-candidate rerun` is preparation evidence; rerun when event/revision/adapter/persistence/RLS/messaging/rebuild boundary changes and never use it to certify unrelated P6 product surfaces. |
| `AR-FLOW-03` | Producer-backed rebuild | ARREQ012, ARREQ031–ARREQ032, ARREQ035, ARREQ037 | `AR-CERT-SRC-001`, `AR-CERT-PRJ-001`, `AR-CERT-WORK-001` | `IMPLEMENTED_REFERENCE / exact-candidate rerun` is preparation evidence; rerun when event/revision/adapter/persistence/RLS/messaging/rebuild boundary changes and never use it to certify unrelated P6 product surfaces. |
| `AR-FLOW-04` | Projection failure and recovery | ARREQ029, ARREQ032, ARREQ038, ARREQ124 | `AR-CERT-PRJ-001`, `AR-CERT-WORK-001`, `AR-CERT-FINAL-001` | `IMPLEMENTED_REFERENCE / exact-candidate rerun` is preparation evidence; rerun when event/revision/adapter/persistence/RLS/messaging/rebuild boundary changes and never use it to certify unrelated P6 product surfaces. |

## 8. Confirmed-gap routing

| Gap | Status | Requirements | Certification records | Closure rule |
|---|---|---|---|---|
| `AR-GAP-01` — No generic Metric registry/runtime | `CONFIRMED` | ARREQ017–ARREQ024 | `AR-CERT-MET-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-02` — Dashboard/Widget is Domain+persistence only | `CONFIRMED` | ARREQ041–ARREQ048, ARREQ090 | `AR-CERT-DASH-001`, `AR-CERT-API-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-03` — Dashboard ownership/visibility contract incomplete | `CONFIRMED` | ARREQ042–ARREQ043, ARREQ049–ARREQ056 | `AR-CERT-DASH-001`, `AR-CERT-SEC-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-04` — Dashboard Source/Widget config is only partially semantic | `CONFIRMED` | ARREQ045–ARREQ047, ARREQ116 | `AR-CERT-DASH-001`, `AR-CERT-MIG-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-05` — Duplicate widget vocabularies | `CONFIRMED` | ARREQ046, ARREQ116 | `AR-CERT-DASH-001`, `AR-CERT-MIG-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-06` — ReportingSnapshot is an architecture LegacyGap | `CONFIRMED` | ARREQ065–ARREQ072 | `AR-CERT-SNAP-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-07` — Infrastructure daily usage models have no proven P6 producer | `CONFIRMED` | ARREQ079 | `AR-CERT-ONBOARD-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-08` — Only Work placement projection is production-hardened reference | `CONFIRMED` | ARREQ033–ARREQ040, ARREQ073–ARREQ080 | `AR-CERT-WORK-001`, `AR-CERT-ONBOARD-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-09` — No released report/Metric/Dashboard API | `CONFIRMED` | ARREQ089–ARREQ091 | `AR-CERT-API-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-10` — No dedicated Analytics frontend surface | `CONFIRMED` | ARREQ092–ARREQ096 | `AR-CERT-API-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-11` — No export/report job pipeline | `CONFIRMED` | ARREQ097–ARREQ104 | `AR-CERT-EXP-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-12` — No cross-context report composition runtime | `CONFIRMED` | ARREQ081–ARREQ088 | `AR-CERT-XREP-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-13` — Freshness/completeness contract not exposed end to end | `PARTIAL_GAP` | ARREQ057–ARREQ060 | `AR-CERT-FRESH-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-14` — No Analytics realtime producer/consumer contract | `CONFIRMED` | ARREQ062–ARREQ064 | `AR-CERT-FRESH-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-15` — Retention/deletion/privacy lifecycle not executable for reporting artifacts | `PARTIAL_GAP` | ARREQ071–ARREQ072, ARREQ055 | `AR-CERT-SEC-001`, `AR-CERT-SNAP-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-16` — Analytics-specific data-quality/ops signals incomplete | `PARTIAL_GAP` | ARREQ109–ARREQ112 | `AR-CERT-OPS-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-17` — Historical TAC status is mechanism evidence, not product readiness | `DOC_STALE_RISK` | ARREQ040, ARREQ123–ARREQ128 | `AR-CERT-WORK-001`, `AR-CERT-FINAL-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |
| `AR-GAP-18` — Existing execution docs are materially under-specified | `CONFIRMED` | ARREQ003–ARREQ008, ARREQ125–ARREQ128 | `AR-CERT-GOV-001`, `AR-CERT-FINAL-001` | close with exact-candidate executable proof or keep affected capability `BLOCKED`/`DEFERRED`; type/source existence alone is insufficient |

## 9. Acceptance-criterion routing

| Acceptance criterion | Meaning | Certification records |
|---|---|---|
| `ARAC001` | Derived-state ownership | `AR-CERT-GOV-001`, `AR-CERT-FINAL-001` |
| `ARAC002` | Source-contract inventory | `AR-CERT-SRC-001` |
| `ARAC003` | Metric authority | `AR-CERT-MET-001` |
| `ARAC004` | Projection correctness | `AR-CERT-PRJ-001`, `AR-CERT-WORK-001` |
| `ARAC005` | Work placement reference | `AR-CERT-WORK-001` |
| `ARAC006` | Dashboard lifecycle | `AR-CERT-DASH-001` |
| `ARAC007` | Widget/source contract | `AR-CERT-DASH-001`, `AR-CERT-MET-001` |
| `ARAC008` | Tenant/security/privacy | `AR-CERT-SEC-001` |
| `ARAC009` | Freshness/completeness | `AR-CERT-FRESH-001`, `AR-CERT-XREP-001` |
| `ARAC010` | Snapshot semantics | `AR-CERT-SNAP-001` |
| `ARAC011` | Source onboarding | `AR-CERT-ONBOARD-001` |
| `ARAC012` | Cross-context composition | `AR-CERT-XREP-001` |
| `ARAC013` | API/frontend parity | `AR-CERT-API-001` |
| `ARAC014` | Export parity | `AR-CERT-EXP-001` |
| `ARAC015` | Performance/data quality | `AR-CERT-OPS-001` |
| `ARAC016` | Migration/compatibility | `AR-CERT-MIG-001` |
| `ARAC017` | Architecture/runtime proof | `AR-CERT-PRJ-001`, `AR-CERT-FINAL-001` |
| `ARAC018` | Exact-candidate release | `AR-CERT-FINAL-001` |

## 10. Product-rule certification routing

| Product rule | ARREQ coverage | Certification records |
|---|---|---|
| `ANA-001` | ARREQ001, ARREQ025, ARREQ065 | `AR-CERT-GOV-001`, `AR-CERT-PRJ-001`, `AR-CERT-SNAP-001` |
| `ANA-002` | ARREQ002, ARREQ048, ARREQ091 | `AR-CERT-GOV-001`, `AR-CERT-DASH-001`, `AR-CERT-API-001` |
| `ANA-003` | ARREQ017, ARREQ024, ARREQ115 | `AR-CERT-MET-001`, `AR-CERT-MIG-001` |
| `ANA-004` | ARREQ018 | `AR-CERT-MET-001` |
| `ANA-005` | ARREQ009–ARREQ016 | `AR-CERT-SRC-001` |
| `ANA-006` | ARREQ041, ARREQ044, ARREQ048 | `AR-CERT-DASH-001` |
| `ANA-007` | ARREQ041–ARREQ043, ARREQ049 | `AR-CERT-DASH-001`, `AR-CERT-SEC-001` |
| `ANA-008` | ARREQ043, ARREQ050–ARREQ052 | `AR-CERT-DASH-001`, `AR-CERT-SEC-001` |
| `ANA-009` | ARREQ011–ARREQ012, ARREQ045 | `AR-CERT-SRC-001`, `AR-CERT-DASH-001` |
| `ANA-010` | ARREQ046–ARREQ047 | `AR-CERT-DASH-001` |
| `ANA-011` | ARREQ048 | `AR-CERT-DASH-001` |
| `ANA-012` | ARREQ020 | `AR-CERT-MET-001` |
| `ANA-013` | ARREQ021 | `AR-CERT-MET-001` |
| `ANA-014` | ARREQ057–ARREQ060 | `AR-CERT-FRESH-001` |
| `ANA-015` | ARREQ065, ARREQ067 | `AR-CERT-SNAP-001` |
| `ANA-016` | ARREQ067–ARREQ070, ARREQ119 | `AR-CERT-SNAP-001`, `AR-CERT-MIG-001` |
| `ANA-017` | ARREQ013, ARREQ071–ARREQ072 | `AR-CERT-SRC-001`, `AR-CERT-SNAP-001` |
| `ANA-018` | ARREQ025 | `AR-CERT-PRJ-001` |
| `ANA-019` | ARREQ031, ARREQ037 | `AR-CERT-PRJ-001`, `AR-CERT-WORK-001` |
| `ANA-020` | ARREQ032 | `AR-CERT-PRJ-001` |
| `ANA-021` | ARREQ105–ARREQ107 | `AR-CERT-OPS-001` |
| `ANA-022` | ARREQ078 | `AR-CERT-ONBOARD-001` |
| `ANA-023` | ARREQ014, ARREQ052, ARREQ055 | `AR-CERT-SRC-001`, `AR-CERT-SEC-001` |
| `ANA-024` | ARREQ053 | `AR-CERT-SEC-001` |
| `ANA-025` | ARREQ054, ARREQ108 | `AR-CERT-SEC-001`, `AR-CERT-OPS-001` |
| `ANA-026` | ARREQ097–ARREQ103 | `AR-CERT-EXP-001` |
| `ANA-027` | ARREQ019–ARREQ021 | `AR-CERT-MET-001` |
| `ANA-028` | ARREQ024, ARREQ070, ARREQ115 | `AR-CERT-MET-001`, `AR-CERT-SNAP-001`, `AR-CERT-MIG-001` |
| `ANA-029` | ARREQ023, ARREQ059, ARREQ096 | `AR-CERT-MET-001`, `AR-CERT-FRESH-001`, `AR-CERT-API-001` |
| `ANA-030` | ARREQ060, ARREQ083, ARREQ086 | `AR-CERT-FRESH-001`, `AR-CERT-XREP-001` |
| `ANA-031` | ARREQ051 | `AR-CERT-SEC-001` |
| `ANA-032` | ARREQ045–ARREQ047 | `AR-CERT-DASH-001` |
| `ANA-033` | ARREQ044 | `AR-CERT-DASH-001` |
| `ANA-034` | ARREQ004–ARREQ005, ARREQ128 | `AR-CERT-GOV-001`, `AR-CERT-FINAL-001` |
| `ANA-035` | ARREQ112 | `AR-CERT-OPS-001` |
| `ANA-036` | ARREQ062–ARREQ064 | `AR-CERT-FRESH-001` |
| `ANA-037` | ARREQ055–ARREQ056, ARREQ118 | `AR-CERT-SEC-001`, `AR-CERT-MIG-001` |
| `ANA-038` | ARREQ068–ARREQ070 | `AR-CERT-SNAP-001` |

## 11. Team-capability certification routing

| Team capability | Meaning | ARREQ coverage | Certification records |
|---|---|---|---|
| `team ANA-001` | Source-event/reporting-contract inventory | ARREQ009–ARREQ016 | `AR-CERT-SRC-001` |
| `team ANA-002` | Metric definition registry | ARREQ017–ARREQ024 | `AR-CERT-MET-001` |
| `team ANA-003` | Analytical projection foundation | ARREQ025–ARREQ032 | `AR-CERT-PRJ-001` |
| `team ANA-004` | Projection identity/idempotency | ARREQ026, ARREQ029 | `AR-CERT-PRJ-001` |
| `team ANA-005` | Projection ordering/late-event policy | ARREQ027–ARREQ028 | `AR-CERT-PRJ-001` |
| `team ANA-006` | Projection rebuild/backfill | ARREQ031–ARREQ032 | `AR-CERT-PRJ-001` |
| `team ANA-007` | Account/workspace metrics | ARREQ049, ARREQ073–ARREQ080 | `AR-CERT-SEC-001`, `AR-CERT-ONBOARD-001` |
| `team ANA-008` | WorkManagement analytics | ARREQ033–ARREQ040, ARREQ073 | `AR-CERT-WORK-001`, `AR-CERT-ONBOARD-001` |
| `team ANA-009` | Documents/Collaboration analytics | ARREQ074–ARREQ075 | `AR-CERT-ONBOARD-001` |
| `team ANA-010` | Automation/Integrations analytics | ARREQ076–ARREQ077 | `AR-CERT-ONBOARD-001` |
| `team ANA-011` | Billing/usage analytics | ARREQ078–ARREQ080 | `AR-CERT-ONBOARD-001` |
| `team ANA-012` | Cross-context report composition | ARREQ081–ARREQ088 | `AR-CERT-XREP-001` |
| `team ANA-013` | Report/query API | ARREQ089–ARREQ091 | `AR-CERT-API-001` |
| `team ANA-014` | Dashboard/report frontend | ARREQ092–ARREQ096 | `AR-CERT-API-001` |
| `team ANA-015` | Filters/date/timezone semantics | ARREQ019–ARREQ023, ARREQ089 | `AR-CERT-MET-001`, `AR-CERT-API-001` |
| `team ANA-016` | Freshness/staleness contract | ARREQ057–ARREQ064 | `AR-CERT-FRESH-001` |
| `team ANA-017` | Export | ARREQ097–ARREQ104 | `AR-CERT-EXP-001` |
| `team ANA-018` | Authorization/visibility | ARREQ049–ARREQ056 | `AR-CERT-SEC-001` |
| `team ANA-019` | Retention/data lifecycle | ARREQ065–ARREQ072 | `AR-CERT-SNAP-001` |
| `team ANA-020` | Performance/storage strategy | ARREQ105–ARREQ108 | `AR-CERT-OPS-001` |
| `team ANA-021` | Observability | ARREQ109–ARREQ112 | `AR-CERT-OPS-001` |
| `team ANA-022` | Data quality/reconciliation | ARREQ109–ARREQ110 | `AR-CERT-OPS-001` |
| `team ANA-023` | Migration/versioning | ARREQ113–ARREQ120 | `AR-CERT-MIG-001` |
| `team ANA-024` | Hardening | ARREQ121–ARREQ128 | `AR-CERT-FINAL-001` |

# Capability certification records

## AR-CERT-GOV-001 — Authority, brownfield posture and release governance

**Lane:** `AR-01`  
**Preparation posture:** `PREPARATION_STRONG / NOT_EVALUATED`  
**Required readiness:** `D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ001, ARREQ002, ARREQ003, ARREQ004, ARREQ005, ARREQ006, ARREQ007, ARREQ008
PLAN: AR-01-INV-001, AR-01-CORE-001, AR-01-SEC-001, AR-01-COMPAT-001
TESTS: AR-TST-REQ-001, AR-TST-REQ-002, AR-TST-REQ-003, AR-TST-REQ-004, AR-TST-REQ-005, AR-TST-REQ-006, AR-TST-REQ-007, AR-TST-REQ-008
Legacy ANA-TST: none
```

### Current contract

Source classification, authority precedence, no-overclaim and exact-evidence governance are explicit.

### Target contract

Final readiness is unrecorded; exact candidate and released slice remain unknown.

### Preparation source anchors

- `docs/product/analytics.md`
- `docs/workstreams/teams/analytics-reporting.md`
- `docs/workstreams/cross-team-dependencies.md`
- `analytics-reporting.spec.md`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to authority hierarchy, released-slice, candidate SHA, evidence governance or architecture-expansion policy invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ001, ARREQ002, ARREQ003, ARREQ004, ARREQ005, ARREQ006, ARREQ007, ARREQ008
Tests: AR-TST-REQ-001, AR-TST-REQ-002, AR-TST-REQ-003, AR-TST-REQ-004, AR-TST-REQ-005, AR-TST-REQ-006, AR-TST-REQ-007, AR-TST-REQ-008
Legacy aliases: none
PLAN work units: AR-01-INV-001, AR-01-CORE-001, AR-01-SEC-001, AR-01-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-SRC-001 — Source contracts and producer-owned inputs

**Lane:** `AR-02`  
**Preparation posture:** `PARTIAL / SOURCE_SPECIFIC`  
**Required readiness:** `D5 per released source contract`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ009, ARREQ010, ARREQ011, ARREQ012, ARREQ013, ARREQ014, ARREQ015, ARREQ016
PLAN: AR-02-INV-001, AR-02-CORE-001, AR-02-SEC-001, AR-02-COMPAT-001
TESTS: AR-TST-REQ-009, AR-TST-REQ-010, AR-TST-REQ-011, AR-TST-REQ-012, AR-TST-REQ-013, AR-TST-REQ-014, AR-TST-REQ-015, AR-TST-REQ-016
Legacy ANA-TST: ANA-TST-SRC-001
```

### Current contract

Each source fact has one owner and approved event/reporting contract with scope/version/correction/replay/security semantics.

### Target contract

Only Work reference is strong; remaining sources require independent handoffs.

### Preparation source anchors

- `backend/src/Notrelix.Application/Events/`
- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- `docs/workstreams/teams/analytics-reporting.md`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to producer contract/version, source owner, correction/deletion/replay, privacy classification or producer adapter invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ009, ARREQ010, ARREQ011, ARREQ012, ARREQ013, ARREQ014, ARREQ015, ARREQ016
Tests: AR-TST-REQ-009, AR-TST-REQ-010, AR-TST-REQ-011, AR-TST-REQ-012, AR-TST-REQ-013, AR-TST-REQ-014, AR-TST-REQ-015, AR-TST-REQ-016
Legacy aliases: ANA-TST-SRC-001
PLAN work units: AR-02-INV-001, AR-02-CORE-001, AR-02-SEC-001, AR-02-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-MET-001 — Metric registry and semantic definitions

**Lane:** `AR-03`  
**Preparation posture:** `MISSING_TARGET`  
**Required readiness:** `D5 per released Metric`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ017, ARREQ018, ARREQ019, ARREQ020, ARREQ021, ARREQ022, ARREQ023, ARREQ024
PLAN: AR-03-INV-001, AR-03-CORE-001, AR-03-SEC-001, AR-03-COMPAT-001
TESTS: AR-TST-REQ-017, AR-TST-REQ-018, AR-TST-REQ-019, AR-TST-REQ-020, AR-TST-REQ-021, AR-TST-REQ-022, AR-TST-REQ-023, AR-TST-REQ-024
Legacy ANA-TST: ANA-TST-MET-001, ANA-TST-MET-002, ANA-TST-MET-TIME-001, ANA-TST-PRJ-CORR-001, ANA-TST-MET-003
```

### Current contract

One typed/versioned Metric definition controls source facts, formula, unit, time basis, timezone, null/zero/unknown, correction and historical comparison.

### Target contract

No first-class Metric registry/runtime exists at the preparation baseline.

### Preparation source anchors

- `docs/product/analytics.md`
- `backend/src/Notrelix.Domain/Analytics/ (no Metrics folder at baseline)`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to Metric identity/version/formula/timezone/unit/dimension/privacy/cache semantics invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ017, ARREQ018, ARREQ019, ARREQ020, ARREQ021, ARREQ022, ARREQ023, ARREQ024
Tests: AR-TST-REQ-017, AR-TST-REQ-018, AR-TST-REQ-019, AR-TST-REQ-020, AR-TST-REQ-021, AR-TST-REQ-022, AR-TST-REQ-023, AR-TST-REQ-024
Legacy aliases: ANA-TST-MET-001, ANA-TST-MET-002, ANA-TST-MET-TIME-001, ANA-TST-PRJ-CORR-001, ANA-TST-MET-003
PLAN work units: AR-03-INV-001, AR-03-CORE-001, AR-03-SEC-001, AR-03-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-PRJ-001 — Projection foundation and Analytics persistence abstraction

**Lane:** `AR-04`  
**Preparation posture:** `PARTIAL / IMPLEMENTATION_DEBT`  
**Required readiness:** `D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ025, ARREQ026, ARREQ027, ARREQ028, ARREQ029, ARREQ030, ARREQ031, ARREQ032
PLAN: AR-04-INV-001, AR-04-CORE-001, AR-04-SEC-001, AR-04-COMPAT-001
TESTS: AR-TST-REQ-025, AR-TST-REQ-026, AR-TST-REQ-027, AR-TST-REQ-028, AR-TST-REQ-029, AR-TST-REQ-030, AR-TST-REQ-031, AR-TST-REQ-032
Legacy ANA-TST: ANA-TST-PRJ-IDEMP-001, ANA-TST-PRJ-ORDER-001, ANA-TST-PRJ-RESTART-001, ANA-TST-PRJ-REPLAY-001, ANA-TST-PRJ-BF-001
```

### Current contract

Projections are idempotent, safely ordered, local-read and rebuildable; new Application code uses Analytics-owned repository/query-store contracts.

### Target contract

Application still exposes EF DbSet through IReportingDbContext; general projection foundation is incomplete outside Work reference.

### Preparation source anchors

- `backend/src/Notrelix.Application/Features/Analytics/Abstractions/IReportingDbContext.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to projection identity/order/checkpoint/rebuild/store contract, IReportingDbContext migration, data-session or settlement semantics invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ025, ARREQ026, ARREQ027, ARREQ028, ARREQ029, ARREQ030, ARREQ031, ARREQ032
Tests: AR-TST-REQ-025, AR-TST-REQ-026, AR-TST-REQ-027, AR-TST-REQ-028, AR-TST-REQ-029, AR-TST-REQ-030, AR-TST-REQ-031, AR-TST-REQ-032
Legacy aliases: ANA-TST-PRJ-IDEMP-001, ANA-TST-PRJ-ORDER-001, ANA-TST-PRJ-RESTART-001, ANA-TST-PRJ-REPLAY-001, ANA-TST-PRJ-BF-001
PLAN work units: AR-04-INV-001, AR-04-CORE-001, AR-04-SEC-001, AR-04-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-WORK-001 — WorkManagement placement reference projection

**Lane:** `AR-05`  
**Preparation posture:** `IMPLEMENTED_REFERENCE / NOT_EVALUATED`  
**Required readiness:** `D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ033, ARREQ034, ARREQ035, ARREQ036, ARREQ037, ARREQ038, ARREQ039, ARREQ040
PLAN: AR-05-INV-001, AR-05-CORE-001, AR-05-SEC-001, AR-05-COMPAT-001
TESTS: AR-TST-REQ-033, AR-TST-REQ-034, AR-TST-REQ-035, AR-TST-REQ-036, AR-TST-REQ-037, AR-TST-REQ-038, AR-TST-REQ-039, AR-TST-REQ-040
Legacy ANA-TST: none
```

### Current contract

AR-FLOW-01..04 preserve producer revision ordering, event-sufficient apply, producer public-source fallback, local read, rebuild/live safety and crash recovery.

### Target contract

Strongest P6 reference only; does not certify other sources or product reporting.

### Preparation source anchors

- `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs`
- `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs`
- `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to Work event V2, SourceRevision, producer source API, Analytics consumer/service/query, RLS, broker/dedup or rebuild algorithm invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ033, ARREQ034, ARREQ035, ARREQ036, ARREQ037, ARREQ038, ARREQ039, ARREQ040
Tests: AR-TST-REQ-033, AR-TST-REQ-034, AR-TST-REQ-035, AR-TST-REQ-036, AR-TST-REQ-037, AR-TST-REQ-038, AR-TST-REQ-039, AR-TST-REQ-040
Legacy aliases: none
PLAN work units: AR-05-INV-001, AR-05-CORE-001, AR-05-SEC-001, AR-05-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-DASH-001 — Dashboard, Source and Widget lifecycle

**Lane:** `AR-06`  
**Preparation posture:** `DOMAIN_PERSISTENCE_ONLY / GAP_CONFIRMED`  
**Required readiness:** `D5 if released`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ041, ARREQ042, ARREQ043, ARREQ044, ARREQ045, ARREQ046, ARREQ047, ARREQ048
PLAN: AR-06-INV-001, AR-06-CORE-001, AR-06-SEC-001, AR-06-COMPAT-001
TESTS: AR-TST-REQ-041, AR-TST-REQ-042, AR-TST-REQ-043, AR-TST-REQ-044, AR-TST-REQ-045, AR-TST-REQ-046, AR-TST-REQ-047, AR-TST-REQ-048
Legacy ANA-TST: none
```

### Current contract

Implement canonical Dashboard Application/API lifecycle, explicit private owner, Private+Workspace visibility, approved sources, one widget vocabulary and typed config.

### Target contract

Domain/EF/RLS exist; no Analytics Application/API product surface. Public visibility is unreleased.

### Preparation source anchors

- `backend/src/Notrelix.Domain/Analytics/Dashboards/`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardSourceConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/DashboardWidgetConfiguration.cs`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to Dashboard owner/visibility/status, source kind, widget vocabulary/config, API lifecycle, frontend contract or persisted config migration invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ041, ARREQ042, ARREQ043, ARREQ044, ARREQ045, ARREQ046, ARREQ047, ARREQ048
Tests: AR-TST-REQ-041, AR-TST-REQ-042, AR-TST-REQ-043, AR-TST-REQ-044, AR-TST-REQ-045, AR-TST-REQ-046, AR-TST-REQ-047, AR-TST-REQ-048
Legacy aliases: none
PLAN work units: AR-06-INV-001, AR-06-CORE-001, AR-06-SEC-001, AR-06-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-SEC-001 — Authorization, tenancy, RLS and privacy

**Lane:** `AR-07`  
**Preparation posture:** `MECHANISM_PARTIAL / NOT_EVALUATED`  
**Required readiness:** `D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ049, ARREQ050, ARREQ051, ARREQ052, ARREQ053, ARREQ054, ARREQ055, ARREQ056
PLAN: AR-07-INV-001, AR-07-CORE-001, AR-07-SEC-001, AR-07-COMPAT-001
TESTS: AR-TST-REQ-049, AR-TST-REQ-050, AR-TST-REQ-051, AR-TST-REQ-052, AR-TST-REQ-053, AR-TST-REQ-054, AR-TST-REQ-055, AR-TST-REQ-056
Legacy ANA-TST: ANA-TST-SEC-002, ANA-TST-PRIV-001, ANA-TST-SEC-001, ANA-TST-SEC-004
```

### Current contract

Report/Dashboard access preserves tenant scope, source authorization, aggregate/detail boundaries, privacy suppression and safe cache partition.

### Target contract

Reporting RLS exists; end-to-end product authorization/privacy is incomplete.

### Preparation source anchors

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs`
- `canonical Application pipeline/ResourceRef rules`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to Governance action/resource, RLS/session scope, privacy policy, cache identity, Dashboard ownership/visibility or drill-down contract invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ049, ARREQ050, ARREQ051, ARREQ052, ARREQ053, ARREQ054, ARREQ055, ARREQ056
Tests: AR-TST-REQ-049, AR-TST-REQ-050, AR-TST-REQ-051, AR-TST-REQ-052, AR-TST-REQ-053, AR-TST-REQ-054, AR-TST-REQ-055, AR-TST-REQ-056
Legacy aliases: ANA-TST-SEC-002, ANA-TST-PRIV-001, ANA-TST-SEC-001, ANA-TST-SEC-004
PLAN work units: AR-07-INV-001, AR-07-CORE-001, AR-07-SEC-001, AR-07-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-FRESH-001 — Freshness, completeness and realtime posture

**Lane:** `AR-08`  
**Preparation posture:** `PARTIAL / QUERY_REFETCH_INITIAL`  
**Required readiness:** `D4+; D5 when interpretation depends on freshness`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ057, ARREQ058, ARREQ059, ARREQ060, ARREQ061, ARREQ062, ARREQ063, ARREQ064
PLAN: AR-08-INV-001, AR-08-CORE-001, AR-08-SEC-001, AR-08-COMPAT-001
TESTS: AR-TST-REQ-057, AR-TST-REQ-058, AR-TST-REQ-059, AR-TST-REQ-060, AR-TST-REQ-061, AR-TST-REQ-062, AR-TST-REQ-063, AR-TST-REQ-064
Legacy ANA-TST: ANA-TST-FRESH-001, ANA-TST-FRESH-002
```

### Current contract

Every report declares freshness/cutoff/partial semantics; initial P6 is query/refetch authoritative; realtime remains unreleased until recoverable.

### Target contract

No general report freshness/completeness contract exists.

### Preparation source anchors

- `docs/product/analytics.md ANA-014/030/036`
- `WorkspaceWorkItemPlacementProjection.SourceRevision/LastOccurredAt`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to freshness class, cutoff/degraded metadata, source SLA, realtime producer/recovery or frontend stale-state semantics invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ057, ARREQ058, ARREQ059, ARREQ060, ARREQ061, ARREQ062, ARREQ063, ARREQ064
Tests: AR-TST-REQ-057, AR-TST-REQ-058, AR-TST-REQ-059, AR-TST-REQ-060, AR-TST-REQ-061, AR-TST-REQ-062, AR-TST-REQ-063, AR-TST-REQ-064
Legacy aliases: ANA-TST-FRESH-001, ANA-TST-FRESH-002
PLAN work units: AR-08-INV-001, AR-08-CORE-001, AR-08-SEC-001, AR-08-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-SNAP-001 — ReportingSnapshot artifact, history and retention

**Lane:** `AR-09`  
**Preparation posture:** `LEGACY_GAP / PARTIAL`  
**Required readiness:** `D4+; D5 if historical reporting is released`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ065, ARREQ066, ARREQ067, ARREQ068, ARREQ069, ARREQ070, ARREQ071, ARREQ072
PLAN: AR-09-INV-001, AR-09-CORE-001, AR-09-SEC-001, AR-09-COMPAT-001
TESTS: AR-TST-REQ-065, AR-TST-REQ-066, AR-TST-REQ-067, AR-TST-REQ-068, AR-TST-REQ-069, AR-TST-REQ-070, AR-TST-REQ-071, AR-TST-REQ-072
Legacy ANA-TST: ANA-TST-SNAP-001, ANA-TST-SNAP-MIG-001, ANA-TST-PRJ-DEL-001
```

### Current contract

ReportingSnapshot moves out of Domain LegacyGap and gains report/Metric lineage, source cutoff, version-aware readers and retention/privacy lifecycle.

### Target contract

Current model/persistence exists but is an architecture LegacyGap with insufficient lineage/lifecycle.

### Preparation source anchors

- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportingSnapshot.cs`
- `backend/src/Notrelix.Domain/Analytics/Snapshots/ReportSnapshotPayload.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Analytics/ReportingSnapshotConfiguration.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to snapshot schema, lineage, retention, privacy deletion, artifact reader or Domain boundary invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ065, ARREQ066, ARREQ067, ARREQ068, ARREQ069, ARREQ070, ARREQ071, ARREQ072
Tests: AR-TST-REQ-065, AR-TST-REQ-066, AR-TST-REQ-067, AR-TST-REQ-068, AR-TST-REQ-069, AR-TST-REQ-070, AR-TST-REQ-071, AR-TST-REQ-072
Legacy aliases: ANA-TST-SNAP-001, ANA-TST-SNAP-MIG-001, ANA-TST-PRJ-DEL-001
PLAN work units: AR-09-INV-001, AR-09-CORE-001, AR-09-SEC-001, AR-09-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-ONBOARD-001 — Transactional source-context onboarding

**Lane:** `AR-10`  
**Preparation posture:** `SOURCE_SPECIFIC / PARTIAL`  
**Required readiness:** `D4+ or D5 per source criticality`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ073, ARREQ074, ARREQ075, ARREQ076, ARREQ077, ARREQ078, ARREQ079, ARREQ080
PLAN: AR-10-INV-001, AR-10-CORE-001, AR-10-SEC-001, AR-10-COMPAT-001
TESTS: AR-TST-REQ-073, AR-TST-REQ-074, AR-TST-REQ-075, AR-TST-REQ-076, AR-TST-REQ-077, AR-TST-REQ-078, AR-TST-REQ-079, AR-TST-REQ-080
Legacy ANA-TST: ANA-TST-X-001
```

### Current contract

Each transactional source is admitted independently through approved facts without redefining source authority.

### Target contract

Work is reference-ready; Automation depends on P5 public facts; others remain partial/incomplete.

### Preparation source anchors

- `backend/src/Notrelix.Application/Events/WorkManagement/`
- `backend/src/Notrelix.Application/Events/Documents/`
- `backend/src/Notrelix.Application/Events/Collaboration/`
- `backend/src/Notrelix.Application/Events/Automation/`
- `backend/src/Notrelix.Application/Events/Integrations/`
- `backend/src/Notrelix.Application/Events/Billing/`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to source event/reporting contract, source-team readiness, privacy class, producer owner or Metric handoff invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ073, ARREQ074, ARREQ075, ARREQ076, ARREQ077, ARREQ078, ARREQ079, ARREQ080
Tests: AR-TST-REQ-073, AR-TST-REQ-074, AR-TST-REQ-075, AR-TST-REQ-076, AR-TST-REQ-077, AR-TST-REQ-078, AR-TST-REQ-079, AR-TST-REQ-080
Legacy aliases: ANA-TST-X-001
PLAN work units: AR-10-INV-001, AR-10-CORE-001, AR-10-SEC-001, AR-10-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-XREP-001 — Cross-context report composition

**Lane:** `AR-11`  
**Preparation posture:** `DEFERRED_INITIAL_P6 / MISSING_TARGET`  
**Required readiness:** `DEFERRED / NON_REACHABLE` for initial P6; D4+ per future admitted report  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ081, ARREQ082, ARREQ083, ARREQ084, ARREQ085, ARREQ086, ARREQ087, ARREQ088
PLAN: AR-11-INV-001, AR-11-CORE-001, AR-11-SEC-001, AR-11-COMPAT-001
TESTS: AR-TST-REQ-081, AR-TST-REQ-082, AR-TST-REQ-083, AR-TST-REQ-084, AR-TST-REQ-085, AR-TST-REQ-086, AR-TST-REQ-087, AR-TST-REQ-088
Legacy ANA-TST: ANA-TST-X-ARCH-001, ANA-TST-X-002, ANA-TST-X-003, ANA-TST-FRESH-003
```

### Current contract

Reports compose certified projections/Metric outputs with stable identities, explicit cutoff consistency, authorization intersection, partial behavior and safe cache identity.

### Target contract

No cross-context report composer exists.

### Preparation source anchors

- `docs/product/analytics.md cross-context sections`
- `docs/workstreams/teams/analytics-reporting.md ANA-012`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to participating source/Metric version, join identity, cutoff, authorization, correction or cache identity invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ081, ARREQ082, ARREQ083, ARREQ084, ARREQ085, ARREQ086, ARREQ087, ARREQ088
Tests: AR-TST-REQ-081, AR-TST-REQ-082, AR-TST-REQ-083, AR-TST-REQ-084, AR-TST-REQ-085, AR-TST-REQ-086, AR-TST-REQ-087, AR-TST-REQ-088
Legacy aliases: ANA-TST-X-ARCH-001, ANA-TST-X-002, ANA-TST-X-003, ANA-TST-FRESH-003
PLAN work units: AR-11-INV-001, AR-11-CORE-001, AR-11-SEC-001, AR-11-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-API-001 — Report/Dashboard API and frontend consumer

**Lane:** `AR-12`  
**Preparation posture:** `MISSING_TARGET / FRONTEND_ABSENT`  
**Required readiness:** `D5 producer public contract + D4+ frontend`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ089, ARREQ090, ARREQ091, ARREQ092, ARREQ093, ARREQ094, ARREQ095, ARREQ096
PLAN: AR-12-INV-001, AR-12-CORE-001, AR-12-SEC-001, AR-12-COMPAT-001
TESTS: AR-TST-REQ-089, AR-TST-REQ-090, AR-TST-REQ-091, AR-TST-REQ-092, AR-TST-REQ-093, AR-TST-REQ-094, AR-TST-REQ-095, AR-TST-REQ-096
Legacy ANA-TST: ANA-TST-API-001
```

### Current contract

Typed bounded APIs and a dedicated Analytics frontend consume server-owned Metric semantics with tenant-safe query identity and degraded-state UX.

### Target contract

No Analytics endpoint tree or dedicated Analytics frontend package exists; Workspace dashboard is not P6 evidence.

### Preparation source anchors

- `no Analytics API endpoints at baseline`
- `frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx`
- `frontend/packages/features/workspace/`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to OpenAPI/DTO/query contract, routes, frontend package/query keys, tenant switch or report-state vocabulary invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ089, ARREQ090, ARREQ091, ARREQ092, ARREQ093, ARREQ094, ARREQ095, ARREQ096
Tests: AR-TST-REQ-089, AR-TST-REQ-090, AR-TST-REQ-091, AR-TST-REQ-092, AR-TST-REQ-093, AR-TST-REQ-094, AR-TST-REQ-095, AR-TST-REQ-096
Legacy aliases: ANA-TST-API-001
PLAN work units: AR-12-INV-001, AR-12-CORE-001, AR-12-SEC-001, AR-12-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-EXP-001 — Export and generated report artifacts

**Lane:** `AR-13`  
**Preparation posture:** `DEFERRED_INITIAL_P6 / NON_REACHABLE_TARGET`  
**Required readiness:** `DEFERRED / NON_REACHABLE` for initial P6; D4+ per future export/scheduled-delivery capability  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ097, ARREQ098, ARREQ099, ARREQ100, ARREQ101, ARREQ102, ARREQ103, ARREQ104
PLAN: AR-13-INV-001, AR-13-CORE-001, AR-13-SEC-001, AR-13-COMPAT-001
TESTS: AR-TST-REQ-097, AR-TST-REQ-098, AR-TST-REQ-099, AR-TST-REQ-100, AR-TST-REQ-101, AR-TST-REQ-102, AR-TST-REQ-103, AR-TST-REQ-104
Legacy ANA-TST: ANA-TST-SEC-003, ANA-TST-EXP-001, ANA-TST-EXP-AUTHZ-001
```

### Current contract

Export reuses report semantics/auth/cutoff; large export uses durable job, safe artifact identity/access, retention and idempotent retry.

### Target contract

No export/report-job runtime exists.

### Preparation source anchors

- `docs/product/analytics.md export/report generation semantics`
- `no Analytics export runtime at baseline`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to export format, size threshold, job/artifact identity, storage/download auth, retention or report semantics invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ097, ARREQ098, ARREQ099, ARREQ100, ARREQ101, ARREQ102, ARREQ103, ARREQ104
Tests: AR-TST-REQ-097, AR-TST-REQ-098, AR-TST-REQ-099, AR-TST-REQ-100, AR-TST-REQ-101, AR-TST-REQ-102, AR-TST-REQ-103, AR-TST-REQ-104
Legacy aliases: ANA-TST-SEC-003, ANA-TST-EXP-001, ANA-TST-EXP-AUTHZ-001
PLAN work units: AR-13-INV-001, AR-13-CORE-001, AR-13-SEC-001, AR-13-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-OPS-001 — Performance, cache, data quality and observability

**Lane:** `AR-14`  
**Preparation posture:** `PARTIAL`  
**Required readiness:** `D4+; critical reconciliation D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ105, ARREQ106, ARREQ107, ARREQ108, ARREQ109, ARREQ110, ARREQ111, ARREQ112
PLAN: AR-14-INV-001, AR-14-CORE-001, AR-14-SEC-001, AR-14-COMPAT-001
TESTS: AR-TST-REQ-105, AR-TST-REQ-106, AR-TST-REQ-107, AR-TST-REQ-108, AR-TST-REQ-109, AR-TST-REQ-110, AR-TST-REQ-111, AR-TST-REQ-112
Legacy ANA-TST: ANA-TST-PERF-001, ANA-TST-PERF-002, ANA-TST-OBS-001
```

### Current contract

Queries avoid arbitrary JSON scans, optimizations are measured, cache identity is complete, divergence/reconciliation and lag/backfill/report health are observable.

### Target contract

Reference projection has focused evidence; broad P6 operational quality is incomplete.

### Preparation source anchors

- `docs/quality/performance-and-scalability.md`
- `reporting schema configurations`
- `reference projection tests/logging`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to query/index/materialization/cache, reconciliation source, performance target, observability dimensions or storage strategy invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ105, ARREQ106, ARREQ107, ARREQ108, ARREQ109, ARREQ110, ARREQ111, ARREQ112
Tests: AR-TST-REQ-105, AR-TST-REQ-106, AR-TST-REQ-107, AR-TST-REQ-108, AR-TST-REQ-109, AR-TST-REQ-110, AR-TST-REQ-111, AR-TST-REQ-112
Legacy aliases: ANA-TST-PERF-001, ANA-TST-PERF-002, ANA-TST-OBS-001
PLAN work units: AR-14-INV-001, AR-14-CORE-001, AR-14-SEC-001, AR-14-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-MIG-001 — Migration, RLS and compatibility

**Lane:** `AR-15`  
**Preparation posture:** `MECHANISM_EXISTS / CANDIDATE_SPECIFIC`  
**Required readiness:** `D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ113, ARREQ114, ARREQ115, ARREQ116, ARREQ117, ARREQ118, ARREQ119, ARREQ120
PLAN: AR-15-INV-001, AR-15-CORE-001, AR-15-SEC-001, AR-15-COMPAT-001
TESTS: AR-TST-REQ-113, AR-TST-REQ-114, AR-TST-REQ-115, AR-TST-REQ-116, AR-TST-REQ-117, AR-TST-REQ-118, AR-TST-REQ-119, AR-TST-REQ-120
Legacy ANA-TST: ANA-TST-MIG-001, ANA-TST-MIG-003, ANA-TST-MIG-002, ANA-TST-OAS-001
```

### Current contract

Reporting schema/config/Metric/source/snapshot changes have clean DB, supported upgrade, no drift, safe rebuild/migration, safe RLS deployment and historical compatibility.

### Target contract

Repo migration/RLS mechanisms exist; P6 exact-candidate proof is unrecorded.

### Preparation source anchors

- `backend/src/Notrelix.Infrastructure/Data/Migrations/`
- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `persisted Dashboard/Snapshot/projection configs`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to migration head/model snapshot, reporting schema/RLS, stored config/Metric version, source backlog or rollback/forward-fix policy invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ113, ARREQ114, ARREQ115, ARREQ116, ARREQ117, ARREQ118, ARREQ119, ARREQ120
Tests: AR-TST-REQ-113, AR-TST-REQ-114, AR-TST-REQ-115, AR-TST-REQ-116, AR-TST-REQ-117, AR-TST-REQ-118, AR-TST-REQ-119, AR-TST-REQ-120
Legacy aliases: ANA-TST-MIG-001, ANA-TST-MIG-003, ANA-TST-MIG-002, ANA-TST-OAS-001
PLAN work units: AR-15-INV-001, AR-15-CORE-001, AR-15-SEC-001, AR-15-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-FINAL-001 — Architecture, production runtime, CI and final release

**Lane:** `AR-16`  
**Preparation posture:** `NOT_EVALUATED`  
**Required readiness:** `D5`  
**Certification:** `NOT_EVALUATED`

### Traceability

```text
SPEC: ARREQ121, ARREQ122, ARREQ123, ARREQ124, ARREQ125, ARREQ126, ARREQ127, ARREQ128
PLAN: AR-16-INV-001, AR-16-CORE-001, AR-16-SEC-001, AR-16-COMPAT-001
TESTS: AR-TST-REQ-121, AR-TST-REQ-122, AR-TST-REQ-123, AR-TST-REQ-124, AR-TST-REQ-125, AR-TST-REQ-126, AR-TST-REQ-127, AR-TST-REQ-128
Legacy ANA-TST: ANA-TST-REL-001
```

### Current contract

No foreign private persistence; production runtime owners, failure paths, CI non-zero evidence, handoffs and released-slice decision bind to one candidate.

### Target contract

No final candidate evidence or release decision exists.

### Preparation source anchors

- `backend/tests/Notrelix.Architecture.Tests/`
- `docs/workstreams/executions/backend-team-architecture-closure/`
- `GitHub CI workflows`

### Required final evidence

- all direct primary scenarios above executed/verified on the accepted candidate;
- non-zero discovery/execution for runnable suites;
- real production composition for DB/RLS/broker/browser/artifact/runtime claims;
- required negative/failure evidence from TESTS;
- source-specific dependency readiness where applicable;
- security/privacy evidence for protected data;
- freshness/completeness evidence where interpretation depends on it;
- migration/compatibility evidence where persisted contracts change;
- durable semantic post-condition plus exact CI/job/artifact references.

### Invalidation boundary

Changes to accepted SHA, architecture/runtime owner, CI/security gate, handoff, released slice or dependency readiness invalidate this record and require targeted rerun/review.

### Execution record

```text
Candidate SHA: NOT_RECORDED
Released slice evaluated: NOT_RECORDED
Requirements: ARREQ121, ARREQ122, ARREQ123, ARREQ124, ARREQ125, ARREQ126, ARREQ127, ARREQ128
Tests: AR-TST-REQ-121, AR-TST-REQ-122, AR-TST-REQ-123, AR-TST-REQ-124, AR-TST-REQ-125, AR-TST-REQ-126, AR-TST-REQ-127, AR-TST-REQ-128
Legacy aliases: ANA-TST-REL-001
PLAN work units: AR-16-INV-001, AR-16-CORE-001, AR-16-SEC-001, AR-16-COMPAT-001
Discovered: NOT_RECORDED
Executed: NOT_RECORDED
Passed: NOT_RECORDED
Failed: NOT_RECORDED
Skipped: NOT_RECORDED
Source/runtime evidence: NOT_EVALUATED
Negative/failure evidence: NOT_EVALUATED
Security/tenant/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
CI/job/artifact evidence: NOT_EVALUATED
Observed durable post-condition: NOT_RECORDED
Blocking debt: NOT_RECORDED
Invalidated by: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision timestamp: NOT_RECORDED
Certification: NOT_EVALUATED
```

# Aggregate gate certification records

## AR-CERT-GATE-001 — Semantic and projection foundation

**PLAN gate:** `AR-GATE-001`  
**Scope:** `ARREQ001–ARREQ040`  
**Required child records:** `AR-CERT-GOV-001`, `AR-CERT-SRC-001`, `AR-CERT-MET-001`, `AR-CERT-PRJ-001`, `AR-CERT-WORK-001`  
**Certification:** `NOT_EVALUATED`

A blocked required child keeps this gate `BLOCKED`. A deferred optional capability is acceptable only if the released slice neither advertises nor depends on it.

```text
Candidate SHA: NOT_RECORDED
Child readiness snapshot: NOT_RECORDED
Blocking child/source dependency: NOT_RECORDED
Tests discovered/executed/passed/failed/skipped: NOT_RECORDED
Security/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
Production runtime evidence: NOT_EVALUATED
CI run/job/artifact evidence: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-GATE-002 — Dashboard, security, freshness and history

**PLAN gate:** `AR-GATE-002`  
**Scope:** `ARREQ041–ARREQ080`  
**Required child records:** `AR-CERT-DASH-001`, `AR-CERT-SEC-001`, `AR-CERT-FRESH-001`, `AR-CERT-SNAP-001`  
**Certification:** `NOT_EVALUATED`

A blocked required child keeps this gate `BLOCKED`. A deferred optional capability is acceptable only if the released slice neither advertises nor depends on it.

```text
Candidate SHA: NOT_RECORDED
Child readiness snapshot: NOT_RECORDED
Blocking child/source dependency: NOT_RECORDED
Tests discovered/executed/passed/failed/skipped: NOT_RECORDED
Security/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
Production runtime evidence: NOT_EVALUATED
CI run/job/artifact evidence: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-GATE-003 — Reporting product delivery

**PLAN gate:** `AR-GATE-003`  
**Scope:** `ARREQ081–ARREQ112`  
**Required child records:** `AR-CERT-ONBOARD-001`, `AR-CERT-XREP-001`, `AR-CERT-API-001`, `AR-CERT-EXP-001`  
**Certification:** `NOT_EVALUATED`

A blocked required child keeps this gate `BLOCKED`. A deferred optional capability is acceptable only if the released slice neither advertises nor depends on it.

```text
Candidate SHA: NOT_RECORDED
Child readiness snapshot: NOT_RECORDED
Blocking child/source dependency: NOT_RECORDED
Tests discovered/executed/passed/failed/skipped: NOT_RECORDED
Security/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
Production runtime evidence: NOT_EVALUATED
CI run/job/artifact evidence: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
Certification: NOT_EVALUATED
```

## AR-CERT-GATE-004 — Migration and final certification

**PLAN gate:** `AR-GATE-004`  
**Scope:** `ARREQ113–ARREQ128`  
**Required child records:** `AR-CERT-OPS-001`, `AR-CERT-MIG-001`, `AR-CERT-FINAL-001`  
**Certification:** `NOT_EVALUATED`

A blocked required child keeps this gate `BLOCKED`. A deferred optional capability is acceptable only if the released slice neither advertises nor depends on it.

```text
Candidate SHA: NOT_RECORDED
Child readiness snapshot: NOT_RECORDED
Blocking child/source dependency: NOT_RECORDED
Tests discovered/executed/passed/failed/skipped: NOT_RECORDED
Security/privacy evidence: NOT_EVALUATED
Freshness/completeness evidence: NOT_EVALUATED
Migration/compatibility evidence: NOT_EVALUATED
Production runtime evidence: NOT_EVALUATED
CI run/job/artifact evidence: NOT_EVALUATED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
Certification: NOT_EVALUATED
```

# 12. Released-entity certification ledger

| Entity | Type | Source(s) | Semantic version / scope | Required records | Initial P6 disposition |
|---|---|---|---|---|---|
| Work item placement reference | projection/read model | WorkManagement | producer SourceRevision per item | `AR-CERT-WORK-001`, `AR-CERT-SRC-001`, `AR-CERT-PRJ-001` | `IMPLEMENTED_REFERENCE / NOT_EVALUATED` |
| `work-item-count:v1` | Metric | Work placement projection | Workspace current state; archiveState; board/group dimensions | `AR-CERT-MET-001`, `AR-CERT-WORK-001`, `AR-CERT-SEC-001`, `AR-CERT-FRESH-001` | `FROZEN_INITIAL_TARGET / NOT_EVALUATED` |
| `work-placement-overview:v1` | Report | `work-item-count:v1` | one Workspace; no historical date range | `AR-CERT-MET-001`, `AR-CERT-API-001`, `AR-CERT-SEC-001`, `AR-CERT-FRESH-001` | `FROZEN_INITIAL_TARGET / NOT_EVALUATED` |
| User Dashboard | Analytics configuration | certified Metric/report source | Private + Workspace only | `AR-CERT-DASH-001`, `AR-CERT-SEC-001` | `TARGET / NOT_EVALUATED` |
| Public Dashboard | visibility | n/a | not released | `AR-CERT-DASH-001` | `DEFERRED / NON_REACHABLE` |
| ReportingSnapshot | historical artifact | released report/Metric | not a public initial surface; LegacyGap must close | `AR-CERT-SNAP-001`, `AR-CERT-MIG-001` | `LEGACY_GAP / NOT_EVALUATED` |
| Cross-context report | report | multiple certified sources | none in initial P6 | `AR-CERT-XREP-001` | `DEFERRED / NON_REACHABLE` |
| Dedicated Analytics frontend | consumer | public P6 API | work-placement-overview:v1 + Dashboard | `AR-CERT-API-001` | `FROZEN_INITIAL_TARGET / NOT_EVALUATED` |
| Export | report artifact | released report | none in initial P6 | `AR-CERT-EXP-001` | `DEFERRED / NON_REACHABLE` |
| Scheduled report delivery | delivery | released report + approved delivery owner | none in initial P6 | `AR-CERT-EXP-001` | `DEFERRED / NON_REACHABLE` |
| Analytics realtime | freshness optimization | durable P6 state | none required in initial P6 | `AR-CERT-FRESH-001`, `AR-CERT-API-001` | `NOT_REQUIRED / NON_REACHABLE` |

Final certification MUST name concrete Metric/report IDs and semantic versions.

# 13. Canonical handoff schemas

## Source → Analytics

```text
Source context:
Business fact:
Event/reporting contract:
Semantic owner:
Account/workspace scope:
Timestamp semantics:
Version:
Replay:
Ordering:
Current readiness:
Required readiness:
Tests:
Candidate SHA:
Source posture:
Breaking/additive:
Migration/backfill:
Backfill scope:
Backfill rate limit:
Production-impact guard:
Checkpoint/resume strategy:
Backfill validation:
Evidence locator:
Known debt/blocker:
Invalidation trigger:
Rollback/forward-fix:
```

## Metric handoff

```text
Metric ID:
Product meaning:
Source facts:
Aggregation:
Time basis:
Timezone:
Filters:
Authorization:
Freshness:
Correction behavior:
Tests:
Metric semantic version:
Historical comparability:
Null/zero/unknown behavior:
Source cutoff:
Candidate SHA:
Evidence locator:
Invalidation trigger:
```

## Report handoff

```text
Report ID:
Metrics:
Scope:
Filters:
Date range:
Freshness:
Authorization:
Frontend owner:
Export:
Performance limit:
Tests:
Completeness/cutoff strategy:
Drill-down authorization:
Cache identity:
Candidate SHA:
Evidence locator:
Known debt:
Invalidation trigger:
```

## Scheduled-report delivery extension

Initial P6 requires `ScheduledDelivery=none` and proves non-reachability. Future admission extends the Report handoff with:

```text
Schedule:
Recipient identity:
Recipient authorization source:
Authorization time:
Delivery owner/channel:
Snapshot/source cutoff:
Retry/idempotency:
Expiration/retention:
Delivery failure visibility:
Tests:
Candidate SHA:
Evidence locator:
```

A scheduled report cannot certify merely because its underlying report query is D5.

# 14. Certification anti-patterns

The following are invalid conclusions:

```text
Dashboard entity exists → Analytics UI ready
EF table exists → Metric exists
Work placement D5 → every source analytics D5
RLS exists → report authorization complete
snapshot row exists → historical reporting complete
Workspace dashboard renders → P6 frontend exists
same database → export semantics equal report semantics
historical AR-FLOW green → every future candidate green
green CI → every ARREQ executed
one P6 score → every Metric/report/source certified
```

# 15. Final P6 decision record

```text
Accepted candidate SHA: NOT_RECORDED

Certified source contracts: NOT_RECORDED
Target initial Metric: work-item-count:v1 — NOT_EVALUATED
Certified Metrics: NOT_RECORDED
Certified projections: NOT_RECORDED
Target initial report: work-placement-overview:v1 — NOT_EVALUATED
Certified reports: NOT_RECORDED
Certified Dashboards/widget contracts: NOT_RECORDED
Certified snapshots/artifacts: NOT_RECORDED
Certified exports: NOT_RECORDED
Certified frontend surfaces: NOT_RECORDED

Frozen initial deferrals: cross-context report, export, scheduled report delivery, Analytics realtime, Public Dashboard
Deferred / NOT_APPLICABLE: NOT_RECORDED
Blocked source lanes: NOT_RECORDED

STABLE(D5) capability records: NOT_RECORDED
VERIFIED(D4) capability records: NOT_RECORDED
BLOCKED capability records: NOT_RECORDED

Critical blockers:
- final candidate evidence not recorded;
- every AR-GAP must be closed or keep the affected released slice blocked/deferred;
- source-specific readiness must remain independent;
- no Metric/report/export/frontend capability may be inferred from Work reference evidence.

Known debt: NOT_RECORDED
Reviewer: NOT_RECORDED
Decision date: NOT_RECORDED
Final certification: NOT_EVALUATED
```

P6 can be complete for a specific released slice while other source contexts/reports/exports remain deferred or blocked. It cannot be declared globally complete from source presence alone.
