---
document_id: WRK-TESTS-ANALYTICS-REPORTING
document_type: workstream-test-plan
status: active
owner: analytics-reporting-team
applies_to: [backend, frontend, analytics, reporting, metrics, projections, dashboards, snapshots, exports, privacy, migrations, exact-candidate-evidence]
evidence:
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.spec.md
  - docs/workstreams/executions/analytics-reporting/analytics-reporting.plan.md
  - docs/product/analytics.md
  - docs/workstreams/teams/analytics-reporting.md
  - docs/workstreams/executions/backend-team-architecture-closure/
review_on: [arreq-change, source-contract-change, metric-semantic-change, projection-runtime-change, dashboard-widget-contract-change, authorization-or-privacy-change, snapshot-export-change, migration-or-rls-change, accepted-candidate-sha-change]
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# TESTS — P6 Analytics & Reporting

## 1. Test authority

This file is the executable verification contract for `ARREQ001..ARREQ128`.

Primary identity is one-to-one:

```text
ARREQ001 → AR-TST-REQ-001
...
ARREQ128 → AR-TST-REQ-128
```

The 37 historical `ANA-TST-*` IDs remain stable aliases/supporting labels. They are not renamed and do not replace the primary `AR-TST-REQ-*` identity used by PLAN/CERT.

Final proof requires accepted candidate SHA, non-zero execution, the real runtime owner when applicable, required failure/negative proof and the observed durable post-condition.

## 2. Common evidence record

Every executed primary scenario records:

```text
Candidate SHA:
Test ID:
Requirement:
PLAN work unit:
Test project/file:
Source/runtime owner:
Command / test filter:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Database/broker/browser/artifact identity where relevant:
Positive/negative evidence:
Security/tenant evidence:
Migration/compatibility evidence:
Observed durable post-condition:
CI run/job/artifact:
Execution evidence:
```

A unit test cannot substitute for production DI/broker/RLS/browser/artifact composition when that boundary is part of the claim. A fixture cannot prove a missing API/frontend runtime exists. A zero-test green command is not evidence.

# Primary scenarios

## AR-TST-REQ-001 — Analytics remains derived state

- Requirement: `ARREQ001` — Analytics remains derived state
- PLAN work unit: `AR-01-INV-001` — Freeze authority and exact-source baseline
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Analytics remains derived state' are available.

**When**

the P6 release/architecture gate evaluates ARREQ001.

**Then**

Analytics-owned projections, metrics, reports, dashboards, snapshots and exports MUST remain derived/read-oriented state. They MUST NOT become the transactional source of truth for Work Management, Documents, Collaboration, Automation, Integrations, Billing, Identity, Workspace or Governance.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-002 — Analytics never hides source mutation

- Requirement: `ARREQ002` — Analytics never hides source mutation
- PLAN work unit: `AR-01-INV-001` — Freeze authority and exact-source baseline
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Analytics never hides source mutation' are available.

**When**

the P6 release/architecture gate evaluates ARREQ002.

**Then**

Dashboard controls, filters, drill-downs and report interactions MUST NOT mutate source business state unless they invoke an explicit source-context command through that context's public contract and normal authorization/invariants.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-003 — Authority precedence is explicit

- Requirement: `ARREQ003` — Authority precedence is explicit
- PLAN work unit: `AR-01-CORE-001` — Freeze the released P6 execution slice
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Authority precedence is explicit' are available.

**When**

the P6 release/architecture gate evaluates ARREQ003.

**Then**

Product Analytics semantics come from `docs/product/analytics.md`; team delivery boundaries come from `docs/workstreams/teams/analytics-reporting.md`; backend/frontend architecture owners define mechanics; this execution SPEC refines but MUST NOT override higher authority.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-004 — Brownfield source posture precedes implementation

- Requirement: `ARREQ004` — Brownfield source posture precedes implementation
- PLAN work unit: `AR-01-CORE-001` — Freeze the released P6 execution slice
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Brownfield source posture precedes implementation' are available.

**When**

the P6 release/architecture gate evaluates ARREQ004.

**Then**

Every P6 capability MUST be classified from exact source as production-reachable, implemented reference, domain-only, infrastructure-only, legacy-gap, contract-drift or absent before implementation. Source existence alone is not readiness.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-005 — Product-source gaps remain explicit

- Requirement: `ARREQ005` — Product-source gaps remain explicit
- PLAN work unit: `AR-01-SEC-001` — Enforce no-overclaim and no-private-table rules
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Product-source gaps remain explicit' are available.

**When**

the P6 release/architecture gate evaluates ARREQ005.

**Then**

Documentation MUST NOT invent a generic Metric engine, report API, dashboard API, export pipeline, realtime producer or frontend Analytics surface when exact source does not contain them. Missing implementation remains named debt until implemented and proven.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-006 — Exact-candidate evidence is mandatory

- Requirement: `ARREQ006` — Exact-candidate evidence is mandatory
- PLAN work unit: `AR-01-SEC-001` — Enforce no-overclaim and no-private-table rules
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Exact-candidate evidence is mandatory' are available.

**When**

the P6 release/architecture gate evaluates ARREQ006.

**Then**

Final readiness MUST bind to one accepted candidate SHA and non-zero execution evidence. Historical TAC/CI evidence is preparation context only unless rerun on the accepted candidate or explicitly reused under a still-valid immutable contract.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-007 — Consumer-specific readiness replaces one global score

- Requirement: `ARREQ007` — Consumer-specific readiness replaces one global score
- PLAN work unit: `AR-01-COMPAT-001` — Establish evidence, handoff and invalidation governance
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'Consumer-specific readiness replaces one global score' are available.

**When**

the P6 release/architecture gate evaluates ARREQ007.

**Then**

Metrics, projections, reports, exports and source contexts are certified independently. One stable WorkManagement projection MUST NOT certify Documents, Billing, Automation, Integrations or cross-context analytics.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-008 — No broad cleanup PR

- Requirement: `ARREQ008` — No broad cleanup PR
- PLAN work unit: `AR-01-COMPAT-001` — Establish evidence, handoff and invalidation governance
- PLAN disposition: `IMPLEMENT_AND_FREEZE`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

the exact candidate and authority inventory for 'No broad cleanup PR' are available.

**When**

the P6 release/architecture gate evaluates ARREQ008.

**Then**

P6 changes MUST be capability-directed. Unrelated refactors, data-platform extraction, provider-neutral abstractions or repository-wide changes require their own authority and MUST NOT be smuggled into Analytics implementation.

**Expected result:** the requirement is enforced and conflicting/stale/broad-scope evidence cannot silently pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)
- Source under test: `docs/product/analytics.md; docs/workstreams/teams/analytics-reporting.md; docs/workstreams/cross-team-dependencies.md; analytics-reporting.spec.md`
- Runtime owner/proof boundary: authority / architecture / release inventory
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsAuthorityArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-009 — Every analytical source has an approved semantic owner

- Requirement: `ARREQ009` — Every analytical source has an approved semantic owner
- PLAN work unit: `AR-02-INV-001` — Inventory admitted analytical sources
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Every analytical source has an approved semantic owner' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

Each projection or Metric MUST name the bounded context that owns the source fact and the approved event/reporting/query contract by which Analytics consumes it.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-010 — Source handoff records stable identity and scope

- Requirement: `ARREQ010` — Source handoff records stable identity and scope
- PLAN work unit: `AR-02-INV-001` — Inventory admitted analytical sources
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Source handoff records stable identity and scope' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

Each admitted source MUST record logical contract identity/version, Account/Workspace/resource scope, source occurrence/business time, producer revision/sequence where applicable, replay/backfill availability and compatibility window.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-011 — Analytics does not create private-table source contracts

- Requirement: `ARREQ011` — Analytics does not create private-table source contracts
- PLAN work unit: `AR-02-CORE-001` — Normalize source ports and producer-owned adapters
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-SRC-001`

**Given**

a source context is proposed under 'Analytics does not create private-table source contracts' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

A source schema/table name, EF entity, CLR type or ad-hoc cross-context join MUST NOT become the semantic source contract for Analytics.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-012 — Source queries are producer-owned public contracts

- Requirement: `ARREQ012` — Source queries are producer-owned public contracts
- PLAN work unit: `AR-02-CORE-001` — Normalize source ports and producer-owned adapters
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Source queries are producer-owned public contracts' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

When an event lacks sufficient information or rebuild needs current source state, Analytics MUST call a producer-owned public reporting/projection-source contract through an Analytics-owned port/ACL; Analytics MUST NOT read the producer DbContext directly.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-013 — Source correction and deletion semantics are admitted before projection

- Requirement: `ARREQ013` — Source correction and deletion semantics are admitted before projection
- PLAN work unit: `AR-02-SEC-001` — Propagate source authorization, deletion and privacy semantics
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Source correction and deletion semantics are admitted before projection' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

Each source handoff MUST define correction/reversal/archive/delete behavior and whether historical derived state is repaired, retained, anonymized or purged.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-014 — Source security classification propagates into Analytics

- Requirement: `ARREQ014` — Source security classification propagates into Analytics
- PLAN work unit: `AR-02-SEC-001` — Propagate source authorization, deletion and privacy semantics
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Source security classification propagates into Analytics' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

Sensitive fields, resource visibility, tenant/data-region obligations and authorization-relevant source facts MUST be carried into the analytical authorization/privacy design; aggregation does not erase those obligations.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-015 — Source-event compatibility includes backlog and replay

- Requirement: `ARREQ015` — Source-event compatibility includes backlog and replay
- PLAN work unit: `AR-02-COMPAT-001` — Freeze source version/backlog/replay compatibility
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Source-event compatibility includes backlog and replay' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

A source event version change MUST review deployed Analytics consumers, retained/backlogged messages, rebuild source, producer revisions and old/new projection compatibility before removing old support.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-016 — Reference projection does not define all source semantics

- Requirement: `ARREQ016` — Reference projection does not define all source semantics
- PLAN work unit: `AR-02-COMPAT-001` — Freeze source version/backlog/replay compatibility
- PLAN disposition: `IMPLEMENT_SOURCE_GATES`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a source context is proposed under 'Reference projection does not define all source semantics' with public-contract and private-persistence alternatives.

**When**

Source→Analytics admission and architecture boundaries are checked.

**Then**

`WorkspaceWorkItemPlacementProjection` is the current production reference mechanism for Work placement only. Its identity/order/rebuild choices MUST NOT be copied blindly into metrics or source contexts with different semantics.

**Expected result:** only the approved semantic owner/public contract with explicit scope/version/replay/security is admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Events/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkItemProjectionSourceAdapter.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs; docs/workstreams/teams/analytics-reporting.md`
- Runtime owner/proof boundary: producer public contract + Analytics ACL
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceContractArchitectureTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-017 — Metric identity is stable and versioned

- Requirement: `ARREQ017` — Metric identity is stable and versioned
- PLAN work unit: `AR-03-INV-001` — Define Metric semantic contract
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-MET-001`

**Given**

a registry contains one canonical Metric key/version and a competing widget/chart-name identity.

**When**

the report layer resolves both.

**Then**

Every released Metric MUST have a stable semantic key plus explicit semantic version. Widget ID, chart title, SQL query filename, endpoint name or frontend label MUST NOT be used as Metric identity.

**Expected result:** only canonical Metric key+version resolves; presentation labels cannot shadow semantic identity.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-018 — Metric definition has one canonical owner

- Requirement: `ARREQ018` — Metric definition has one canonical owner
- PLAN work unit: `AR-03-INV-001` — Define Metric semantic contract
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

a released Metric fixture exercises normal and boundary data for 'Metric definition has one canonical owner'.

**When**

the canonical Metric registry/evaluator resolves it.

**Then**

A released Metric MUST have one canonical definition containing source facts, scope, filters, aggregation, dimensions, time basis, unit, rounding, freshness and privacy/authorization class. Backend, export and frontend MUST consume that definition rather than reimplement formulas.

**Expected result:** ARREQ018 is deterministic across report/export/frontend consumers and ambiguous semantics are rejected.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-019 — Displayed precision and comparison basis require defined meaning

- Requirement: `ARREQ019` — Displayed precision and comparison basis require defined meaning
- PLAN work unit: `AR-03-CORE-001` — Implement canonical Metric catalog and deterministic value semantics
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

the `work-item-count:v1` report is rendered with current-period output and a previous-period comparison fixture.

**When**

the report/UI evaluates current and comparison windows.

**Then**

Every user-visible number MUST have a defined unit, reporting period, inclusion/exclusion rule, freshness state and comparison basis. If the UI shows a comparison such as “vs previous period”, the current window and comparison window MUST define exact start/end boundaries, inclusivity, timezone/calendar basis and Metric semantic-version compatibility. The UI MUST NOT display apparently precise numbers whose denominator, source cutoff or comparison window is undefined.

**Expected result:** the number exposes unit/period/freshness and any comparison uses explicit window boundaries, timezone/calendar basis and a compatible Metric version.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-020 — Rate and percentage metrics define numerator and denominator

- Requirement: `ARREQ020` — Rate and percentage metrics define numerator and denominator
- PLAN work unit: `AR-03-CORE-001` — Implement canonical Metric catalog and deterministic value semantics
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-MET-002`

**Given**

a rate Metric has explicit numerator, denominator, exclusions and zero-denominator fixtures.

**When**

the evaluator calculates all variants.

**Then**

Percent/rate Metrics MUST explicitly define numerator, denominator, excluded states, zero-denominator behavior, unit/rounding and applicable time window.

**Expected result:** normal, excluded and zero-denominator results follow the declared formula/unit/rounding contract.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-021 — Time-based Metrics define calendar and timezone semantics

- Requirement: `ARREQ021` — Time-based Metrics define calendar and timezone semantics
- PLAN work unit: `AR-03-SEC-001` — Implement time, correction and precision semantics
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-MET-TIME-001`

**Given**

source facts straddle timezone midnight and a DST transition.

**When**

the same Metric is bucketed in two declared report timezones.

**Then**

Daily/weekly/monthly and relative-window Metrics MUST define event/business timestamp, report timezone, boundary inclusivity, DST behavior and Account/user timezone precedence where product-defined. Browser locale or chart-library defaults are not authority.

**Expected result:** bucket membership follows business timestamp/timezone/boundary semantics, not browser/chart defaults.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-022 — Metric corrections are explicit

- Requirement: `ARREQ022` — Metric corrections are explicit
- PLAN work unit: `AR-03-SEC-001` — Implement time, correction and precision semantics
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-CORR-001`

**Given**

a released Metric fixture exercises normal and boundary data for 'Metric corrections are explicit'.

**When**

the canonical Metric registry/evaluator resolves it.

**Then**

If source facts can be corrected/reversed/archived, the Metric definition MUST specify whether and how historical buckets/aggregates are repaired, reopened, compensated or frozen.

**Expected result:** ARREQ022 is deterministic across report/export/frontend consumers and ambiguous semantics are rejected.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-023 — Zero, no data, unknown, unavailable and unauthorized are distinct

- Requirement: `ARREQ023` — Zero, no data, unknown, unavailable and unauthorized are distinct
- PLAN work unit: `AR-03-COMPAT-001` — Version Metric semantics and historical comparison
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-MET-003`

**Given**

fixtures represent zero, no data, unknown/unavailable and unauthorized.

**When**

the report contract serializes each state.

**Then**

Metric/query contracts MUST preserve these states and MUST NOT coerce all missing/forbidden/stale inputs to numeric zero.

**Expected result:** all states remain distinguishable and none is coerced to numeric zero.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-024 — Breaking Metric meaning requires historical-comparability policy

- Requirement: `ARREQ024` — Breaking Metric meaning requires historical-comparability policy
- PLAN work unit: `AR-03-COMPAT-001` — Version Metric semantics and historical comparison
- PLAN disposition: `IMPLEMENT_TYPED_CODE_OWNED_REGISTRY`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

two historical series use different Metric semantic versions and a previous-period comparison is requested.

**When**

the comparison/trend builder evaluates them.

**Then**

A material semantic change MUST be classified as implementation fix, new Metric version or breaking replacement and MUST define backfill/recompute, old/new history behavior, comparison/trend break and API/frontend migration. Previous-period or year-over-year comparison is valid only when both windows use semantically compatible Metric versions; otherwise the product MUST show a break marker, separate series or explicitly recomputed comparable history.

**Expected result:** incompatible versions never form one continuous comparison; use a break marker, separate series or explicitly recomputed comparable history.

- Test project/file: backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricCatalog.cs; backend/src/Notrelix.Application/Features/Analytics/Metrics/MetricDefinition.cs; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/WorkPlacementOverviewDefinition.cs (target)`
- Runtime owner/proof boundary: typed/versioned Metric registry
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / application`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Application.Tests/Features/Analytics/Metrics/MetricDefinitionTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-025 — Projection names its authoritative source and derived identity

- Requirement: `ARREQ025` — Projection names its authoritative source and derived identity
- PLAN work unit: `AR-04-INV-001` — Define projection identity, ordering and store contracts
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a projection fixture challenges 'Projection names its authoritative source and derived identity' with duplicate/stale/restart/rebuild conditions as applicable.

**When**

the production projection/rebuild path executes.

**Then**

Each projection MUST record source contract(s), projection key, tenant scope, semantic version, update rule and rebuilding authority.

**Expected result:** derived state converges to ARREQ025 without source mutation or false progress.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-026 — Projection update is idempotent

- Requirement: `ARREQ026` — Projection update is idempotent
- PLAN work unit: `AR-04-INV-001` — Define projection identity, ordering and store contracts
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-IDEMP-001`

**Given**

the same logical source occurrence is delivered twice and a distinct occurrence follows.

**When**

the production projection path handles all deliveries.

**Then**

Duplicate delivery of the same logical source occurrence MUST NOT double-apply derived state. Distinct occurrences with the same event type MUST remain distinct.

**Expected result:** the duplicate changes derived state once and the distinct occurrence still applies.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-027 — Ordering is the smallest semantics-required scope

- Requirement: `ARREQ027` — Ordering is the smallest semantics-required scope
- PLAN work unit: `AR-04-CORE-001` — Migrate Analytics use cases off IReportingDbContext
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a projection fixture challenges 'Ordering is the smallest semantics-required scope' with duplicate/stale/restart/rebuild conditions as applicable.

**When**

the production projection/rebuild path executes.

**Then**

Each projection MUST declare whether it is order-independent, per-resource ordered, per-workspace/account ordered or window-ordered. Global ordering MUST NOT be introduced when source revision or a narrower stream is sufficient.

**Expected result:** derived state converges to ARREQ027 without source mutation or false progress.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-028 — Late and out-of-order facts cannot regress projection state

- Requirement: `ARREQ028` — Late and out-of-order facts cannot regress projection state
- PLAN work unit: `AR-04-CORE-001` — Migrate Analytics use cases off IReportingDbContext
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-ORDER-001`

**Given**

a newer projection revision exists and an older source fact arrives later.

**When**

the projection apply path receives it.

**Then**

When ordering/version proves an incoming fact is stale, it MUST NOT overwrite newer derived state. When ordering cannot be proven, the projection MUST use a safe reconciliation/invalidation policy instead of guessing.

**Expected result:** the old fact cannot regress state; unprovable ordering triggers reconciliation rather than timestamp guessing.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-029 — Projection failure commits no false checkpoint

- Requirement: `ARREQ029` — Projection failure commits no false checkpoint
- PLAN work unit: `AR-04-SEC-001` — Prove idempotent apply, ordering and checkpoint settlement
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-RESTART-001`

**Given**

projection persistence fails after delivery begins but before commit.

**When**

the production consumer fails then redelivers.

**Then**

Projection state, dedup/consumer settlement and any progress/checkpoint MUST advance only after the required derived effect durably succeeds; crash-before-commit MUST remain safely retryable under Platform delivery semantics.

**Expected result:** no false checkpoint/settlement survives and redelivery converges to one durable outcome.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-030 — Projection normal reads are local

- Requirement: `ARREQ030` — Projection normal reads are local
- PLAN work unit: `AR-04-SEC-001` — Prove idempotent apply, ordering and checkpoint settlement
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a projection fixture challenges 'Projection normal reads are local' with duplicate/stale/restart/rebuild conditions as applicable.

**When**

the production projection/rebuild path executes.

**Then**

Normal report/query reads MUST use Analytics-owned derived state or explicit transactional source query when that is the declared design. A local projection query MUST NOT synchronously re-read foreign source persistence on every request.

**Expected result:** derived state converges to ARREQ030 without source mutation or false progress.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-031 — Projection rebuild and backfill are operationally bounded

- Requirement: `ARREQ031` — Projection rebuild and backfill are operationally bounded
- PLAN work unit: `AR-04-COMPAT-001` — Build rebuild/backfill foundation without a generic analytics engine
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-REPLAY-001`

**Given**

a production backfill declares workspace/resource/date scope, approved source, rate limit and durable checkpoint.

**When**

the rebuild pauses, resumes and validates its result.

**Then**

Critical derived state MUST have a deterministic rebuild/reconciliation path using approved producer snapshots, replayable facts or another approved source. Every rebuild/backfill contract MUST define Account/Workspace/resource/date scope, source, rate limit, production-impact guard, durable checkpoint, resumability, duplicate handling, validation/reconciliation evidence and source-version handling. Unbounded ad-hoc production scripts are not an accepted rebuild mechanism.

**Expected result:** the resumed job respects rate/production-impact limits, does not restart from zero and completion requires approved producer reconciliation evidence.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-032 — Backfill and live traffic converge without double counting

- Requirement: `ARREQ032` — Backfill and live traffic converge without double counting
- PLAN work unit: `AR-04-COMPAT-001` — Build rebuild/backfill foundation without a generic analytics engine
- PLAN disposition: `IMPLEMENT_AND_MIGRATE_ANALYTICS_EF_COUPLING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-BF-001`

**Given**

a checkpointed backfill overlaps with newer live deliveries then restarts.

**When**

the job resumes while live consumers continue.

**Then**

Backfill/rebuild MUST coordinate with concurrent live consumers through source revision, idempotency, cutover or another explicit mechanism so live facts cannot be applied twice or overwritten by an older snapshot. Resuming from a durable checkpoint MUST preserve the same rule, and final validation MUST compare the rebuilt result with an approved producer invariant/snapshot before the backfill is declared complete.

**Expected result:** no occurrence is double-applied/overwritten and final validation matches the approved producer invariant.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/; backend/src/Notrelix.Application/Features/Analytics/Abstractions/IWorkspaceWorkItemPlacementStore.cs; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ (target/current)`
- Runtime owner/proof boundary: projection consumer/store/rebuild runtime
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsProjectionContractIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-033 — Work placement reference keeps producer revision authority

- Requirement: `ARREQ033` — Work placement reference keeps producer revision authority
- PLAN work unit: `AR-05-INV-001` — Re-baseline AR-FLOW-01 and AR-FLOW-02
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

placement state has SourceRevision=7 while a later wall-clock event has producer revision=6.

**When**

the projection evaluates the event.

**Then**

The existing Work placement projection MUST continue to use WorkManagement producer revision as semantic ordering authority. Wall-clock timestamps MUST NOT replace producer revision as ordering truth.

**Expected result:** revision 6 is rejected regardless of timestamp.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Projections/WorkItemPlacement/WorkspaceWorkItemPlacementProjection.cs; backend/tests/Notrelix.Application.Tests/Features/Analytics/WorkspaceWorkItemPlacementProjectionTests.cs`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-034 — Work placement live consumers use event facts when sufficient

- Requirement: `ARREQ034` — Work placement live consumers use event facts when sufficient
- PLAN work unit: `AR-05-INV-001` — Re-baseline AR-FLOW-01 and AR-FLOW-02
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

moved/archived V2 events contain all required placement facts.

**When**

their production consumers run.

**Then**

Moved/archived consumers MUST project from the committed event payload when it contains all required placement facts; they MUST NOT synchronously fetch Work source state merely for convenience.

**Expected result:** projection updates from event facts with no synchronous Work source lookup.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs; BoardItemMovedPlacementConsumer / BoardItemArchivedPlacementConsumer`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-035 — Work placement incomplete events use the producer public source

- Requirement: `ARREQ035` — Work placement incomplete events use the producer public source
- PLAN work unit: `AR-05-CORE-001` — Retain producer-revision apply and local-read behavior
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

a created V2 event lacks a required placement fact.

**When**

BoardItemCreatedPlacementConsumer runs.

**Then**

When a committed event such as item creation lacks required placement fields, the consumer MAY use the producer-owned `IWorkItemProjectionSource` path; no WorkManagement private persistence dependency is permitted.

**Expected result:** it uses the Analytics port/delegate adapter to WorkManagement public projection source, never a Work DbContext.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Analytics/WorkItemPlacementConsumers.cs; backend/src/Notrelix.Infrastructure/CrossContext/Analytics/WorkManagement/WorkItemProjectionSourceAdapter.cs`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-036 — Work placement local query remains Analytics-owned

- Requirement: `ARREQ036` — Work placement local query remains Analytics-owned
- PLAN work unit: `AR-05-CORE-001` — Retain producer-revision apply and local-read behavior
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

two workspaces contain placement rows and one workspace is queried.

**When**

GetWorkspacePlacementsQuery executes through the request pipeline.

**Then**

`GetWorkspacePlacementsQuery` or its replacement MUST read only Analytics reporting state under canonical authorization/tenant scope and MUST NOT become a hidden live WorkManagement query.

**Expected result:** only the requested workspace's local Analytics rows return; no Work source read occurs.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Placements/Queries/GetWorkspacePlacements/GetWorkspacePlacements.cs`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-037 — Work placement rebuild preserves newer live facts

- Requirement: `ARREQ037` — Work placement rebuild preserves newer live facts
- PLAN work unit: `AR-05-SEC-001` — Re-prove AR-FLOW-03 rebuild safety
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

rebuild revision=5 races with live revision=7 and the snapshot omits an item.

**When**

reconcile/delete logic runs.

**Then**

Rebuild snapshot application MUST never overwrite a strictly newer producer revision. Rows absent from an old snapshot require producer revalidation before deletion.

**Expected result:** revision 7 survives and missing-row deletion requires producer revalidation.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Placements/Services/WorkspaceWorkItemPlacementService.cs; WorkspacePlacementProjectionIntegrationTests.cs`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-038 — Work placement crash recovery remains atomic

- Requirement: `ARREQ038` — Work placement crash recovery remains atomic
- PLAN work unit: `AR-05-SEC-001` — Re-prove AR-FLOW-03 rebuild safety
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

the real moved-item chain is configured to fail projection persistence before commit.

**When**

MassTransit processing fails and redelivers.

**Then**

A projection persistence failure before commit MUST roll back the derived mutation and consumer claim/settlement so Platform redelivery can converge without mutating Work source state.

**Expected result:** projection mutation and claim roll back atomically; Work source stays unchanged; one final state commits.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/tests/Notrelix.Integration.Tests/Messaging/BoardItemMovedPlacementFailureRecoveryRuntimeTests.cs`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-039 — Reference projection is internally consumable but not falsely exposed as a product report

- Requirement: `ARREQ039` — Reference projection is internally consumable but not falsely exposed as a product report
- PLAN work unit: `AR-05-COMPAT-001` — Re-prove AR-FLOW-04 failure and recovery
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

the internal placement query exists but no product Metric/report has admitted it.

**When**

API/OpenAPI/frontend inventories are evaluated.

**Then**

The placement query may remain an internal/read-model capability until a product Metric/report consumes it. Its existence MUST NOT be presented as a complete Analytics dashboard/report API.

**Expected result:** the placement model remains internal and is not advertised as a complete Analytics report.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Placements/Queries/GetWorkspacePlacements/GetWorkspacePlacements.cs (explicitly not HTTP exposed)`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-040 — Reference projection exact-candidate proof is reused correctly

- Requirement: `ARREQ040` — Reference projection exact-candidate proof is reused correctly
- PLAN work unit: `AR-05-COMPAT-001` — Re-prove AR-FLOW-04 failure and recovery
- PLAN disposition: `RETAIN_AND_RECERTIFY_REFERENCE`
- Preparation evidence state: `EXISTS_STRONG_REFERENCE`
- Legacy alias/supporting ID(s): none

**Given**

AR-FLOW production code or its runtime dependencies change.

**When**

TAC evidence reuse is evaluated.

**Then**

TAC `AR-FLOW-01..04` evidence may be reused as preparation anchors, but P6 final certification MUST rerun the affected production graph when source event, revision, rebuild, RLS, broker/dedup or projection persistence behavior changes.

**Expected result:** invalidated production proofs rerun on the accepted candidate; historical evidence remains preparation-only.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)
- Source under test: `docs/workstreams/executions/backend-team-architecture-closure/ AR-FLOW-01..04 + production Analytics tests`
- Runtime owner/proof boundary: Work placement production reference chain
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + messaging`
- Exact-candidate rerun: `YES`
- Existing evidence: `EXISTS_STRONG_REFERENCE`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/WorkspacePlacementProjectionIntegrationTests.cs (extend where required)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-041 — Dashboard is user-managed Analytics configuration

- Requirement: `ARREQ041` — Dashboard is user-managed Analytics configuration
- PLAN work unit: `AR-06-INV-001` — Harden Dashboard ownership and canonical lifecycle
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Dashboard/Widget/Source fixture challenges 'Dashboard is user-managed Analytics configuration' through valid and invalid lifecycle/configuration states.

**When**

Domain plus canonical Application/API/persistence path processes it.

**Then**

Dashboard identity, name, lifecycle, ownership, visibility, sources, widgets and layout are Analytics-owned configuration. Dashboard mutation MUST NOT change source Boards, Items, Pages, Automations, Integration state or Billing facts.

**Expected result:** only Analytics configuration changes and ARREQ041 is enforced.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-042 — Private Dashboard has explicit owner

- Requirement: `ARREQ042` — Private Dashboard has explicit owner
- PLAN work unit: `AR-06-INV-001` — Harden Dashboard ownership and canonical lifecycle
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

two users have Private Dashboards and audit metadata changes independently.

**When**

one user accesses the other's Dashboard.

**Then**

A Private Dashboard MUST persist an explicit owner/principal identity and define transfer/deletion behavior; `CreatedBy` audit metadata alone MUST NOT silently serve as mutable authorization ownership.

**Expected result:** explicit persisted owner identity—not CreatedBy audit metadata—enforces ownership.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-043 — Released Dashboard visibility is Governance-backed

- Requirement: `ARREQ043` — Released Dashboard visibility is Governance-backed
- PLAN work unit: `AR-06-CORE-001` — Implement Dashboard Application lifecycle
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

Private/Workspace/Public enum values exist.

**When**

release reachability and authorization are tested.

**Then**

P6 releases Private and Workspace visibility only. `Public` visibility remains non-reachable until a separate public/share authorization contract exists. Workspace visibility MUST still respect source/resource authorization.

**Expected result:** Private+Workspace are reachable with Governance checks; Public remains non-reachable.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-044 — Dashboard archive/delete is non-destructive

- Requirement: `ARREQ044` — Dashboard archive/delete is non-destructive
- PLAN work unit: `AR-06-CORE-001` — Implement Dashboard Application lifecycle
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Dashboard/Widget/Source fixture challenges 'Dashboard archive/delete is non-destructive' through valid and invalid lifecycle/configuration states.

**When**

Domain plus canonical Application/API/persistence path processes it.

**Then**

Archiving/deleting Dashboard/Widget/Source removes Analytics configuration according to policy and MUST NOT cascade into source bounded-context records.

**Expected result:** only Analytics configuration changes and ARREQ044 is enforced.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-045 — Dashboard Source references stable analytical/source contracts

- Requirement: `ARREQ045` — Dashboard Source references stable analytical/source contracts
- PLAN work unit: `AR-06-SEC-001` — Normalize source and widget vocabularies
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Dashboard/Widget/Source fixture challenges 'Dashboard Source references stable analytical/source contracts' through valid and invalid lifecycle/configuration states.

**When**

Domain plus canonical Application/API/persistence path processes it.

**Then**

Dashboard Source MUST reference an approved Metric/report/source-contract identity and version rather than private table semantics. Existing `Search`/`External` enum values are not released until an explicit source contract is admitted.

**Expected result:** only Analytics configuration changes and ARREQ045 is enforced.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-046 — DashboardWidgetType is the canonical P6 widget-kind vocabulary

- Requirement: `ARREQ046` — DashboardWidgetType is the canonical P6 widget-kind vocabulary
- PLAN work unit: `AR-06-SEC-001` — Normalize source and widget vocabularies
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

DashboardWidgetType and Widgets.WidgetType contain divergent values.

**When**

stored data/API/frontend contracts load them.

**Then**

For P6, `DashboardWidgetType` is the persisted/public Dashboard widget-kind authority. The standalone `Widgets.WidgetType` duplicate MUST be retired, migrated or kept non-reachable so two independent widget vocabularies cannot diverge.

**Expected result:** one canonical DashboardWidgetType vocabulary remains reachable and duplicate semantics cannot diverge.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-047 — Widget configuration is typed and versioned by widget kind

- Requirement: `ARREQ047` — Widget configuration is typed and versioned by widget kind
- PLAN work unit: `AR-06-COMPAT-001` — Replace JSON-shape-only validation with typed versioned config
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a syntactically valid ChartWidget references an unknown Metric/version/dimension.

**When**

the Dashboard command validates it.

**Then**

Persistence MAY remain JSON, but every released widget kind MUST have a typed/versioned config contract validated against the selected Metric/source semantics. Basic JSON-shape validation alone is insufficient.

**Expected result:** basic JSON shape is insufficient; typed semantic validation rejects it.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-048 — Widget layout never changes source ordering

- Requirement: `ARREQ048` — Widget layout never changes source ordering
- PLAN work unit: `AR-06-COMPAT-001` — Replace JSON-shape-only validation with typed versioned config
- PLAN disposition: `IMPLEMENT_APPLICATION_API_AND_HARDEN_DOMAIN`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Dashboard/Widget/Source fixture challenges 'Widget layout never changes source ordering' through valid and invalid lifecycle/configuration states.

**When**

Domain plus canonical Application/API/persistence path processes it.

**Then**

Move/resize/reorder operations affect only Dashboard layout. Widget order/position MUST NOT update Work item order, document order or any source-context ordering field.

**Expected result:** only Analytics configuration changes and ARREQ048 is enforced.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Dashboards/; backend/src/Notrelix.API/Endpoints/Analytics/Dashboards/; reporting Dashboard EF mappings (target/current)`
- Runtime owner/proof boundary: Dashboard aggregate + Application/API + reporting persistence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / domain + integration + API`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/DashboardLifecycleIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-049 — Analytics request scope is explicit Account and Workspace

- Requirement: `ARREQ049` — Analytics request scope is explicit Account and Workspace
- PLAN work unit: `AR-07-INV-001` — Define Analytics resource/action matrix without speculative global permissions
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

two tenants/principals with different permissions exercise 'Analytics request scope is explicit Account and Workspace'.

**When**

Governance pipeline, report query and RLS-protected persistence execute.

**Then**

Every workspace analytical request, query, projection operation, export and background rebuild MUST carry/restore the correct Account/Workspace scope and use canonical tenant/RLS enforcement.

**Expected result:** ARREQ049 prevents unauthorized disclosure without blocking permitted analytics.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-050 — Dashboard visibility does not grant source permission

- Requirement: `ARREQ050` — Dashboard visibility does not grant source permission
- PLAN work unit: `AR-07-INV-001` — Define Analytics resource/action matrix without speculative global permissions
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a user can read a Workspace Dashboard but lacks one source-resource permission.

**When**

Dashboard widgets are queried.

**Then**

Viewer access to a Dashboard determines access to the Dashboard configuration, not automatic access to all rows/resources underlying its Widgets.

**Expected result:** Dashboard config may be visible but protected source data remains filtered/denied.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-051 — Aggregate visibility does not imply drill-down visibility

- Requirement: `ARREQ051` — Aggregate visibility does not imply drill-down visibility
- PLAN work unit: `AR-07-CORE-001` — Enforce aggregate and drill-down authorization separately
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-SEC-002`

**Given**

a viewer can see an aggregate but not one underlying resource.

**When**

the viewer drills down.

**Then**

A principal allowed to see an aggregate MAY still be forbidden from individual resources. Drill-down MUST re-evaluate the source/report authorization contract.

**Expected result:** detail authorization is re-evaluated and denied data is not exposed.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-052 — Aggregation does not erase confidentiality

- Requirement: `ARREQ052` — Aggregation does not erase confidentiality
- PLAN work unit: `AR-07-CORE-001` — Enforce aggregate and drill-down authorization separately
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PRIV-001`

**Given**

two tenants/principals with different permissions exercise 'Aggregation does not erase confidentiality'.

**When**

Governance pipeline, report query and RLS-protected persistence execute.

**Then**

Sensitive/low-cardinality groups MUST be masked, suppressed or restricted where product/security policy requires it. Analytics MUST NOT expose protected PII merely because values are grouped.

**Expected result:** ARREQ052 prevents unauthorized disclosure without blocking permitted analytics.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-053 — Cross-tenant analytics is privileged and explicit

- Requirement: `ARREQ053` — Cross-tenant analytics is privileged and explicit
- PLAN work unit: `AR-07-SEC-001` — Implement privacy and cross-tenant isolation
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-SEC-001`

**Given**

two tenants share identical report IDs and filters.

**When**

one tenant attempts direct/cached access to the other.

**Then**

Ordinary Workspace users MUST NOT query cross-tenant projections or caches. Any global/admin analytics requires a separately authorized product capability and MUST NOT arise accidentally from shared storage.

**Expected result:** no cross-tenant row or cached result is observable.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-054 — Authorization-sensitive caching is partitioned

- Requirement: `ARREQ054` — Authorization-sensitive caching is partitioned
- PLAN work unit: `AR-07-SEC-001` — Implement privacy and cross-tenant isolation
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-SEC-004`

**Given**

two principals share workspace/report filters but have different source visibility.

**When**

both use the report cache.

**Then**

Cache identity MUST include tenant plus Metric/report version, filters/time range/timezone and any authorization boundary needed to prevent principals with different visibility from sharing unsafe results.

**Expected result:** authorization-sensitive cache identity prevents result widening.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-055 — Derived data inherits security and data-location obligations

- Requirement: `ARREQ055` — Derived data inherits security and data-location obligations
- PLAN work unit: `AR-07-COMPAT-001` — Prove RLS plus application authorization and cache partition
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

two tenants/principals with different permissions exercise 'Derived data inherits security and data-location obligations'.

**When**

Governance pipeline, report query and RLS-protected persistence execute.

**Then**

Replicating source data into reporting tables, caches, exports or a future warehouse does not remove Account region, privacy, retention or access restrictions.

**Expected result:** ARREQ055 prevents unauthorized disclosure without blocking permitted analytics.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql; backend/src/Notrelix.Domain/Governance/Permissions/PermissionAction.cs; canonical Application pipeline/ResourceRef rules`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-056 — RLS is defense in depth, not the sole report authorization policy

- Requirement: `ARREQ056` — RLS is defense in depth, not the sole report authorization policy
- PLAN work unit: `AR-07-COMPAT-001` — Prove RLS plus application authorization and cache partition
- PLAN disposition: `IMPLEMENT_AND_PROVE_D5`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

two tenants/principals with different permissions exercise 'RLS is defense in depth, not the sole report authorization policy'.

**When**

Governance pipeline, report query and RLS-protected persistence execute.

**Then**

Reporting tables remain protected by RLS, while Application/Governance still determines report actions, Dashboard visibility, sensitive dimensions and drill-down permissions.

**Expected result:** ARREQ056 prevents unauthorized disclosure without blocking permitted analytics.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- Runtime owner/proof boundary: Governance pipeline + RLS + report authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsAuthorizationIsolationIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-057 — Every released Metric/report has a freshness class

- Requirement: `ARREQ057` — Every released Metric/report has a freshness class
- PLAN work unit: `AR-08-INV-001` — Define freshness classes and source-cutoff vocabulary
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

one Metric is near-realtime and another batch.

**When**

API/frontend responses are inspected.

**Then**

Declare transactional/current, near-realtime/eventually-consistent, scheduled/batch or another explicit class. The API/UI MUST NOT call an asynchronous projection 'live' without meeting that contract.

**Expected result:** each exposes the declared freshness class; batch data is never labelled live.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-058 — Freshness metadata is queryable

- Requirement: `ARREQ058` — Freshness metadata is queryable
- PLAN work unit: `AR-08-INV-001` — Define freshness classes and source-cutoff vocabulary
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Metric/report has controlled cutoff, lag and availability for 'Freshness metadata is queryable'.

**When**

API/consumer interprets the state.

**Then**

Where interpretation depends on freshness, report responses MUST include meaningful `dataThrough`/source cutoff, generated/last-updated time, projection lag/degraded state or equivalent metadata.

**Expected result:** ARREQ058 remains explicit and stale/missing state is never silently exact truth.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-059 — Stale, unavailable and not-yet-projected are distinct

- Requirement: `ARREQ059` — Stale, unavailable and not-yet-projected are distinct
- PLAN work unit: `AR-08-CORE-001` — Implement stale, partial and unavailable query semantics
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-FRESH-001`

**Given**

the last projection is older than SLA and a fresh source read is unavailable.

**When**

the report is queried.

**Then**

A stale projection MAY be shown with explicit metadata, refreshed or temporarily unavailable; it MUST NOT be silently presented as exact current truth.

**Expected result:** the result is explicitly stale/unavailable, never exact-current by implication.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-060 — Multi-source reports declare completeness and cutoff strategy

- Requirement: `ARREQ060` — Multi-source reports declare completeness and cutoff strategy
- PLAN work unit: `AR-08-CORE-001` — Implement stale, partial and unavailable query semantics
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-FRESH-002`

**Given**

a report has Work data through T2 and Documents only through T1.

**When**

the combined report is queried.

**Then**

A report combining sources with different freshness MUST expose partial/degraded state or use a defined common cutoff. It MUST NOT silently mix incompatible windows.

**Expected result:** it uses a declared common cutoff or explicit partial/degraded metadata.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-061 — Analytics failure does not block unrelated transactions

- Requirement: `ARREQ061` — Analytics failure does not block unrelated transactions
- PLAN work unit: `AR-08-SEC-001` — Keep initial P6 query/refetch authoritative
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Metric/report has controlled cutoff, lag and availability for 'Analytics failure does not block unrelated transactions'.

**When**

API/consumer interprets the state.

**Then**

Projection/report outages are downstream failures by default. WorkManagement/Documents/Automation/Billing transactional commands MUST continue unless an explicit source invariant says otherwise.

**Expected result:** ARREQ061 remains explicit and stale/missing state is never silently exact truth.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-062 — Realtime is freshness, not Analytics authority

- Requirement: `ARREQ062` — Realtime is freshness, not Analytics authority
- PLAN work unit: `AR-08-SEC-001` — Keep initial P6 query/refetch authoritative
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a Metric/report has controlled cutoff, lag and availability for 'Realtime is freshness, not Analytics authority'.

**When**

API/consumer interprets the state.

**Then**

Durable query/projection state remains authoritative. Realtime messages MUST NOT become the only record of a Metric, Dashboard configuration, report job or snapshot.

**Expected result:** ARREQ062 remains explicit and stale/missing state is never silently exact truth.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-063 — Initial P6 certification does not require Analytics realtime

- Requirement: `ARREQ063` — Initial P6 certification does not require Analytics realtime
- PLAN work unit: `AR-08-COMPAT-001` — Gate any future Analytics realtime behind recoverability
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

the initial P6 candidate has no Analytics realtime producer.

**When**

release discovery and frontend behavior are inspected.

**Then**

P6 MAY ship query/refetch-driven Analytics without a realtime producer. No realtime surface may be advertised as released unless its producer contract and recovery path are implemented and certified.

**Expected result:** query/refetch Analytics may certify while no realtime capability is advertised.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-064 — Any Analytics realtime path is gap-recoverable

- Requirement: `ARREQ064` — Any Analytics realtime path is gap-recoverable
- PLAN work unit: `AR-08-COMPAT-001` — Gate any future Analytics realtime behind recoverability
- PLAN disposition: `IMPLEMENT_FRESHNESS_DEFER_REALTIME`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a future realtime stream drops one sequence then reconnects with later envelopes.

**When**

the consumer performs recovery.

**Then**

If realtime is released, duplicate/out-of-order/gap/reconnect handling MUST converge by authoritative refetch/reconciliation and tenant-scoped subscription. Missing realtime MUST never permanently corrupt Dashboard or Metric state.

**Expected result:** durable refetch reconciles state/checkpoint once and delivery resumes without permanent loss/duplication.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; backend/src/Notrelix.API/Endpoints/Analytics/Reports/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: Metric/report freshness/completeness/recovery contract
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsFreshnessContractIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-065 — Reporting Snapshot is a derived report artifact, not transactional Domain authority

- Requirement: `ARREQ065` — Reporting Snapshot is a derived report artifact, not transactional Domain authority
- PLAN work unit: `AR-09-INV-001` — Relocate ReportingSnapshot out of Domain LegacyGap
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): none

**Given**

a ReportingSnapshot is captured and source state changes later.

**When**

snapshot and current source/rebuild are compared.

**Then**

P6 MUST treat `ReportingSnapshot` semantics as an Analytics reporting artifact/projection. It MUST NOT be promoted into source business authority or used as the reference projection for unrelated Metrics.

**Expected result:** the snapshot stays a historical reporting artifact and never authorizes/mutates source truth.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-066 — Reporting Snapshot leaves the Domain LegacyGap classification

- Requirement: `ARREQ066` — Reporting Snapshot leaves the Domain LegacyGap classification
- PLAN work unit: `AR-09-INV-001` — Relocate ReportingSnapshot out of Domain LegacyGap
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): none

**Given**

ReportingSnapshot is classified LegacyGap.

**When**

the P6 relocation is applied.

**Then**

The current Domain `ReportingSnapshot` LegacyGap MUST be closed by relocating snapshot persistence/modeling to the Analytics Application/Infrastructure reporting artifact boundary while retaining product semantics and migration compatibility.

**Expected result:** LegacyGap is removed only after reporting-artifact boundary, persistence and compatibility proof pass.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-067 — Snapshot identity contains reproducibility lineage

- Requirement: `ARREQ067` — Snapshot identity contains reproducibility lineage
- PLAN work unit: `AR-09-CORE-001` — Implement version-aware capture and read contracts
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): `ANA-TST-SNAP-001`

**Given**

two snapshots share report ID but differ in Metric version/cutoff.

**When**

artifact metadata is persisted/read.

**Then**

A released Snapshot MUST record report/Metric identity+version, Account/Workspace scope, schema version, source cutoff/watermark, captured/generated time and payload. `ReportType + SchemaVersion + Data + CapturedAt` alone is insufficient for certification.

**Expected result:** each records Metric/report version, tenant, schema, source cutoff and capture/generation lineage.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-068 — Snapshot schema readers are version-aware

- Requirement: `ARREQ068` — Snapshot schema readers are version-aware
- PLAN work unit: `AR-09-CORE-001` — Implement version-aware capture and read contracts
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): `ANA-TST-SNAP-MIG-001`

**Given**

an old snapshot remains after a new schema/Metric version ships.

**When**

new code reads old and new artifacts.

**Then**

Retained historical snapshot payloads MUST be read with their stored schema/Metric version. New code MUST NOT deserialize/reinterpret old data under a new schema silently.

**Expected result:** stored version selects the compatible reader; old payload is not silently reinterpreted.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-069 — Snapshot and current rebuilt projection may legitimately differ

- Requirement: `ARREQ069` — Snapshot and current rebuilt projection may legitimately differ
- PLAN work unit: `AR-09-SEC-001` — Implement historical-comparison and privacy semantics
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): none

**Given**

privacy correction changes current source after historical capture.

**When**

historical snapshot and current recomputation are shown.

**Then**

UI/API MUST distinguish 'what was reported then' from 'what current source recomputation says now' when retention, deletion, anonymization or Metric-version changes make them differ.

**Expected result:** the product distinguishes 'reported then' from 'recomputed now'.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-070 — Historical comparisons preserve Metric compatibility

- Requirement: `ARREQ070` — Historical comparisons preserve Metric compatibility
- PLAN work unit: `AR-09-SEC-001` — Implement historical-comparison and privacy semantics
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): none

**Given**

retained reporting artifacts from multiple versions/lifecycles exercise 'Historical comparisons preserve Metric compatibility'.

**When**

artifact reader/retention/migration path executes.

**Then**

A Snapshot or historical series MUST NOT be compared as one continuous semantic series across incompatible Metric versions unless explicit recomputation/mapping policy permits it.

**Expected result:** ARREQ070 preserves lineage/history/compatibility and source non-authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-071 — Source deletion follows explicit derived-data policy

- Requirement: `ARREQ071` — Source deletion follows explicit derived-data policy
- PLAN work unit: `AR-09-COMPAT-001` — Certify snapshot schema evolution and lifecycle
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): `ANA-TST-PRJ-DEL-001`

**Given**

retained reporting artifacts from multiple versions/lifecycles exercise 'Source deletion follows explicit derived-data policy'.

**When**

artifact reader/retention/migration path executes.

**Then**

Analytics-owned projection/snapshot rows are purged, anonymized, retained aggregate-only or recomputed according to product/privacy policy; deletion MUST NOT depend on accidental foreign SQL cascade.

**Expected result:** ARREQ071 preserves lineage/history/compatibility and source non-authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-072 — Snapshot/export retention and access are explicit

- Requirement: `ARREQ072` — Snapshot/export retention and access are explicit
- PLAN work unit: `AR-09-COMPAT-001` — Certify snapshot schema evolution and lifecycle
- PLAN disposition: `RELOCATE_LEGACY_GAP_AND_IMPLEMENT_ARTIFACT_LIFECYCLE`
- Preparation evidence state: `PARTIAL_LEGACY_GAP`
- Legacy alias/supporting ID(s): none

**Given**

retained reporting artifacts from multiple versions/lifecycles exercise 'Snapshot/export retention and access are explicit'.

**When**

artifact reader/retention/migration path executes.

**Then**

Retention, expiration, deletion, legal/privacy handling and download/read authorization for retained reporting artifacts MUST be governed independently from source active-state lifetime.

**Expected result:** ARREQ072 preserves lineage/history/compatibility and source non-authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Features/Analytics/Snapshots/; backend/src/Notrelix.Infrastructure/Data/Repositories/Analytics/ReportingSnapshotStore.cs; snapshot migrations/readers (target/current)`
- Runtime owner/proof boundary: reporting artifact store + compatibility + retention
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + migration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_LEGACY_GAP`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/ReportingSnapshotArtifactIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-073 — WorkManagement analytics consumes canonical Work facts

- Requirement: `ARREQ073` — WorkManagement analytics consumes canonical Work facts
- PLAN work unit: `AR-10-INV-001` — Admit Work/Workspace/Documents sources only from approved facts
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a Work Metric needs completion/overdue/workload meaning.

**When**

source onboarding reviews public Work contracts.

**Then**

Completion, overdue, active, archived, workload, field/status or cycle semantics MUST come from approved WorkManagement contracts. Report-local guesses such as label='Done' are forbidden.

**Expected result:** approved Work semantics are used; report-local guesses such as label='Done' are forbidden.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-074 — Documents analytics minimizes content exposure

- Requirement: `ARREQ074` — Documents analytics minimizes content exposure
- PLAN work unit: `AR-10-INV-001` — Admit Work/Workspace/Documents sources only from approved facts
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a Documents metric asks for counts plus rich content.

**When**

source onboarding runs.

**Then**

Documents metrics SHOULD consume metadata/events/reporting contracts; rich content or sensitive block data requires explicit product/security approval and source authorization.

**Expected result:** metadata/public reporting contracts are used and rich content is blocked unless explicitly admitted.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-075 — Collaboration analytics remains distinct from Activity/Audit

- Requirement: `ARREQ075` — Collaboration analytics remains distinct from Activity/Audit
- PLAN work unit: `AR-10-CORE-001` — Admit Collaboration and Automation sources with semantic boundaries
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

Collaboration facts coexist with Activity/Audit stores.

**When**

Collaboration Metrics are defined.

**Then**

Comment/reaction/collaboration Metrics MAY derive from collaboration facts, but user-facing Activity and governed Audit remain separate contracts and MUST NOT be reconstructed from Analytics counts.

**Expected result:** Analytics consumes Collaboration facts without reconstructing Activity/Audit authority.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-076 — Automation analytics preserves Automation execution semantics

- Requirement: `ARREQ076` — Automation analytics preserves Automation execution semantics
- PLAN work unit: `AR-10-CORE-001` — Admit Collaboration and Automation sources with semantic boundaries
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

Automation exposes execution/retry/provider-outcome facts.

**When**

an execution-success Metric is evaluated.

**Then**

Execution count, success/failure, duration and retry Metrics MUST consume Automation-owned execution facts and classifications; Analytics MUST NOT redefine execution success or provider outcome.

**Expected result:** Automation-owned terminal semantics are preserved; Analytics does not reclassify outcomes.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-077 — Integrations analytics preserves provider/sync semantics

- Requirement: `ARREQ077` — Integrations analytics preserves provider/sync semantics
- PLAN work unit: `AR-10-SEC-001` — Admit Integrations and Billing sources without redefining authority
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

Integration health facts coexist with credentials/raw provider payloads.

**When**

connection/sync/lag Metrics are defined.

**Then**

Connection health, sync failure, rate-limit or lag Metrics MUST consume Integrations-owned facts without provider credentials/raw sensitive payloads and without redefining provider state.

**Expected result:** only approved facts enter Analytics; credentials/raw sensitive payloads do not.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-078 — Billing analytics never becomes commercial authority

- Requirement: `ARREQ078` — Billing analytics never becomes commercial authority
- PLAN work unit: `AR-10-SEC-001` — Admit Integrations and Billing sources without redefining authority
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

Billing exposes subscription/usage/entitlement facts.

**When**

P6 renders trends beside Billing-calculated quantities.

**Then**

Subscription/usage/entitlement trends MAY be reported, but invoice totals, billable usage, entitlement decisions and commercial ledger semantics remain Billing-owned.

**Expected result:** Analytics reports but cannot charge, grant entitlement or own billable usage.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-079 — Account/workspace metrics use canonical Workspace/Identity facts and legacy usage models remain no-growth

- Requirement: `ARREQ079` — Account/workspace metrics use canonical Workspace/Identity facts and legacy usage models remain no-growth
- PLAN work unit: `AR-10-COMPAT-001` — Certify every source independently and freeze no-growth legacy read models
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `INFRASTRUCTURE_ONLY`
- Legacy alias/supporting ID(s): none

**Given**

AccountCreated, WorkspaceCreated and WorkspaceMemberAdded/Removed facts exist while WorkspaceUsageDaily/FeatureUsageDaily have no approved population path.

**When**

P6 defines account/workspace metrics and a developer attempts to use legacy usage tables directly.

**Then**

Account/workspace activity, workspace count and membership-derived Metrics MUST consume approved Account/Workspace/Identity public facts such as AccountCreated, WorkspaceCreated and WorkspaceMemberAdded/Removed (or a producer-owned reporting contract) and MUST NOT turn Analytics into a membership store. `WorkspaceUsageDaily` and `FeatureUsageDaily` remain infrastructure-only/no-growth until a named Metric owner, Source→Analytics handoff and production population/rebuild path are explicit.

**Expected result:** workspace/membership metrics use approved Workspace/Identity facts; Analytics never becomes membership authority and legacy usage models remain no-growth.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `INFRASTRUCTURE_ONLY`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-080 — Each source context is certified independently

- Requirement: `ARREQ080` — Each source context is certified independently
- PLAN work unit: `AR-10-COMPAT-001` — Certify every source independently and freeze no-growth legacy read models
- PLAN disposition: `BLOCK_UNTIL_SOURCE_HANDOFF_THEN_IMPLEMENT`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-X-001`

**Given**

Work is reference-ready while Workspace/Identity, Documents, Collaboration, Automation, Integrations and Billing have different readiness.

**When**

P6 source certification evaluates every lane.

**Then**

WorkManagement, Workspace/Account/Identity, Documents, Collaboration, Automation, Integrations and Billing source onboarding each require their own contract/readiness/security/rebuild evidence. A green Work reference projection cannot substitute for Workspace/Identity or any other source.

**Expected result:** each source retains independent evidence/readiness and no green source promotes another.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)
- Source under test: `backend/src/Notrelix.Application/Events/{Accounts,Workspaces,Identity,WorkManagement,Documents,Collaboration,Automation,Integrations,Billing}/; source-specific Analytics adapters/projections`
- Runtime owner/proof boundary: per-source public event/reporting-contract adapter
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / architecture + integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsSourceOnboardingArchitectureTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-081 — Cross-context report composes approved derived/source contracts

- Requirement: `ARREQ081` — Cross-context report composes approved derived/source contracts
- PLAN work unit: `AR-11-INV-001` — Define report identity, join and cutoff contracts
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-X-ARCH-001`

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

Cross-context Analytics MUST combine certified projections/Metric outputs or approved reporting contracts; it MUST NOT establish a permanent semantic dependency on foreign private tables.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ081 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-082 — Cross-context joins use stable shared identity

- Requirement: `ARREQ082` — Cross-context joins use stable shared identity
- PLAN work unit: `AR-11-INV-001` — Define report identity, join and cutoff contracts
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

Join keys MUST be explicitly compatible Account/Workspace/resource identities or another approved contract. Display name, email, CLR type or unrelated private database PK is not an analytical join contract.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ082 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-083 — Cross-context consistency is not overstated

- Requirement: `ARREQ083` — Cross-context consistency is not overstated
- PLAN work unit: `AR-11-CORE-001` — Implement report composition over certified projections
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-X-002`

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

The report MUST declare point-in-time/common-cutoff, bounded-staleness, or independent-source freshness semantics. P6 MUST NOT imply atomic multi-context consistency that the architecture does not provide.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ083 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-084 — Authorization is the intersection of participating sources

- Requirement: `ARREQ084` — Authorization is the intersection of participating sources
- PLAN work unit: `AR-11-CORE-001` — Implement report composition over certified projections
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-X-003`

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

A cross-context report MUST NOT broaden access. The viewer must satisfy the report action plus the participating source/privacy contracts required for the returned aggregate/detail.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ084 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-085 — Cross-context correction propagates deterministically

- Requirement: `ARREQ085` — Cross-context correction propagates deterministically
- PLAN work unit: `AR-11-SEC-001` — Implement partial/degraded and correction behavior
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

Source correction/version/deletion in any participating projection MUST update or invalidate the composed report according to declared semantics without retaining impossible combinations.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ085 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-086 — Partial multi-source results are explicit

- Requirement: `ARREQ086` — Partial multi-source results are explicit
- PLAN work unit: `AR-11-SEC-001` — Implement partial/degraded and correction behavior
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): `ANA-TST-FRESH-003`

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

If one source is unavailable or behind its accepted freshness target, the report MUST return explicit partial/degraded metadata or fail according to its contract; it MUST NOT manufacture zero/default values.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ086 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-087 — Cross-context cache identity includes every semantic dimension

- Requirement: `ARREQ087` — Cross-context cache identity includes every semantic dimension
- PLAN work unit: `AR-11-COMPAT-001` — Version cross-context reports and cache identity
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

Cache keys MUST include all source/Metric versions, tenant scope, report filters/timezone/cutoff and authorization-sensitive boundary required for safe reuse.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ087 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-088 — No synchronous transactional dependency on Analytics

- Requirement: `ARREQ088` — No synchronous transactional dependency on Analytics
- PLAN work unit: `AR-11-COMPAT-001` — Version cross-context reports and cache identity
- PLAN disposition: `IMPLEMENT_ONLY_FROM_CERTIFIED_INPUTS`
- Preparation evidence state: `MISSING_TARGET`
- Legacy alias/supporting ID(s): none

**Given**

the frozen initial catalog contains only single-source `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/report-catalog plus a synthetic future cross-context fixture are inspected.

**Then**

Source contexts MUST NOT call P6 reporting projections synchronously to authorize or decide their own transactional invariants. Analytics remains replaceable/downstream.

**Expected result:** no cross-context report is reachable now; future admission still enforces ARREQ088 without private-table/authorization/consistency shortcuts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)
- Source under test: `P6 release catalog + backend/src/Notrelix.Application/Features/Analytics/Reports/ architecture guard (cross-context non-reachable in initial P6)`
- Runtime owner/proof boundary: report composer over certified projections/Metric outputs
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_TARGET`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/CrossContextReportIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-089 — Report/Metric API is typed and bounded

- Requirement: `ARREQ089` — Report/Metric API is typed and bounded
- PLAN work unit: `AR-12-INV-001` — Define typed API surface and bounded query contract
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-API-001`

**Given**

`work-placement-overview:v1` and `work-item-count:v1` are the frozen initial query slice.

**When**

the public API validates valid/invalid filters, unsupported date range, grouping and freshness metadata.

**Then**

Released query APIs MUST expose report/Metric identity, Account/Workspace scope, filters, date range, timezone, grouping, sorting/pagination/limits and freshness/completeness metadata with deterministic validation/error semantics.

**Expected result:** only the frozen contract is exposed; unsupported date range/invalid grouping/filter fails deterministically.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-090 — Dashboard Application/API lifecycle is implemented before release

- Requirement: `ARREQ090` — Dashboard Application/API lifecycle is implemented before release
- PLAN work unit: `AR-12-INV-001` — Define typed API surface and bounded query contract
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

Dashboard Domain/EF exists but P6 API starts absent.

**When**

the frozen P6 lifecycle is implemented and called.

**Then**

P6 MUST provide canonical Application/API commands/queries for create/list/detail/rename, visibility, source management, widget add/update/move/remove and archive/delete behavior before claiming Dashboard production readiness.

**Expected result:** create/list/detail/rename/visibility/source/widget/move/archive/delete execute through canonical pipeline/persistence.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-091 — No direct HTTP source-table analytics

- Requirement: `ARREQ091` — No direct HTTP source-table analytics
- PLAN work unit: `AR-12-CORE-001` — Implement Application→API production composition
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

public contracts/frontend state exercise valid, invalid and tenant-switch cases for 'No direct HTTP source-table analytics'.

**When**

generated API/client and dedicated Analytics consumer execute.

**Then**

API handlers MUST call Analytics Application queries/use cases over Analytics-owned projections/Metric services. They MUST NOT perform ad-hoc joins across other bounded-context DbSets.

**Expected result:** ARREQ091 remains server-authoritative, bounded and tenant-safe.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-092 — Frontend uses server-owned Metric semantics

- Requirement: `ARREQ092` — Frontend uses server-owned Metric semantics
- PLAN work unit: `AR-12-CORE-001` — Implement Application→API production composition
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

public contracts/frontend state exercise valid, invalid and tenant-switch cases for 'Frontend uses server-owned Metric semantics'.

**When**

generated API/client and dedicated Analytics consumer execute.

**Then**

Frontend formats and visualizes returned Metric/report contracts but MUST NOT independently recalculate a semantically different numerator, denominator, timezone bucket or source filter.

**Expected result:** ARREQ092 remains server-authoritative, bounded and tenant-safe.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-093 — Workspace operational dashboard is not Analytics product evidence

- Requirement: `ARREQ093` — Workspace operational dashboard is not Analytics product evidence
- PLAN work unit: `AR-12-SEC-001` — Create dedicated frontend Analytics boundary
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

the Workspace operational dashboard and new dedicated Analytics route both exist.

**When**

P6 frontend readiness is evaluated.

**Then**

`frontend/apps/web/src/routes/workspaces/$workspaceId/dashboard.tsx` remains a Workspace-owned operational surface that queries Boards/Pages directly. It MUST NOT be counted as P6 Analytics frontend readiness.

**Expected result:** Workspace dashboard contributes zero P6 evidence; only the dedicated Analytics consumer counts.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-094 — P6 frontend has a dedicated Analytics contract boundary

- Requirement: `ARREQ094` — P6 frontend has a dedicated Analytics contract boundary
- PLAN work unit: `AR-12-SEC-001` — Create dedicated frontend Analytics boundary
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

the dedicated Analytics UI is introduced for `work-placement-overview:v1`.

**When**

package/network/query dependencies are inspected.

**Then**

If Dashboard/report UI is released, create/route a dedicated Analytics frontend package/feature using generated public API contracts and canonical tenant-aware query keys rather than extending the Workspace dashboard with handwritten report semantics.

**Expected result:** it consumes generated public contracts and canonical tenant query keys from `frontend/packages/features/analytics/`.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-095 — Frontend query keys partition tenant and report semantics

- Requirement: `ARREQ095` — Frontend query keys partition tenant and report semantics
- PLAN work unit: `AR-12-COMPAT-001` — Prove frontend semantic parity and tenant-safe state
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

public contracts/frontend state exercise valid, invalid and tenant-switch cases for 'Frontend query keys partition tenant and report semantics'.

**When**

generated API/client and dedicated Analytics consumer execute.

**Then**

Account, Workspace, report/Metric ID+version, filters, time range, timezone, grouping and authorization-sensitive identity MUST participate in server-state identity as required.

**Expected result:** ARREQ095 remains server-authoritative, bounded and tenant-safe.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-096 — Frontend state distinguishes zero/no-data/stale/partial/forbidden/unavailable

- Requirement: `ARREQ096` — Frontend state distinguishes zero/no-data/stale/partial/forbidden/unavailable
- PLAN work unit: `AR-12-COMPAT-001` — Prove frontend semantic parity and tenant-safe state
- PLAN disposition: `IMPLEMENT_PUBLIC_CONTRACT_AND_CONSUMER`
- Preparation evidence state: `MISSING_OR_PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

frontend receives zero/no-data/stale/partial/forbidden/unavailable then switches tenant.

**When**

UI/query state transitions occur.

**Then**

Loading/empty/error/degraded UX MUST preserve backend analytical meaning; Account/Workspace switch MUST clear/isolate previous tenant analytical data and pending work.

**Expected result:** states stay distinct and previous-tenant data/pending work cannot leak.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)
- Source under test: `backend/src/Notrelix.API/Endpoints/Analytics/; backend/src/Notrelix.Application/Features/Analytics/Reports/WorkPlacementOverview/; frontend/packages/features/analytics/ (target)`
- Runtime owner/proof boundary: public API/OpenAPI + dedicated Analytics frontend
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: `backend-ci + frontend-ci`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING_OR_PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsApiContractIntegrationTests.cs + frontend Analytics contract tests (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-097 — Export is a protected Analytics operation

- Requirement: `ARREQ097` — Export is a protected Analytics operation
- PLAN work unit: `AR-13-INV-001` — Admit export per report, not globally
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): none

**Given**

the initial Report handoff declares Export=none.

**When**

API/OpenAPI/frontend/background capability discovery is inspected.

**Then**

Every released export MUST name report/Metric version, tenant scope, filters/date/timezone, selected columns/dimensions, authorization policy, size/row limits and artifact retention.

**Expected result:** no export endpoint, worker, artifact route or UI action is reachable.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-098 — Export reuses the same query semantics as interactive reporting

- Requirement: `ARREQ098` — Export reuses the same query semantics as interactive reporting
- PLAN work unit: `AR-13-INV-001` — Admit export per report, not globally
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): `ANA-TST-SEC-003`, `ANA-TST-EXP-001`

**Given**

initial P6 declares no export format.

**When**

an export route/job is requested or accidentally registered.

**Then**

Export MUST use the certified Metric/report definition and source authorization. It MUST NOT recalculate formulas or bypass field/resource visibility.

**Expected result:** the capability remains non-reachable; future export must reuse interactive Metric/report authorization semantics.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-099 — Large export uses asynchronous report job semantics

- Requirement: `ARREQ099` — Large export uses asynchronous report job semantics
- PLAN work unit: `AR-13-CORE-001` — Implement durable export job only where synchronous limits are exceeded
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): none

**Given**

initial P6 declares no async export job.

**When**

background registrations and API routes are scanned.

**Then**

When synchronous limits are exceeded, P6 MUST use a durable report/export job with stable job identity and explicit queued/running/completed/failed/expired states; the request path MUST NOT hold an unbounded query/download open.

**Expected result:** no Analytics export job lifecycle is reachable.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-100 — Export artifact captures a source cutoff

- Requirement: `ARREQ100` — Export artifact captures a source cutoff
- PLAN work unit: `AR-13-CORE-001` — Implement durable export job only where synchronous limits are exceeded
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): none

**Given**

initial P6 has no generated export artifact.

**When**

the frozen report is queried.

**Then**

Generated files MUST record/report the data cutoff or snapshot time used so UI totals and downloaded content can be explained when freshness differs.

**Expected result:** no export artifact/cutoff contract is advertised.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-101 — Export download remains authorized

- Requirement: `ARREQ101` — Export download remains authorized
- PLAN work unit: `AR-13-SEC-001` — Protect artifact contents and download access
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): none

**Given**

initial P6 has no downloadable report artifact.

**When**

download routes/storage links are inspected.

**Then**

Artifact storage/download MUST use expiring or authenticated access and revalidate appropriate tenant/report authorization; permanent public URLs are forbidden unless separately designed.

**Expected result:** no export download surface is reachable.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-102 — Export artifact contains no hidden sensitive dimensions

- Requirement: `ARREQ102` — Export artifact contains no hidden sensitive dimensions
- PLAN work unit: `AR-13-SEC-001` — Protect artifact contents and download access
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): `ANA-TST-EXP-AUTHZ-001`

**Given**

initial P6 has no export payload.

**When**

API/frontend capability discovery is inspected.

**Then**

Hidden source fields, private resources and restricted PII MUST remain excluded under the same report policy even if the file format could technically include them.

**Expected result:** no hidden sensitive export dimension is reachable.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-103 — Export job retries cannot duplicate artifacts/effects unsafely

- Requirement: `ARREQ103` — Export job retries cannot duplicate artifacts/effects unsafely
- PLAN work unit: `AR-13-COMPAT-001` — Prove idempotent retry, retention and export/report parity
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): none

**Given**

initial P6 has no export worker/artifact lifecycle.

**When**

background registrations/retry policies are inspected.

**Then**

Retry uses stable job/artifact identity and MUST NOT produce ambiguous multiple final files or advance completed state before durable artifact creation.

**Expected result:** no export effect can be retried in initial P6.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-104 — Export and scheduled-report delivery are independently certifiable

- Requirement: `ARREQ104` — Export and scheduled-report delivery are independently certifiable
- PLAN work unit: `AR-13-COMPAT-001` — Prove idempotent retry, retention and export/report parity
- PLAN disposition: `BLOCK_UNTIL_REPORT_HANDOFF_ADMITS_EXPORT_THEN_IMPLEMENT`
- Preparation evidence state: `MISSING`
- Legacy alias/supporting ID(s): none

**Given**

initial P6 declares Export=none and ScheduledDelivery=none for `work-placement-overview:v1`.

**When**

API/OpenAPI/frontend/background/scheduler inventories are inspected.

**Then**

Initial P6 releases neither export nor scheduled-report delivery for `work-placement-overview:v1`; both MUST be non-reachable in API/frontend/background-worker capability discovery. If a later released slice admits export, ARREQ097–ARREQ103 apply in full. If a later slice admits scheduled reports, its handoff MUST define report definition/version, tenant scope, recipient identity, source cutoff/snapshot, delivery owner/channel, authorization at request time and delivery time, retry/idempotency, retention/expiration and failure visibility. Delivery belongs to Collaboration/Platform or another approved owner rather than becoming an ad-hoc Analytics mailer. Dashboard/Metric readiness MUST NOT imply export or scheduled-delivery readiness.

**Expected result:** export and scheduled delivery are DEFERRED/NON_REACHABLE; future schedule admission requires recipient, authorization-at-delivery, cutoff, delivery owner/channel, retry/idempotency, retention and failure visibility.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)
- Source under test: `P6 API/OpenAPI/background/frontend capability inventory (export and scheduled delivery non-reachable in initial P6)`
- Runtime owner/proof boundary: export/report job + artifact authorization
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / integration + artifact-store proof`
- Exact-candidate rerun: `YES`
- Existing evidence: `MISSING`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsExportJobIntegrationTests.cs (add)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-105 — Enterprise dashboard requests do not full-scan arbitrary JSON

- Requirement: `ARREQ105` — Enterprise dashboard requests do not full-scan arbitrary JSON
- PLAN work unit: `AR-14-INV-001` — Measure query patterns before storage expansion
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PERF-001`

**Given**

a high-cardinality Dashboard uses flexible config over large projection.

**When**

representative query plan is captured.

**Then**

Recurring high-cardinality filters/grouping/aggregation MUST use typed/indexed/materialized projection fields or another measured strategy rather than scanning arbitrary flexible JSON on every request.

**Expected result:** typed/indexed/materialized fields avoid arbitrary JSON full-scan per request.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-106 — Storage technology follows measured need

- Requirement: `ARREQ106` — Storage technology follows measured need
- PLAN work unit: `AR-14-INV-001` — Measure query patterns before storage expansion
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a production-like dataset challenges 'Storage technology follows measured need'.

**When**

query/reconciliation/observability evidence is captured.

**Then**

P6 stays in the modular-monolith/reporting store until measured load and isolation justify another data platform. A warehouse/service is not introduced solely because Analytics might scale differently.

**Expected result:** ARREQ106 meets measured correctness/performance/operational bounds without changing semantic authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-107 — Index/partition/pre-aggregation is metric-driven

- Requirement: `ARREQ107` — Index/partition/pre-aggregation is metric-driven
- PLAN work unit: `AR-14-CORE-001` — Add targeted indexes/materialization/pre-aggregation and safe cache
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a production-like dataset challenges 'Index/partition/pre-aggregation is metric-driven'.

**When**

query/reconciliation/observability evidence is captured.

**Then**

Indexes, partitions and pre-aggregates MUST map to measured query patterns and canonical Metric semantics and must retain tenant scope, rebuild and correction behavior.

**Expected result:** ARREQ107 meets measured correctness/performance/operational bounds without changing semantic authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-108 — Analytics cache keys are semantically complete

- Requirement: `ARREQ108` — Analytics cache keys are semantically complete
- PLAN work unit: `AR-14-CORE-001` — Add targeted indexes/materialization/pre-aggregation and safe cache
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a production-like dataset challenges 'Analytics cache keys are semantically complete'.

**When**

query/reconciliation/observability evidence is captured.

**Then**

Cache keys MUST include tenant, Metric/report version, filters, time range, timezone, grouping/cutoff/freshness identity and authorization scope needed for correctness.

**Expected result:** ARREQ108 meets measured correctness/performance/operational bounds without changing semantic authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-109 — Data-quality checks detect projection divergence

- Requirement: `ARREQ109` — Data-quality checks detect projection divergence
- PLAN work unit: `AR-14-SEC-001` — Implement data-quality reconciliation
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

critical projection contains seeded missing/duplicate/stale/orphaned rows.

**When**

data-quality reconciliation runs.

**Then**

Critical projections/metrics MUST detect missing/duplicate application, impossible counts, source/projection divergence, orphaned identities, unsupported source versions and lag beyond declared targets.

**Expected result:** all divergence classes are detected without unapproved private-source repair.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-110 — Critical Metrics have an approved reconciliation source

- Requirement: `ARREQ110` — Critical Metrics have an approved reconciliation source
- PLAN work unit: `AR-14-SEC-001` — Implement data-quality reconciliation
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

reconciliation requires authoritative current state.

**When**

repair resolves comparison source.

**Then**

Reconciliation MUST compare against an architecture-approved producer snapshot/invariant, not casually query private source tables. Mismatch policy and repair path are explicit.

**Expected result:** it uses an approved producer snapshot/invariant with explicit mismatch/repair policy.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-111 — Operational observability exposes lag/backfill/report health

- Requirement: `ARREQ111` — Operational observability exposes lag/backfill/report health
- PLAN work unit: `AR-14-COMPAT-001` — Implement observability and degraded-readiness evidence
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): `ANA-TST-PERF-002`, `ANA-TST-OBS-001`

**Given**

a production-like dataset challenges 'Operational observability exposes lag/backfill/report health'.

**When**

query/reconciliation/observability evidence is captured.

**Then**

Track consumer/projection failures, lag/oldest age where relevant, duplicate/stale apply, backfill progress, report latency, export backlog and recovery results without exposing secrets or unnecessary PII.

**Expected result:** ARREQ111 meets measured correctness/performance/operational bounds without changing semantic authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-112 — Operational telemetry is not product Analytics

- Requirement: `ARREQ112` — Operational telemetry is not product Analytics
- PLAN work unit: `AR-14-COMPAT-001` — Implement observability and degraded-readiness evidence
- PLAN disposition: `IMPLEMENT_MEASURED_HARDENING`
- Preparation evidence state: `PARTIAL_OR_TO_ADD`
- Legacy alias/supporting ID(s): none

**Given**

a production-like dataset challenges 'Operational telemetry is not product Analytics'.

**When**

query/reconciliation/observability evidence is captured.

**Then**

Queue depth, HTTP latency, CPU/memory and tracing metrics remain Operations signals unless intentionally transformed through an approved product Metric definition.

**Expected result:** ARREQ112 meets measured correctness/performance/operational bounds without changing semantic authority.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)
- Source under test: `work-placement-overview query plan/index/cache/reconciliation/observability target + existing Work placement projection runtime`
- Runtime owner/proof boundary: query plan/cache/reconciliation/observability
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: `backend-ci / integration + performance evidence`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL_OR_TO_ADD`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Analytics/AnalyticsPerformanceAndRecoveryIntegrationTests.cs (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-113 — Reporting schema migration has clean and supported-upgrade proof

- Requirement: `ARREQ113` — Reporting schema migration has clean and supported-upgrade proof
- PLAN work unit: `AR-15-INV-001` — Plan clean/upgrade migration and rebuild strategy per change
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-MIG-001`, `ANA-TST-MIG-003`

**Given**

clean DB and supported prior DB/config exist.

**When**

candidate migrations run on both.

**Then**

Changes to reporting tables, RLS, JSON config, projection identity, snapshot artifact schema or Metric registry MUST pass clean database, supported upgrade and pending-model-drift checks.

**Expected result:** clean create + supported upgrade succeed and no pending model drift remains.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-114 — Projection migration chooses migrate or rebuild explicitly

- Requirement: `ARREQ114` — Projection migration chooses migrate or rebuild explicitly
- PLAN work unit: `AR-15-INV-001` — Plan clean/upgrade migration and rebuild strategy per change
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-MIG-002`

**Given**

a clean DB plus supported previous schema/config/artifact version exercise 'Projection migration chooses migrate or rebuild explicitly'.

**When**

candidate migration/compatibility path executes.

**Then**

For each schema/semantic change, state whether rows migrate in place, rebuild into v2, dual-read/cutover or are discarded/recomputed; destructive reinterpretation is forbidden.

**Expected result:** ARREQ114 holds with no silent reinterpretation, RLS gap or unreadable supported history.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-115 — Metric semantic changes version public contracts

- Requirement: `ARREQ115` — Metric semantic changes version public contracts
- PLAN work unit: `AR-15-CORE-001` — Version Metric/Dashboard/Widget persisted contracts
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-OAS-001`

**Given**

a clean DB plus supported previous schema/config/artifact version exercise 'Metric semantic changes version public contracts'.

**When**

candidate migration/compatibility path executes.

**Then**

API/cache/snapshot/export/frontend contracts that identify a Metric MUST preserve old versions or provide an explicit compatible rollout/backfill and retirement window.

**Expected result:** ARREQ115 holds with no silent reinterpretation, RLS gap or unreadable supported history.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-116 — Persisted Dashboard/Widget configuration is migration-aware

- Requirement: `ARREQ116` — Persisted Dashboard/Widget configuration is migration-aware
- PLAN work unit: `AR-15-CORE-001` — Version Metric/Dashboard/Widget persisted contracts
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

stored widget/source config uses an old discriminator/schema.

**When**

candidate removes/renames shape.

**Then**

Widget/source discriminator/config schema changes MUST include readers/migration for stored dashboards before removing old shape support.

**Expected result:** compatibility reader/migration preserves supported stored dashboards before old support is removed.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-117 — Source contract version changes preserve consumer/backfill compatibility

- Requirement: `ARREQ117` — Source contract version changes preserve consumer/backfill compatibility
- PLAN work unit: `AR-15-SEC-001` — Preserve source-version backlog and RLS deployment safety
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a clean DB plus supported previous schema/config/artifact version exercise 'Source contract version changes preserve consumer/backfill compatibility'.

**When**

candidate migration/compatibility path executes.

**Then**

P6 MUST support the agreed old/new source event/reporting versions across backlog/replay and rebuild windows and MUST NOT silently abandon retained analytical inputs.

**Expected result:** ARREQ117 holds with no silent reinterpretation, RLS gap or unreadable supported history.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-118 — RLS policy deploy order is safe

- Requirement: `ARREQ118` — RLS policy deploy order is safe
- PLAN work unit: `AR-15-SEC-001` — Preserve source-version backlog and RLS deployment safety
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a workspace reporting table/RLS policy changes.

**When**

deployment ordering is exercised.

**Then**

Reporting tables/policies/indexes/migrations MUST deploy in an order that never creates an interval of broader tenant access or background processing without required session context.

**Expected result:** no interval permits broader tenant access or worker execution without required RLS scope.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-119 — Historical artifacts remain readable after code evolution

- Requirement: `ARREQ119` — Historical artifacts remain readable after code evolution
- PLAN work unit: `AR-15-COMPAT-001` — Prove historical artifacts and rollback/forward-fix
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a clean DB plus supported previous schema/config/artifact version exercise 'Historical artifacts remain readable after code evolution'.

**When**

candidate migration/compatibility path executes.

**Then**

Supported retained snapshots/exports/report definitions MUST either remain readable by compatibility readers or be migrated under an explicit retention policy.

**Expected result:** ARREQ119 holds with no silent reinterpretation, RLS gap or unreadable supported history.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-120 — Rollback/forward-fix preserves analytical meaning

- Requirement: `ARREQ120` — Rollback/forward-fix preserves analytical meaning
- PLAN work unit: `AR-15-COMPAT-001` — Prove historical artifacts and rollback/forward-fix
- PLAN disposition: `IMPLEMENT_D5_MIGRATION_DISCIPLINE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a clean DB plus supported previous schema/config/artifact version exercise 'Rollback/forward-fix preserves analytical meaning'.

**When**

candidate migration/compatibility path executes.

**Then**

Rollback MUST NOT restore code that interprets new persisted Metric/config/snapshot data under incompatible old semantics. When unsafe, use forward-fix/cutover instead.

**Expected result:** ARREQ120 holds with no silent reinterpretation, RLS gap or unreadable supported history.

- Test project/file: backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)
- Source under test: `backend/src/Notrelix.Infrastructure/Data/Migrations/; reporting RLS scripts; Dashboard/Metric/Snapshot compatibility readers`
- Runtime owner/proof boundary: EF migrations + reporting RLS + compatibility readers
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci / migration + RLS`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs + Analytics migration suites (extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-121 — Architecture gates forbid foreign persistence ownership and new Application EF coupling

- Requirement: `ARREQ121` — Architecture gates forbid foreign persistence ownership and new Application EF coupling
- PLAN work unit: `AR-16-INV-001` — Close architecture boundaries and production ownership
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

an Analytics handler/adapter directly references a foreign DbContext/repository.

**When**

architecture scans production assemblies.

**Then**

Architecture tests MUST prevent Analytics Application/Infrastructure consumers from depending on foreign bounded-context DbContexts/private repository types outside approved producer-owned public adapters. New P6 Application contracts MUST NOT expose `DbSet<T>`/EF Core persistence primitives. The existing `IReportingDbContext` is migration debt: P6 MUST replace its Analytics use cases with feature-owned repository/query-store contracts implemented in Infrastructure, without introducing a generic repository or a second transaction boundary.

**Expected result:** the build fails; only approved public source adapters pass.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-122 — Production runtime owner is proved

- Requirement: `ARREQ122` — Production runtime owner is proved
- PLAN work unit: `AR-16-INV-001` — Close architecture boundaries and production ownership
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

unit tests pass but production DI/broker/data-session composition is broken.

**When**

release verification executes actual graph.

**Then**

Projection/rebuild/report/background claims MUST be proven through actual production DI/message/data-session composition. Isolated Domain/unit tests alone cannot certify runtime reachability.

**Expected result:** certification fails until production owner/composition works.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-123 — TAC AR-FLOW reference is reused without becoming product authority

- Requirement: `ARREQ123` — TAC AR-FLOW reference is reused without becoming product authority
- PLAN work unit: `AR-16-CORE-001` — Execute required failure and production-runtime scenarios
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

AR-FLOW-01..04 are green while Metrics/Dashboard/Export remain absent.

**When**

P6 readiness is calculated.

**Then**

`AR-FLOW-01` live projection, `AR-FLOW-02` local read, `AR-FLOW-03` producer-backed rebuild and `AR-FLOW-04` failure/recovery remain canonical mechanism evidence for the Work placement reference; P6 product Metrics/Dashboard semantics remain owned here.

**Expected result:** TAC reference evidence cannot promote unrelated product capabilities.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-124 — Required failure paths are executable evidence

- Requirement: `ARREQ124` — Required failure paths are executable evidence
- PLAN work unit: `AR-16-CORE-001` — Execute required failure and production-runtime scenarios
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): `ANA-TST-REL-001`

**Given**

duplicate/stale/crash/source-unavailable/rebuild/cross-tenant/stale/migration failure fixtures exist.

**When**

applicable production tests execute.

**Then**

Duplicate, stale/out-of-order, crash-before-commit, source unavailable, rebuild retry, cross-tenant, forbidden drill-down, stale/partial, migration and large-query limit paths MUST have executable proof where applicable.

**Expected result:** every mandatory deliberate failure reaches the safe durable state or blocks certification.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-125 — CI reports non-zero execution and exact evidence

- Requirement: `ARREQ125` — CI reports non-zero execution and exact evidence
- PLAN work unit: `AR-16-SEC-001` — Enforce CI non-zero and capability-specific readiness
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

CI is green but a required test discovers zero or evidence is for another SHA.

**When**

evidence ingestion evaluates the run.

**Then**

Required backend/frontend/architecture/migration/runtime suites MUST report discovered/executed/passed/failed/skipped counts and exact workflow/job/artifact locators for the accepted candidate.

**Expected result:** candidate remains uncertified until non-zero counts and exact run/job/artifact bind to accepted SHA.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-126 — Readiness targets are capability-specific

- Requirement: `ARREQ126` — Readiness targets are capability-specific
- PLAN work unit: `AR-16-SEC-001` — Enforce CI non-zero and capability-specific readiness
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

Work placement is D5 while Dashboard/Export/source lanes differ.

**When**

P6 summary is produced.

**Then**

P6 certification MUST distinguish source readiness, Metric semantics, projection correctness, Dashboard/API/frontend, snapshot/export, privacy/security, freshness and operations. Deferred/unsupported slices stay explicit.

**Expected result:** readiness remains capability-specific and blocked/deferred slices remain visible.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-127 — Cross-team handoffs preserve team authority

- Requirement: `ARREQ127` — Cross-team handoffs preserve team authority
- PLAN work unit: `AR-16-COMPAT-001` — Publish team handoffs and final exact-candidate decision
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

a source/Metric/report crosses ownership.

**When**

the handoff is reviewed.

**Then**

P6 MUST use the source-to-Analytics, Metric and Report handoff schemas from the team authority and append candidate/evidence/invalidation metadata without replacing their required semantic fields.

**Expected result:** team-authoritative fields plus candidate/evidence/invalidation metadata are mandatory.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

## AR-TST-REQ-128 — P6 completion is an exact released-slice decision

- Requirement: `ARREQ128` — P6 completion is an exact released-slice decision
- PLAN work unit: `AR-16-COMPAT-001` — Publish team handoffs and final exact-candidate decision
- PLAN disposition: `VERIFY_AND_CERTIFY_EXACT_RELEASED_SLICE`
- Preparation evidence state: `PARTIAL`
- Legacy alias/supporting ID(s): none

**Given**

domain classes and unrelated green CI exist but only part of P6 is proven.

**When**

final decision runs.

**Then**

P6 is complete only for explicitly listed Metrics, source projections, reports, Dashboards, exports and frontend surfaces whose requirements/tests/certification reach target readiness on the accepted candidate; domain classes or green unrelated CI do not imply completion.

**Expected result:** only the explicitly named released slice certifies; remaining capabilities stay blocked/deferred/N/A.

- Test project/file: backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)
- Source under test: `backend/tests/Notrelix.Architecture.Tests/Analytics/ + production DI/messaging/RLS/frontend/CI graph`
- Runtime owner/proof boundary: architecture gates + production graph + exact evidence
- Positive/negative: YES — exercise the forbidden/inverse boundary or explicit non-reachability in addition to the happy path.
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: `backend-ci + frontend-ci + required runtime/security jobs`
- Exact-candidate rerun: `YES`
- Existing evidence: `PARTIAL`
- Required proof target: Execute this exact Given/When/Then in `backend/tests/Notrelix.Architecture.Tests/Analytics/AnalyticsReleaseArchitectureTests.cs + CI evidence (add/extend)`. Use production composition whenever broker, database/RLS, browser, artifact storage, migration or runtime reachability is claimed. Assert the expected durable result and the named negative/failure boundary.
- Execution evidence: `NOT_RECORDED`

# Legacy test ID preservation

| Legacy ID | Primary scenario | Requirement |
|---|---|---|
| `ANA-TST-SRC-001` | `AR-TST-REQ-011` | `ARREQ011` — Analytics does not create private-table source contracts |
| `ANA-TST-MET-001` | `AR-TST-REQ-017` | `ARREQ017` — Metric identity is stable and versioned |
| `ANA-TST-MET-002` | `AR-TST-REQ-020` | `ARREQ020` — Rate and percentage metrics define numerator and denominator |
| `ANA-TST-MET-003` | `AR-TST-REQ-023` | `ARREQ023` — Zero, no data, unknown, unavailable and unauthorized are distinct |
| `ANA-TST-MET-TIME-001` | `AR-TST-REQ-021` | `ARREQ021` — Time-based Metrics define calendar and timezone semantics |
| `ANA-TST-PRJ-IDEMP-001` | `AR-TST-REQ-026` | `ARREQ026` — Projection update is idempotent |
| `ANA-TST-PRJ-ORDER-001` | `AR-TST-REQ-028` | `ARREQ028` — Late and out-of-order facts cannot regress projection state |
| `ANA-TST-PRJ-REPLAY-001` | `AR-TST-REQ-031` | `ARREQ031` — Projection rebuild is an explicit supported path |
| `ANA-TST-PRJ-RESTART-001` | `AR-TST-REQ-029` | `ARREQ029` — Projection failure commits no false checkpoint |
| `ANA-TST-PRJ-CORR-001` | `AR-TST-REQ-022` | `ARREQ022` — Metric corrections are explicit |
| `ANA-TST-PRJ-DEL-001` | `AR-TST-REQ-071` | `ARREQ071` — Source deletion follows explicit derived-data policy |
| `ANA-TST-PRJ-BF-001` | `AR-TST-REQ-032` | `ARREQ032` — Backfill and live traffic converge without double counting |
| `ANA-TST-X-001` | `AR-TST-REQ-080` | `ARREQ080` — Each source context is certified independently |
| `ANA-TST-X-002` | `AR-TST-REQ-083` | `ARREQ083` — Cross-context consistency is not overstated |
| `ANA-TST-X-003` | `AR-TST-REQ-084` | `ARREQ084` — Authorization is the intersection of participating sources |
| `ANA-TST-X-ARCH-001` | `AR-TST-REQ-081` | `ARREQ081` — Cross-context report composes approved derived/source contracts |
| `ANA-TST-SEC-001` | `AR-TST-REQ-053` | `ARREQ053` — Cross-tenant analytics is privileged and explicit |
| `ANA-TST-SEC-002` | `AR-TST-REQ-051` | `ARREQ051` — Aggregate visibility does not imply drill-down visibility |
| `ANA-TST-SEC-003` | `AR-TST-REQ-098` | `ARREQ098` — Export reuses the same query semantics as interactive reporting |
| `ANA-TST-SEC-004` | `AR-TST-REQ-054` | `ARREQ054` — Authorization-sensitive caching is partitioned |
| `ANA-TST-PRIV-001` | `AR-TST-REQ-052` | `ARREQ052` — Aggregation does not erase confidentiality |
| `ANA-TST-FRESH-001` | `AR-TST-REQ-059` | `ARREQ059` — Stale, unavailable and not-yet-projected are distinct |
| `ANA-TST-FRESH-002` | `AR-TST-REQ-060` | `ARREQ060` — Multi-source reports declare completeness and cutoff strategy |
| `ANA-TST-FRESH-003` | `AR-TST-REQ-086` | `ARREQ086` — Partial multi-source results are explicit |
| `ANA-TST-SNAP-001` | `AR-TST-REQ-067` | `ARREQ067` — Snapshot identity contains reproducibility lineage |
| `ANA-TST-SNAP-MIG-001` | `AR-TST-REQ-068` | `ARREQ068` — Snapshot schema readers are version-aware |
| `ANA-TST-EXP-001` | `AR-TST-REQ-098` | `ARREQ098` — Export reuses the same query semantics as interactive reporting |
| `ANA-TST-EXP-AUTHZ-001` | `AR-TST-REQ-102` | `ARREQ102` — Export artifact contains no hidden sensitive dimensions |
| `ANA-TST-PERF-001` | `AR-TST-REQ-105` | `ARREQ105` — Enterprise dashboard requests do not full-scan arbitrary JSON |
| `ANA-TST-PERF-002` | `AR-TST-REQ-111` | `ARREQ111` — Operational observability exposes lag/backfill/report health |
| `ANA-TST-REL-001` | `AR-TST-REQ-124` | `ARREQ124` — Required failure paths are executable evidence |
| `ANA-TST-OBS-001` | `AR-TST-REQ-111` | `ARREQ111` — Operational observability exposes lag/backfill/report health |
| `ANA-TST-MIG-001` | `AR-TST-REQ-113` | `ARREQ113` — Reporting schema migration has clean and supported-upgrade proof |
| `ANA-TST-MIG-002` | `AR-TST-REQ-114` | `ARREQ114` — Projection migration chooses migrate or rebuild explicitly |
| `ANA-TST-MIG-003` | `AR-TST-REQ-113` | `ARREQ113` — Reporting schema migration has clean and supported-upgrade proof |
| `ANA-TST-API-001` | `AR-TST-REQ-089` | `ARREQ089` — Report/Metric API is typed and bounded |
| `ANA-TST-OAS-001` | `AR-TST-REQ-115` | `ARREQ115` — Metric semantic changes version public contracts |

# Product-rule verification routing

| Product rule | ARREQ coverage | Primary tests |
|---|---|---|
| `ANA-001` | ARREQ001, ARREQ025, ARREQ065 | `AR-TST-REQ-001`, `AR-TST-REQ-025`, `AR-TST-REQ-065` |
| `ANA-002` | ARREQ002, ARREQ048, ARREQ091 | `AR-TST-REQ-002`, `AR-TST-REQ-048`, `AR-TST-REQ-091` |
| `ANA-003` | ARREQ017, ARREQ024, ARREQ115 | `AR-TST-REQ-017`, `AR-TST-REQ-024`, `AR-TST-REQ-115` |
| `ANA-004` | ARREQ018 | `AR-TST-REQ-018` |
| `ANA-005` | ARREQ009–ARREQ016 | `AR-TST-REQ-009`, `AR-TST-REQ-010`, `AR-TST-REQ-011`, `AR-TST-REQ-012`, `AR-TST-REQ-013`, `AR-TST-REQ-014`, `AR-TST-REQ-015`, `AR-TST-REQ-016` |
| `ANA-006` | ARREQ041, ARREQ044, ARREQ048 | `AR-TST-REQ-041`, `AR-TST-REQ-044`, `AR-TST-REQ-048` |
| `ANA-007` | ARREQ041–ARREQ043, ARREQ049 | `AR-TST-REQ-041`, `AR-TST-REQ-042`, `AR-TST-REQ-043`, `AR-TST-REQ-049` |
| `ANA-008` | ARREQ043, ARREQ050–ARREQ052 | `AR-TST-REQ-043`, `AR-TST-REQ-050`, `AR-TST-REQ-051`, `AR-TST-REQ-052` |
| `ANA-009` | ARREQ011–ARREQ012, ARREQ045 | `AR-TST-REQ-011`, `AR-TST-REQ-012`, `AR-TST-REQ-045` |
| `ANA-010` | ARREQ046–ARREQ047 | `AR-TST-REQ-046`, `AR-TST-REQ-047` |
| `ANA-011` | ARREQ048 | `AR-TST-REQ-048` |
| `ANA-012` | ARREQ020 | `AR-TST-REQ-020` |
| `ANA-013` | ARREQ021 | `AR-TST-REQ-021` |
| `ANA-014` | ARREQ057–ARREQ060 | `AR-TST-REQ-057`, `AR-TST-REQ-058`, `AR-TST-REQ-059`, `AR-TST-REQ-060` |
| `ANA-015` | ARREQ065, ARREQ067 | `AR-TST-REQ-065`, `AR-TST-REQ-067` |
| `ANA-016` | ARREQ067–ARREQ070, ARREQ119 | `AR-TST-REQ-067`, `AR-TST-REQ-068`, `AR-TST-REQ-069`, `AR-TST-REQ-070`, `AR-TST-REQ-119` |
| `ANA-017` | ARREQ013, ARREQ071–ARREQ072 | `AR-TST-REQ-013`, `AR-TST-REQ-071`, `AR-TST-REQ-072` |
| `ANA-018` | ARREQ025 | `AR-TST-REQ-025` |
| `ANA-019` | ARREQ031, ARREQ037 | `AR-TST-REQ-031`, `AR-TST-REQ-037` |
| `ANA-020` | ARREQ032 | `AR-TST-REQ-032` |
| `ANA-021` | ARREQ105–ARREQ107 | `AR-TST-REQ-105`, `AR-TST-REQ-106`, `AR-TST-REQ-107` |
| `ANA-022` | ARREQ078 | `AR-TST-REQ-078` |
| `ANA-023` | ARREQ014, ARREQ052, ARREQ055 | `AR-TST-REQ-014`, `AR-TST-REQ-052`, `AR-TST-REQ-055` |
| `ANA-024` | ARREQ053 | `AR-TST-REQ-053` |
| `ANA-025` | ARREQ054, ARREQ108 | `AR-TST-REQ-054`, `AR-TST-REQ-108` |
| `ANA-026` | ARREQ097–ARREQ103 | `AR-TST-REQ-097`, `AR-TST-REQ-098`, `AR-TST-REQ-099`, `AR-TST-REQ-100`, `AR-TST-REQ-101`, `AR-TST-REQ-102`, `AR-TST-REQ-103` |
| `ANA-027` | ARREQ019–ARREQ021 | `AR-TST-REQ-019`, `AR-TST-REQ-020`, `AR-TST-REQ-021` |
| `ANA-028` | ARREQ024, ARREQ070, ARREQ115 | `AR-TST-REQ-024`, `AR-TST-REQ-070`, `AR-TST-REQ-115` |
| `ANA-029` | ARREQ023, ARREQ059, ARREQ096 | `AR-TST-REQ-023`, `AR-TST-REQ-059`, `AR-TST-REQ-096` |
| `ANA-030` | ARREQ060, ARREQ083, ARREQ086 | `AR-TST-REQ-060`, `AR-TST-REQ-083`, `AR-TST-REQ-086` |
| `ANA-031` | ARREQ051 | `AR-TST-REQ-051` |
| `ANA-032` | ARREQ045–ARREQ047 | `AR-TST-REQ-045`, `AR-TST-REQ-046`, `AR-TST-REQ-047` |
| `ANA-033` | ARREQ044 | `AR-TST-REQ-044` |
| `ANA-034` | ARREQ004–ARREQ005, ARREQ128 | `AR-TST-REQ-004`, `AR-TST-REQ-005`, `AR-TST-REQ-128` |
| `ANA-035` | ARREQ112 | `AR-TST-REQ-112` |
| `ANA-036` | ARREQ062–ARREQ064 | `AR-TST-REQ-062`, `AR-TST-REQ-063`, `AR-TST-REQ-064` |
| `ANA-037` | ARREQ055–ARREQ056, ARREQ118 | `AR-TST-REQ-055`, `AR-TST-REQ-056`, `AR-TST-REQ-118` |
| `ANA-038` | ARREQ068–ARREQ070 | `AR-TST-REQ-068`, `AR-TST-REQ-069`, `AR-TST-REQ-070` |

# Team-capability verification routing

| Team capability | Meaning | ARREQ coverage | Primary tests |
|---|---|---|---|
| `team ANA-001` | Source-event/reporting-contract inventory | ARREQ009–ARREQ016 | `AR-TST-REQ-009`, `AR-TST-REQ-010`, `AR-TST-REQ-011`, `AR-TST-REQ-012`, `AR-TST-REQ-013`, `AR-TST-REQ-014`, `AR-TST-REQ-015`, `AR-TST-REQ-016` |
| `team ANA-002` | Metric definition registry | ARREQ017–ARREQ024 | `AR-TST-REQ-017`, `AR-TST-REQ-018`, `AR-TST-REQ-019`, `AR-TST-REQ-020`, `AR-TST-REQ-021`, `AR-TST-REQ-022`, `AR-TST-REQ-023`, `AR-TST-REQ-024` |
| `team ANA-003` | Analytical projection foundation | ARREQ025–ARREQ032 | `AR-TST-REQ-025`, `AR-TST-REQ-026`, `AR-TST-REQ-027`, `AR-TST-REQ-028`, `AR-TST-REQ-029`, `AR-TST-REQ-030`, `AR-TST-REQ-031`, `AR-TST-REQ-032` |
| `team ANA-004` | Projection identity/idempotency | ARREQ026, ARREQ029 | `AR-TST-REQ-026`, `AR-TST-REQ-029` |
| `team ANA-005` | Projection ordering/late-event policy | ARREQ027–ARREQ028 | `AR-TST-REQ-027`, `AR-TST-REQ-028` |
| `team ANA-006` | Projection rebuild/backfill | ARREQ031–ARREQ032 | `AR-TST-REQ-031`, `AR-TST-REQ-032` |
| `team ANA-007` | Account/workspace metrics | ARREQ049, ARREQ073–ARREQ080 | `AR-TST-REQ-049`, `AR-TST-REQ-073`, `AR-TST-REQ-074`, `AR-TST-REQ-075`, `AR-TST-REQ-076`, `AR-TST-REQ-077`, `AR-TST-REQ-078`, `AR-TST-REQ-079`, `AR-TST-REQ-080` |
| `team ANA-008` | WorkManagement analytics | ARREQ033–ARREQ040, ARREQ073 | `AR-TST-REQ-033`, `AR-TST-REQ-034`, `AR-TST-REQ-035`, `AR-TST-REQ-036`, `AR-TST-REQ-037`, `AR-TST-REQ-038`, `AR-TST-REQ-039`, `AR-TST-REQ-040`, `AR-TST-REQ-073` |
| `team ANA-009` | Documents/Collaboration analytics | ARREQ074–ARREQ075 | `AR-TST-REQ-074`, `AR-TST-REQ-075` |
| `team ANA-010` | Automation/Integrations analytics | ARREQ076–ARREQ077 | `AR-TST-REQ-076`, `AR-TST-REQ-077` |
| `team ANA-011` | Billing/usage analytics | ARREQ078–ARREQ080 | `AR-TST-REQ-078`, `AR-TST-REQ-079`, `AR-TST-REQ-080` |
| `team ANA-012` | Cross-context report composition | ARREQ081–ARREQ088 | `AR-TST-REQ-081`, `AR-TST-REQ-082`, `AR-TST-REQ-083`, `AR-TST-REQ-084`, `AR-TST-REQ-085`, `AR-TST-REQ-086`, `AR-TST-REQ-087`, `AR-TST-REQ-088` |
| `team ANA-013` | Report/query API | ARREQ089–ARREQ091 | `AR-TST-REQ-089`, `AR-TST-REQ-090`, `AR-TST-REQ-091` |
| `team ANA-014` | Dashboard/report frontend | ARREQ092–ARREQ096 | `AR-TST-REQ-092`, `AR-TST-REQ-093`, `AR-TST-REQ-094`, `AR-TST-REQ-095`, `AR-TST-REQ-096` |
| `team ANA-015` | Filters/date/timezone semantics | ARREQ019–ARREQ023, ARREQ089 | `AR-TST-REQ-019`, `AR-TST-REQ-020`, `AR-TST-REQ-021`, `AR-TST-REQ-022`, `AR-TST-REQ-023`, `AR-TST-REQ-089` |
| `team ANA-016` | Freshness/staleness contract | ARREQ057–ARREQ064 | `AR-TST-REQ-057`, `AR-TST-REQ-058`, `AR-TST-REQ-059`, `AR-TST-REQ-060`, `AR-TST-REQ-061`, `AR-TST-REQ-062`, `AR-TST-REQ-063`, `AR-TST-REQ-064` |
| `team ANA-017` | Export | ARREQ097–ARREQ104 | `AR-TST-REQ-097`, `AR-TST-REQ-098`, `AR-TST-REQ-099`, `AR-TST-REQ-100`, `AR-TST-REQ-101`, `AR-TST-REQ-102`, `AR-TST-REQ-103`, `AR-TST-REQ-104` |
| `team ANA-018` | Authorization/visibility | ARREQ049–ARREQ056 | `AR-TST-REQ-049`, `AR-TST-REQ-050`, `AR-TST-REQ-051`, `AR-TST-REQ-052`, `AR-TST-REQ-053`, `AR-TST-REQ-054`, `AR-TST-REQ-055`, `AR-TST-REQ-056` |
| `team ANA-019` | Retention/data lifecycle | ARREQ065–ARREQ072 | `AR-TST-REQ-065`, `AR-TST-REQ-066`, `AR-TST-REQ-067`, `AR-TST-REQ-068`, `AR-TST-REQ-069`, `AR-TST-REQ-070`, `AR-TST-REQ-071`, `AR-TST-REQ-072` |
| `team ANA-020` | Performance/storage strategy | ARREQ105–ARREQ108 | `AR-TST-REQ-105`, `AR-TST-REQ-106`, `AR-TST-REQ-107`, `AR-TST-REQ-108` |
| `team ANA-021` | Observability | ARREQ109–ARREQ112 | `AR-TST-REQ-109`, `AR-TST-REQ-110`, `AR-TST-REQ-111`, `AR-TST-REQ-112` |
| `team ANA-022` | Data quality/reconciliation | ARREQ109–ARREQ110 | `AR-TST-REQ-109`, `AR-TST-REQ-110` |
| `team ANA-023` | Migration/versioning | ARREQ113–ARREQ120 | `AR-TST-REQ-113`, `AR-TST-REQ-114`, `AR-TST-REQ-115`, `AR-TST-REQ-116`, `AR-TST-REQ-117`, `AR-TST-REQ-118`, `AR-TST-REQ-119`, `AR-TST-REQ-120` |
| `team ANA-024` | Hardening | ARREQ121–ARREQ128 | `AR-TST-REQ-121`, `AR-TST-REQ-122`, `AR-TST-REQ-123`, `AR-TST-REQ-124`, `AR-TST-REQ-125`, `AR-TST-REQ-126`, `AR-TST-REQ-127`, `AR-TST-REQ-128` |

# TAC AR-FLOW verification routing

| Flow | Meaning | ARREQ | Primary tests | Preparation rule |
|---|---|---|---|---|
| `AR-FLOW-01` | Live Work placement projection | ARREQ026–ARREQ029, ARREQ033–ARREQ035, ARREQ038 | `AR-TST-REQ-026`, `AR-TST-REQ-027`, `AR-TST-REQ-028`, `AR-TST-REQ-029`, `AR-TST-REQ-033`, `AR-TST-REQ-034`, `AR-TST-REQ-035`, `AR-TST-REQ-038` | `IMPLEMENTED_REFERENCE / exact-candidate rerun`; rerun when event/revision/rebuild/RLS/messaging/persistence changes. |
| `AR-FLOW-02` | Analytics local read | ARREQ030, ARREQ036, ARREQ039 | `AR-TST-REQ-030`, `AR-TST-REQ-036`, `AR-TST-REQ-039` | `IMPLEMENTED_INTERNAL / exact-candidate rerun`; rerun when event/revision/rebuild/RLS/messaging/persistence changes. |
| `AR-FLOW-03` | Producer-backed rebuild | ARREQ012, ARREQ031–ARREQ032, ARREQ035, ARREQ037 | `AR-TST-REQ-012`, `AR-TST-REQ-031`, `AR-TST-REQ-032`, `AR-TST-REQ-035`, `AR-TST-REQ-037` | `IMPLEMENTED_REFERENCE / exact-candidate rerun`; rerun when event/revision/rebuild/RLS/messaging/persistence changes. |
| `AR-FLOW-04` | Projection failure and recovery | ARREQ029, ARREQ032, ARREQ038, ARREQ124 | `AR-TST-REQ-029`, `AR-TST-REQ-032`, `AR-TST-REQ-038`, `AR-TST-REQ-124` | `IMPLEMENTED_REFERENCE / exact-candidate rerun`; rerun when event/revision/rebuild/RLS/messaging/persistence changes. |

# Confirmed-gap proof routing

| Gap | Status | Requirement coverage | Primary proof | Closure rule |
|---|---|---|---|---|
| `AR-GAP-01` — No generic Metric registry/runtime | `CONFIRMED` | ARREQ017–ARREQ024 | `AR-TST-REQ-017`, `AR-TST-REQ-018`, `AR-TST-REQ-019`, `AR-TST-REQ-020`, `AR-TST-REQ-021`, `AR-TST-REQ-022`, `AR-TST-REQ-023`, `AR-TST-REQ-024` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-02` — Dashboard/Widget is Domain+persistence only | `CONFIRMED` | ARREQ041–ARREQ048, ARREQ090 | `AR-TST-REQ-041`, `AR-TST-REQ-042`, `AR-TST-REQ-043`, `AR-TST-REQ-044`, `AR-TST-REQ-045`, `AR-TST-REQ-046`, `AR-TST-REQ-047`, `AR-TST-REQ-048`, `AR-TST-REQ-090` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-03` — Dashboard ownership/visibility contract incomplete | `CONFIRMED` | ARREQ042–ARREQ043, ARREQ049–ARREQ056 | `AR-TST-REQ-042`, `AR-TST-REQ-043`, `AR-TST-REQ-049`, `AR-TST-REQ-050`, `AR-TST-REQ-051`, `AR-TST-REQ-052`, `AR-TST-REQ-053`, `AR-TST-REQ-054`, `AR-TST-REQ-055`, `AR-TST-REQ-056` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-04` — Dashboard Source/Widget config is only partially semantic | `CONFIRMED` | ARREQ045–ARREQ047, ARREQ116 | `AR-TST-REQ-045`, `AR-TST-REQ-046`, `AR-TST-REQ-047`, `AR-TST-REQ-116` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-05` — Duplicate widget vocabularies | `CONFIRMED` | ARREQ046, ARREQ116 | `AR-TST-REQ-046`, `AR-TST-REQ-116` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-06` — ReportingSnapshot is an architecture LegacyGap | `CONFIRMED` | ARREQ065–ARREQ072 | `AR-TST-REQ-065`, `AR-TST-REQ-066`, `AR-TST-REQ-067`, `AR-TST-REQ-068`, `AR-TST-REQ-069`, `AR-TST-REQ-070`, `AR-TST-REQ-071`, `AR-TST-REQ-072` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-07` — Infrastructure daily usage models have no proven P6 producer | `CONFIRMED` | ARREQ079 | `AR-TST-REQ-079` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-08` — Only Work placement projection is production-hardened reference | `CONFIRMED` | ARREQ033–ARREQ040, ARREQ073–ARREQ080 | `AR-TST-REQ-033`, `AR-TST-REQ-034`, `AR-TST-REQ-035`, `AR-TST-REQ-036`, `AR-TST-REQ-037`, `AR-TST-REQ-038`, `AR-TST-REQ-039`, `AR-TST-REQ-040`, `AR-TST-REQ-073`, `AR-TST-REQ-074`, `AR-TST-REQ-075`, `AR-TST-REQ-076`, `AR-TST-REQ-077`, `AR-TST-REQ-078`, `AR-TST-REQ-079`, `AR-TST-REQ-080` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-09` — No released report/Metric/Dashboard API | `CONFIRMED` | ARREQ089–ARREQ091 | `AR-TST-REQ-089`, `AR-TST-REQ-090`, `AR-TST-REQ-091` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-10` — No dedicated Analytics frontend surface | `CONFIRMED` | ARREQ092–ARREQ096 | `AR-TST-REQ-092`, `AR-TST-REQ-093`, `AR-TST-REQ-094`, `AR-TST-REQ-095`, `AR-TST-REQ-096` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-11` — No export/report job pipeline | `CONFIRMED` | ARREQ097–ARREQ104 | `AR-TST-REQ-097`, `AR-TST-REQ-098`, `AR-TST-REQ-099`, `AR-TST-REQ-100`, `AR-TST-REQ-101`, `AR-TST-REQ-102`, `AR-TST-REQ-103`, `AR-TST-REQ-104` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-12` — No cross-context report composition runtime | `CONFIRMED` | ARREQ081–ARREQ088 | `AR-TST-REQ-081`, `AR-TST-REQ-082`, `AR-TST-REQ-083`, `AR-TST-REQ-084`, `AR-TST-REQ-085`, `AR-TST-REQ-086`, `AR-TST-REQ-087`, `AR-TST-REQ-088` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-13` — Freshness/completeness contract not exposed end to end | `PARTIAL_GAP` | ARREQ057–ARREQ060 | `AR-TST-REQ-057`, `AR-TST-REQ-058`, `AR-TST-REQ-059`, `AR-TST-REQ-060` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-14` — No Analytics realtime producer/consumer contract | `CONFIRMED` | ARREQ062–ARREQ064 | `AR-TST-REQ-062`, `AR-TST-REQ-063`, `AR-TST-REQ-064` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-15` — Retention/deletion/privacy lifecycle not executable for reporting artifacts | `PARTIAL_GAP` | ARREQ071–ARREQ072, ARREQ055 | `AR-TST-REQ-055`, `AR-TST-REQ-071`, `AR-TST-REQ-072` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-16` — Analytics-specific data-quality/ops signals incomplete | `PARTIAL_GAP` | ARREQ109–ARREQ112 | `AR-TST-REQ-109`, `AR-TST-REQ-110`, `AR-TST-REQ-111`, `AR-TST-REQ-112` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-17` — Historical TAC status is mechanism evidence, not product readiness | `DOC_STALE_RISK` | ARREQ040, ARREQ123–ARREQ128 | `AR-TST-REQ-040`, `AR-TST-REQ-123`, `AR-TST-REQ-124`, `AR-TST-REQ-125`, `AR-TST-REQ-126`, `AR-TST-REQ-127`, `AR-TST-REQ-128` | exact-candidate executable proof or affected capability remains blocked/deferred |
| `AR-GAP-18` — Existing execution docs are materially under-specified | `CONFIRMED` | ARREQ003–ARREQ008, ARREQ125–ARREQ128 | `AR-TST-REQ-003`, `AR-TST-REQ-004`, `AR-TST-REQ-005`, `AR-TST-REQ-006`, `AR-TST-REQ-007`, `AR-TST-REQ-008`, `AR-TST-REQ-125`, `AR-TST-REQ-126`, `AR-TST-REQ-127`, `AR-TST-REQ-128` | exact-candidate executable proof or affected capability remains blocked/deferred |

# Aggregate release-gate routing

| PLAN gate | Scope | Required primary tests | Gate rule |
|---|---|---|---|
| `AR-GATE-001` | `ARREQ001–ARREQ040` | `AR-TST-REQ-001..040` | authority/source/Metric/projection/Work reference meet target readiness |
| `AR-GATE-002` | `ARREQ041–ARREQ072` | `AR-TST-REQ-041..072` | Dashboard/security/freshness/snapshot are safe and honest |
| `AR-GATE-003` | `ARREQ073–ARREQ104` | `AR-TST-REQ-073..104` | source onboarding plus released API/frontend slice pass; cross-context/export/scheduled delivery remain explicitly deferred unless admitted |
| `AR-GATE-004` | `ARREQ105–ARREQ128` | `AR-TST-REQ-105..128` | performance/data-quality plus migration/architecture/runtime/exact-candidate evidence pass |

# Critical production-proof scenarios

```text
AR-TST-REQ-026 duplicate projection application
AR-TST-REQ-028 stale/out-of-order projection
AR-TST-REQ-029 crash-before-commit / false checkpoint
AR-TST-REQ-031 producer-owned rebuild
AR-TST-REQ-032 rebuild + live overlap
AR-TST-REQ-033 producer revision ordering
AR-TST-REQ-034 event-sufficient no-source-read
AR-TST-REQ-035 incomplete-event producer public source
AR-TST-REQ-037 rebuild does not overwrite newer live fact
AR-TST-REQ-038 real runtime crash/retry convergence
AR-TST-REQ-043 Public Dashboard non-reachability
AR-TST-REQ-047 typed/versioned Widget semantic validation
AR-TST-REQ-050 Dashboard visibility != source permission
AR-TST-REQ-051 aggregate != detail authorization
AR-TST-REQ-053 cross-tenant denial
AR-TST-REQ-054 authorization-sensitive cache isolation
AR-TST-REQ-060 multi-source completeness/cutoff
AR-TST-REQ-066 ReportingSnapshot LegacyGap closure
AR-TST-REQ-068 retained snapshot compatibility reader
AR-TST-REQ-079 infrastructure usage-model no-growth
AR-TST-REQ-080 source-specific readiness isolation
AR-TST-REQ-083 no false cross-context atomic consistency
AR-TST-REQ-084 authorization intersection
AR-TST-REQ-086 partial/unavailable source behavior
AR-TST-REQ-090 full Dashboard Application/API lifecycle
AR-TST-REQ-093 Workspace dashboard is not P6 evidence
AR-TST-REQ-094 dedicated Analytics frontend boundary
AR-TST-REQ-098 export/report semantic parity
AR-TST-REQ-101 artifact download authorization
AR-TST-REQ-103 export crash/idempotency
AR-TST-REQ-105 no arbitrary JSON full scan
AR-TST-REQ-109 divergence detection
AR-TST-REQ-113 clean + supported upgrade + no model drift
AR-TST-REQ-118 RLS-safe deployment order
AR-TST-REQ-121 no foreign private persistence
AR-TST-REQ-122 production runtime owner proof
AR-TST-REQ-124 deliberate failure-path execution
AR-TST-REQ-125 non-zero exact-candidate CI evidence
AR-TST-REQ-128 exact released-slice decision
```

# Evidence recording schema

```text
Candidate SHA:
ARREQ:
AR-TST:
Legacy ANA-TST alias where applicable:
PLAN work unit:
Source/runtime owner:
Command/test filter:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Database/broker/browser/artifact identity:
Security/tenant fixture:
Migration baseline/head:
Positive result:
Negative/failure result:
Observed durable post-condition:
CI workflow/run/job:
Artifact/log:
Known debt/blocker:
Invalidated by:
Reviewer:
Decision timestamp:
```

# TESTS completion rule

Document-level completeness requires:

```text
128 / 128 primary AR-TST scenarios
128 / 128 direct ARREQ mappings
37 / 37 legacy ANA-TST IDs preserved
38 / 38 product ANA rules routed
24 / 24 team ANA capabilities routed
4 / 4 TAC AR-FLOW routed
18 / 18 AR-GAP records routed
4 / 4 PLAN aggregate gates routed
```

Implementation completion is separate. `MISSING_TARGET`, `NEW_TEST_REQUIRED`, blocked source contracts, deferred capabilities or `Execution evidence: NOT_RECORDED` cannot be silently converted to PASS.
