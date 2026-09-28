---
document_id: WRK-TESTS-PLATFORM-FOUNDATION
document_type: workstream-tests
status: active
owner: platform-foundation-team
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
applies_to:
  - backend-platform
  - backend-cross-cutting-foundation
  - frontend-foundation
  - frontend-runtime
  - frontend-ui-foundation
  - architecture-tooling
  - generated-contracts
  - ci-quality-gates
evidence:
  - platform-foundation.spec.md
  - platform-foundation.plan.md
  - docs/workstreams/teams/platform-foundation.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend-team-architecture-closure PF-FLOW-01..07
review_on:
  - shared-runtime-contract-change
  - authorization-enforcement-change
  - tenancy-context-change
  - messaging-delivery-change
  - idempotency-change
  - realtime-foundation-change
  - query-foundation-change
  - ui-foundation-change
  - architecture-gate-change
---

# TESTS — Platform & Foundation

## 1. Verification authority

Every normative `PFREQ001..PFREQ128` has exactly one stable direct `PF-TST-*` scenario ID.

A scenario is a **verification contract**, not a prose restatement and not a PASS merely because matching source/tests exist.

Final PASS requires:

```text
accepted candidate SHA
+ executable scenario
+ non-zero execution where a runnable suite is claimed
+ expected positive/negative behavior
+ required production-like boundary proof
+ migration/security evidence where marked
+ exact CI/job/artifact locator
```

Historical TAC evidence and baseline CI may be reused only as preparation evidence under their invalidation rules.

## 2. Mandatory scenario schema

Every scenario below records:

```text
Requirement
Lane
Preparation state

Given
When
Then

Test project
Source under test
Positive/negative
Security-sensitive
Migration-sensitive
CI gate
Existing evidence
Evidence state
Required test to add
Required proof target
Expected result
Execution evidence
```

`Positive/negative: BOTH` means the scenario is incomplete unless at least one accepted path and one rejected/failure path are exercised.

## 3. Evidence-state rules

```text
SOURCE_PRESENT
  source/mechanism exists; not certification

TEST_PRESENT
  test artifact exists; not certification

HISTORICAL_VERIFIED
  previously executed evidence exists; reusable only if invalidation rules do not fire

CANDIDATE_VERIFIED
  focused proof passed on accepted candidate SHA

D4 / VERIFIED
  contract is executable and consumer-usable at required verification depth

D5 / STABLE
  exact-candidate proof includes required production-like, failure, migration/security,
  compatibility and CI evidence for consumers that require D5
```

A skipped, filtered-to-zero, mocked-only or mechanism-only test cannot satisfy a production-runtime claim unless the requirement explicitly concerns only that mechanism.

## 4. TAC PF-FLOW reuse routing

| TAC flow | Direct Platform verification set | Required candidate treatment |
|---|---|---|
| `PF-FLOW-01` request execution pipeline | `PF-TST-CTX-016..022`, `PF-TST-AUTHZ-023..029`, `PF-TST-DATA-030..036` | Re-run when execution context, pipeline order, data-session or authorization semantics change. |
| `PF-FLOW-02` DomainEvent → IntegrationEvent → outbox | `PF-TST-MSG-051`, `PF-TST-MSG-052`, `PF-TST-MSG-055`, `PF-TST-MSG-057` | Re-run source-transaction/outbox atomicity and contract identity on candidate. |
| `PF-FLOW-03` broker delivery + tenant restoration + dedup | `PF-TST-CTX-019`, `PF-TST-DATA-032`, `PF-TST-DATA-036`, `PF-TST-MSG-053`, `PF-TST-MSG-056` | Historical proof is insufficient while command-owned stale-claim crash recovery remains open. |
| `PF-FLOW-04` delivery retry/failure | `PF-TST-MSG-054..056`, `PF-TST-ORDER-058..064`, `PF-TST-OBS-104..106` | Bind proof to actual production runtime owner; reference `Platform.Tests` alone cannot certify MassTransit production semantics. |
| `PF-FLOW-05` contract evolution/recovery | `PF-TST-MSG-052..057`, `PF-TST-API-072..078`, `PF-TST-CROSS-126` | Re-run for schema/version/routing/backlog/replay/recovery changes and old deployed consumers. |
| `PF-FLOW-06` background actor/security context | `PF-TST-CTX-019`, `PF-TST-AUTHZ-028`, `PF-TST-DATA-036` | Re-run on worker/System/RLS/authorization scope changes. |
| `PF-FLOW-07` scoped Integration Event tenant envelope | `PF-TST-CTX-016`, `PF-TST-CTX-017`, `PF-TST-CTX-019`, `PF-TST-DATA-032`, `PF-TST-MSG-052`, `PF-TST-MSG-056` | Re-run on envelope scope/restoration/fallback semantics; scoped events must not silently become System. |

## 5. Canonical BE-PLT rule routing

The following table prevents `PFREQ` coverage from hiding canonical messaging-rule regressions.

| Canonical rule | PFREQ routing | Stable verification references |
|---|---|---|
| `BE-PLT-001` | PFREQ001, PFREQ057 | `PF-TST-GLOBAL-001`, `PF-TST-MSG-057` |
| `BE-PLT-002` | PFREQ052, PFREQ126 | `PF-TST-MSG-052`, `PF-TST-CROSS-126` |
| `BE-PLT-003` | PFREQ052, PFREQ076, PFREQ126 | `PF-TST-MSG-052`, `PF-TST-API-076`, `PF-TST-CROSS-126` |
| `BE-PLT-004` | PFREQ057, PFREQ127 | `PF-TST-MSG-057`, `PF-TST-CROSS-127` |
| `BE-PLT-005` | PFREQ052, PFREQ123 | `PF-TST-MSG-052`, `PF-TST-CROSS-123` |
| `BE-PLT-006` | PFREQ052, PFREQ100 | `PF-TST-MSG-052`, `PF-TST-OBS-100` |
| `BE-PLT-007` | PFREQ053, PFREQ126 | `PF-TST-MSG-053`, `PF-TST-CROSS-126` |
| `BE-PLT-008` | PFREQ046, PFREQ126 | `PF-TST-IDEM-046`, `PF-TST-CROSS-126` |
| `BE-PLT-009` | PFREQ076, PFREQ126 | `PF-TST-API-076`, `PF-TST-CROSS-126` |
| `BE-PLT-010` | PFREQ076, PFREQ126 | `PF-TST-API-076`, `PF-TST-CROSS-126` |
| `BE-PLT-011` | PFREQ051 | `PF-TST-MSG-051` |
| `BE-PLT-012` | PFREQ051, PFREQ064, PFREQ126 | `PF-TST-MSG-051`, `PF-TST-ORDER-064`, `PF-TST-CROSS-126` |
| `BE-PLT-013` | PFREQ054, PFREQ056, PFREQ123 | `PF-TST-MSG-054`, `PF-TST-MSG-056`, `PF-TST-CROSS-123` |
| `BE-PLT-014` | PFREQ056, PFREQ123 | `PF-TST-MSG-056`, `PF-TST-CROSS-123` |
| `BE-PLT-015` | PFREQ053, PFREQ056 | `PF-TST-MSG-053`, `PF-TST-MSG-056` |
| `BE-PLT-016` | PFREQ046, PFREQ056 | `PF-TST-IDEM-046`, `PF-TST-MSG-056` |
| `BE-PLT-017` | PFREQ056, PFREQ060, PFREQ124 | `PF-TST-MSG-056`, `PF-TST-ORDER-060`, `PF-TST-CROSS-124` |
| `BE-PLT-018` | PFREQ049, PFREQ104, PFREQ124 | `PF-TST-IDEM-049`, `PF-TST-OBS-104`, `PF-TST-CROSS-124` |
| `BE-PLT-019` | PFREQ049, PFREQ125 | `PF-TST-IDEM-049`, `PF-TST-CROSS-125` |
| `BE-PLT-020` | PFREQ104, PFREQ124 | `PF-TST-OBS-104`, `PF-TST-CROSS-124` |
| `BE-PLT-021` | PFREQ062, PFREQ063 | `PF-TST-ORDER-062`, `PF-TST-ORDER-063` |
| `BE-PLT-022` | PFREQ055, PFREQ124 | `PF-TST-MSG-055`, `PF-TST-CROSS-124` |
| `BE-PLT-023` | PFREQ059, PFREQ125 | `PF-TST-ORDER-059`, `PF-TST-CROSS-125` |
| `BE-PLT-024` | PFREQ060 | `PF-TST-ORDER-060` |
| `BE-PLT-025` | PFREQ055, PFREQ061, PFREQ104 | `PF-TST-MSG-055`, `PF-TST-ORDER-061`, `PF-TST-OBS-104` |
| `BE-PLT-026` | PFREQ057, PFREQ127 | `PF-TST-MSG-057`, `PF-TST-CROSS-127` |
| `BE-PLT-027` | PFREQ019, PFREQ036, PFREQ122 | `PF-TST-CTX-019`, `PF-TST-DATA-036`, `PF-TST-CROSS-122` |
| `BE-PLT-028` | PFREQ010, PFREQ025, PFREQ121, PFREQ124 | `PF-TST-CSRF-010`, `PF-TST-AUTHZ-025`, `PF-TST-CROSS-121`, `PF-TST-CROSS-124` |
| `BE-PLT-029` | PFREQ044, PFREQ052, PFREQ100, PFREQ127 | `PF-TST-IDEM-044`, `PF-TST-MSG-052`, `PF-TST-OBS-100`, `PF-TST-CROSS-127` |
| `BE-PLT-030` | PFREQ076, PFREQ126 | `PF-TST-API-076`, `PF-TST-CROSS-126` |
| `BE-PLT-031` | PFREQ076, PFREQ077, PFREQ126 | `PF-TST-API-076`, `PF-TST-API-077`, `PF-TST-CROSS-126` |
| `BE-PLT-032` | PFREQ055, PFREQ125, PFREQ126 | `PF-TST-MSG-055`, `PF-TST-CROSS-125`, `PF-TST-CROSS-126` |
| `BE-PLT-033` | PFREQ055, PFREQ123 | `PF-TST-MSG-055`, `PF-TST-CROSS-123` |
| `BE-PLT-034` | PFREQ055, PFREQ124, PFREQ126 | `PF-TST-MSG-055`, `PF-TST-CROSS-124`, `PF-TST-CROSS-126` |
| `BE-PLT-035` | PFREQ122, PFREQ125 | `PF-TST-CROSS-122`, `PF-TST-CROSS-125` |
| `BE-PLT-036` | PFREQ057, PFREQ127 | `PF-TST-MSG-057`, `PF-TST-CROSS-127` |
| `BE-PLT-037` | PFREQ064, PFREQ120 | `PF-TST-ORDER-064`, `PF-TST-CI-120` |
| `BE-PLT-038` | PFREQ090, PFREQ105, PFREQ120 | `PF-TST-RUNTIME-090`, `PF-TST-OBS-105`, `PF-TST-CI-120` |
| `BE-PLT-039` | PFREQ049, PFREQ123 | `PF-TST-IDEM-049`, `PF-TST-CROSS-123` |
| `BE-PLT-040` | PFREQ044, PFREQ047, PFREQ123 | `PF-TST-IDEM-044`, `PF-TST-IDEM-047`, `PF-TST-CROSS-123` |
| `BE-PLT-041` | PFREQ049, PFREQ125 | `PF-TST-IDEM-049`, `PF-TST-CROSS-125` |
| `BE-PLT-042` | PFREQ100, PFREQ101, PFREQ103 | `PF-TST-OBS-100`, `PF-TST-OBS-101`, `PF-TST-OBS-103` |
| `BE-PLT-043` | PFREQ055, PFREQ104, PFREQ105 | `PF-TST-MSG-055`, `PF-TST-OBS-104`, `PF-TST-OBS-105` |
| `BE-PLT-044` | PFREQ127 | `PF-TST-CROSS-127` |
| `BE-PLT-045` | PFREQ053, PFREQ054, PFREQ056 | `PF-TST-MSG-053`, `PF-TST-MSG-054`, `PF-TST-MSG-056` |
| `BE-PLT-046` | PFREQ123, PFREQ126 | `PF-TST-CROSS-123`, `PF-TST-CROSS-126` |
| `BE-PLT-047` | PFREQ102, PFREQ121 | `PF-TST-OBS-102`, `PF-TST-CROSS-121` |
| `BE-PLT-048` | PFREQ019, PFREQ028, PFREQ124 | `PF-TST-CTX-019`, `PF-TST-AUTHZ-028`, `PF-TST-CROSS-124` |
| `BE-PLT-049` | PFREQ055, PFREQ106, PFREQ127 | `PF-TST-MSG-055`, `PF-TST-OBS-106`, `PF-TST-CROSS-127` |
| `BE-PLT-050` | PFREQ076, PFREQ126 | `PF-TST-API-076`, `PF-TST-CROSS-126` |
| `BE-PLT-051` | PFREQ056, PFREQ126 | `PF-TST-MSG-056`, `PF-TST-CROSS-126` |
| `BE-PLT-052` | PFREQ044, PFREQ046, PFREQ052 | `PF-TST-IDEM-044`, `PF-TST-IDEM-046`, `PF-TST-MSG-052` |
| `BE-PLT-053` | PFREQ055, PFREQ062, PFREQ126 | `PF-TST-MSG-055`, `PF-TST-ORDER-062`, `PF-TST-CROSS-126` |
| `BE-PLT-054` | PFREQ057, PFREQ124 | `PF-TST-MSG-057`, `PF-TST-CROSS-124` |
| `BE-PLT-055` | PFREQ051, PFREQ054, PFREQ055 | `PF-TST-MSG-051`, `PF-TST-MSG-054`, `PF-TST-MSG-055` |
| `BE-PLT-056` | PFREQ124 | `PF-TST-CROSS-124` |
| `BE-PLT-057` | PFREQ049, PFREQ123, PFREQ125 | `PF-TST-IDEM-049`, `PF-TST-CROSS-123`, `PF-TST-CROSS-125` |
| `BE-PLT-058` | PFREQ061, PFREQ125 | `PF-TST-ORDER-061`, `PF-TST-CROSS-125` |
| `BE-PLT-059` | PFREQ069, PFREQ070, PFREQ071, PFREQ127 | `PF-TST-RT-069`, `PF-TST-RT-070`, `PF-TST-RT-071`, `PF-TST-CROSS-127` |
| `BE-PLT-060` | PFREQ024, PFREQ028, PFREQ057, PFREQ123 | `PF-TST-AUTHZ-024`, `PF-TST-AUTHZ-028`, `PF-TST-MSG-057`, `PF-TST-CROSS-123` |
| `BE-PLT-061` | PFREQ044, PFREQ047, PFREQ050, PFREQ123 | `PF-TST-IDEM-044`, `PF-TST-IDEM-047`, `PF-TST-IDEM-050`, `PF-TST-CROSS-123` |
| `BE-PLT-062` | PFREQ005, PFREQ030, PFREQ032, PFREQ054, PFREQ056, PFREQ120 | `PF-TST-GLOBAL-005`, `PF-TST-DATA-030`, `PF-TST-DATA-032`, `PF-TST-MSG-054`, `PF-TST-MSG-056`, `PF-TST-CI-120` |
| `BE-PLT-063` | PFREQ005, PFREQ120, PFREQ124 | `PF-TST-GLOBAL-005`, `PF-TST-CI-120`, `PF-TST-CROSS-124` |

## 6. Scenario execution record

Every executed scenario must append or externally link an evidence record with:

```text
Test ID:
Candidate SHA:
Test project / command:
Source under test:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Positive path:
Negative path:
Security evidence:
Migration/compatibility evidence:
Runtime/integration evidence:
CI workflow:
CI run/job:
Artifact/log locator:
Observed result:
Certification disposition:
```

`Source/test exists` is never a valid `Observed result`.

# 7. Scenarios


## PF-TST-GLOBAL-001 — Platform has no business bounded context

### Traceability

```text
Requirement: PFREQ001
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-001
```

### Executable scenario

**Given**

- the candidate Platform graph, ownership docs and a negative fixture that introduces business-specific branching into a reusable Platform component.

**When**

- architecture/dependency checks and source inventory are executed.

**Then**

- the real graph contains mechanism-only Platform code, while the negative fixture is rejected and no service boundary/project is created by Platform alone.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-001` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform/Foundation owns reusable technical mechanisms only and MUST NOT become semantic owner of business contexts. Platform mechanisms may support service extraction, but they MUST NOT select service boundaries or create a new production service/project without bounded-context ownership and architecture approval.


## PF-TST-GLOBAL-002 — Architecture authority precedence

### Traceability

```text
Requirement: PFREQ002
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- an accepted ADR/architecture rule and an execution-package statement that would conflict with it.

**When**

- the structural gates and execution review are evaluated.

**Then**

- the accepted architecture/ADR remains authoritative; the execution package cannot override it and any semantic gate change requires escalation.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-002` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Accepted architecture docs/ADRs and executable architecture gates remain structural authority; this execution package orchestrates delivery and does not redefine those boundaries. Any new cross-cutting framework, production project/service, authorization architecture, delivery guarantee, architecture-manifest semantic, global-state model, repository-wide dependency, or security weakening is an escalation decision and MUST NOT be decided unilaterally by the Platform implementation agent.


## PF-TST-GLOBAL-003 — TAC evidence reuse without duplicate authority

### Traceability

```text
Requirement: PFREQ003
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- historical PF-FLOW evidence plus the exact current candidate source and invalidation rules.

**When**

- a referenced mechanism is unchanged, then a controlled source change is introduced.

**Then**

- unchanged evidence can be reused as baseline only; the controlled change invalidates reuse and forces exact-candidate rerun without creating a duplicate mechanism.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-003` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

`backend-team-architecture-closure` evidence may satisfy Platform mechanisms when it proves the same contract, but this package MUST reclassify that evidence by Platform lane and MUST NOT invent a second contradictory mechanism. Reuse MUST record source/test evidence, evidence state, exact-candidate rerun requirement and invalidating changes; historical `VERIFIED` evidence MUST NOT be promoted directly to current D5.


## PF-TST-GLOBAL-004 — Brownfield-first execution

### Traceability

```text
Requirement: PFREQ004
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a lane with existing brownfield source/tests and no candidate classification.

**When**

- implementation work proposes a new abstraction.

**Then**

- execution is blocked until source/test inventory and RETAIN/HARDEN/GAP/DEPENDENCY_BLOCKED/NOT_APPLICABLE classification exist.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-004` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Every lane MUST inventory candidate-SHA source/tests first and classify `RETAIN`, `HARDEN`, `GAP`, `DEPENDENCY_BLOCKED` or `NOT_APPLICABLE` before adding abstractions.


## PF-TST-GLOBAL-005 — Exact-candidate evidence discipline

### Traceability

```text
Requirement: PFREQ005
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-062, BE-PLT-063
```

### Executable scenario

**Given**

- green test/CI evidence from a SHA different from the accepted certification SHA.

**When**

- certification evidence is assembled.

**Then**

- historical evidence is recorded as preparation only and cannot satisfy final D4/D5 until the accepted SHA is executed.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-005` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Source existence, test existence and historical green CI are not certification. Final evidence MUST bind to the accepted candidate SHA.


## PF-TST-GLOBAL-006 — Consumer-specific readiness

### Traceability

```text
Requirement: PFREQ006
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- two consumers with different Platform dependency/readiness targets.

**When**

- one dependency is below its required target while an unrelated capability is ready.

**Then**

- only affected consumers remain blocked; readiness is evaluated per contract and no blanket Platform-ready state is inferred.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-006` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform capabilities may reach D4/D5 independently. A consumer may proceed only when its exact required Platform contracts meet the readiness matrix in §6. Readiness is a dependency contract, not a lane-status label: unmet consumer-specific D4/D5 targets remain blocking even when neighboring lanes are certified.


## PF-TST-GLOBAL-007 — No broad cleanup PR

### Traceability

```text
Requirement: PFREQ007
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a proposed change set touching unrelated Platform lanes without an explicit dependency reason.

**When**

- execution slicing and review gates are applied.

**Then**

- the change is split or explicitly dependency-justified; an unbounded foundation cleanup cannot pass as one work unit.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-007` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Unrelated Platform lanes MUST NOT be bundled into one unbounded foundation rewrite; execution follows lane-scoped work units and explicit dependency gates.


## PF-TST-GLOBAL-008 — No silent compatibility break

### Traceability

```text
Requirement: PFREQ008
Lane: GLOBAL
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a shared Platform contract change affecting at least one consuming team.

**When**

- the handoff is reviewed before release.

**Then**

- current/target contract, affected teams, breaking status, migration, both-team actions, verification, readiness, rollback/forward-fix and invalidation rules are all present; missing fields block release.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + repository governance/docs validation
Source under test: docs/workstreams/teams/platform-foundation.md; backend/docs/architecture/platform-and-messaging.md; backend-team-architecture-closure execution artifacts; platform execution package
Positive/negative: BOTH
Security-sensitive: CONDITIONAL
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: architecture/docs/package inventory and exact-SHA evidence review
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-GLOBAL-008` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared mechanism changes MUST classify current contract, target contract, affected teams, breaking/additive status, migration strategy, feature-team action, Platform action, verification, required readiness, rollback/forward-fix and invalidation conditions before release. Persisted/backlogged messages, dedup/replay state, generated artifacts and old consumers count as compatibility surfaces.


## PF-TST-CSRF-009 — Canonical browser CSRF contract

### Traceability

```text
Requirement: PFREQ009
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- an authenticated browser session bootstrapped through the CSRF endpoint.

**When**

- the client performs a protected mutation and a legacy XSRF-named mutation is also attempted.

**Then**

- the canonical `csrf_token` + response-body token + `X-CSRF-Token` path succeeds and legacy `XSRF-TOKEN`/`X-XSRF-TOKEN` conventions do not become an accepted second protocol.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-009` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Protected browser mutations MUST use the single ADR-approved CSRF bootstrap/echo contract. Current source uses the HttpOnly `csrf_token` cookie plus bootstrap response body and `X-CSRF-Token` request header; legacy `XSRF-TOKEN` / `X-XSRF-TOKEN` conventions are forbidden.


## PF-TST-CSRF-010 — Unsafe-request classification

### Traceability

```text
Requirement: PFREQ010
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-028
```

### Executable scenario

**Given**

- safe and unsafe HTTP methods plus a signature-authenticated provider callback endpoint.

**When**

- CSRF middleware classifies each request.

**Then**

- unsafe browser mutations require CSRF, safe requests follow the approved classifier, and provider callback exemption occurs only when explicit endpoint metadata authorizes it.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-010` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

CSRF applicability MUST be decided by the shared API/Infrastructure classifier, not by feature-local endpoint code. Signature-authenticated provider callbacks may be exempt only through explicit endpoint metadata.


## PF-TST-CSRF-011 — Token secrecy and storage

### Traceability

```text
Requirement: PFREQ011
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a browser runtime after successful CSRF bootstrap.

**When**

- token handling is inspected while requests, logout/session rollover and reload behavior are exercised.

**Then**

- the token remains instance-scoped memory, is never read from the HttpOnly cookie and is absent from localStorage/sessionStorage; a new bootstrap supplies the next token.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-011` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

The frontend MUST keep the CSRF token in instance-scoped memory from the bootstrap response. It MUST NOT read the API cookie or persist the token to localStorage/sessionStorage.


## PF-TST-CSRF-012 — Valid and invalid pair behavior

### Traceability

```text
Requirement: PFREQ012
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a protected mutation endpoint with an authenticated session.

**When**

- valid, missing, malformed and mismatched CSRF pairs are submitted.

**Then**

- only the valid pair reaches the protected mutation; invalid cases fail before handler side effects.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-012` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

A valid browser session plus valid CSRF pair MUST proceed; missing or invalid CSRF MUST fail before the protected mutation.


## PF-TST-CSRF-013 — Session-expiry behavior

### Traceability

```text
Requirement: PFREQ013
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a client with an expired/revoked session during a protected mutation.

**When**

- the API returns the typed session-expiry outcome while CSRF bootstrap/retry logic is active.

**Then**

- the client emits/handles typed session expiry and does not hide the failure behind an unbounded or blind mutation retry.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-013` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Session expiry and CSRF refresh/bootstrap behavior MUST remain typed and observable; the client MUST NOT hide session failure behind blind retry.


## PF-TST-CSRF-014 — Cross-origin production policy

### Traceability

```text
Requirement: PFREQ014
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- production-like cookie, CORS and credential configuration for the approved browser origin and one unapproved origin.

**When**

- credentialed bootstrap/mutation requests are made cross-origin.

**Then**

- the approved topology succeeds with Secure/SameSite rules intact and the unapproved origin is rejected without wildcard credential weakening.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-014` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Production cookie/CORS/credential settings MUST support the approved browser topology without weakening SameSite/Secure rules or broadening origins.


## PF-TST-CSRF-015 — CSRF contract compatibility evidence

### Traceability

```text
Requirement: PFREQ015
Lane: PF-01
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a deliberate change to CSRF cookie/header/bootstrap shape.

**When**

- backend API tests, frontend contract tests and tracked/generated public contract evidence are run.

**Then**

- all affected surfaces change atomically or the drift/compatibility gates fail; partial protocol rollout is not certifiable.

### Verification metadata

```text
Test project: Notrelix.API.Tests + frontend @notrelix/contracts Vitest
Source under test: backend/src/Notrelix.API/Middleware/CsrfValidationMiddleware.cs; backend/src/Notrelix.Infrastructure/Auth/Csrf/CsrfProtector.cs; backend/src/Notrelix.API/Endpoints/Identity/Auth/Commands/IssueCsrfTokenEndpoint.cs; frontend/packages/foundation/contracts/src/client/csrf.ts; frontend/packages/foundation/contracts/src/client/api-client.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: frontend/packages/foundation/contracts/src/client/__tests__/csrf-transport.unit.test.ts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CSRF-015` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Any CSRF cookie/header/bootstrap change MUST update backend/API tests, frontend contract tests and generated/public contract evidence atomically.


## PF-TST-CTX-016 — Distinct context identities

### Traceability

```text
Requirement: PFREQ016
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01, PF-FLOW-07
```

### Executable scenario

**Given**

- one authenticated request carrying Actor, Account and Workspace context.

**When**

- request context is resolved and snapshotted into Application execution.

**Then**

- Actor, Account and Workspace remain separately addressable values and none is silently substituted for another.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CTX-016` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Authenticated actor, Account and Workspace MUST remain distinct runtime concepts. No opaque mutable global context may collapse their ownership.


## PF-TST-CTX-017 — Trusted HTTP context resolution

### Traceability

```text
Requirement: PFREQ017
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01, PF-FLOW-07
```

### Executable scenario

**Given**

- a valid authenticated actor plus spoofed/inconsistent Account or Workspace scope input.

**When**

- HttpRequestContextMiddleware and trusted context resolution run.

**Then**

- trusted membership/scope facts win and spoofed or inconsistent scope is rejected before protected work.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CTX-017` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Account/Workspace context MUST be derived through approved request middleware/context contracts and MUST reject spoofed or inconsistent scope.


## PF-TST-CTX-018 — ExecutionContext immutability

### Traceability

```text
Requirement: PFREQ018
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- an Application request after ExecutionContext snapshot creation.

**When**

- ambient/request context is changed or disposed while the handler continues.

**Then**

- the handler observes the immutable approved snapshot/readers, not a mutable ambient value.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CTX-018` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Application handlers MUST consume the request snapshot/readers rather than mutating ambient request state during execution.


## PF-TST-CTX-019 — Background scope reconstruction

### Traceability

```text
Requirement: PFREQ019
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-027, BE-PLT-048; TAC flow: PF-FLOW-01, PF-FLOW-03, PF-FLOW-06, PF-FLOW-07
```

### Executable scenario

**Given**

- a tenant-scoped background message and a second background invocation with no approved tenant/System classification.

**When**

- worker execution reconstructs context and accesses tenant persistence.

**Then**

- the scoped message establishes trusted tenant/security context; the unclassified invocation cannot inherit global/System authority.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CTX-019` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Background work MUST reconstruct explicit trusted tenant/security context; lack of HTTP does not imply global or System authority.


## PF-TST-CTX-020 — Account transition state isolation

### Traceability

```text
Requirement: PFREQ020
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- Account A with populated cache, permissions, optimistic state and realtime subscriptions.

**When**

- the user transitions A → B and then back to A.

**Then**

- B cannot observe A-derived state, transition cleanup/partitioning is atomic, subscriptions follow the active Account, and returning to A yields only correctly partitioned/refetched A state.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/apps/web — add/extend Account transition integration test exercising cache, permissions, optimistic state and realtime subscription disposal.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Frontend Account transitions MUST prevent cached Account-A data, permission state, optimistic state or realtime subscriptions from leaking into Account B.


## PF-TST-CTX-021 — Workspace query partitioning

### Traceability

```text
Requirement: PFREQ021
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- workspace-scoped resources for Workspace W1 and W2 plus an empty workspace ID.

**When**

- query keys are constructed and validated.

**Then**

- W1/W2 keys differ by explicit workspace ID and the empty/malformed workspace key is rejected.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CTX-021` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Workspace-scoped server-state keys MUST include a non-empty Workspace ID and remain compatible with workspace changes.


## PF-TST-CTX-022 — Account query-key closure

### Traceability

```text
Requirement: PFREQ022
Lane: PF-02
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- the current account-query helper contract and identical resource names in Account A and B.

**When**

- the final isolation mechanism is exercised through A → B → A transitions.

**Then**

- either Account ID is part of account query identity everywhere or an atomic hard reset/partition proves equivalent isolation; without that proof D5 is blocked.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests + frontend query/app-web Vitest
Source under test: backend/src/Notrelix.API/Middleware/HttpRequestContextMiddleware.cs; backend/src/Notrelix.Application/Common/Context/CurrentRequestContext.cs; backend/src/Notrelix.Application/Common/Behaviors/ExecutionContextBehavior.cs; backend/src/Notrelix.Infrastructure/Identity/Services/CurrentTenantContext.cs; frontend/packages/foundation/query/src/query-key-scope.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: pipeline/context integration tests + query-key/realtime lifecycle tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/packages/foundation/query + app-web — add a direct account key identity/reset contract test and A→B→A host proof.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Because `accountQueryKey(resource, ...)` currently omits `accountId`, Platform MUST either add Account identity to the key or prove an atomic hard reset/partition mechanism before declaring Account server-state isolation D5.


## PF-TST-AUTHZ-023 — Single enforcement pipeline

### Traceability

```text
Requirement: PFREQ023
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a representative protected command resolved through production DI.

**When**

- the request traverses the MediatR pipeline.

**Then**

- the frozen seven-behavior order is observed and `AccessControlBehavior` is the single business-action enforcement point before the handler.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests — retain/extend production pipeline authorization tests using real DI/DataSession/AccessControl.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Protected Application requests MUST flow through the canonical seven-behavior pipeline with `AccessControlBehavior` as the business-action enforcement point.


## PF-TST-AUTHZ-024 — Policy ownership separation

### Traceability

```text
Requirement: PFREQ024
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-060; TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- Governance permission policy, resource-owned action semantics and Platform enforcement mechanics.

**When**

- a protected request is evaluated and source dependencies are inspected.

**Then**

- policy meaning remains with Governance/resource owners, Platform only enforces through approved seams, and no second Platform policy engine appears.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-AUTHZ-024` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Governance owns permission policy; resource contexts own resource/business-action semantics; Platform/Application owns enforcement mechanics. Platform MUST NOT implement a second policy engine.


## PF-TST-AUTHZ-025 — Facts before policy

### Traceability

```text
Requirement: PFREQ025
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-028; TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a user request with forged client role/permission claims but server-side facts that deny the action.

**When**

- AccessControlBehavior evaluates the request.

**Then**

- authorization follows trusted server-side facts/evaluator results and the forged client claim or route presence cannot grant access.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-AUTHZ-025` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Authorization MUST evaluate trusted server-side facts through the approved facts/evaluator seams. Client claims and route presence alone are not authorization facts.


## PF-TST-AUTHZ-026 — DataSession ordering

### Traceability

```text
Requirement: PFREQ026
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a facts provider that requires the active database connection/transaction.

**When**

- the protected request executes through the registered pipeline.

**Then**

- `DataSessionBehavior` establishes its session before `AccessControlBehavior` queries facts and the frozen order test fails if these behaviors are swapped.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-AUTHZ-026` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

`DataSessionBehavior` MUST precede `AccessControlBehavior` where the facts provider requires the active connection/transaction.


## PF-TST-AUTHZ-027 — Unauthorized side-effect exclusion

### Traceability

```text
Requirement: PFREQ027
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a protected command that would mutate database state and/or enqueue an outbox event.

**When**

- authorization is denied or request contract validation fails.

**Then**

- the handler mutation, outbox enrollment and external effect do not occur.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-AUTHZ-027` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Denied or malformed protected requests MUST NOT execute protected handler side effects.


## PF-TST-AUTHZ-028 — System-internal exception

### Traceability

```text
Requirement: PFREQ028
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-048, BE-PLT-060; TAC flow: PF-FLOW-01, PF-FLOW-06
```

### Executable scenario

**Given**

- an ordinary authenticated/background request and a separately declared `ISystemInternalRequest`.

**When**

- both traverse execution/authorization context resolution.

**Then**

- only the explicit trusted System contract receives the frozen System path; ordinary code cannot opt in by payload/header/type spoofing.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-AUTHZ-028` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

The explicit `ISystemInternalRequest` contract may use the frozen trusted System path; ordinary background/user requests MUST NOT be able to opt into it.


## PF-TST-AUTHZ-029 — Architecture guard

### Traceability

```text
Requirement: PFREQ029
Lane: PF-03
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a negative fixture adding legacy `AuthorizationBehavior`, `PermissionService` authority or handler-local RBAC branching.

**When**

- architecture tests run.

**Then**

- the fixture is rejected while the canonical pipeline implementation remains green.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/DependencyInjection.cs; backend/src/Notrelix.Application/Common/Behaviors/AccessControlBehavior.cs; backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml
Existing evidence: PipelineOrderTests; PipelineRuntimeOrderTests; RenameAccountPipelineAuthorizationTests; WorkspaceCreationPipelineAuthorizationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-AUTHZ-029` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Architecture tests MUST prevent reintroduction of legacy parallel `AuthorizationBehavior` / `PermissionService` / handler-local RBAC authority.


## PF-TST-DATA-030 — Request data-session boundary

### Traceability

```text
Requirement: PFREQ030
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-062; TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a normal write command and a negative path attempting ad-hoc/nested transaction authority.

**When**

- DataSessionBehavior/EfRequestDataSession execute the operation.

**Then**

- the approved request transaction owns commit/rollback and ad-hoc nested transaction authority is rejected or remains outside the certified path.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-DATA-030` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Write requests MUST use the approved request data-session/transaction mechanism and must not create ad-hoc nested transaction authority.


## PF-TST-DATA-031 — Owner-local persistence

### Traceability

```text
Requirement: PFREQ031
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- bounded-context persistence plus a negative fixture introducing a universal cross-context repository/DbContext abstraction.

**When**

- architecture/ownership checks run.

**Then**

- owner-local persistence stays intact and the generic cross-context authority is rejected.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-DATA-031` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared persistence helpers MUST preserve bounded-context ownership; Platform MUST NOT expose a generic cross-context repository or universal business DbContext.


## PF-TST-DATA-032 — RLS session context

### Traceability

```text
Requirement: PFREQ032
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-062; TAC flow: PF-FLOW-01, PF-FLOW-03, PF-FLOW-07
```

### Executable scenario

**Given**

- two sequential tenant requests reusing pooled database infrastructure.

**When**

- each request opens a transaction and RlsSessionContext applies tenant variables.

**Then**

- transaction-local RLS context is correct for each tenant and no prior tenant session value leaks after commit/rollback/connection reuse.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs — add transaction-local set/clear and pooled-connection cross-tenant leakage proof.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Tenant-scoped persistence MUST set and clear the PostgreSQL request/session context according to the approved transaction-local mechanism.


## PF-TST-DATA-033 — RLS defense-in-depth

### Traceability

```text
Requirement: PFREQ033
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- an Application authorization denial and a separate database row belonging to another tenant.

**When**

- authorization and RLS protections are exercised independently.

**Then**

- authorization still blocks disallowed actions even if persistence might be reachable, while RLS independently blocks cross-tenant rows; neither is treated as a substitute for the other.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-DATA-033` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

RLS proves persistence isolation and MUST NOT be documented as a replacement for Application authorization.


## PF-TST-DATA-034 — Cross-context private-table rule

### Traceability

```text
Requirement: PFREQ034
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a negative source fixture reading another bounded context's private table without an approved classified seam.

**When**

- architecture/raw-SQL ownership checks execute.

**Then**

- the hidden cross-context read is detected or explicitly classified; shared physical database location alone does not make it valid.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-DATA-034` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Foreign private-table reads remain exceptional and explicitly classified; a shared database is not permission to create hidden business coupling.


## PF-TST-DATA-035 — Concurrency/version support

### Traceability

```text
Requirement: PFREQ035
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- an aggregate/resource update using an expected version that has become stale.

**When**

- the write is committed through the shared persistence mechanism.

**Then**

- the stale update fails with the approved concurrency conflict and does not overwrite the newer committed version.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-DATA-035` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared persistence mechanisms MUST preserve optimistic-concurrency/expected-version semantics required by product aggregates.


## PF-TST-DATA-036 — Worker persistence scope

### Traceability

```text
Requirement: PFREQ036
Lane: PF-04
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-027; TAC flow: PF-FLOW-01, PF-FLOW-03, PF-FLOW-06
```

### Executable scenario

**Given**

- a tenant worker path and an attempted broad System/bypass worker path.

**When**

- both perform persistence through production-like RLS/data-session setup.

**Then**

- the tenant worker has explicit trusted scope and RLS semantics; broad bypass is rejected unless its exact System contract is approved and tested.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Architecture.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/DataSessionBehavior.cs; backend/src/Notrelix.Infrastructure/Data/EfRequestDataSession.cs; backend/src/Notrelix.Infrastructure/Data/Rls/RlsSessionContext.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/PersistenceRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: RlsRuntimeEnforcementTests; ExpectedVersionConcurrencyIntegrationTests; ApplicationArchitectureTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests — add tenant worker RLS/context proof and negative unclassified-System worker case.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Tenant worker paths MUST have explicit Platform-approved scope semantics before they are treated as tenant-safe; broad worker bypass is not a default capability.


## PF-TST-MIG-037 — Migration authority

### Traceability

```text
Requirement: PFREQ037
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the EF model, migration history and model snapshot for the exact candidate.

**When**

- schema/model drift verification runs.

**Then**

- migration history/snapshot represent the candidate model and ungoverned drift is detected rather than suppressed.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MIG-037` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

EF migration history and model snapshot are canonical schema evolution evidence; generated/model drift MUST be detected rather than suppressed.


## PF-TST-MIG-038 — Clean database proof

### Traceability

```text
Requirement: PFREQ038
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- an empty supported PostgreSQL database/container.

**When**

- the service applies migrations, RLS/bootstrap and initialization.

**Then**

- the schema is created reproducibly, startup becomes healthy only after successful initialization, and required seed/bootstrap state is present.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs — clean PostgreSQL bootstrap from empty database.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Schema-affecting delivery MUST prove clean database creation/startup for the supported environment.


## PF-TST-MIG-039 — Supported upgrade proof

### Traceability

```text
Requirement: PFREQ039
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a database at the explicitly supported previous release/rebaseline boundary with representative tenant/security data.

**When**

- the candidate migration sequence is applied.

**Then**

- upgrade succeeds while preserving required data, RLS/security semantics and migration history; unsupported boundaries are not silently claimed.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs — upgrade fixture from explicitly supported prior/rebaseline state.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Where an upgrade boundary is claimed, the supported previous state MUST migrate successfully with data/security semantics preserved.


## PF-TST-MIG-040 — Pending model changes

### Traceability

```text
Requirement: PFREQ040
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a deliberate EF model change with no matching migration/rebaseline evidence.

**When**

- startup/model validation runs.

**Then**

- `PendingModelChangesWarning` or equivalent drift blocks certification and is not silenced to manufacture green.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend CI/model-drift proof — candidate model change without migration must fail; warning suppression is not accepted evidence.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

`PendingModelChangesWarning` or equivalent model drift MUST block certification unless resolved by an intentional governed rebaseline.


## PF-TST-MIG-041 — RLS deployment order

### Traceability

```text
Requirement: PFREQ041
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a clean database requiring both relational migrations and RLS policy/bootstrap deployment.

**When**

- initialization executes in the documented order.

**Then**

- tables/columns exist before dependent RLS policy installation, the ordering is repeatable, and policy verification passes.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MIG-041` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Relational migration and RLS policy deployment ordering MUST be explicit and reproducible.


## PF-TST-MIG-042 — Initialization failure visibility

### Traceability

```text
Requirement: PFREQ042
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a forced migration/initialization failure such as invalid SQL or unavailable database.

**When**

- service startup/readiness is evaluated.

**Then**

- failure is observable and the service does not report healthy/ready with partially initialized persistence.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MIG-042` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Database/runtime initialization failure MUST be observable and MUST NOT degrade into a partially initialized service that reports healthy.


## PF-TST-MIG-043 — Migration discipline

### Traceability

```text
Requirement: PFREQ043
Lane: PF-05
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a proposed migration-history rewrite outside the documented development rebaseline window.

**When**

- migration governance is applied.

**Then**

- the rewrite is rejected or escalated; outside the allowed rebaseline window schema evolution is append-only/explicitly approved.

### Verification metadata

```text
Test project: Notrelix.Integration.Tests + migration/startup CI proof
Source under test: backend/src/Notrelix.Infrastructure/Data/ApplicationDbContextInitialiser.cs; backend/src/Notrelix.API/Extensions/DatabaseStartupExtensions.cs; backend/src/Notrelix.Infrastructure/Data/Migrations/; backend/docs/operations/migrations-and-data-change.md
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: MigrationResiliencyTests; SeedDataInitialiserTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MIG-043` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

History rewrites are allowed only inside the documented development rebaseline policy; outside that window migrations are append-only or require explicit architecture approval.


## PF-TST-IDEM-044 — Explicit operation identity

### Traceability

```text
Requirement: PFREQ044
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-029, BE-PLT-040, BE-PLT-052, BE-PLT-061
```

### Executable scenario

**Given**

- an idempotent request contract and two unrelated operations reusing the same raw client key.

**When**

- IdempotencyBehavior builds operation identity.

**Then**

- the logical operation/producer namespace distinguishes unrelated operations and raw client key alone is not global identity.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-IDEM-044` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Each idempotent request MUST declare a stable operation identity; raw client keys are insufficient as the sole namespace.


## PF-TST-IDEM-045 — Tenant partitioning

### Traceability

```text
Requirement: PFREQ045
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the same raw idempotency key and payload used in Account A and Account B.

**When**

- both requests execute through IdempotencyPartitionFactory/store.

**Then**

- tenant partitions prevent collision or replay across accounts.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-IDEM-045` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Idempotency state MUST be partitioned by the required Account/tenant scope so identical raw keys in different tenants cannot collide.


## PF-TST-IDEM-046 — Payload fingerprint

### Traceability

```text
Requirement: PFREQ046
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-008, BE-PLT-016, BE-PLT-052
```

### Executable scenario

**Given**

- one completed logical operation plus a repeat with identical payload and another repeat with materially different payload.

**When**

- request fingerprint and BeginAsync conflict logic run.

**Then**

- identical replay is recognized as the same operation while different payload deterministically returns payload-mismatch/conflict rather than an unrelated cached result.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Application.Tests/Common/Idempotency/JsonIdempotencyRequestFingerprintTests.cs + integration store conflict test.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

A repeated key with a materially different request payload MUST fail deterministically rather than replaying an unrelated result.


## PF-TST-IDEM-047 — Concurrent duplicate coordination

### Traceability

```text
Requirement: PFREQ047
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-040, BE-PLT-061
```

### Executable scenario

**Given**

- two or more concurrent requests with the same tenant, operation identity and payload.

**When**

- they race through the canonical persisted idempotency store.

**Then**

- only one business mutation executes and all followers observe the approved in-flight/replay outcome without duplicate side effects.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Data/Ops/IdempotencyStoreIntegrationTests.cs — true concurrent duplicate race with one mutation.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Concurrent duplicates MUST coordinate through the canonical store/behavior and MUST NOT execute the business mutation twice.


## PF-TST-IDEM-048 — Success replay

### Traceability

```text
Requirement: PFREQ048
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a previously completed idempotent command with stored/reconstructable result semantics.

**When**

- the same logical request is submitted again.

**Then**

- the approved result is replayed/reconstructed without invoking the business mutation a second time.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests — replay completed operation without handler re-execution.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

A completed duplicate MUST return the approved persisted/reconstructed result semantics without re-running the mutation.


## PF-TST-IDEM-049 — Failure/retry semantics

### Traceability

```text
Requirement: PFREQ049
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-018, BE-PLT-019, BE-PLT-039, BE-PLT-041, BE-PLT-057
```

### Executable scenario

**Given**

- an operation that fails or crashes after idempotency begin but before approved completion.

**When**

- the request is retried according to the producer contract.

**Then**

- incomplete/failed state is not returned as success; retry/recovery is explicit, bounded and cannot create an uncontrolled second effect.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-IDEM-049` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Failed/incomplete idempotency state MUST have explicit retry/recovery semantics and cannot be treated as successful completion.


## PF-TST-IDEM-050 — Producer ownership

### Traceability

```text
Requirement: PFREQ050
Lane: PF-06
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-061
```

### Executable scenario

**Given**

- two business teams using the same Platform idempotency mechanism.

**When**

- their contracts are inspected and requests are executed.

**Then**

- each team defines whether/what logical operation is idempotent while Platform supplies only reusable partition/fingerprint/store/execution mechanics.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Application/Common/Behaviors/IdempotencyBehavior.cs; backend/src/Notrelix.Application/Common/Idempotency/IdempotencyPartitionFactory.cs; backend/src/Notrelix.Application/Common/Idempotency/JsonIdempotencyRequestFingerprint.cs; backend/src/Notrelix.Infrastructure/Operations/Idempotency/EfIdempotencyStore.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: JsonIdempotencyRequestFingerprintTests; IdempotencyStoreIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-IDEM-050` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Business teams own which operation is idempotent and the logical operation identity; Platform owns the reusable execution/store mechanism.


## PF-TST-MSG-051 — Transactional outbox

### Traceability

```text
Requirement: PFREQ051
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-011, BE-PLT-012, BE-PLT-055; TAC flow: PF-FLOW-02
```

### Executable scenario

**Given**

- a transaction that mutates authoritative business state and emits a required integration/realtime intent.

**When**

- one successful commit and one forced rollback are executed.

**Then**

- source mutation and durable outbox enrollment commit atomically on success and neither survives the rollback independently.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MSG-051` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Published integration/realtime intents that require atomicity MUST be persisted with the producing business transaction through the approved outbox mechanism.


## PF-TST-MSG-052 — Stable message identity

### Traceability

```text
Requirement: PFREQ052
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-002, BE-PLT-003, BE-PLT-005, BE-PLT-006, BE-PLT-029, BE-PLT-052; TAC flow: PF-FLOW-02, PF-FLOW-05, PF-FLOW-07
```

### Executable scenario

**Given**

- one committed message delivered repeatedly through retry/replay.

**When**

- the delivery pipeline serializes, dispatches and retries it.

**Then**

- logical EventId/name/version and relevant correlation/causation identity remain stable across attempts; retries do not mint a fake new event.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MSG-052` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Every delivered envelope MUST carry non-empty message identity suitable for retry, deduplication and poison tracking.


## PF-TST-MSG-053 — Consumer identity

### Traceability

```text
Requirement: PFREQ053
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-007, BE-PLT-015, BE-PLT-045; TAC flow: PF-FLOW-03, PF-FLOW-05
```

### Executable scenario

**Given**

- one event consumed by two logical consumers and a controlled consumer rename/deploy scenario.

**When**

- dedup/consumer registry behavior is evaluated.

**Then**

- dedup state is per stable logical consumer, one consumer's success does not suppress the other, and an identity rename requires explicit migration rather than silently resetting history.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MSG-053` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Consumer/dedup state MUST include enough consumer identity to prevent unrelated consumers from sharing completion state accidentally.


## PF-TST-MSG-054 — Outbox claiming and reclaim

### Traceability

```text
Requirement: PFREQ054
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-013, BE-PLT-045, BE-PLT-055, BE-PLT-062; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- multiple outbox dispatchers, one claimed item and a simulated worker death before completion.

**When**

- the claim lease/reclaim horizon elapses and another dispatcher polls.

**Then**

- the item becomes claimable again, no permanent lock/lost work occurs, and final delivery state has at most the approved single logical completion.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Data/OutboxClaimReclaimTests.cs — multi-dispatcher lease expiry/crash reclaim proof.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Concurrent dispatchers MUST claim/reclaim durable work without duplicate committed delivery state or permanent starvation. Any durable `Processing`/claimed state MUST have an explicit lease, stale-claim reclamation or equivalent recovery owner so a process crash cannot make eligible work permanently invisible.


## PF-TST-MSG-055 — Delivery state visibility

### Traceability

```text
Requirement: PFREQ055
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-022, BE-PLT-025, BE-PLT-032, BE-PLT-033, BE-PLT-034, BE-PLT-043, BE-PLT-049, BE-PLT-053, BE-PLT-055; TAC flow: PF-FLOW-02, PF-FLOW-04, PF-FLOW-05
```

### Executable scenario

**Given**

- messages in pending/retrying/failed(dead-letter)/replay states.

**When**

- operators/tests inspect delivery diagnostics and recovery inputs.

**Then**

- state, attempts, failure code/age and stable identities are observable without deleting failure evidence or dumping unsafe payload.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MSG-055` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Retry/attempt/terminal delivery state MUST be observable through approved diagnostics/health mechanisms using semantic identities. Retry-exhausted/dead-letter state is recovery input, not deletion. Replay/re-drive MUST be bounded, tenant/consumer-scoped, checkpointed after successful units, governed when forced, and observable for backlog age/freshness without dumping sensitive payloads.


## PF-TST-MSG-056 — Inbox claim discipline

### Traceability

```text
Requirement: PFREQ056
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-013, BE-PLT-014, BE-PLT-015, BE-PLT-016, BE-PLT-017, BE-PLT-045, BE-PLT-051, BE-PLT-062; TAC flow: PF-FLOW-03, PF-FLOW-04, PF-FLOW-07
```

### Executable scenario

**Given**

- both the default transactional dedup path and the command-owned path; for the latter the process is terminated after durable `Processing` claim commit but before success/normal catch cleanup.

**When**

- the same EventId/ConsumerName is redelivered after the configured stale/lease recovery condition.

**Then**

- default-path rollback keeps retry safe; command-owned stale claim is deterministically reclaimed/reconciled, redelivery is not permanently suppressed, and exactly one final business outcome plus `Succeeded` claim remains.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Messaging — add command-owned dedup PROCESS-DEATH/stale-claim recovery integration test; normal catch cleanup alone is insufficient.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Current production source has a durable consumer dedup/claim mechanism through `MessagingProcessedEvent`, `MessageDeduplicationStore` and `DeduplicationConsumeFilter`, keyed by `(EventId, ConsumerName)`. Certification MUST prove duplicate, conflicting-identity, concurrent-delivery, rollback and retry semantics per logical consumer. The command-owned path MUST additionally prove process-crash recovery after a durable `Processing` claim commits but before command settlement/cleanup; a stale claim MUST NOT permanently suppress redelivery or the intended business effect.


## PF-TST-MSG-057 — Event meaning ownership

### Traceability

```text
Requirement: PFREQ057
Lane: PF-07
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-001, BE-PLT-004, BE-PLT-026, BE-PLT-036, BE-PLT-054, BE-PLT-060; TAC flow: PF-FLOW-02, PF-FLOW-04, PF-FLOW-05
```

### Executable scenario

**Given**

- a generic Platform consumer host/filter and multiple business event types.

**When**

- source/dependency inspection and dispatch tests run.

**Then**

- Platform code handles envelope/delivery mechanics only; business-specific branching/effects remain in registered consumers/Application owners.

### Verification metadata

```text
Test project: Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests
Source under test: backend/src/Notrelix.Infrastructure/Data/Interceptors/DomainEventInterceptor.cs; backend/src/Notrelix.Infrastructure/BackgroundJobs/OutboxDispatcher.cs; backend/src/Notrelix.Application/Common/Messaging/IMessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs; backend/src/Notrelix.Infrastructure/Messaging/DeduplicationConsumeFilter.cs; backend/src/Notrelix.Infrastructure/Data/Messaging/MessagingProcessedEvent.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OutboxAtomicityTests; OutboxClaimReclaimTests; DeduplicationConsumeFilterFullIntegrationTests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-MSG-057` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform owns reusable envelope/delivery/runtime mechanics only; producer and consumer business contexts retain event meaning and reaction semantics. Provider-specific broker/persistence wiring remains Infrastructure-owned behind Platform contracts. Generic hosts/diagnostics MUST NOT acquire product policy or become permission to mutate target-context state directly.


## PF-TST-ORDER-058 — Ordering requires real sequence

### Traceability

```text
Requirement: PFREQ058
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- a consumer candidate with no stable sequence/ordering key and another with an explicit sequence contract.

**When**

- ordering capability is configured and exercised.

**Then**

- the first cannot be certified as ordered (NOT_APPLICABLE/PARTIAL as appropriate); the second validates a real sequence rather than arrival order.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ORDER-058` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

An ordering-enabled consumer MUST reject an envelope without an explicit sequence; Platform MUST NOT synthesize one. A production ordering claim also requires proof that the actual production runtime owner wires or implements the required ordering semantics; `Notrelix.Platform.Tests` alone cannot certify MassTransit/Infrastructure delivery.


## PF-TST-ORDER-059 — Partition ordering

### Traceability

```text
Requirement: PFREQ059
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-023; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- messages for ordering key A and key B, including out-of-order delivery within A.

**When**

- production-owned ordering behavior processes them concurrently.

**Then**

- A advances only in approved sequence while B remains independently progressable; no unnecessary global serialization occurs.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ORDER-059` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Ordering MUST be scoped by the approved partition identity and MUST allow unrelated partitions to make progress independently.


## PF-TST-ORDER-060 — Commit after handler success

### Traceability

```text
Requirement: PFREQ060
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-017, BE-PLT-024; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- an ordered message whose handler fails before completion followed by a successful retry.

**When**

- the production runtime processes both attempts.

**Then**

- ack/cursor/sequence is not advanced on failure and advances exactly once only after the handler's approved success condition.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Messaging — production runtime proof: handler failure must not advance/ack ordered sequence.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Ordering state MUST advance only after successful handler completion; failed handling MUST permit correct retry of the same sequence.


## PF-TST-ORDER-061 — Backpressure is observable

### Traceability

```text
Requirement: PFREQ061
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-025, BE-PLT-058; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- a constrained consumer under backlog with configured prefetch/concurrency limits.

**When**

- load exceeds immediate processing capacity.

**Then**

- backpressure remains bounded and observable through queue/backlog/blocked-age or equivalent signals; the runtime does not create unbounded concurrency/memory amplification.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ORDER-061` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Concurrency-slot timeout/backpressure MUST fail observably for transport retry rather than dropping the message.


## PF-TST-ORDER-062 — Poison identity

### Traceability

```text
Requirement: PFREQ062
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-021, BE-PLT-053; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- the same message delivered to two consumers and repeated deterministic failure in only one consumer.

**When**

- poison classification reaches its threshold.

**Then**

- poison identity is scoped at least by message + logical consumer and does not globally poison the event for independent consumers.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ORDER-062` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Poison tracking MUST be keyed by real message identity, not only event-name string. Current source uses `(eventName, messageId)`.


## PF-TST-ORDER-063 — Poison does not poison siblings

### Traceability

```text
Requirement: PFREQ063
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-021; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- two sibling ordering keys/consumers where one becomes poison or blocked.

**When**

- subsequent messages for both siblings arrive.

**Then**

- the poisoned/blocked sibling is contained while the unrelated sibling continues unless an explicitly broader invariant says otherwise.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ORDER-063` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

A terminally failing message MUST NOT make another message of the same event type poison by association.


## PF-TST-ORDER-064 — Restart/durability classification

### Traceability

```text
Requirement: PFREQ064
Lane: PF-08
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-012, BE-PLT-037; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- reference `ConsumerHost` in-memory ordering/poison tests and the actual production MassTransit/Infrastructure runtime after restart.

**When**

- the capability is certified for a concrete consumer.

**Then**

- reference unit proof alone is insufficient: production owner/wiring and restart durability/equivalent semantics must be proven, otherwise the capability stays PARTIAL/NOT_APPLICABLE.

### Verification metadata

```text
Test project: Notrelix.Platform.Tests + Notrelix.Integration.Tests production-runtime proof
Source under test: backend/src/Notrelix.Platform/Messaging/Consumers/ConsumerHost.cs; backend/src/Notrelix.Platform/Messaging/Reliability/OrderingEnforcer.cs; backend/src/Notrelix.Platform/Messaging/Reliability/PoisonDetector.cs; backend/src/Notrelix.Infrastructure/Messaging/ConsumerRegistrySetup.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/MessagingRegistration.cs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml
Existing evidence: OrderingEnforcerTests; PoisonDetectorTests; ConsumerHostDeliveryContractTests plus production MassTransit integration evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/tests/Notrelix.Integration.Tests/Messaging — restart/durability proof for the actual MassTransit/Infrastructure owner or explicit NOT_APPLICABLE/PARTIAL disposition.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Process-local ordering/poison state MUST be documented honestly. Durable delivery/dead-letter guarantees belong to the production transport/store evidence, not to in-memory counters or isolated mechanism tests. For each consumer requiring ordering/poison guarantees, certification MUST identify the production runtime owner, DI/wiring, durable state owner and exact runtime proof; otherwise the capability remains `PARTIAL_GAP` or explicitly `NOT_APPLICABLE` for that consumer.


## PF-TST-RT-065 — Connection lifecycle

### Traceability

```text
Requirement: PFREQ065
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- an authenticated runtime with session generation and an active realtime connection.

**When**

- connect, duplicate-connect, disconnect, session-generation change and reconnect are exercised.

**Then**

- connection lifecycle is deterministic, stale connections/subscriptions are disposed, and the active session owns the live connection.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: TO_ADD_OR_EXTEND — add app-web lifecycle coverage if existing transport tests do not prove host connect/disconnect/session/workspace transition cleanup.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RT-065` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Realtime foundation MUST expose deterministic connect/reconnect/offline/close state and bounded reconnect/heartbeat behavior.


## PF-TST-RT-066 — Subscription scoping

### Traceability

```text
Requirement: PFREQ066
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-07
```

### Executable scenario

**Given**

- subscriptions for Workspace W1 and W2 and envelopes addressed to each.

**When**

- realtime dispatch/filtering runs.

**Then**

- only matching workspace/subscription listeners receive each envelope; cross-workspace events are not delivered to the wrong consumer.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: TO_ADD — app-web subscription/workspace isolation across reconnect and scope switch.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RT-066` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Subscriptions MUST carry explicit Workspace/tenant scope and MUST be rebuilt safely after reconnect/context change.


## PF-TST-RT-067 — Message validation and dedup

### Traceability

```text
Requirement: PFREQ067
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- valid, duplicate, malformed and schema-invalid realtime envelopes.

**When**

- RealtimeClient validation/dedup dispatch runs.

**Then**

- the valid event is processed once, duplicates are ignored safely, malformed/invalid envelopes do not mutate client state and failures are observable.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: CONDITIONAL — extend realtime-client validation/dedup tests only if current unit coverage misses the scenario boundary; app-host recovery proof is not a substitute.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RT-067` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Malformed envelopes MUST be rejected/observed, and duplicate event IDs MUST be boundedly deduplicated.


## PF-TST-RT-068 — Gap detection

### Traceability

```text
Requirement: PFREQ068
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a stream whose checkpoint expects sequence 5 and receives sequence 7, with later envelopes arriving during recovery.

**When**

- gap detection fires.

**Then**

- one recovery transition records expected/received values and applies the documented later-envelope suspend/drop/queue policy rather than blindly continuing normal delivery.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: TO_ADD_OR_EXTEND — sequence-gap state-machine test that proves the later-envelope suspend/drop/buffer policy while recovery is active.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/packages/foundation/realtime — sequence-gap state-machine test with later-envelope policy.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Ordered envelopes MUST detect sequence gaps per subscription/workspace rather than silently delivering an inconsistent stream. Gap detection MUST enter an explicit recovery state that defines whether later envelopes are suspended, dropped, buffered or otherwise controlled until convergence.


## PF-TST-RT-069 — Consumer recovery contract

### Traceability

```text
Requirement: PFREQ069
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-059
```

### Executable scenario

**Given**

- a detected workspace gap with authoritative API recovery available.

**When**

- the app recovery owner runs.

**Then**

- recovery invalidates/refetches/rebuilds using canonical query-key identities for the affected workspace/resources and does not rely on ad-hoc noncanonical key arrays.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: TO_ADD — app-web authoritative workspace recovery test using canonical query-key helpers and observed refetch/rebuild completion.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/apps/web realtime tests — authoritative workspace recovery using canonical query-key helpers.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Each realtime-critical consumer MUST define what happens after a detected gap: authoritative reload, replay, checkpoint/rebase or another explicit mechanism. Recovery MUST target canonical query/cache identities rather than ad-hoc key shapes and MUST define deterministic behavior for recovery failure and retry.


## PF-TST-RT-070 — Invalidate is not universal recovery

### Traceability

```text
Requirement: PFREQ070
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-059
```

### Executable scenario

**Given**

- a gap where query invalidation has been requested but authoritative refetch/rebuild or sequence reconciliation has not completed.

**When**

- the recovery state machine evaluates success.

**Then**

- recovery remains incomplete; invalidation alone cannot mark the stream healthy or resume normal sequence processing.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: TO_ADD — prove invalidation request alone cannot mark recovery complete or resume normal sequence delivery.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/apps/web realtime tests — prove invalidation request is not equivalent to recovery completion.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

The current web Workspace recovery policy invalidates key shapes that do not fully match canonical query identities. Platform MUST NOT certify generic ordered-stream recovery from invalidation alone. The owning recovery policy MUST prove that the authoritative state actually refreshed/reconciled is the state represented by canonical query keys.


## PF-TST-RT-071 — Post-recovery guarantee

### Traceability

```text
Requirement: PFREQ071
Lane: PF-09
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-059
```

### Executable scenario

**Given**

- a gap followed by successful authoritative recovery, plus variants for failed recovery, duplicate/out-of-order post-recovery events and disconnect/reconnect during recovery.

**When**

- the sequence checkpoint is reconciled/reset and delivery resumes.

**Then**

- the next expected sequence is handled normally with no repeated-gap loop; failed recovery retries deterministically, duplicates/out-of-order remain safe, and workspace/subscription isolation survives reconnect.

### Verification metadata

```text
Test project: frontend @notrelix/realtime Vitest (existing) + app-web realtime recovery/lifecycle Vitest suite (TO_ADD)
Source under test: frontend/packages/foundation/realtime/src/transport/realtime-client.ts; frontend/apps/web/src/realtime/workspace-recovery-policy.ts; frontend/apps/web/src/providers/realtime-lifecycle.tsx; frontend/packages/foundation/query/src/query-key-scope.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/realtime/src/__tests__/realtime-client.unit.test.ts; frontend/packages/foundation/realtime/src/__tests__/reconnect-policy.unit.test.ts. No app-web workspace-recovery-policy/realtime-lifecycle integration test exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — transport/reconnect mechanisms have tests, but app-owner authoritative recovery/convergence proof is missing at the preparation baseline.
Required test to add: TO_ADD — gap → authoritative recovery → checkpoint reconcile/reset → next-sequence convergence matrix, including failed recovery, duplicate/out-of-order and reconnect-during-recovery.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/apps/web + @notrelix/realtime — end-to-end gap→recovery→checkpoint reconcile→next-sequence convergence matrix, including failed recovery/reconnect.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

WorkManagement/Documents/Collaboration realtime consumers MUST prove their final consistency guarantee after duplicate, out-of-order, gap and reconnect scenarios before their recovery dependency is D4+/D5. Successful authoritative recovery MUST reconcile/reset the sequence checkpoint so the next valid sequence is delivered normally; failed recovery, duplicate-after-recovery, out-of-order-after-recovery and disconnect/reconnect-during-recovery MUST have deterministic outcomes.


## PF-TST-API-072 — OpenAPI producer authority

### Traceability

```text
Requirement: PFREQ072
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- backend API source and a generated frontend client/type that represents it.

**When**

- contract ownership is evaluated during a producer change.

**Then**

- server API/OpenAPI source remains transport authority and generated client shape cannot redefine Domain/Application meaning.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-API-072` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Backend API is the canonical transport/OpenAPI producer; Domain/Application semantics MUST NOT be inferred from generated clients.


## PF-TST-API-073 — Deterministic export

### Traceability

```text
Requirement: PFREQ073
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- the same exact backend source/configuration exported twice and a deliberately modified tracked OpenAPI artifact.

**When**

- canonical OpenAPI export/drift comparison runs.

**Then**

- identical source produces deterministic compatible output while the modified artifact is detected as drift.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend OpenAPI export/drift job — run export twice from same candidate and compare tracked artifact.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

A fresh canonical OpenAPI export MUST be byte/semantic compatible with the tracked artifact for the same source candidate.


## PF-TST-API-074 — Security metadata

### Traceability

```text
Requirement: PFREQ074
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- protected, public and idempotent API operations.

**When**

- OpenAPI generation inspects operation security/transport metadata.

**Then**

- authentication/authorization/idempotency requirements are represented accurately without exposing internal policy-engine details or secrets.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-API-074` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

OpenAPI MUST accurately expose authentication/authorization/idempotency transport requirements without leaking internal policy implementation.


## PF-TST-API-075 — Generated frontend contracts

### Traceability

```text
Requirement: PFREQ075
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- canonical backend OpenAPI plus the tracked generated frontend REST schema.

**When**

- codegen is regenerated and a manual generated-file edit is introduced.

**Then**

- generated contracts are derived from canonical input and manual drift is overwritten/detected by the gate rather than treated as source authority.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-API-075` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Frontend generated contracts MUST derive from canonical artifacts and MUST NOT be hand-maintained copies of server DTOs where generated coverage exists.


## PF-TST-API-076 — Breaking-change review

### Traceability

```text
Requirement: PFREQ076
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-003, BE-PLT-009, BE-PLT-010, BE-PLT-030, BE-PLT-031, BE-PLT-050; TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- a producer change removing/renaming/changing a consumer-visible field or operation.

**When**

- semantic diff, affected-consumer review and compatibility workflow run.

**Then**

- the breaking change is classified, consumer migration/deployment order is recorded, and release is blocked until compatible disposition exists.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/frontend contract compatibility suite — deliberate breaking change fixture must be caught before release.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Breaking producer changes MUST be classified, reviewed with affected consumers and migrated compatibly.


## PF-TST-API-077 — Realtime contract generation

### Traceability

```text
Requirement: PFREQ077
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-031; TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- a changed realtime/public event envelope or event version.

**When**

- event manifest/generated contract drift checks run alongside REST contract checks.

**Then**

- version/envelope artifacts are updated deterministically and old/backlogged consumer compatibility is explicitly handled.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-API-077` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Realtime envelope/generated contract artifacts MUST be versioned and drift-checked alongside OpenAPI where applicable.


## PF-TST-API-078 — Consumer compile gate

### Traceability

```text
Requirement: PFREQ078
Lane: PF-10
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- a backend contract candidate and all generated frontend consumers.

**When**

- OpenAPI export, frontend codegen, typecheck and affected consumer tests execute.

**Then**

- an incompatible change fails before compatibility is declared; compatible change produces a clean generated/consumer build.

### Verification metadata

```text
Test project: Notrelix.API/Architecture contract tests + frontend OpenAPI codegen/typecheck
Source under test: backend/src/Notrelix.API/OpenApi/OpenApiExportCommand.cs; backend/contracts/openapi/notrelix.v1.json; frontend/tooling/codegen/openapi/generate-openapi.mjs; frontend/packages/foundation/contracts/src/generated/rest/schema.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: OpenAPI drift/export gates + generated frontend contract typecheck
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend OpenAPI codegen + typecheck + consumer tests driven from candidate backend artifact.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Contract changes MUST run codegen/typecheck/consumer tests before being declared compatible.


## PF-TST-QUERY-079 — Canonical query-key roots

### Traceability

```text
Requirement: PFREQ079
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- representative global, account and workspace resources plus malformed keys.

**When**

- canonical query-key helpers and validation run.

**Then**

- valid keys use only approved roots and malformed workspace/root/resource shapes are rejected.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: CONDITIONAL — existing query-key-scope tests may satisfy the root-shape portion; extend only for the final helper signature/validation contract.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-QUERY-079` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Server-state keys MUST use the approved `global`, `account` or `workspace` roots and reject malformed workspace keys.


## PF-TST-QUERY-080 — Workspace identity in keys

### Traceability

```text
Requirement: PFREQ080
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the same workspace-scoped resource in W1 and W2 plus an empty workspace identifier.

**When**

- workspaceQueryKey/assertion are exercised.

**Then**

- W1/W2 keys cannot alias and empty workspace identity is rejected.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: CONDITIONAL — retain/extend workspace-key identity tests if final helper changes.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-QUERY-080` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Workspace-scoped keys MUST include Workspace ID so concurrent/transitioned workspaces cannot alias.


## PF-TST-QUERY-081 — Account isolation

### Traceability

```text
Requirement: PFREQ081
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- Account A and B request the same account-scoped resource name.

**When**

- the final account-key/reset mechanism stores and reads both states.

**Then**

- B cannot read A's cached value; Account identity or an equivalent atomic partition/reset differentiates the states.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: TO_ADD — account-aware query identity or atomic hard-reset isolation test proving Account A state cannot appear under Account B.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend @notrelix/query — account-scoped key/partition contract tests with same resource across A/B.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Account-scoped query identity/reset MUST prevent Account A cache from being observable as Account B.


## PF-TST-QUERY-082 — Optimistic update ownership

### Traceability

```text
Requirement: PFREQ082
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a generic optimistic-command coordinator and two feature mutations with different business rollback/invalidation needs.

**When**

- optimistic success and failure paths execute.

**Then**

- foundation coordinates generic cache mechanics only while each feature supplies business patch/rollback/invalidation semantics.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: CONDITIONAL — extend optimistic-command tests only if final account-transition semantics change rollback/invalidation ownership.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-QUERY-082` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Generic optimistic-command infrastructure may coordinate cache mechanics; business teams own mutation meaning, rollback and invalidation semantics.


## PF-TST-QUERY-083 — No duplicate server truth

### Traceability

```text
Requirement: PFREQ083
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a stale/manually altered client cache and a different authoritative server result.

**When**

- normal invalidation/refetch/recovery occurs.

**Then**

- server authority wins and cached/optimistic state cannot become an independent source of business truth.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: CONDITIONAL — add a regression only if existing boundary tests cannot prove cache remains derived state under recovery/transition.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-QUERY-083` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Frontend query/cache state is derived state and MUST NOT become an independent source of business truth.


## PF-TST-QUERY-084 — Account-switch proof

### Traceability

```text
Requirement: PFREQ084
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- Account A with cached account/workspace resources, permission state, an in-flight optimistic mutation and realtime state.

**When**

- the host executes A → B → A transitions.

**Then**

- no A state appears under B; pending optimistic/subscription state is partitioned or cancelled correctly; returning to A yields only valid A state without cross-account collision.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: TO_ADD — app-host Account A→B→A integration test covering cached resources, permissions, optimistic state and realtime-derived state.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend app-web — mandatory A→B→A full-state isolation integration scenario.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Execution evidence MUST exercise A→B→A account transition with cached resources, permissions and realtime state.


## PF-TST-QUERY-085 — Generator compliance

### Traceability

```text
Requirement: PFREQ085
Lane: PF-11
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the feature generator/template is used to create account- and workspace-scoped query code.

**When**

- generated output and its tests are inspected against the final isolation contract.

**Then**

- new code emits compliant key helpers including required identities and cannot regenerate the old unsafe account-key shape.

### Verification metadata

```text
Test project: frontend @notrelix/query Vitest + feature/app-web integration tests
Source under test: frontend/packages/foundation/query/src/query-key-scope.ts; frontend/packages/features/account/src/query/keys.ts; frontend/packages/features/workspace/src/query/keys.ts; frontend/packages/features/notifications/src/query/keys.ts; frontend/tooling/generators/create-feature/index.mjs
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: frontend/packages/foundation/query/src/__tests__/query-key-scope.unit.test.ts; optimistic-command.unit.test.ts; query-boundary.unit.test.ts; feature query-key source. No accepted Account A→B→A host/cache/permission/realtime isolation proof exists at the preparation baseline.
Evidence state: PARTIAL_TEST_PRESENT — query primitives are tested, but Account-transition isolation is not proven end-to-end at the preparation baseline.
Required test to add: TO_ADD_OR_EXTEND — generator regression proving newly generated features use the final Account/Workspace query-key contract.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/tooling/generators/create-feature — generator golden test for final account/workspace key contract.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Feature generators/templates MUST emit query keys and tests consistent with the final Account/Workspace isolation contract.


## PF-TST-RUNTIME-086 — Runtime composition root

### Traceability

```text
Requirement: PFREQ086
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- web/mobile app startup composition and a negative feature package attempting to construct environment/runtime infrastructure directly.

**When**

- dependency/composition tests execute.

**Then**

- runtime/service construction occurs at approved host composition roots and the feature package cannot own global runtime construction.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-086` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Environment/service/runtime construction MUST occur in approved runtime/host composition roots rather than feature packages.


## PF-TST-RUNTIME-087 — Host-specific adapters

### Traceability

```text
Requirement: PFREQ087
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a reusable foundation package and host-specific browser/mobile APIs.

**When**

- dependency rules and host package tests run.

**Then**

- host-specific APIs stay in runtime/host packages and importing a web-only adapter into reusable/mobile foundation is rejected.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-087` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Web/mobile/marketing host framework concerns MUST remain in their host/runtime packages; reusable foundation packages cannot assume one host.


## PF-TST-RUNTIME-088 — Typed session-expired plumbing

### Traceability

```text
Requirement: PFREQ088
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a runtime session expires while a feature is active.

**When**

- generic auth/runtime emits session-expired state and the app host handles it.

**Then**

- the event/state is typed, host navigation policy executes outside foundation, and the runtime does not hardcode product route behavior.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-088` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Generic auth/runtime infrastructure MUST emit typed session-expired state/events while the app host owns navigation policy.


## PF-TST-RUNTIME-089 — Realtime factory ownership

### Traceability

```text
Requirement: PFREQ089
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the foundation realtime protocol client and browser/mobile WebSocket construction.

**When**

- composition/dependency tests inspect ownership.

**Then**

- protocol/connection state stays in foundation while concrete WebSocket factories live in host runtime packages.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-089` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Web/mobile-specific WebSocket factories belong to runtimes while protocol/connection state remains foundation-owned.


## PF-TST-RUNTIME-090 — Environment validation

### Traceability

```text
Requirement: PFREQ090
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-038
```

### Executable scenario

**Given**

- missing, malformed and valid required runtime configuration.

**When**

- supported host startup validates environment/config.

**Then**

- invalid/missing configuration fails fast with observable typed diagnostics; valid configuration composes successfully.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-090` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Required runtime configuration MUST fail fast and observably when invalid or absent.


## PF-TST-RUNTIME-091 — No route hardcoding in foundation

### Traceability

```text
Requirement: PFREQ091
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a negative fixture adding product route strings/navigation decisions to generic foundation/runtime code.

**When**

- dependency/source guard tests run.

**Then**

- the fixture fails and route ownership remains in app host/router layers.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-091` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Generic foundation code MUST NOT own product route strings when routing authority belongs to app hosts.


## PF-TST-RUNTIME-092 — Composition tests

### Traceability

```text
Requirement: PFREQ092
Lane: PF-12
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- supported web/mobile hosts with their production-like runtime dependencies.

**When**

- composition smoke tests instantiate each host and exercise shared services.

**Then**

- each consumed Platform service is wired through the correct adapter and no host relies on an untested hidden composition path.

### Verification metadata

```text
Test project: frontend runtime-web/runtime-mobile Vitest + app composition smoke tests
Source under test: frontend/packages/runtimes/web/src/runtime/app-runtime.tsx; frontend/packages/runtimes/web/src/runtime/session-event-bus.ts; frontend/packages/runtimes/web/src/realtime/browser-websocket-factory.ts; frontend/apps/web/src/providers/app-providers.tsx; frontend/apps/web/src/composition/application-services.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: frontend-ci.yml
Existing evidence: runtime package tests + app composition/host smoke tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-RUNTIME-092` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Each supported host MUST have production-like composition evidence for the shared services it consumes.


## PF-TST-UI-093 — Token vs product component boundary

### Traceability

```text
Requirement: PFREQ093
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- shared token/primitive packages and a product-semantic component such as an entitlement/board-specific control.

**When**

- package placement/dependency review runs.

**Then**

- generic tokens/primitives stay reusable while product-semantic components remain in product/feature packages.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: CONDITIONAL — use existing architecture/component evidence unless token-vs-product boundary lacks a negative regression.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-UI-093` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared UI MUST distinguish generic design token/primitive from product-semantic component and host-specific composition.


## PF-TST-UI-094 — CSS export validity

### Traceability

```text
Requirement: PFREQ094
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the published `@notrelix/ui-tokens` package metadata and its `./css` subpath.

**When**

- package export resolution/import verification runs on the candidate.

**Then**

- the subpath resolves to a real governed CSS source/build artifact or the unused export is intentionally removed with compatibility review; a dangling path cannot pass D4.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: TO_ADD — package-export resolution regression that fails when `./css` points to a missing source/output and passes only for the governed real artifact or approved export removal.
Execution evidence: NOT_RECORDED
```

### Required proof target

- frontend/packages/ui/tokens — package-export resolution test for `@notrelix/ui-tokens/css`; dangling export must fail.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

`@notrelix/ui-tokens` declares `./css -> ./src/css/index.css`, but the audited source path is absent. This export MUST either be backed by real source/build output or removed/migrated intentionally.


## PF-TST-UI-095 — Theme/token stability

### Traceability

```text
Requirement: PFREQ095
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a semantic/theme token change with web/mobile/marketing consumers.

**When**

- consumer build/visual tests run and source is searched for duplicated replacement literals.

**Then**

- token changes propagate through the shared source with reviewed impact and feature packages do not silently fork the same design token.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: CONDITIONAL — extend token/theme regression only if the final CSS/token source path or generation contract changes.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-UI-095` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Primitive/semantic tokens and themes MUST be versioned/changed with consumer impact review rather than duplicated inside feature packages.


## PF-TST-UI-096 — Accessibility foundation

### Traceability

```text
Requirement: PFREQ096
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- shared interactive primitives under keyboard, focus and reduced-motion conditions.

**When**

- a11y/unit/interaction tests execute.

**Then**

- keyboard navigation, visible focus, semantic roles/labels and reduced-motion behavior meet the shared accessibility contract.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: CONDITIONAL — retain/extend shared primitive accessibility tests for affected primitives.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-UI-096` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared primitives MUST preserve keyboard, focus, reduced-motion and semantic accessibility expectations required by the quality standard.


## PF-TST-UI-097 — Responsive foundation

### Traceability

```text
Requirement: PFREQ097
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- shared layout/primitives rendered at approved narrow/medium/wide viewport classes.

**When**

- responsive evidence tests run.

**Then**

- foundation remains usable across supported viewport classes without embedding WorkManagement/Documents-specific behavior.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: CONDITIONAL — retain/extend responsive evidence for affected primitives/layouts.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-UI-097` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared layout/primitives MUST support approved viewport classes without embedding WorkManagement/Documents-specific product behavior.


## PF-TST-UI-098 — Evidence manifests

### Traceability

```text
Requirement: PFREQ098
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the owner-local UI evidence manifest and critical foundation surfaces.

**When**

- manifest/generator/evidence drift validation runs.

**Then**

- required critical surfaces have traceable visual/a11y evidence and stale/missing manifest entries fail the governed gate.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: CONDITIONAL — extend UI evidence-manifest drift validation if the governed surface set changes.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-UI-098` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Critical UI foundation surfaces MUST participate in the repository's owner-local visual/a11y evidence mechanism where applicable.


## PF-TST-UI-099 — No business semantics in primitives

### Traceability

```text
Requirement: PFREQ099
Lane: PF-13
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a negative primitive implementation branching on role, entitlement, Board/Page state or another business concept.

**When**

- source/dependency/architecture checks run.

**Then**

- the primitive is rejected; business semantics stay with the owning feature/product context.

### Verification metadata

```text
Test project: frontend ui-tokens/ui-web typecheck + ui-web Vitest (existing) + package-export resolution regression (TO_ADD for PF-TST-UI-094)
Source under test: frontend/packages/ui/tokens/package.json; frontend/packages/ui/tokens/src/; frontend/packages/ui/web/src/; frontend/packages/ui/web/verification/ui-evidence.manifest.json
Positive/negative: BOTH
Security-sensitive: NO
Migration-sensitive: YES
CI gate: frontend-ci.yml + docs-ci.yml where UI evidence is governed
Existing evidence: frontend/packages/ui/web/src/__tests__/ui-web-components.component.test.tsx; frontend/packages/ui/web/verification/ui-evidence.manifest.json; @notrelix/ui-tokens package metadata. No baseline test proves the declared `@notrelix/ui-tokens/css` export resolves to a real source artifact; the target path is absent.
Evidence state: PARTIAL_TEST_PRESENT — UI component/evidence tests exist, while PF-13 remains blocked by the dangling CSS export and lacks export-resolution proof.
Required test to add: CONDITIONAL — add/retain negative architecture/source guard rejecting business semantics in shared primitives.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-UI-099` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform UI primitives MUST NOT encode roles, entitlements, Board/Page state or other bounded-context business rules.


## PF-TST-OBS-100 — Correlation propagation

### Traceability

```text
Requirement: PFREQ100
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-006, BE-PLT-029, BE-PLT-042; TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- an HTTP request that causes Application work, durable messaging and realtime/consumer activity.

**When**

- the request executes end-to-end with correlation/operation metadata.

**Then**

- safe correlation identity is propagated across supported boundaries and can be joined without using secrets or raw payload as correlation.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend integration + frontend observability/realtime — correlation chain proof across supported boundaries.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

HTTP/Application/messaging/realtime paths MUST preserve safe correlation/operation identity across the supported chain.


## PF-TST-OBS-101 — Trace and metric mechanics

### Traceability

```text
Requirement: PFREQ101
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-042
```

### Executable scenario

**Given**

- generic tracing/metrics infrastructure and one domain-specific metric.

**When**

- instrumentation tests inspect emitted activities/metrics and ownership.

**Then**

- Platform supplies reusable mechanics with bounded dimensions while domain-specific metric meaning remains with its owning business team.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-OBS-101` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform owns reusable tracing/metrics/logging mechanics; business teams own domain-specific metric meaning.


## PF-TST-OBS-102 — Secret redaction

### Traceability

```text
Requirement: PFREQ102
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-047
```

### Executable scenario

**Given**

- requests/events containing Authorization, session, CSRF and provider credentials plus ordinary safe metadata.

**When**

- backend and frontend logging/telemetry paths emit diagnostics.

**Then**

- reusable secrets are removed/redacted and safe diagnostic metadata remains usable.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/frontend observability suites — explicit credential/session/CSRF/provider-secret redaction matrix.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Observability MUST redact credentials, authorization headers, session/CSRF/provider secrets and other reusable sensitive material.


## PF-TST-OBS-103 — Tenant-safe context

### Traceability

```text
Requirement: PFREQ103
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-042
```

### Executable scenario

**Given**

- two tenants producing telemetry with representative Account/Workspace/resource data.

**When**

- metric/log/trace dimensions are captured.

**Then**

- tenant context is bounded/safe, no other tenant payload leaks, and unbounded raw IDs/payload fields are not used as high-cardinality labels by default.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-OBS-103` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Account/Workspace identifiers may be attached only in safe bounded form; telemetry MUST NOT leak another tenant's data or create unsafe high-cardinality labels.


## PF-TST-OBS-104 — Retry/failure signals

### Traceability

```text
Requirement: PFREQ104
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-018, BE-PLT-020, BE-PLT-025, BE-PLT-043; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- idempotency retries, outbox retry/terminal failure and realtime recovery/gap paths.

**When**

- failure/retry states are triggered.

**Then**

- bounded actionable signals expose attempt/outcome/terminal state and backlog/gap freshness without requiring raw payload inspection.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend/frontend reliability telemetry tests — retry/terminal/backlog/gap signals with bounded safe dimensions.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Async delivery and idempotency/realtime mechanisms MUST expose bounded retry/failure/terminal state signals.


## PF-TST-OBS-105 — Health checks

### Traceability

```text
Requirement: PFREQ105
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-038, BE-PLT-043; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- healthy and failed DB, Redis, messaging and outbox/backlog dependency conditions.

**When**

- health/readiness endpoints/checks execute.

**Then**

- readiness reflects meaningful dependency degradation and does not report only process liveness while a required dependency is unusable.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- backend integration health tests — DB/Redis/messaging/outbox degraded-state readiness matrix.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Database, Redis, messaging and outbox health checks MUST reflect meaningful readiness/dependency state rather than only process liveness.


## PF-TST-OBS-106 — Failure observability

### Traceability

```text
Requirement: PFREQ106
Lane: PF-14
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-049; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- forced initialization, pipeline and background-processing failures.

**When**

- the failures propagate through observability and service state.

**Then**

- logs/metrics/traces identify the failure and the actual error is not swallowed or converted into false success.

### Verification metadata

```text
Test project: Notrelix.Application.Tests + Notrelix.Infrastructure.Tests + Notrelix.Integration.Tests + frontend observability Vitest
Source under test: backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs; backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs; backend/src/Notrelix.Infrastructure/DependencyInjection/ObservabilityRegistration.cs; frontend/packages/foundation/observability/src/telemetry/redaction.ts; frontend/packages/foundation/observability/src/telemetry/telemetry.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: NO
CI gate: backend-ci.yml + frontend-ci.yml
Existing evidence: PipelineObservabilityTests; MetricsServiceTests; PipelineTelemetryIntegrationTests; frontend observability tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-OBS-106` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Initialization, pipeline and background failures MUST be discoverable through logs/metrics/traces without swallowing the actual failure.


## PF-TST-ARCH-107 — Domain purity gates

### Traceability

```text
Requirement: PFREQ107
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- Domain source plus a negative fixture referencing Infrastructure/API/framework dependencies.

**When**

- backend architecture tests execute.

**Then**

- the negative dependency is rejected and Domain stays framework/infrastructure-pure.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-107` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Executable architecture tests MUST prevent framework/Infrastructure/API leakage into Domain.


## PF-TST-ARCH-108 — Layer dependency gates

### Traceability

```text
Requirement: PFREQ108
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the accepted backend project/layer graph plus a forbidden dependency edge.

**When**

- layer dependency gates run.

**Then**

- the forbidden edge fails while accepted dependency directions remain green.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-108` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Backend project/layer dependencies MUST remain within accepted architecture and ADR boundaries.


## PF-TST-ARCH-109 — Authorization ownership gates

### Traceability

```text
Requirement: PFREQ109
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: TAC flow: PF-FLOW-01
```

### Executable scenario

**Given**

- a negative feature implementation adding handler-local RBAC/legacy permission authority.

**When**

- authorization ownership architecture gates run.

**Then**

- the violation fails and the canonical AccessControl pipeline remains the only protected-action authority.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-109` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Architecture tests MUST forbid feature-local/legacy authorization authority and protect the canonical pipeline.


## PF-TST-ARCH-110 — Bounded-context ownership gates

### Traceability

```text
Requirement: PFREQ110
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a negative cross-context private persistence/semantic ownership edge covered by declared repository rules.

**When**

- bounded-context architecture gates run.

**Then**

- the forbidden edge is caught or explicitly governed; hidden private-table/business ownership coupling cannot silently pass.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-110` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Cross-context private persistence or semantic ownership violations MUST be caught where the repository has declared enforceable boundaries.


## PF-TST-ARCH-111 — Frontend dependency manifest

### Traceability

```text
Requirement: PFREQ111
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the frontend `architecture-manifest.ts`, package manifests and generated package-boundaries document.

**When**

- dependency-rule tests/generation run.

**Then**

- the manifest remains executable authority, package manifests conform, and the generated document matches it rather than becoming competing authority.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-111` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

`frontend/tooling/dependency-rules/src/architecture-manifest.ts` remains executable frontend dependency authority; generated docs are evidence, not competing authority.


## PF-TST-ARCH-112 — Generated drift gates

### Traceability

```text
Requirement: PFREQ112
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a manually edited generated project/package/contract artifact with unchanged producer source.

**When**

- drift gates regenerate/compare outputs.

**Then**

- manual drift fails and canonical producer source must be changed instead.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-112` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Generated project/package/contract inventories MUST be drift-checked rather than manually edited as canonical authority.


## PF-TST-ARCH-113 — Gate-change governance

### Traceability

```text
Requirement: PFREQ113
Lane: PF-15
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- a failing architecture gate caused by a real semantic violation.

**When**

- a change attempts to weaken/delete the gate without approved architecture decision evidence.

**Then**

- the change remains blocked; only implementation repair or explicit approved architecture-policy change can alter the gate.

### Verification metadata

```text
Test project: Notrelix.Architecture.Tests + frontend dependency-rules tests
Source under test: backend/tests/Notrelix.Architecture.Tests/; frontend/tooling/dependency-rules/src/architecture-manifest.ts; frontend/tooling/dependency-rules/src/check-package-manifests.ts; frontend/tooling/dependency-rules/src/generate-architecture-docs.ts
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: backend-ci.yml + frontend-ci.yml + docs-ci.yml
Existing evidence: backend architecture suite + frontend closed-world dependency-rule tests
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-ARCH-113` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

A failing architecture gate MUST be fixed in implementation or changed through explicit architecture approval; tests are not weakened merely for convenience.


## PF-TST-CI-114 — Suite non-zero execution

### Traceability

```text
Requirement: PFREQ114
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- each critical CI job with its intended test filter/suite plus a deliberately empty/mismatched filter case.

**When**

- CI executes and records discovered/executed counts.

**Then**

- required suites run non-zero tests and the empty/skipped proof cannot be interpreted as green.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- CI meta-proof — each critical test invocation records non-zero discovery/execution; empty filter fixture must fail.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Critical CI jobs MUST prove the intended suites executed non-zero tests; a skipped/empty suite cannot be interpreted as success.


## PF-TST-CI-115 — Dependency-aware final gate

### Traceability

```text
Requirement: PFREQ115
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- an aggregate/final gate whose prerequisite job succeeds, fails and is skipped in separate controlled cases.

**When**

- dependency-aware result evaluation runs.

**Then**

- only the fully satisfied required prerequisite set can make the aggregate green; failed/skipped required proof is preserved as non-success.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- CI aggregate-gate proof — skipped/failed required prerequisite cannot become green.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Aggregate gates MUST distinguish success, failure and skipped prerequisite jobs rather than converting a skipped required proof into green.


## PF-TST-CI-116 — Container runtime proof

### Traceability

```text
Requirement: PFREQ116
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- built backend/web/marketing images from the candidate.

**When**

- container CI starts each required image, inspects runtime user and probes a real health/HTTP route.

**Then**

- the container runs non-root and the real route becomes healthy; image-start-only proof is insufficient.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- .github/workflows/container-ci.yml — run built images non-root and probe real health/HTTP routes.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Container validation MUST run the built image as non-root and probe a real health/HTTP route. Current container workflow does this for backend/web/marketing.


## PF-TST-CI-117 — Security/SBOM evidence

### Traceability

```text
Requirement: PFREQ117
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- the selected candidate images/artifacts.

**When**

- governed vulnerability scan and SBOM/provenance generation execute.

**Then**

- policy-threshold vulnerabilities fail as configured and required SBOM/provenance artifacts are produced and tied to the candidate image.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- .github/workflows/container-ci.yml + security-ci.yml — Trivy/policy plus SBOM/provenance artifact verification.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Selected images MUST pass the governed vulnerability scan and produce the required SBOM/provenance evidence.


## PF-TST-CI-118 — Exact-source publication

### Traceability

```text
Requirement: PFREQ118
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- an image digest validated by CI and the publication/promotion path.

**When**

- release publication is performed.

**Then**

- the published artifact digest is the exact validated digest for the recorded source SHA; no unverified rebuild substitutes new bytes.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- release/promotion proof — compare validated digest to published digest; no rebuild after validation.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Trusted publication MUST publish the exact validated image bytes for the recorded source SHA rather than rebuilding unverified bytes.


## PF-TST-CI-119 — Docs/generated governance

### Traceability

```text
Requirement: PFREQ119
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- governed generated docs/contracts with a deliberate source/output mismatch.

**When**

- docs/generated CI checks execute.

**Then**

- drift fails the job and CI does not silently rewrite/commit canonical artifacts to manufacture green.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CI-119` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

CI MUST fail on governed documentation/generated-artifact drift and MUST NOT silently rewrite canonical artifacts.


## PF-TST-CI-120 — Exact-candidate certification

### Traceability

```text
Requirement: PFREQ120
Lane: PF-16
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-037, BE-PLT-038, BE-PLT-062, BE-PLT-063
```

### Executable scenario

**Given**

- the accepted final certification SHA and historical green evidence from baseline `902bc9c5...`.

**When**

- Platform D4/D5 evidence is assembled.

**Then**

- final records name the exact accepted SHA and required run/job/runtime/security/container artifacts; baseline runs remain preparation evidence if the final SHA differs.

### Verification metadata

```text
Test project: GitHub Actions CI/runtime evidence
Source under test: .github/workflows/backend-ci.yml; .github/workflows/frontend-ci.yml; .github/workflows/container-ci.yml; .github/workflows/security-ci.yml; .github/workflows/docs-ci.yml; backend/tests/ci-proofs.json; tools/deliveryctl/architecture.py; scripts/ci/validate-infra.py
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: ci.yml / backend-ci.yml / frontend-ci.yml / container-ci.yml / security-ci.yml / docs-ci.yml
Existing evidence: exact GitHub Actions run/job logs and produced artifacts
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- certification evidence check — exact final SHA, run/job IDs, runtime/container/security evidence; historical baseline run is insufficient for another SHA.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform STABLE/D5 claims MUST record exact source SHA, CI run/job evidence and required runtime/security/container proofs. For baseline `902bc9c5b39a003df3dce6673edc124984d2b398`, CodeQL run `36227690203` and Notrelix CI run `36227690334` are available successful exact-head evidence, but remain `NOT_EVALUATED` for Platform certification until bound to the applicable PFREQ/PFAC records.


## PF-TST-CROSS-121 — Cross-lane secret safety

### Traceability

```text
Requirement: PFREQ121
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-028, BE-PLT-047
```

### Executable scenario

**Given**

- representative secrets injected at HTTP, logs, client persistence, message metadata and diagnostics boundaries.

**When**

- all affected Platform lanes execute their transport/observability/persistence paths.

**Then**

- secrets remain confined to intended transport boundaries and are never emitted into logs, generated artifacts, messages or client storage.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CROSS-121` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

No Platform mechanism may expose reusable credentials/secrets in logs, generated artifacts, client persistence, messages or diagnostics outside the intended transport boundary.


## PF-TST-CROSS-122 — Cross-lane tenant isolation

### Traceability

```text
Requirement: PFREQ122
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-027, BE-PLT-035; TAC flow: PF-FLOW-01, PF-FLOW-03, PF-FLOW-06, PF-FLOW-07
```

### Executable scenario

**Given**

- two Accounts/Workspaces carrying similarly shaped data through HTTP, Application, persistence, messaging, realtime, cache and telemetry.

**When**

- cross-layer operations and transitions execute.

**Then**

- every layer preserves tenant partitioning and no state from tenant A becomes observable/actionable in tenant B.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- cross-layer tenant isolation matrix spanning HTTP/Application/RLS/messaging/realtime/query/telemetry.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Account/Workspace isolation MUST be preserved across HTTP, Application, persistence, messaging, realtime, frontend cache and observability.


## PF-TST-CROSS-123 — Cross-lane idempotent/retry safety

### Traceability

```text
Requirement: PFREQ123
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-005, BE-PLT-013, BE-PLT-014, BE-PLT-033, BE-PLT-039, BE-PLT-040, BE-PLT-046, BE-PLT-057, BE-PLT-060, BE-PLT-061; TAC flow: PF-FLOW-02, PF-FLOW-03, PF-FLOW-04
```

### Executable scenario

**Given**

- one business operation exposed to HTTP retry, outbox redelivery, consumer retry and realtime recovery duplication.

**When**

- failure/retry conditions are injected at each layer.

**Then**

- the end-to-end operation produces no duplicate business effect beyond the explicitly allowed semantics.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- end-to-end duplicate/retry matrix spanning HTTP idempotency, outbox redelivery, consumer dedup and realtime recovery.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Retries across HTTP, outbox, consumers and realtime recovery MUST not create duplicate business side effects.


## PF-TST-CROSS-124 — Cross-lane failure transparency

### Traceability

```text
Requirement: PFREQ124
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-017, BE-PLT-018, BE-PLT-020, BE-PLT-022, BE-PLT-028, BE-PLT-034, BE-PLT-048, BE-PLT-054, BE-PLT-056, BE-PLT-063; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- required dependencies/configurations that fail at HTTP, persistence, messaging, realtime and startup boundaries.

**When**

- each failure path is exercised.

**Then**

- the system fails closed or emits an observable degraded/error state and never reports silent semantic success.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CROSS-124` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform failures MUST fail closed or fail observably according to the contract; silent success is forbidden.


## PF-TST-CROSS-125 — Cross-lane performance bounds

### Traceability

```text
Requirement: PFREQ125
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-019, BE-PLT-023, BE-PLT-032, BE-PLT-035, BE-PLT-041, BE-PLT-057, BE-PLT-058; TAC flow: PF-FLOW-04
```

### Executable scenario

**Given**

- load/backlog cases stressing retry budgets, in-memory queues, database queries and telemetry dimensions.

**When**

- shared mechanisms operate at/over configured bounds.

**Then**

- retry/concurrency/memory/query/cardinality behavior remains bounded and actionable rather than growing without limit.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CROSS-125` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Shared mechanisms MUST use bounded memory/retry/query/queue behavior and MUST not impose unbounded scans or high-cardinality telemetry by default.


## PF-TST-CROSS-126 — Cross-lane compatibility and migration

### Traceability

```text
Requirement: PFREQ126
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-002, BE-PLT-003, BE-PLT-007, BE-PLT-008, BE-PLT-009, BE-PLT-010, BE-PLT-012, BE-PLT-030, BE-PLT-031, BE-PLT-032, BE-PLT-034, BE-PLT-046, BE-PLT-050, BE-PLT-051, BE-PLT-053; TAC flow: PF-FLOW-02, PF-FLOW-05
```

### Executable scenario

**Given**

- a change to query keys, message envelopes, operation identities, generated contracts, RLS/session variables, UI exports or delivery state while old consumers/backlog/persisted state still exist.

**When**

- compatibility review and rollout/recovery tests run.

**Then**

- migration/dual-read/versioned rollout/drain/rebaseline strategy is explicit or release is blocked; old bytes/state are treated as deployed compatibility surfaces.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- compatibility fixture retaining old consumer/backlog/persisted state while changing each governed shared contract class.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Changes to keys, envelopes, operation identities, generated contracts, RLS/session variables, UI exports, delivery state or CI evidence formats require explicit compatibility/migration review. Old consumers, queued/outbox/dead-letter/replay bytes, persisted dedup/order/checkpoint state and supported upgrade baselines are deployed compatibility surfaces and MUST be inventoried before the change is accepted.


## PF-TST-CROSS-127 — Cross-lane source-of-truth rule

### Traceability

```text
Requirement: PFREQ127
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: BE-PLT: BE-PLT-004, BE-PLT-026, BE-PLT-029, BE-PLT-036, BE-PLT-044, BE-PLT-049, BE-PLT-059; TAC flow: PF-FLOW-05
```

### Executable scenario

**Given**

- a deliberately stale cache/projection/generated client/telemetry or delivery-status record disagreeing with authoritative product state.

**When**

- the product read/recovery path evaluates the disagreement.

**Then**

- the declared source authority wins and supporting technical state is rebuilt/refetched rather than promoted to competing business truth.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: CONDITIONAL
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- Run/extend the lane-owned focused suite for `PF-TST-CROSS-127` using the named project(s); add a dedicated regression test if the current suite cannot exercise the stated Given/When/Then boundary.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Caches, projections, generated clients, telemetry and delivery records are supporting technical state and MUST NOT become competing business sources of truth.


## PF-TST-CROSS-128 — Platform exit/handoff contract

### Traceability

```text
Requirement: PFREQ128
Lane: CROSS
Preparation state: NOT_EVALUATED
Canonical links: none directly assigned; verify through PFREQ and lane gates
```

### Executable scenario

**Given**

- all lane certification results, consumer dependency targets and unresolved debt.

**When**

- the final Platform handoff is produced.

**Then**

- it reports capability-by-capability readiness, affected consumers, blockers, invalidating changes and full cross-team handoff fields; a blanket `Platform complete` statement is forbidden while a required lane is below target.

### Verification metadata

```text
Test project: cross-lane focused suites + architecture/integration/frontend/CI evidence
Source under test: all changed Platform mechanisms and their direct consumers; use the lane source map in PLAN §9
Positive/negative: BOTH
Security-sensitive: YES
Migration-sensitive: YES
CI gate: all affected required workflows; final aggregate gate
Existing evidence: lane-specific focused tests plus exact-candidate aggregate evidence
Evidence state: PREPARATION_ONLY — named source/test/docs evidence may exist, but candidate PASS is not recorded by this line.
Required test to add: CONDITIONAL — add or extend a focused regression only if the named evidence cannot exercise this scenario's exact Given/When/Then boundary.
Execution evidence: NOT_RECORDED
```

### Required proof target

- final handoff schema validation against readiness matrix and consumer blockers.
- Exercise the exact boundary described above; mocks may support setup but cannot replace a required database/browser/broker/container/production-DI boundary.
- Record exact candidate SHA and non-zero discovered/executed/passed/failed/skipped counts for every runnable suite claimed as evidence.
- Preserve failure evidence: do not convert expected negative-path rejection into a skipped/ignored test.

### Expected result

`PASS` only when the **Then** state is observed on the accepted candidate and every required negative path fails in the intended way. Otherwise record `BLOCKED` or `FAIL`; do not infer readiness from source presence.

### Normative context

Platform certification MUST publish capability-by-capability readiness, blocking debt, consumers affected and invalidation rules; there is no single blanket `Platform done` claim that hides lane gaps. Every shared-mechanism handoff MUST record: `Platform capability`, `Current contract`, `Target contract`, `Affected teams`, `Breaking/additive`, `Migration strategy`, `Feature-team action`, `Platform action`, `Verification`, `Required readiness`, `Rollback/forward-fix`, plus candidate SHA and exact evidence references.
