---
document_id: WRK-TESTS-AUTOMATION-INTEGRATIONS
document_type: workstream-test-plan
status: active
owner: automation-integrations-team
applies_to:
  - backend
  - frontend
  - automation
  - integrations
  - p5
  - testing
  - certification
evidence:
  - docs/workstreams/executions/automation-integrations/automation-integrations.spec.md
  - docs/workstreams/executions/automation-integrations/automation-integrations.plan.md
  - docs/product/automation.md
  - docs/product/integrations.md
  - backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
review_on:
  - p5-requirement-change
  - producer-contract-change
  - rule-schema-change
  - provider-protocol-change
  - retry-or-reconciliation-change
  - frontend-contract-change
  - realtime-contract-change
  - migration-change
  - exact-candidate-change
---

# TESTS — P5 Automation & Integrations

## 1. Purpose

This is the executable verification contract for the P5 Automation & Integrations workstream.

It is not a checklist of requirement prose. Every scenario binds one `AIREQ` to an exact source/runtime owner, an existing evidence state, a required proof target and an exact-candidate execution record.

Baseline used while preparing this file:

```text
Repository: Nqv1208/Notrelix
Preparation SHA: 902bc9c5b39a003df3dce6673edc124984d2b398
Preparation state: NOT_EVALUATED
```

A source file, test file, fixture, Domain type or historical green CI run is **preparation evidence only**. It is not PASS until the required proof is executed on the candidate being certified.

## 2. Stable-ID rule

The 42 pre-existing `AI-TST-*` IDs are retained unchanged. New direct requirement scenarios use `AI-TST-AIREQ-###`.

Each of the 128 requirements has exactly one primary direct scenario in this file. Other existing tests may provide supporting evidence to that primary scenario.

## 3. Evidence-state vocabulary

```text
EXISTING_TEST_REUSE
  Existing exact test substantially proves the contract, but must be rerun on the candidate.

EXISTING_TEST_EXTEND
  Useful evidence exists but one or more acceptance/failure/runtime dimensions must be added.

NEW_TEST_REQUIRED
  Current source/tests expose the gap but do not prove the target.

NEW_TEST_REQUIRED_IF_RELEASED
  Capability is optional/deferred; a runtime test becomes mandatory if it enters release scope.

NEW_TEST_REQUIRED_PER_RELEASED_PROVIDER
  One generic adapter test cannot certify unrelated provider protocols.

NEW_TEST_REQUIRED_ON_SCHEMA_CHANGE
  Migration/compatibility fixture is mandatory when the governed persisted/contracted schema changes.

ADMISSION_DECISION_PROOF
  Domain/source existence is not a release signal; product/architecture admission is the first proof.

CI_RUNTIME_PROOF
  PASS requires real production-composition/CI execution evidence, not helper-level tests.

STATIC_GOVERNANCE_PROOF
  Repository/architecture/contract rules are verified statically plus exact-candidate source inspection.
```

## 4. Global PASS/BLOCKED rules

A scenario may be `PASS` only when:

- the exact candidate SHA is recorded;
- the named source/runtime owner is the one actually executed or inspected;
- positive and required negative/failure paths pass;
- required migration/compatibility evidence passes;
- suite execution is non-zero and has no unexplained skips;
- secrets/tenant isolation are preserved where marked security-sensitive;
- `Execution evidence` contains a concrete run/command/artifact/CI locator.

A scenario is `BLOCKED`, not PASS, when a mandatory upstream dependency or provider protocol is unavailable. Deferred capabilities may be `NOT_APPLICABLE` only when the release decision explicitly excludes them.

## 5. Deep-proof priorities

The following baseline gaps require especially strong evidence and MUST NOT be closed by source existence alone:

```text
AI-GAP-01  Rule/API configuration drift
AI-GAP-02  immutable Rule revision missing
AI-GAP-03  Condition/multi-action runtime absent
AI-GAP-04  trigger/action vocabulary > executors
AI-GAP-05  Schedule/Templates/Agents Domain-only
AI-GAP-06  Automation frontend contract/wiring drift
AI-GAP-07  Automation permission vocabulary review
AI-GAP-08  Billing capacity release lifecycle
AI-GAP-09  provider OAuth/install incomplete
AI-GAP-10  provider/catalog frontend drift
AI-GAP-11  N8n reconciliation operational gap
AI-GAP-12  manual Calendar sync stub
AI-GAP-13  generic outbound webhook runtime not certified
AI-GAP-14  provider-specific webhook authenticity not proven
AI-GAP-15  Domain execution facts exist but public/realtime mapping is missing
AI-GAP-16  stale historical TAC dispositions
```

## 6. Executable requirement scenarios

## AI-TST-AIREQ-001 — Authority precedence and conflict handling

### Traceability

- Requirement: `AIREQ001` — Authority precedence and conflict handling
- PLAN: `AI-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact SHA is pinned and product/architecture/ADR/workstream/source authorities are all available.

**When**  
a historical TAC/workstream statement contradicts the exact source status or a lower-level implementation contradicts product semantics.

**Then**  
the verifier keeps the higher semantic authority, labels stale source-status prose `DOC_STALE`, and records any source debt instead of silently choosing one. specifically: Automation & Integrations MUST follow canonical product semantics first, then system/architecture authority, accepted ADRs, team/workstream ownership, and finally implementation evidence. When authority and source disagree, the execution package MUST classify the difference as `DOC_STALE`, `SOURCE_DEBT`, `TRANSITION`, `CONTRACT_CHANGE`, or `UNRESOLVED` before implementation.

**Expected result:** the decision trail names both authorities and the selected classification before implementation proceeds.

### Verification binding

- Test ID: `AI-TST-AIREQ-001`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: the decision trail names both authorities and the selected classification before implementation proceeds. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-ACT-ARCH-002 — Automation and Integrations remain separate bounded contexts

### Traceability

- Requirement: `AIREQ002` — Automation and Integrations remain separate bounded contexts
- PLAN: `AI-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
Automation and Integrations projects/namespaces plus their public ports and persistence ownership are inventoried.

**When**  
architecture scanning inspects Automation Domain/Application references and Integrations provider/client references, including a negative fixture that imports provider-specific code into Automation.

**Then**  
Automation remains owner of Rule/Execution semantics while Integrations remains owner of Connection/provider semantics; the negative fixture is rejected. specifically: Automation owns rule, trigger interpretation, condition/action orchestration and execution product state. Integrations owns provider connection, provider translation, credentials, webhook/sync/provider-operation semantics. One delivery team MUST NOT merge their state models or business ownership.

**Expected result:** no provider SDK/model or Integration aggregate ownership leaks into Automation semantics.

### Verification binding

- Test ID: `AI-TST-ACT-ARCH-002`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: no provider SDK/model or Integration aggregate ownership leaks into Automation semantics. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-003 — Platform mechanisms do not become P5 semantics

### Traceability

- Requirement: `AIREQ003` — Platform mechanisms do not become P5 semantics
- PLAN: `AI-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
P5 uses Platform outbox, broker, dedup, RLS restoration, retry and realtime transport mechanisms.

**When**  
a P5 flow is traced from business intent through those mechanisms and a negative design attempts to place Rule/provider semantics inside Platform.

**Then**  
mechanical reliability remains Platform-owned while P5 retains business/provider decisions and failure taxonomy. specifically: Outbox, broker delivery, deduplication, generic retry plumbing, tenant restoration, realtime transport, secret-encryption mechanics and observability transport remain Platform/Infrastructure mechanisms. P5 owns only the business/provider semantics that consume those mechanisms.

**Expected result:** mechanism reuse is visible without duplicating or relocating P5 semantic authority.

### Verification binding

- Test ID: `AI-TST-AIREQ-003`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: mechanism reuse is visible without duplicating or relocating P5 semantic authority. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-004 — Brownfield-first implementation

### Traceability

- Requirement: `AIREQ004` — Brownfield-first implementation
- PLAN: `AI-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate already contains Automation/Integrations aggregates, ports, consumers, adapters, migrations and tests.

**When**  
the implementation plan for a requirement is compared to the existing source graph and a proposed duplicate abstraction/service/repository is introduced as a negative fixture.

**Then**  
the existing owner is reused or deliberately migrated; duplicate greenfield abstractions are rejected unless an escalation decision exists. specifically: Execution MUST begin from the exact current source graph. Existing aggregates, ports, consumers, adapters, migrations, tests and runtime wiring MUST be reused or deliberately changed; missing folders or idealized diagrams are not permission to introduce duplicate abstractions.

**Expected result:** every change has a current-source anchor and an explicit replace/extend/retire decision.

### Verification binding

- Test ID: `AI-TST-AIREQ-004`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: every change has a current-source anchor and an explicit replace/extend/retire decision. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-005 — Exact-candidate evidence

### Traceability

- Requirement: `AIREQ005` — Exact-candidate evidence
- PLAN: `AI-INV-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
a candidate SHA, migration head and test/CI commands are available.

**When**  
the verifier compares source inspection with actually executed suites and intentionally supplies a historical-green result from a different SHA.

**Then**  
only evidence from the exact candidate can move the scenario to PASS; historical/source-only evidence remains preparation evidence. specifically: Every implementation/certification decision MUST name the candidate SHA and distinguish source inspection from executed proof. Source/test existence alone is never sufficient for D4/D5.

**Expected result:** the execution record contains exact SHA and concrete run locators.

### Verification binding

- Test ID: `AI-TST-AIREQ-005`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: the execution record contains exact SHA and concrete run locators. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-006 — Production reachability classification

### Traceability

- Requirement: `AIREQ006` — Production reachability classification
- PLAN: `AI-INV-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
all P5 capabilities are inventoried across Domain, Application, API, worker/consumer and frontend composition.

**When**  
the verifier attempts to classify a Domain-only or fixture-only capability as released without a production path.

**Then**  
the classification gate reports the correct `PRODUCTION_REACHABLE`/`PARTIAL_GAP`/`DOMAIN_ONLY`/`CONTRACT_DRIFT`/`EXPLICIT_ADMISSION_REQUIRED` state and blocks false release. specifically: Each capability MUST be classified as `PRODUCTION_REACHABLE`, `PARTIAL_GAP`, `DOMAIN_ONLY`, `CONTRACT_DRIFT`, `EXPLICIT_ADMISSION_REQUIRED`, or `NOT_APPLICABLE` before it is planned. Domain types and fixtures MUST NOT be represented as released product behavior without a production path.

**Expected result:** release scope matches actual production reachability, not folder/type existence.

### Verification binding

- Test ID: `AI-TST-AIREQ-006`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: release scope matches actual production reachability, not folder/type existence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-007 — Domain-only capability does not silently enter release scope

### Traceability

- Requirement: `AIREQ007` — Domain-only capability does not silently enter release scope
- PLAN: `AI-INV-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
Scheduled Jobs, Templates, AI Agents, generic WebhookDelivery and sync primitives exist in source with varying runtime reachability.

**When**  
release inventory is generated and a Domain-only capability is added to the advertised API/UI capability set without Application/API/runtime evidence.

**Then**  
the gate fails or the capability remains explicitly deferred/admission-required. specifically: Scheduled Jobs, Templates, AI Agents, generic webhook delivery, sync primitives or other source areas that lack production reachability MUST remain explicit deferred/admission items until product authority, Application/API/runtime paths and tests are defined.

**Expected result:** no Domain-only capability silently becomes a product promise.

### Verification binding

- Test ID: `AI-TST-AIREQ-007`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: no Domain-only capability silently becomes a product promise. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-008 — No architecture expansion by convenience

### Traceability

- Requirement: `AIREQ008` — No architecture expansion by convenience
- PLAN: `AI-GOV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the modular-monolith architecture and approved cross-cutting mechanisms are frozen.

**When**  
a P5 change proposes a new service/project/workflow engine/scripting engine/global SDK/secret architecture/background-principal model only for implementation convenience.

**Then**  
the work stops and requires the documented architecture decision/cross-team approval before such expansion can proceed. specifically: P5 MUST NOT create a new production project, service, workflow engine, scripting engine, repository-wide provider SDK dependency, secret-storage architecture, or background-principal model without the required architecture decision and cross-team approval.  ## Frozen P5 delivery decisions  The following choices are fixed for this execution package. Implementation MUST NOT reopen them as local options:  ```text Rule revision:   capture immutable semantic revision/config snapshot when Execution is created   queued/running Execution continues that snapshot   later Rule edits affect future Executions only  Released Rule management:   create / list / detail / update / enable / disable / archive / soft-delete / restore   no P5 test-preview execution endpoint  Rule execution:   Conditions are implemented in the production evaluator   Actions are an ordered sequential list backed by durable Execution Steps   arbitrary DAG/parallel Action execution is out of P5  Released event triggers:   ItemAssigned / ItemMovedToGroup / ItemCreated   additional trigger discriminators remain unavailable until a production consumer is added  Released target/provider actions:   MoveItem   N8n/Webhook provider effect under ADR-008   other Action discriminators remain unavailable until a real executor is admitted  Deferred from P5 release:   Scheduled Automation   Automation Templates   AI Agents   generic outbound WebhookSubscription/WebhookDelivery product surface   their Domain/persistence code remains non-reachability evidence only  Calendar provider surface:   Google and Microsoft/Outlook only   real provider authorization and callback verification must be implemented/certified   provider entries without a production adapter remain hidden  Calendar manual sync:   implement the command/runtime path; a throw-only handler is not an allowed final state  Realtime:   reuse/map existing Automation execution lifecycle Domain facts into a public/realtime contract   do not invent a second execution lifecycle   Automation and Integration progress recover from durable state after gaps ```  Any change to these decisions is an authority/product/architecture change and requires updating SPEC, PLAN, TESTS and CERT in the same change.

**Expected result:** local feature work cannot smuggle in a new cross-cutting architecture.

### Verification binding

- Test ID: `AI-TST-AIREQ-008`
- Test project: repository governance + architecture verification
- Source under test: Repository architecture/workstream authority and exact candidate tree
- Existing evidence: `docs/product/automation.md`; `docs/product/integrations.md`; `docs/workstreams/teams/automation-integrations.md`; `backend/docs/architecture/platform-and-messaging.md`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationIntegrationsAuthorityArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through Repository architecture/workstream authority and exact candidate tree; assert the complete Then contract and durable result: local feature work cannot smuggle in a new cross-cutting architecture. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: architecture/document consistency gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-009 — Automation Rule stable identity and tenant scope

### Traceability

- Requirement: `AIREQ009` — Automation Rule stable identity and tenant scope
- PLAN: `AI-01-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: Every Automation Rule MUST have stable RuleId, AccountId and WorkspaceId ownership independent of mutable display/configuration fields.

**Expected result:** Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload.

### Verification binding

- Test ID: `AI-TST-AIREQ-009`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-010 — Rule lifecycle is explicit

### Traceability

- Requirement: `AIREQ010` — Rule lifecycle is explicit
- PLAN: `AI-01-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: Rule lifecycle MUST distinguish at least the current supported states `Draft`, `Active`, `Disabled`, and `Archived` where exposed. Delete/soft-delete/restore behavior MUST be explicit and MUST NOT erase required execution history.

**Expected result:** Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload.

### Verification binding

- Test ID: `AI-TST-AIREQ-010`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-RULE-001 — Enable is authoritative server validation

### Traceability

- Requirement: `AIREQ011` — Enable is authoritative server validation
- PLAN: `AI-01-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: A Rule may enter `Active` only after server-side validation of its released trigger, condition and action configuration. Frontend validation is advisory UX, not authority.

**Expected result:** Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload.

### Verification binding

- Test ID: `AI-TST-RULE-001`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-RULE-002 — Disable stops new executions without erasing history

### Traceability

- Requirement: `AIREQ012` — Disable stops new executions without erasing history
- PLAN: `AI-01-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: Disabling a Rule MUST prevent new matching/scheduled executions after the defined effective point while retaining historical executions and audit evidence.

**Expected result:** Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload.

### Verification binding

- Test ID: `AI-TST-RULE-002`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: Rule state, history, Billing capacity and in-flight execution semantics remain deterministic after reload. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-013 — Rule edits affect only future executions

### Traceability

- Requirement: `AIREQ013` — Rule edits affect only future executions
- PLAN: `AI-01-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: Editing an enabled Rule creates new Rule semantics for future matching occurrences only. An Execution already created in `Queued` or `Running` MUST continue with the immutable Rule revision/configuration captured when that Execution was created. Editing MUST NOT cancel, rewrite, or silently retarget an existing Execution.

**Expected result:** An Execution created before the edit completes with its captured revision; only later Executions use the edited Rule.

### Verification binding

- Test ID: `AI-TST-AIREQ-013`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: An Execution created before the edit completes with its captured revision; only later Executions use the edited Rule. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-RULE-003 — Execution persists immutable Rule revision semantics

### Traceability

- Requirement: `AIREQ014` — Execution persists immutable Rule revision semantics
- PLAN: `AI-01-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: At Execution creation, Automation MUST persist an immutable semantic revision reference plus the normalized trigger/condition/action configuration snapshot required to reproduce that run. The snapshot/reference is the authority for queued/running execution. Aggregate optimistic-concurrency `Version` remains a mutation guard and MUST NOT substitute for product Rule revision.

**Expected result:** Reloaded queued/running Executions reproduce the exact trigger/condition/action semantics captured at creation; aggregate Version is never used as semantic revision.

### Verification binding

- Test ID: `AI-TST-RULE-003`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `AutomationExecution.cs`; `AutomationRule.cs`; `N8nDispatchUseCase.cs`; `AutomationMoveItemUseCase.cs`
- Existing evidence: Current Domain/runtime tests do not prove immutable Rule revision; baseline loads current Rule during dispatch.
- Required proof target: planned: `AutomationRuleRevisionExecutionIntegrationTests.cs` — queue under revision R1, edit Rule to R2, dispatch, and prove execution still uses R1 semantics.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-015 — Rule archive/delete/restore and Billing capacity have fixed lifecycle semantics

### Traceability

- Requirement: `AIREQ015` — Rule archive/delete/restore and Billing capacity have fixed lifecycle semantics
- PLAN: `AI-01-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: P5 lifecycle semantics are fixed: `Disable` stops new Executions but continues to consume the Automation Rule capacity slot; `Archive` and soft `Delete` stop new Executions and release exactly one slot after the lifecycle mutation is accepted; `Restore` MUST re-consume capacity atomically before the Rule becomes available again and MUST fail closed when capacity is unavailable. Executions already created continue against their immutable Rule snapshot, subject to current target authorization/invariants. History is retained according to policy.

**Expected result:** Disable retains capacity; archive/delete release once; restore atomically re-consumes or fails without restoring reachability; historical Executions remain queryable.

### Verification binding

- Test ID: `AI-TST-AIREQ-015`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: Disable retains capacity; archive/delete release once; restore atomically re-consumes or fails without restoring reachability; historical Executions remain queryable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-016 — P5 Rule management surface is explicit

### Traceability

- Requirement: `AIREQ016` — P5 Rule management surface is explicit
- PLAN: `AI-01-COMPAT-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace/account Rule is exercised through the canonical Application/API pipeline with persisted historical executions available.

**When**  
the relevant create/enable/disable/edit/delete/archive/restore operation and at least one invalid/concurrent/in-flight edge case are executed.

**Then**  
the durable Rule and Execution state obey the governed lifecycle rather than frontend/process-local assumptions; specifically: The P5 Rule management surface is: create, list, detail, update configuration/name, enable, disable, archive, soft-delete, and restore. A dedicated “test/preview Rule” execution endpoint is NOT part of P5 and MUST NOT be advertised. Domain methods without Application/API paths do not count as released operations; missing target operations above MUST be implemented before Rule-management certification.

**Expected result:** Only the frozen P5 Rule operations are reachable; all are pipeline-authorized and persistence-backed, and no preview/test endpoint is advertised.

### Verification binding

- Test ID: `AI-TST-AIREQ-016`
- Test project: `backend/tests/Notrelix.Domain.Tests`; `Notrelix.Application.Tests`; `Notrelix.Integration.Tests`
- Source under test: `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path
- Existing evidence: `AutomationRuleLifecycleTests.cs`; `AutomationRuleActivationInvariantTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationRuleLifecycleIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`; Rule Application/API surface; Billing capacity path; assert the complete Then contract and durable result: Only the frozen P5 Rule operations are reachable; all are pipeline-authorized and persistence-backed, and no preview/test endpoint is advertised. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend domain/application/integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-017 — Persisted automation configuration is typed and versioned

### Traceability

- Requirement: `AIREQ017` — Persisted automation configuration is typed and versioned
- PLAN: `AI-02-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: Trigger, condition and action configuration MUST be represented behind stable discriminators and schema versions. Arbitrary JSON MAY be an encoding but MUST NOT be the semantic authority.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-AIREQ-017`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`
- Existing evidence: `AutomationDefinitionSchemaTests.cs`; `AutomationConditionDefinitionTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationConfigurationRuntimeIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`; assert the complete Then contract and durable result: unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-018 — Trigger and action configuration are independent contracts

### Traceability

- Requirement: `AIREQ018` — Trigger and action configuration are independent contracts
- PLAN: `AI-02-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: Trigger configuration and Action configuration MUST be independently represented and validated. The current single `Configuration` request field MUST NOT force the same JSON payload to satisfy two unrelated schemas.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-AIREQ-018`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `CreateAutomationRuleRequest.cs`; `CreateAutomationRule.cs`; `CreateAutomationRuleCommandValidator.cs`; FE `automation-core` input types
- Existing evidence: Current create validator proves a single JSON can be parsed for both definitions; this is evidence of the gap, not target proof.
- Required proof target: planned API/Application contract test with distinct triggerConfig/actionConfig payloads plus persistence round-trip; same JSON must no longer be implicitly reused for both schemas.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-019 — Persisted discriminator and referenced-schema evolution is explicit

### Traceability

- Requirement: `AIREQ019` — Persisted discriminator and referenced-schema evolution is explicit
- PLAN: `AI-02-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: Changing trigger/action/condition discriminator names, schema versions, defaults, field/resource identifiers or referenced schema MUST include compatibility/migration behavior for stored Rules and independently deployed consumers. A Rule whose referenced field/resource/schema no longer resolves MUST be marked/surfaced as broken and MUST NOT remain apparently enabled while it cannot execute correctly.

**Expected result:** Persisted discriminator/reference migrations preserve supported Rules; broken field/resource/schema references become explicit non-executable Rule state instead of silently enabled dead configuration.

### Verification binding

- Test ID: `AI-TST-AIREQ-019`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`
- Existing evidence: `AutomationDefinitionSchemaTests.cs`; `AutomationConditionDefinitionTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`
- Required proof target: planned migration/compatibility fixture containing old discriminator/schema rows; old rows must read/migrate/fail-safe without silent reinterpretation.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-COND-001 — Condition evaluation is deterministic

### Traceability

- Requirement: `AIREQ020` — Condition evaluation is deterministic
- PLAN: `AI-02-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: Released Conditions MUST define operands, operators, types, missing/null behavior, fact-loading consistency point and versioning. Evaluation MUST NOT depend on ambient randomness or hidden mutable process state.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-COND-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationConditionDefinition.cs`; `N8nAutomationRuleEvaluator.cs`
- Existing evidence: Condition Domain tests exist; no production evaluator condition proof found.
- Required proof target: planned production evaluator condition test: matching trigger + false condition creates no execution/effect; true condition creates exactly one.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-COND-002 — Condition false is distinct from condition failure

### Traceability

- Requirement: `AIREQ021` — Condition false is distinct from condition failure
- PLAN: `AI-02-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: A valid Condition evaluating `false` MUST be represented as a non-error skip. Invalid configuration, unavailable facts, authorization failure and transient evaluation failure MUST retain distinct behavior.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-COND-002`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationConditionDefinition.cs`; production evaluator/condition admission path
- Existing evidence: Condition Domain tests exist; no production runtime false-vs-failure matrix found.
- Required proof target: planned evaluator matrix separating `false`, invalid condition, unavailable facts/authorization and transient evaluation failure.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-TRG-001 — Trigger identity follows producer contract

### Traceability

- Requirement: `AIREQ022` — Trigger identity follows producer contract
- PLAN: `AI-02-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: Event triggers MUST bind to stable logical producer event identity/version and required scope, not CLR type, database table name, private namespace or frontend-only names.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-TRG-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`
- Existing evidence: `AutomationDefinitionSchemaTests.cs`; `AutomationConditionDefinitionTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationConfigurationRuntimeIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`; assert the complete Then contract and durable result: unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-TRG-CON-001 — Producer evolution includes Automation as a consumer

### Traceability

- Requirement: `AIREQ023` — Producer evolution includes Automation as a consumer
- PLAN: `AI-02-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: When a producer event used by Automation changes, Automation MUST be inventoried as a consumer and backlog/replay, additive/breaking compatibility and rollout order MUST be evaluated.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-TRG-CON-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`
- Existing evidence: `AutomationDefinitionSchemaTests.cs`; `AutomationConditionDefinitionTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationConfigurationRuntimeIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/RulesEngine/*`; `CreateAutomationRule*`; `N8nAutomationRuleEvaluator.cs`; `AutomationExecution.cs`; assert the complete Then contract and durable result: unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-024 — Released trigger vocabulary matches runtime reachability

### Traceability

- Requirement: `AIREQ024` — Released trigger vocabulary matches runtime reachability
- PLAN: `AI-02-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: A trigger type MUST NOT be advertised as executable until a production consumer/evaluator exists and is tested. The current definition/runtime gap (`ValidTriggers` broader than the reachable WorkManagement trigger matrix) MUST remain visible until closed.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-AIREQ-024`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationTriggerDefinition.cs`; Automation consumer definitions; `N8nAutomationRuleEvaluator.cs`
- Existing evidence: `AutomationTriggerMatrixIntegrationTests.cs` covers reachable Work trigger matrix and unsupported cases.
- Required proof target: extend `AutomationTriggerMatrixIntegrationTests.cs` so every advertised trigger has a real consumer/evaluator and every non-reachable discriminator is rejected/not advertised.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-025 — Released action vocabulary matches an executor

### Traceability

- Requirement: `AIREQ025` — Released action vocabulary matches an executor
- PLAN: `AI-02-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: An Action type MUST have a target semantic owner, config schema, authorization model, idempotency boundary, failure taxonomy and production executor before it is advertised as executable. Unsupported actions MUST continue to fail closed rather than be routed to the wrong executor.

**Expected result:** unsupported or incompatible configuration fails closed and released vocabulary has deterministic runtime semantics.

### Verification binding

- Test ID: `AI-TST-AIREQ-025`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationActionDefinition.cs`; `N8nAutomationRuleEvaluator.cs`; MoveItem/N8n executors
- Existing evidence: `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; N8n runtime tests.
- Required proof target: extend action matrix: `MoveItem` and `Webhook` real executor proof; unsupported actions remain visible terminal failure until an executor is admitted.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-026 — P5 multi-action execution is ordered and step-backed

### Traceability

- Requirement: `AIREQ026` — P5 multi-action execution is ordered and step-backed
- PLAN: `AI-02-COMPAT-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
a persisted Rule uses declared trigger/action/condition discriminator and schema-version data and a real producer/runtime executor inventory is available.

**When**  
the Rule is validated, persisted/reloaded and evaluated against matching/non-matching/invalid facts through the production evaluator where the capability is released.

**Then**  
configuration and runtime reachability remain aligned; specifically: P5 MUST support an ordered Action list. Actions execute sequentially in Rule order; each Action has a durable `AutomationExecutionStep` with stable step identity and status. A later Action MAY consume only explicitly declared output from an earlier successful Action. Terminal failure stops dependent later Actions; retry resumes the current retryable Step under the same logical operation identity and MUST NOT re-run already committed Steps. Arbitrary DAG/parallel execution is outside P5.

**Expected result:** Actions execute sequentially with durable Step identities; retry resumes the failed retryable Step without re-running committed Steps; arbitrary DAG/parallel behavior is absent.

### Verification binding

- Test ID: `AI-TST-AIREQ-026`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationConfiguration.cs`; `AutomationExecutionStep`; execution persistence
- Existing evidence: `AutomationExecutionStep` Domain tests/entity shape only; production attachment/orchestration not found.
- Required proof target: if multi-action is admitted, add persisted ordered steps + sequential/parallel failure semantics; otherwise assert public contract remains single-action and `AutomationExecutionStep` is not advertised.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend domain + production-trigger integration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-EXE-001 — Execution identity is logical and stable

### Traceability

- Requirement: `AIREQ027` — Execution identity is logical and stable
- PLAN: `AI-03-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: One logical Rule occurrence MUST resolve to one logical Automation Execution identity independent of worker attempt. Event-driven identity MUST include or derive from stable Rule identity plus source occurrence identity.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-EXE-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-EXE-CONC-001 — Concurrent duplicate creation is constrained durably

### Traceability

- Requirement: `AIREQ028` — Concurrent duplicate creation is constrained durably
- PLAN: `AI-03-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Duplicate/concurrent source delivery MUST NOT create two logical executions for one Rule occurrence. Database uniqueness and message deduplication MUST remain compatible with the execution identity contract.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-EXE-CONC-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-029 — Execution records semantic provenance

### Traceability

- Requirement: `AIREQ029` — Execution records semantic provenance
- PLAN: `AI-03-INV-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: An Execution MUST retain Rule revision/configuration identity, trigger/source identity, Account/Workspace, correlation/causation and enough safe input provenance to explain the run without storing provider secrets.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-AIREQ-029`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; dispatch events; execution DTO/query; persistence config
- Existing evidence: Execution persistence + runtime chain exist, but immutable Rule revision provenance is absent.
- Required proof target: planned execution-provenance persistence test proving Rule revision, source event, correlation/causation and tenant scope survive reload without secrets.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-030 — Execution lifecycle transitions are validated

### Traceability

- Requirement: `AIREQ030` — Execution lifecycle transitions are validated
- PLAN: `AI-03-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Execution status transitions among `Queued`, `Running`, `Succeeded`, `Failed`, and `Cancelled` MUST be explicit, durable and rejected when invalid.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-AIREQ-030`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-031 — Action-step state is real product state only when attached to execution

### Traceability

- Requirement: `AIREQ031` — Action-step state is real product state only when attached to execution
- PLAN: `AI-03-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: If Action Steps are released, they MUST be created, ordered, persisted and updated by the production execution path. Standalone entity/test existence does not prove step orchestration.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-AIREQ-031`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: if steps are released, production execution must create/update persisted steps; otherwise contract/DTO must not imply production step orchestration.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-032 — Success follows durable effect evidence

### Traceability

- Requirement: `AIREQ032` — Success follows durable effect evidence
- PLAN: `AI-03-CORE-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Execution/step success MUST NOT be committed before the target-context or provider effect is durably known successful according to the owning contract.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-AIREQ-032`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-033 — Target/business failure and technical failure remain distinct

### Traceability

- Requirement: `AIREQ033` — Target/business failure and technical failure remain distinct
- PLAN: `AI-03-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Authorization, not-found, invariant/conflict, invalid configuration, retryable technical failure and unknown external outcome MUST not collapse into one generic retryable exception.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-AIREQ-033`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-034 — Transport retry and Automation business retry have one coherent budget

### Traceability

- Requirement: `AIREQ034` — Transport retry and Automation business retry have one coherent budget
- PLAN: `AI-03-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Platform delivery retry and Automation retry policy MUST have explicit ownership and MUST NOT multiply into an uncontrolled retry storm.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-AIREQ-034`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: extend N8n/MoveItem runtime tests to count transport attempts and durable Automation attempt count under bounded retry; prove no multiplicative retry layers.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-EXE-IDEMP-001 — Retry preserves operation identity

### Traceability

- Requirement: `AIREQ035` — Retry preserves operation identity
- PLAN: `AI-03-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Every retryable target/provider action MUST reuse a stable business operation identity or prove a reconciliation mechanism that makes repetition safe.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-EXE-IDEMP-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-EXE-UNK-001 — Unknown external outcome is never blindly re-fired

### Traceability

- Requirement: `AIREQ036` — Unknown external outcome is never blindly re-fired
- PLAN: `AI-03-SEC-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Timeout/reset/indeterminate provider outcomes MUST enter reconciliation/terminal semantics according to the provider contract; redelivery alone MUST NOT grant permission to repeat the external side effect.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-EXE-UNK-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-EXE-REC-001 — Causation and recursion are bounded

### Traceability

- Requirement: `AIREQ037` — Causation and recursion are bounded
- PLAN: `AI-03-COMPAT-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Automation-produced events MUST carry enough causation/origin/depth identity to enforce bounded self-trigger/rule-to-rule recursion and detect runaway chains.

**Expected result:** one final logical business outcome is durable, with correct provenance, status, attempt evidence and safe history.

### Verification binding

- Test ID: `AI-TST-EXE-REC-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: Automation source events, evaluator, causation/correlation propagation and fanout controls
- Existing evidence: Correlation/causation exists on event paths; no end-to-end recursion/depth guard proof found.
- Required proof target: planned chain-depth/causation test with a self-triggering rule graph and enforced maximum/loop rejection before unbounded work is created.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-EXE-HIST-001 — Execution history is safe product evidence and remains distinct from Audit

### Traceability

- Requirement: `AIREQ038` — Execution history is safe product evidence and remains distinct from Audit
- PLAN: `AI-03-COMPAT-001`, `AI-GATE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a logical source occurrence and Rule produce a durable Automation Execution and the target/provider path can be forced through success, duplicate, retryable, terminal and indeterminate branches as applicable.

**When**  
delivery is duplicated/concurrent/retried or the Rule/target/provider state changes across the execution lifecycle.

**Then**  
the same logical execution remains explainable and no unsafe duplicate effect is created; specifically: Execution history MUST expose stable Automation meaning: Rule revision, trigger/source identity, ordered Action-step outcomes, attempts/failures and correlation data without secrets, provider credentials, unsafe payloads or internal stack traces. Execution history explains Automation behavior; governed security/business Audit remains a separate authority and MUST NOT be reconstructed or replaced by Automation history.

**Expected result:** Automation history explains execution semantics without secrets and cannot substitute for governed Audit evidence.

### Verification binding

- Test ID: `AI-TST-EXE-HIST-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation/Executions`; `Notrelix.Integration.Tests/Automation`
- Source under test: `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths
- Existing evidence: `AutomationExecutionTests.cs`; `AutomationExecutionRetryTests.cs`; `AutomationTriggerMatrixIntegrationTests.cs`; `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationExecutionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `AutomationExecution.cs`; `AutomationExecutionConfiguration.cs`; evaluator + MoveItem/N8n execution paths; assert the complete Then contract and durable result: Automation history explains execution semantics without secrets and cannot substitute for governed Audit evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: backend execution + runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-039 — Schedule intent is Automation product state

### Traceability

- Requirement: `AIREQ039` — Schedule intent is Automation product state
- PLAN: `AI-04-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: Schedule definition, timezone, lifecycle and missed-fire policy belong to Automation semantics; clock/timer/scheduler mechanics belong to Platform/Infrastructure.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-039`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationCapabilityAdmissionArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations; assert the complete Then contract and durable result: Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-040 — Timezone, DST and missed-fire policy are explicit

### Traceability

- Requirement: `AIREQ040` — Timezone, DST and missed-fire policy are explicit
- PLAN: `AI-04-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: Recurring schedules MUST define timezone/DST behavior and whether missed occurrences are skipped, fired once, caught up, or bounded.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-040`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: admission test/decision must define timezone, DST overlap/gap and missed-fire behavior before any scheduler runtime is enabled.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-041 — Scheduled occurrence identity prevents duplicate execution

### Traceability

- Requirement: `AIREQ041` — Scheduled occurrence identity prevents duplicate execution
- PLAN: `AI-04-CORE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_IF_ADMITTED`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: A logical scheduled occurrence MUST have durable identity suitable for scale-out, restart and duplicate scheduler delivery.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-041`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: planned scale-out scheduler test using a stable scheduled-occurrence identity; concurrent schedulers must yield one logical execution.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-042 — Scheduler production reachability is required before release

### Traceability

- Requirement: `AIREQ042` — Scheduler production reachability is required before release
- PLAN: `AI-04-CORE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: `ScheduledJob` persistence/domain state alone does not certify scheduling. A released schedule capability requires a production scheduler/claim/delivery path and crash/restart proof.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-042`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationCapabilityAdmissionArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations; assert the complete Then contract and durable result: Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-043 — Template is creation input, not live shared authority

### Traceability

- Requirement: `AIREQ043` — Template is creation input, not live shared authority
- PLAN: `AI-04-SEC-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: Automation Template application MUST copy/instantiate validated Rule configuration according to explicit semantics; later template edits MUST NOT silently mutate existing Rules.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-043`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationCapabilityAdmissionArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations; assert the complete Then contract and durable result: Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-044 — Template schemas participate in compatibility

### Traceability

- Requirement: `AIREQ044` — Template schemas participate in compatibility
- PLAN: `AI-04-SEC-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: Published Template trigger/action/config schema versions MUST follow the same discriminator/migration rules as Rules.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-044`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationCapabilityAdmissionArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations; assert the complete Then contract and durable result: Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-045 — AI Agent requires explicit product admission

### Traceability

- Requirement: `AIREQ045` — AI Agent requires explicit product admission
- PLAN: `AI-04-COMPAT-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: `AiAgent`/`AiAgentRun` source types MUST remain `EXPLICIT_ADMISSION_REQUIRED` until product semantics, model/provider policy, tool permissions, authorization, data handling, cost/entitlement, audit and execution boundaries are approved.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-045`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationCapabilityAdmissionArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations; assert the complete Then contract and durable result: Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-046 — No hidden AI runtime capability

### Traceability

- Requirement: `AIREQ046` — No hidden AI runtime capability
- PLAN: `AI-04-COMPAT-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `ADMISSION_DECISION_PROOF`

### Executable contract

**Given**  
Schedule/Template/AI-Agent Domain models and persistence exist but the capability is not automatically considered production-reachable.

**When**  
the release/admission gate inspects product semantics, Application/API/runtime composition, security model and failure/recovery contract.

**Then**  
the capability stays deferred unless all admission evidence exists; specifically: Tests/domain objects for AI Agents MUST NOT be interpreted as a production AI automation feature in API/frontend/certification until a complete runtime path exists.

**Expected result:** Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release.

### Verification binding

- Test ID: `AI-TST-AIREQ-046`
- Test project: `backend/tests/Notrelix.Domain.Tests/Automation` plus admission/production-composition proof
- Source under test: `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations
- Existing evidence: `ScheduleDefinitionTests.cs`; `ScheduledJobTests.cs`; `AutomationTemplateTests.cs`; `AiAgent*Tests.cs`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests/AutomationCapabilityAdmissionArchitectureTests.cs` (add/extend), execute the scenario's exact Given/When through `backend/src/Notrelix.Domain/Automation/Scheduled`; `Templates`; `Agents`; corresponding EF configurations; assert the complete Then contract and durable result: Domain/unit-test existence cannot promote the capability; admitted paths receive dedicated runtime tests before release. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: capability-admission gate; production runtime proof if admitted
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-047 — Source transactions do not execute Automation side effects inline

### Traceability

- Requirement: `AIREQ047` — Source transactions do not execute Automation side effects inline
- PLAN: `AI-05-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: WorkManagement/Documents/Collaboration producer transactions MUST publish committed facts; Automation reactions occur after durable fact publication unless an explicit synchronous invariant is approved.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-047`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts
- Existing evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationWorkActionChainIntegrationTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationCrossContextContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts; assert the complete Then contract and durable result: no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-048 — Target bounded context revalidates every action

### Traceability

- Requirement: `AIREQ048` — Target bounded context revalidates every action
- PLAN: `AI-05-INV-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: Automation Conditions or prior authorization do not lock foreign state. The target capability MUST validate current authorization, scope, lifecycle, concurrency and invariants when the Action executes.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-048`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts
- Existing evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationWorkActionChainIntegrationTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationCrossContextContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts; assert the complete Then contract and durable result: no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AUTHZ-001 — Background Automation principal is explicit

### Traceability

- Requirement: `AIREQ049` — Background Automation principal is explicit
- PLAN: `AI-05-CORE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: Each Action MUST execute under an explicit trusted actor/delegation/service-principal model. `background` or `system` MUST NOT mean unrestricted authority.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AUTHZ-001`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts
- Existing evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationWorkActionChainIntegrationTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationCrossContextContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts; assert the complete Then contract and durable result: no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AUTHZ-002 — Permission revocation is honored at execution time

### Traceability

- Requirement: `AIREQ050` — Permission revocation is honored at execution time
- PLAN: `AI-05-CORE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: A Rule authorized at creation MUST NOT retain stale permission forever. Revoked membership/permission/resource access MUST cause future Actions to fail safely according to current policy.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AUTHZ-002`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `AutomationMoveItemUseCase.cs`; `WorkActionAcl.cs`; target WorkManagement authorization path
- Existing evidence: MoveItem production composition proves target public action; explicit revocation-between-trigger-and-action proof still required.
- Required proof target: planned execution-time permission revocation test: authorize Rule creation, revoke target permission before dispatch, and prove target mutation is denied/terminal without bypass.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-ACT-001 — Cross-context actions use public capability contracts

### Traceability

- Requirement: `AIREQ051` — Cross-context actions use public capability contracts
- PLAN: `AI-05-CORE-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: Automation→WorkManagement/Documents/Collaboration/etc. MUST use consumer-owned ports/ACLs and target public Application contracts, never private DbContext/table access.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-ACT-001`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts
- Existing evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationWorkActionChainIntegrationTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationCrossContextContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts; assert the complete Then contract and durable result: no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-052 — Billing supplies capability/capacity facts, not plan branching

### Traceability

- Requirement: `AIREQ052` — Billing supplies capability/capacity facts, not plan branching
- PLAN: `AI-05-SEC-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: Automation/Integrations MUST consume Billing-owned capability/capacity contracts and MUST NOT branch on plan names or duplicate entitlement calculation.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-052`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts
- Existing evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationWorkActionChainIntegrationTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationCrossContextContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts; assert the complete Then contract and durable result: no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-053 — Capacity consumption and release semantics are operation-safe

### Traceability

- Requirement: `AIREQ053` — Capacity consumption and release semantics are operation-safe
- PLAN: `AI-05-SEC-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: Capacity consume/release MUST use stable logical operation identity and transaction semantics; retries, delete/archive/restore and failed create flows MUST not leak or double-release capacity.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-053`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `CreateAutomationRule.cs`; Billing capacity public actions; Rule deletion/archive/restore surface
- Existing evidence: Create consumes capacity transactionally; release lifecycle is accepted debt `M9-CAPACITY-RELEASE-LIFECYCLE`.
- Required proof target: planned Billing capacity release/re-consumption tests for archive/delete/restore under duplicate/concurrent lifecycle commands.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-054 — Cross-context handoffs preserve source identity and ownership

### Traceability

- Requirement: `AIREQ054` — Cross-context handoffs preserve source identity and ownership
- PLAN: `AI-05-COMPAT-001`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a committed source fact or Automation execution invokes a foreign target through the declared public port under a concrete actor/tenant and Billing/Governance state.

**When**  
authorization/capacity/target state is changed before execution, or the target call succeeds/fails/retries through the real adapter.

**Then**  
ownership and target revalidation remain at the correct boundary; specifically: AIX source-event, target-action, provider-operation, entitlement and analytics handoffs MUST name semantic owner, producer/consumer, identity/scope, compatibility, authorization and evidence without moving business ownership.

**Expected result:** no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-054`
- Test project: `backend/tests/Notrelix.Integration.Tests/Automation`; Billing/Governance integration tests; architecture tests
- Source under test: `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts
- Existing evidence: `AutomationMoveItemExecutorCompositionIntegrationTests.cs`; `AutomationWorkActionChainIntegrationTests.cs`; `CreateAutomationRulePipelineTests.cs`; `BillingCapacityFlowIntegrationTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationCrossContextContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `N8nAutomationRuleEvaluator.cs`; `AutomationMoveItemUseCase.cs`; `IWorkActionPort.cs`; `WorkItemActionAdapter.cs`; Billing/Governance contracts; assert the complete Then contract and durable result: no private target persistence write, privilege bypass, duplicate capacity mutation or identity loss occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: cross-context production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-CONN-001 — Integration Connection has explicit lifecycle

### Traceability

- Requirement: `AIREQ055` — Integration Connection has explicit lifecycle
- PLAN: `AI-06-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: Connection identity and lifecycle MUST distinguish active, expired, revoked and error states plus reconnect/delete/restore semantics without conflating provider health or sync progress.

**Expected result:** no raw reusable secret leaks and restart/revocation semantics are explicit and testable.

### Verification binding

- Test ID: `AI-TST-CONN-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`
- Existing evidence: `CalendarConnectionFlowIntegrationTests.cs`; `IntegrationConnection*Tests.cs`; `SecretRotationBoundaryTests.cs`; `IntegrationConnectionRevokedConsumerTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/IntegrationConnectionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`; assert the complete Then contract and durable result: no raw reusable secret leaks and restart/revocation semantics are explicit and testable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-056 — Provider catalog and provider-specific connection configuration are canonical and typed

### Traceability

- Requirement: `AIREQ056` — Provider catalog and provider-specific connection configuration are canonical and typed
- PLAN: `AI-06-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: Supported provider identities/capabilities and provider-specific connection configuration MUST come from one canonical/versioned contract or generated source. Provider configuration MAY use flexible persistence, but it MUST remain typed/versioned and MUST NOT become a schema-less generic JSON dumping ground. For the P5 Calendar release, only Google and Microsoft/Outlook provider capabilities proven by backend contracts may be advertised; unrelated frontend/provider enum entries remain unavailable.

**Expected result:** Only canonical released providers/config schemas are exposed; provider-specific configuration remains typed/versioned rather than arbitrary JSON.

### Verification binding

- Test ID: `AI-TST-AIREQ-056`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationProvider.cs`; `CalendarProvider.cs`; frontend `integrations.ts` provider union
- Existing evidence: Backend and frontend provider vocabularies demonstrably differ at baseline.
- Required proof target: planned contract test generated from one canonical provider catalog; backend enum, Calendar support and frontend options must not diverge silently.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-CONN-AUTHZ-001 — Connection installation is Account/Workspace scoped and governed

### Traceability

- Requirement: `AIREQ057` — Connection installation is Account/Workspace scoped and governed
- PLAN: `AI-06-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: Connect/reconnect/disconnect/configure operations MUST resolve Account/Workspace from trusted context/resource location and enforce Governance permission through the canonical pipeline.

**Expected result:** no raw reusable secret leaks and restart/revocation semantics are explicit and testable.

### Verification binding

- Test ID: `AI-TST-CONN-AUTHZ-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`
- Existing evidence: `CalendarConnectionFlowIntegrationTests.cs`; `IntegrationConnection*Tests.cs`; `SecretRotationBoundaryTests.cs`; `IntegrationConnectionRevokedConsumerTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/IntegrationConnectionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`; assert the complete Then contract and durable result: no raw reusable secret leaks and restart/revocation semantics are explicit and testable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-058 — Integration provider identity is separate from Identity login and Workspace membership

### Traceability

- Requirement: `AIREQ058` — Integration provider identity is separate from Identity login and Workspace membership
- PLAN: `AI-06-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_IF_RELEASED`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: External provider installation/authentication has Integration Connection semantics and MUST NOT reuse user-login linkage as provider connection ownership. Provider users, attendee emails or external identities MUST NOT create or grant Notrelix User/Account/Workspace membership automatically; Identity, Workspaces and Governance remain authoritative.

**Expected result:** Provider identities/attendees never create Notrelix membership or bypass Identity/Workspace/Governance authority.

### Verification binding

- Test ID: `AI-TST-AIREQ-058`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: Calendar connect/provider authorization surface versus Identity OAuth implementation
- Existing evidence: Identity OAuth tests exist, but they are not evidence for Integration OAuth.
- Required proof target: provider OAuth capability must have a separate integration-OAuth flow test; raw access-token connect is not sufficient evidence.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-059 — Provider authorization is state/PKCE secured and least-privilege

### Traceability

- Requirement: `AIREQ059` — Provider authorization is state/PKCE secured and least-privilege
- PLAN: `AI-06-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_IF_RELEASED`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: Released OAuth/install providers MUST define state, PKCE where applicable, redirect/callback lifetime, initiating actor/scope, token exchange, reconnect and revoke behavior. They MUST request only scopes required by the released feature set; scope expansion requires explicit re-consent/reauthorization. Supplying a raw access token directly is not equivalent to a complete provider authorization flow.

**Expected result:** Provider install uses state/PKCE where required, least-privilege scopes and explicit re-consent when scope requirements expand.

### Verification binding

- Test ID: `AI-TST-AIREQ-059`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: provider-specific integration authorization callback/state/PKCE implementation when admitted
- Existing evidence: No complete Integration OAuth state/PKCE/callback flow found at baseline.
- Required proof target: for each released OAuth provider, test state correlation, PKCE when required, expiry, callback replay and workspace/account binding.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-SEC-001 — Reusable secrets live behind opaque references

### Traceability

- Requirement: `AIREQ060` — Reusable secrets live behind opaque references
- PLAN: `AI-06-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: Raw access/refresh tokens and reusable provider secrets MUST NOT be Domain properties, normal read DTOs, logs, events, analytics or frontend persisted state. Aggregates store opaque references/version metadata.

**Expected result:** no raw reusable secret leaks and restart/revocation semantics are explicit and testable.

### Verification binding

- Test ID: `AI-TST-SEC-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`
- Existing evidence: `CalendarConnectionFlowIntegrationTests.cs`; `IntegrationConnection*Tests.cs`; `SecretRotationBoundaryTests.cs`; `IntegrationConnectionRevokedConsumerTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/IntegrationConnectionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`; assert the complete Then contract and durable result: no raw reusable secret leaks and restart/revocation semantics are explicit and testable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-061 — Physical secret storage is restart-safe

### Traceability

- Requirement: `AIREQ061` — Physical secret storage is restart-safe
- PLAN: `AI-06-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: The current DataProtection-backed secret store is acceptable only when the production key ring is durable and backup/rotation/recovery behavior is proven. Restart MUST NOT make stored provider credentials undecryptable.

**Expected result:** no raw reusable secret leaks and restart/revocation semantics are explicit and testable.

### Verification binding

- Test ID: `AI-TST-AIREQ-061`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `DataProtectionIntegrationSecretStore.cs`; DataProtection key-ring production configuration
- Existing evidence: Secret store and Calendar connection tests exist; persistent key-ring restart proof must be explicit.
- Required proof target: extend secret-store integration with process restart and durable DataProtection key-ring; stored secret must decrypt after restart and fail readiness if key ring is not durable.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-SEC-002 — Secret versions and current pointers are durable authority

### Traceability

- Requirement: `AIREQ062` — Secret versions and current pointers are durable authority
- PLAN: `AI-06-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: `IntegrationSecretVersion` is the durable version/reference history; `CurrentSecretVersion` is a pointer. Rotation MUST atomically establish a new reference/version and avoid orphan plaintext/encrypted blobs.

**Expected result:** no raw reusable secret leaks and restart/revocation semantics are explicit and testable.

### Verification binding

- Test ID: `AI-TST-SEC-002`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`
- Existing evidence: `CalendarConnectionFlowIntegrationTests.cs`; `IntegrationConnection*Tests.cs`; `SecretRotationBoundaryTests.cs`; `IntegrationConnectionRevokedConsumerTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/IntegrationConnectionContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`; assert the complete Then contract and durable result: no raw reusable secret leaks and restart/revocation semantics are explicit and testable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-063 — Disconnect, secret retirement, provider revocation and provider-held copies are distinct

### Traceability

- Requirement: `AIREQ063` — Disconnect, secret retirement, provider revocation and provider-held copies are distinct
- PLAN: `AI-06-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: Calendar binding deactivation, generic Connection revocation, local secret cleanup, remote token/subscription revocation, and cleanup of provider-held copies MUST have explicit ordering and independently recorded outcomes. Provider-held data cleanup MUST be classified per capability as required, best-effort, user-managed, or impossible after disconnect. Local DB success MUST NOT falsely imply remote provider cleanup or deletion succeeded.

**Expected result:** Local disconnect/revocation, secret cleanup, remote subscription/token revocation and provider-held-copy cleanup have separate durable outcomes.

### Verification binding

- Test ID: `AI-TST-AIREQ-063`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`
- Existing evidence: `CalendarConnectionFlowIntegrationTests.cs`; `IntegrationConnection*Tests.cs`; `SecretRotationBoundaryTests.cs`; `IntegrationConnectionRevokedConsumerTests.cs`
- Required proof target: extend disconnect cleanup tests across local deactivate, last-binding Connection revoke, secret retirement and provider-revoke success/retryable/terminal/unknown outcomes.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-064 — Connection state and sync/provider health remain distinct

### Traceability

- Requirement: `AIREQ064` — Connection state and sync/provider health remain distinct
- PLAN: `AI-06-COMPAT-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a workspace installs/reconnects/disconnects a provider connection using the canonical request pipeline and opaque secret reference/version model.

**When**  
the flow is exercised across valid/invalid authorization, reconnect/rotation, restart, expiry/revocation and last-binding cleanup cases as applicable.

**Then**  
Connection/product state, secret authority and Governance scope remain distinct and durable; specifically: A transient provider request, sync failure or rate limit MUST NOT automatically rewrite durable Connection lifecycle without an explicit policy.

**Expected result:** no raw reusable secret leaks and restart/revocation semantics are explicit and testable.

### Verification binding

- Test ID: `AI-TST-AIREQ-064`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`; `Notrelix.Infrastructure.Tests`
- Source under test: `IntegrationConnection.cs`; `ConnectCalendar.cs`; `DisconnectCalendar.cs`; `IIntegrationSecretStore.cs`; `DataProtectionIntegrationSecretStore.cs`
- Existing evidence: `CalendarConnectionFlowIntegrationTests.cs`; `IntegrationConnection*Tests.cs`; `SecretRotationBoundaryTests.cs`; `IntegrationConnectionRevokedConsumerTests.cs`
- Required proof target: add state matrix proving Connection Active/Revoked/Error is independent from sync health/provider transient outage.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: connection/security production-composition gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-065 — Outbound provider operation uses an Integration-owned contract

### Traceability

- Requirement: `AIREQ065` — Outbound provider operation uses an Integration-owned contract
- PLAN: `AI-07-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: Automation requests provider effects through Integrations-owned ports/contracts; provider SDK/client details MUST remain Infrastructure/adapter concerns.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-AIREQ-065`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-PROV-001 — Provider anti-corruption layer translates types and errors

### Traceability

- Requirement: `AIREQ066` — Provider anti-corruption layer translates types and errors
- PLAN: `AI-07-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: Provider DTOs, enums, errors, rate-limit semantics and capabilities MUST be translated into stable Integration-owned outcomes rather than propagated through Automation or product domains.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-PROV-001`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-OUT-001 — Outbound operation has stable logical idempotency identity

### Traceability

- Requirement: `AIREQ067` — Outbound operation has stable logical idempotency identity
- PLAN: `AI-07-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: Every retriable provider side effect MUST define logical operation identity, provider idempotency key support where available, duplicate behavior and retention.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-OUT-001`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-068 — Timeout/rate-limit behavior follows provider evidence

### Traceability

- Requirement: `AIREQ068` — Timeout/rate-limit behavior follows provider evidence
- PLAN: `AI-07-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: Retry classification MUST distinguish reject-before-execution evidence from indeterminate outcomes. Retry-After/backpressure MAY inform scheduling but MUST NOT override unknown-outcome safety.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-AIREQ-068`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-OUT-IDEMP-001 — One end-to-end retry budget owns provider re-attempts

### Traceability

- Requirement: `AIREQ069` — One end-to-end retry budget owns provider re-attempts
- PLAN: `AI-07-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: HTTP-client retry, MassTransit retry and Automation retry MUST not independently retry the same provider effect. Adapter-internal retries are forbidden unless the provider operation contract explicitly proves them safe.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-OUT-IDEMP-001`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-OUT-UNK-001 — Unknown provider outcome has an executable reconciliation path

### Traceability

- Requirement: `AIREQ070` — Unknown provider outcome has an executable reconciliation path
- PLAN: `AI-07-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: A failure marked `reconciliation required` MUST have an operationally executable, auditable resolution path and ownership. A log message or runbook-only dead end is insufficient for D5.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-OUT-UNK-001`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: ADR-008; `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; recovery runbook
- Existing evidence: N8n durability/runtime tests prove unknown outcome is terminal/no auto re-fire; operational reconciliation remains partial.
- Required proof target: existing unknown-outcome tests prove no auto re-fire; add executable reconciliation path evidence that can move/resolve durable state without ad-hoc DB mutation.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-071 — ADR-008 is authoritative for current N8n settlement

### Traceability

- Requirement: `AIREQ071` — ADR-008 is authoritative for current N8n settlement
- PLAN: `AI-07-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: The N8n prepare/effect/settle protocol, outcome classification and consumer-owned claim lifecycle MUST conform to accepted ADR-008 until superseded by another accepted decision.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-AIREQ-071`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-072 — Provider-effect claim freshness does not grant re-fire authority

### Traceability

- Requirement: `AIREQ072` — Provider-effect claim freshness does not grant re-fire authority
- PLAN: `AI-07-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: Fresh `Processing` means an active attempt may own the effect; stale `Processing` permits durable-state reconciliation only. Neither condition by itself permits repeating the provider call.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-AIREQ-072`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-073 — Stale queued provider claim is governed recovery debt

### Traceability

- Requirement: `AIREQ073` — Stale queued provider claim is governed recovery debt
- PLAN: `AI-07-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: A stale `Processing` claim with a `Queued` execution MUST be resolved by a controlled operator/reconciliation mechanism that records decision/evidence; it MUST NOT remain an undocumented permanent blocker or be auto-deleted.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-AIREQ-073`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchConsumer.HandleClaimResidueAsync`; `IProviderEffectClaimStore`; recovery runbook
- Existing evidence: Durability test pins stale Queued+Processing as operator sweep; governed executable repair remains to be proven.
- Required proof target: planned stale `Queued + Processing` operator/recovery test invoking the governed repair mechanism; verify lineage, no provider re-fire and deterministic final claim/execution state.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-074 — External provider effect runs outside the database transaction

### Traceability

- Requirement: `AIREQ074` — External provider effect runs outside the database transaction
- PLAN: `AI-07-COMPAT-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
an Automation execution reaches the real N8n provider-effect consumer with a stable message/execution identity and durable claim state.

**When**  
the provider returns success, terminal failure, connection-phase retryable failure, timeout/reset/unknown outcome, duplicate delivery or crash residue as applicable.

**Then**  
the ADR-008 prepare/effect/settle and reconciliation contract is preserved; specifically: Provider calls MUST execute outside held database transactions while durable prepare and settle phases preserve tenant/RLS scope, effect identity and crash-recoverable evidence.

**Expected result:** provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner.

### Verification binding

- Test ID: `AI-TST-AIREQ-074`
- Test project: `backend/tests/Notrelix.Application.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Automation`
- Source under test: `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008
- Existing evidence: `N8nDispatchUseCaseTests.cs`; `N8nClientTests.cs`; `AutomationN8nDurabilityIntegrationTests.cs`; `N8nDispatchRuntimeChainIntegrationTests.cs`
- Required proof target: In `AutomationN8nDurabilityIntegrationTests.cs` + `N8nDispatchRuntimeChainIntegrationTests.cs` (extend as needed), execute the scenario's exact Given/When through `N8nDispatchUseCase.cs`; `N8nDispatchConsumer.cs`; `N8nClient.cs`; `IProviderEffectClaimStore.cs`; `MessageDeduplicationStore.cs`; ADR-008; assert the complete Then contract and durable result: provider call count, execution state, attempt count and claim state prove no blind re-fire and one coherent retry owner. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: N8n provider-effect runtime-chain gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-075 — Webhook raw HTTP boundary is bounded before trust

### Traceability

- Requirement: `AIREQ075` — Webhook raw HTTP boundary is bounded before trust
- PLAN: `AI-08-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Inbound provider callbacks MUST enforce method/route, allowed content type, body-size/read-time bounds, UTF-8/format requirements and abuse/rate controls before business processing.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-AIREQ-075`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-WH-SEC-001 — Signature verification uses the exact provider-defined bytes

### Traceability

- Requirement: `AIREQ076` — Signature verification uses the exact provider-defined bytes
- PLAN: `AI-08-INV-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Signature/timestamp/replay verification MUST operate on the exact raw request bytes and provider protocol; reserialized JSON MUST NOT be substituted for signed content.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-WH-SEC-001`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-WH-ROUTE-001 — Tenant routing derives from trusted binding, never payload

### Traceability

- Requirement: `AIREQ077` — Tenant routing derives from trusted binding, never payload
- PLAN: `AI-08-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_REUSE`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Webhook AccountId/WorkspaceId/ConnectionId MUST derive from a trusted route/binding/connection resolved independently of provider JSON and only be adopted after authenticity verification.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-WH-ROUTE-001`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-WH-IDEMP-001 — Inbound provider delivery identity is scoped correctly

### Traceability

- Requirement: `AIREQ078` — Inbound provider delivery identity is scoped correctly
- PLAN: `AI-08-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_REUSE`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Dedup identity MUST use provider-documented uniqueness plus the required Connection/provider scope. Provider delivery identity and downstream product fact identity remain distinct.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-WH-IDEMP-001`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-079 — Inbound claim and durable processing intent are atomic

### Traceability

- Requirement: `AIREQ079` — Inbound claim and durable processing intent are atomic
- PLAN: `AI-08-CORE-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_REUSE`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Accepted webhook receipt/claim and provider-neutral downstream processing message MUST be committed with one durable fate so an accepted callback cannot be silently lost.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-AIREQ-079`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-WH-AUT-001 — Tenant-scoped semantic processing occurs after commit

### Traceability

- Requirement: `AIREQ080` — Tenant-scoped semantic processing occurs after commit
- PLAN: `AI-08-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_REUSE`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Downstream webhook processing MUST restore derived Account/Workspace through the messaging/RLS pipeline and load protected payload/state only within that trusted scope.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-WH-AUT-001`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-081 — Receipt becomes processed only after semantic reconciliation

### Traceability

- Requirement: `AIREQ081` — Receipt becomes processed only after semantic reconciliation
- PLAN: `AI-08-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_REUSE`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: A callback is not semantically complete merely because signature verification/receipt storage succeeded. Terminal `Processed` requires the Integration-owned reconciliation outcome to commit.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-AIREQ-081`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-082 — Malformed/conflicting webhook payload fails closed

### Traceability

- Requirement: `AIREQ082` — Malformed/conflicting webhook payload fails closed
- PLAN: `AI-08-SEC-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_REUSE`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Malformed schema, mismatched verified event identity, unsupported resource mapping or conflicting identity links MUST produce deterministic terminal failure without mutating unrelated product state.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-AIREQ-082`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-083 — Inbound receipt is not outbound WebhookDelivery

### Traceability

- Requirement: `AIREQ083` — Inbound receipt is not outbound WebhookDelivery
- PLAN: `AI-08-COMPAT-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: Infrastructure inbound receipt/dedup state MUST remain distinct from outbound `WebhookDelivery`; legacy `InboundWebhookEvent` MUST not grow into a second unreviewed source of truth.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-AIREQ-083`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`
- Existing evidence: `CalendarWebhookHttpContractTests.cs`; `CalendarWebhookIntakeIntegrationTests.cs`; `CalendarWebhookDataSessionIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `CalendarWebhookHttpContractTests.cs` + `CalendarWebhookIntakeIntegrationTests.cs` + `CalendarWebhookProcessingRuntimeTests.cs` (extend with provider-protocol fixtures), execute the scenario's exact Given/When through `CalendarEndpoints.cs`; `HandleCalendarWebhook.cs`; `CalendarWebhookVerifier.cs`; `CalendarWebhookIntake.cs`; `CalendarWebhookProcessingUseCase.cs`; assert the complete Then contract and durable result: only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-084 — Provider-specific verification support is truthful

### Traceability

- Requirement: `AIREQ084` — Provider-specific verification support is truthful
- PLAN: `AI-08-COMPAT-001`, `AI-GATE-002`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_PER_RELEASED_PROVIDER`

### Executable contract

**Given**  
a raw Calendar webhook request arrives with bounded HTTP metadata/body and a pre-existing trusted Connection/CalendarIntegration binding.

**When**  
valid, invalid, replayed, malformed, cross-tenant, provider-mismatched and duplicate callbacks are sent through the real HTTP/Application/MassTransit/PostgreSQL chain as applicable.

**Then**  
trust is established before tenant adoption and semantic completion; specifically: A generic HMAC test adapter MUST NOT be advertised as real Google/Microsoft/other provider webhook support unless the actual provider authenticity/subscription protocol and contract tests are implemented.

**Expected result:** only verified scoped deliveries can create one durable receipt/intent and reach tenant-scoped reconciliation.

### Verification binding

- Test ID: `AI-TST-AIREQ-084`
- Test project: `backend/tests/Notrelix.API.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Integrations|Messaging`
- Source under test: `CalendarWebhookVerifier.cs`; released provider-specific webhook protocol adapters/configuration
- Existing evidence: Generic HMAC verifier tests/integration exist; provider-specific Google/Microsoft authenticity is not established by naming the provider.
- Required proof target: for every provider named as released (e.g. Google/Microsoft), run that provider’s real documented verification/handshake semantics; generic HMAC is evidence only for providers using that contract.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: webhook hostile-input + tenant-runtime gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-SYNC-001 — Sync direction and source-of-truth are explicit

### Traceability

- Requirement: `AIREQ085` — Sync direction and source-of-truth are explicit
- PLAN: `AI-09-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: Every integration sync declares push/pull/bidirectional direction, authoritative owner and which provider facts may update Integration projection versus target product state.

**Expected result:** one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success.

### Verification binding

- Test ID: `AI-TST-SYNC-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarSyncContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path; assert the complete Then contract and durable result: one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-SYNC-CUR-001 — Sync cursor advances after durable successful processing

### Traceability

- Requirement: `AIREQ086` — Sync cursor advances after durable successful processing
- PLAN: `AI-09-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_IF_SYNC_RELEASED`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: Cursor/checkpoint/progress MUST advance only after the corresponding mapped changes are durable; restart MUST resume without skipping or double-applying events.

**Expected result:** one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success.

### Verification binding

- Test ID: `AI-TST-SYNC-CUR-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: if sync is released, planned crash-boundary test: cursor/state updates only in same durable success boundary as reconciled data.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-WH-ORDER-001 — Sync conflict policy is product/provider semantics

### Traceability

- Requirement: `AIREQ087` — Sync conflict policy is product/provider semantics
- PLAN: `AI-09-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_IF_SYNC_RELEASED`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: ETag/version/timestamp conflicts MUST resolve by an explicit policy; last-write-wins or provider-wins MUST NOT be accidental implementation behavior.

**Expected result:** one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success.

### Verification binding

- Test ID: `AI-TST-WH-ORDER-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: if bidirectional/conflicting sync is released, planned deterministic conflict matrix covering local-newer/provider-newer/equal/unknown cases.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-088 — Mappings preserve internal and external identities

### Traceability

- Requirement: `AIREQ088` — Mappings preserve internal and external identities
- PLAN: `AI-09-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: Calendar/event mappings MUST retain both Notrelix resource identity and provider external identity/ETag as appropriate, with uniqueness/conflict behavior explicitly tested.

**Expected result:** one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success.

### Verification binding

- Test ID: `AI-TST-AIREQ-088`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarSyncContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path; assert the complete Then contract and durable result: one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-089 — Calendar mapping preserves ownership and date/time meaning

### Traceability

- Requirement: `AIREQ089` — Calendar mapping preserves ownership and date/time meaning
- PLAN: `AI-09-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: CalendarIntegration/CalendarEvent state is provider mapping/projection, not WorkManagement/Documents lifecycle authority. Mapping MUST preserve the semantic distinction among date-only, instant, local date-time, time zone, all-day event and recurrence. A date-only WorkManagement value MUST NOT be silently converted into a UTC instant, and DST/time-zone conversion MUST be explicit and testable.

**Expected result:** Calendar mapping preserves date-only/instant/local/time-zone/all-day/recurrence semantics across DST and never silently converts date-only values to UTC instants.

### Verification binding

- Test ID: `AI-TST-AIREQ-089`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarSyncContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path; assert the complete Then contract and durable result: Calendar mapping preserves date-only/instant/local/time-zone/all-day/recurrence semantics across DST and never silently converts date-only values to UTC instants. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-090 — Manual Calendar sync is production-reachable

P5 MUST implement manual Calendar sync through the canonical authenticated/authorized Application path and provider adapter. The command MUST create/reuse stable sync operation identity, honor direction/conflict rules, advance durable cursor only after successful durable reconciliation, and expose explicit retry/terminal outcomes. A `NotImplementedException`, fake success, or hidden unsupported branch is not an allowed final P5 state.

### Traceability

- Requirement: `AIREQ090` — Manual Calendar sync is production-reachable

P5 MUST implement manual Calendar sync through the canonical authenticated/authorized Application path and provider adapter. The command MUST create/reuse stable sync operation identity, honor direction/conflict rules, advance durable cursor only after successful durable reconciliation, and expose explicit retry/terminal outcomes. A `NotImplementedException`, fake success, or hidden unsupported branch is not an allowed final P5 state.
- PLAN: `AI-09-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: 

**Expected result:** manual sync runs through the real provider/reconciliation path, never fake-succeeds, and advances the cursor only after durable success.

### Verification binding

- Test ID: `AI-TST-AIREQ-090`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `TriggerCalendarSync.cs` (`NotImplementedException` at baseline)
- Existing evidence: Baseline handler throws `NotImplementedException`; no runtime PASS is possible until implemented or unsupported scope is declared.
- Required proof target: replace `TriggerCalendarSyncCommandHandler` throw with implementation plus full-pipeline test, or explicitly remove/mark manual sync unsupported in API/product surface.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-SYNC-BF-001 — Backfill and live processing converge without duplication

### Traceability

- Requirement: `AIREQ091` — Backfill and live processing converge without duplication
- PLAN: `AI-09-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_IF_SYNC_RELEASED`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: When sync/backfill and webhook/live traffic overlap, stable external/internal identities and cursor semantics MUST prevent double application or lost updates.

**Expected result:** one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success.

### Verification binding

- Test ID: `AI-TST-SYNC-BF-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: if sync/backfill is released, run backfill concurrently with live webhook processing and prove one final mapping/effect per external identity.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-SYNC-DISC-001 — Provider deletion/disconnect semantics do not delete Notrelix resources implicitly

### Traceability

- Requirement: `AIREQ092` — Provider deletion/disconnect semantics do not delete Notrelix resources implicitly
- PLAN: `AI-09-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a Calendar integration has explicit connection, direction, internal/external mappings and sync state/cursor under tenant scope.

**When**  
manual sync, webhook/live reconciliation, conflicts, backfill, disconnect/provider deletion and crash boundaries are exercised where the capability is released.

**Then**  
sync state advances only under owned durable semantics; specifically: Remote deletion, Connection revoke or Calendar binding deactivate MUST follow explicit mapping/retention policy and MUST NOT cascade-delete foreign bounded-context business resources by persistence convenience.

**Expected result:** one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success.

### Verification binding

- Test ID: `AI-TST-SYNC-DISC-001`
- Test project: `backend/tests/Notrelix.Domain.Tests/Integrations`; `Notrelix.Integration.Tests/Integrations`
- Source under test: `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path
- Existing evidence: `CalendarIntegrationTests.cs`; `IntegrationSyncCursorTests.cs`; `CalendarConnectionFlowIntegrationTests.cs`; `CalendarWebhookProcessingRuntimeTests.cs`
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarSyncContractIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through `CalendarIntegration.cs`; `IntegrationSyncCursor`; `TriggerCalendarSync.cs`; Calendar webhook reconciliation and disconnect path; assert the complete Then contract and durable result: one converged mapping/state exists without implicit foreign-resource deletion or cursor advancement before success. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: calendar sync/reconciliation gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-093 — Backend/public contract is the automation/integration API authority

### Traceability

- Requirement: `AIREQ093` — Backend/public contract is the automation/integration API authority
- PLAN: `AI-10-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: OpenAPI/public Application contracts and approved event schemas are producer authority. Frontend handwritten types are consumers and MUST NOT become a second semantic source.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-093`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: Backend OpenAPI/API contracts plus `frontend/packages/product/automation/*`, `frontend/packages/features/integrations/*`, app composition and realtime transport
- Existing evidence: `automation.unit.test.ts`; `automation-state.unit.test.ts`; `automation-query-keys.unit.test.ts`; `automations-tab.interaction.component.test.tsx`; `integrations.unit.test.ts`
- Required proof target: In frontend Automation/Integrations contract integration tests + backend public/realtime contract tests (add/extend), execute the scenario's exact Given/When through Backend OpenAPI/API contracts plus `frontend/packages/product/automation/*`, `frontend/packages/features/integrations/*`, app composition and realtime transport; assert the complete Then contract and durable result: contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-094 — Frontend contract drift is a blocking compatibility defect

### Traceability

- Requirement: `AIREQ094` — Frontend contract drift is a blocking compatibility defect
- PLAN: `AI-10-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Frontend automation/integration vocabularies, routes, DTOs and status values MUST match the released backend contract or be migrated through an explicit compatibility adapter.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-094`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: backend OpenAPI + generated frontend contracts + handwritten Automation/Integrations types
- Existing evidence: Frontend/backend vocabulary drift is visible in source; no deterministic semantic drift gate currently closes it.
- Required proof target: planned backend-openapi ↔ generated frontend semantic contract test for trigger/action/provider/status/routes; current handwritten unions must fail the gate when divergent.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-095 — Automation authoring vocabulary is generated/shared deliberately

### Traceability

- Requirement: `AIREQ095` — Automation authoring vocabulary is generated/shared deliberately
- PLAN: `AI-10-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Trigger/action/status/config schema used by authoring UI MUST come from generated/contracted metadata or an approved shared public contract, not independent hard-coded unions that diverge from backend runtime.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-095`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: backend trigger/action definitions + frontend authoring vocabulary
- Existing evidence: Frontend hard-coded unions and backend discriminator sets currently diverge.
- Required proof target: planned authoring metadata contract test proving trigger/action/schema vocabulary comes from generated/shared producer authority rather than independent hard-coded unions.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-096 — Frontend Automation repository has a real production adapter

### Traceability

- Requirement: `AIREQ096` — Frontend Automation repository has a real production adapter
- PLAN: `AI-10-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: `AutomationRuleRepository`/Execution/Template ports MUST have a production implementation bound at composition before authoring/history UI can be certified. Test fakes and TypeScript interfaces are not production reachability.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-096`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: `frontend/packages/product/automation/state/src/data/repositories.ts`; web app composition
- Existing evidence: Repository interfaces/fakes exist; source search found no real production Automation repository implementation.
- Required proof target: planned web composition integration test that resolves concrete Automation repositories and performs list/create/enable/history calls against the real API adapter; fakes/interfaces cannot satisfy this.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-097 — Demo fixture data is not production server state

### Traceability

- Requirement: `AIREQ097` — Demo fixture data is not production server state
- PLAN: `AI-10-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Default/demo Automation Rules in UI components MUST be confined to story/test/demo composition and MUST NOT be the fallback for real application state.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-097`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: `frontend/packages/product/automation/web/src/components/automations-tab.tsx` defaultRules and app composition
- Existing evidence: `automations-tab.tsx` contains default demo Rules; story/UI evidence does not prove app production state is server-backed.
- Required proof target: planned app composition/UI test with repositories returning empty state; production surface must render empty/loading/error, never fallback demo Rules.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-098 — Integrations frontend exposes only backend-supported capabilities

### Traceability

- Requirement: `AIREQ098` — Integrations frontend exposes only backend-supported capabilities
- PLAN: `AI-10-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Provider list, connection statuses, connection/webhook endpoints and configuration forms MUST match actual backend-supported provider/endpoint contracts; generic FE `connections/webhooks` APIs cannot be certified while backend only exposes a different Calendar surface.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-098`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: `frontend/packages/features/integrations/*`; backend `CalendarEndpoints.cs`
- Existing evidence: Generic FE integration endpoints/providers differ from backend Calendar surface.
- Required proof target: planned Integrations frontend contract test binding only supported Calendar/provider endpoints/statuses; unsupported generic webhook/provider options must be hidden or explicitly feature-gated.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-099 — Automation/Integrations query keys use canonical scope partitioning

### Traceability

- Requirement: `AIREQ099` — Automation/Integrations query keys use canonical scope partitioning
- PLAN: `AI-10-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: All Account/Workspace server-state keys MUST use the canonical query-key scope helpers and remain safe across Account A→B→A and Workspace transitions.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-099`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: Backend OpenAPI/API contracts plus `frontend/packages/product/automation/*`, `frontend/packages/features/integrations/*`, app composition and realtime transport
- Existing evidence: `automation.unit.test.ts`; `automation-state.unit.test.ts`; `automation-query-keys.unit.test.ts`; `automations-tab.interaction.component.test.tsx`; `integrations.unit.test.ts`
- Required proof target: In frontend Automation/Integrations contract integration tests + backend public/realtime contract tests (add/extend), execute the scenario's exact Given/When through Backend OpenAPI/API contracts plus `frontend/packages/product/automation/*`, `frontend/packages/features/integrations/*`, app composition and realtime transport; assert the complete Then contract and durable result: contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-100 — Realtime execution status reuses Domain lifecycle facts through a real public producer

### Traceability

- Requirement: `AIREQ100` — Realtime execution status reuses Domain lifecycle facts through a real public producer
- PLAN: `AI-10-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Automation already raises Domain execution lifecycle facts such as started/succeeded/failed. P5 MUST map the owned lifecycle facts into a versioned public/realtime producer contract consumed by frontend event names/payloads; it MUST NOT create a second competing Execution lifecycle merely to satisfy the UI. Fixture-only frontend events or Domain events with no public/realtime mapping are not production evidence.

**Expected result:** Existing Automation Execution Domain lifecycle facts are mapped into one versioned public/realtime contract consumed by the frontend; no parallel lifecycle is invented.

### Verification binding

- Test ID: `AI-TST-AIREQ-100`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: FE `execution-adapter.ts` + backend realtime/public event producer inventory
- Existing evidence: Automation Execution Domain events (`automation.automation-execution-started/succeeded/failed`) exist and FE adapter/fixtures consume `automation.execution.*`; no production public/realtime mapper connecting those contracts was found.
- Required proof target: planned public/realtime mapper from existing Automation Execution lifecycle facts + frontend consumer test using the versioned/generated public schema; creating duplicate Domain lifecycle events or relying on fixtures does not pass.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-RT-001 — Automation and Integration realtime recovery converge from durable truth

### Traceability

- Requirement: `AIREQ101` — Automation and Integration realtime recovery converge from durable truth
- PLAN: `AI-10-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Realtime is freshness only. Automation execution status MUST recover from durable execution/history queries, and Integration connection/sync/error progress MUST recover from durable Integration state. On gap/reconnect the consumer MUST suspend or queue later envelopes, refetch authoritative state, reconcile/reset sequence checkpoints, then resume according to the Platform recovery contract. Missing realtime MUST never permanently lose current product state.

**Expected result:** Automation and Integration clients recover durable current state after a realtime gap, reconcile the checkpoint once, then resume without losing/duplicating semantic state.

### Verification binding

- Test ID: `AI-TST-RT-001`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: FE realtime transport/recovery + durable Automation execution/history queries
- Existing evidence: Frontend adapter unit tests exist; no P5 end-to-end gap→authoritative reload→checkpoint reconcile proof found.
- Required proof target: planned end-to-end realtime recovery test: gap → suspend later envelopes → authoritative execution/history reload → sequence reconcile → normal delivery resumes; failed recovery retries deterministically.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-102 — OpenAPI/codegen and mixed-version behavior are gated

### Traceability

- Requirement: `AIREQ102` — OpenAPI/codegen and mixed-version behavior are gated
- PLAN: `AI-10-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the backend OpenAPI/public contracts and actual frontend web/mobile composition are built from the same exact candidate.

**When**  
generated contracts/typecheck/tests run and the UI is exercised with empty/real/error data, account/workspace transitions and realtime gaps as applicable.

**Then**  
frontend behavior consumes producer authority rather than independent vocabulary/fixtures; specifically: Contract-affecting changes MUST regenerate deterministic producer artifacts, compile/semantically test consumers and define old/new backend-web/mobile/worker combinations where deployment is non-atomic.

**Expected result:** contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-102`
- Test project: frontend Vitest/integration + backend OpenAPI/API contract tests
- Source under test: Backend OpenAPI/API contracts plus `frontend/packages/product/automation/*`, `frontend/packages/features/integrations/*`, app composition and realtime transport
- Existing evidence: `automation.unit.test.ts`; `automation-state.unit.test.ts`; `automation-query-keys.unit.test.ts`; `automations-tab.interaction.component.test.tsx`; `integrations.unit.test.ts`
- Required proof target: In frontend Automation/Integrations contract integration tests + backend public/realtime contract tests (add/extend), execute the scenario's exact Given/When through Backend OpenAPI/API contracts plus `frontend/packages/product/automation/*`, `frontend/packages/features/integrations/*`, app composition and realtime transport; assert the complete Then contract and durable result: contract drift fails CI and only production adapters/producers/recovery paths count as reachability evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: frontend typecheck/unit/integration + generated-contract drift gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-ACT-ARCH-001 — Automation and Integrations own only their persistence

### Traceability

- Requirement: `AIREQ103` — Automation and Integrations own only their persistence
- PLAN: `AI-11-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: P5 may persist its own Rule/Execution/Schedule/Template/Agent/Connection/secret-reference/webhook/sync/calendar state. It MUST NOT directly write another bounded context's private tables.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-ACT-ARCH-001`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AUTHZ-004 — Workspace-scoped tables remain under RLS defense-in-depth

### Traceability

- Requirement: `AIREQ104` — Workspace-scoped tables remain under RLS defense-in-depth
- PLAN: `AI-11-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Production requests and workers touching Automation/Integrations workspace state MUST apply the canonical tenant/RLS session context. RLS is defense-in-depth and does not replace Application authorization.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AUTHZ-004`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-105 — Technical secret/receipt state has constrained access paths

### Traceability

- Requirement: `AIREQ105` — Technical secret/receipt state has constrained access paths
- PLAN: `AI-11-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Secret blobs and inbound receipt reliability state that intentionally do not model tenant columns MUST be reachable only through trusted opaque references/bindings and must not become general query surfaces.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-105`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-106 — Migration evidence includes clean database and supported upgrade

### Traceability

- Requirement: `AIREQ106` — Migration evidence includes clean database and supported upgrade
- PLAN: `AI-11-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Schema/config/discriminator/index changes MUST prove clean database creation, supported upgrade from the declared baseline, migration discipline and no pending model drift at the exact candidate.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-106`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-107 — Persisted JSON/discriminator changes are migration-sensitive

### Traceability

- Requirement: `AIREQ107` — Persisted JSON/discriminator changes are migration-sensitive
- PLAN: `AI-11-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_ON_SCHEMA_CHANGE`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Rule configuration, provider config, status/discriminator and mapping-key evolution MUST include dual-reader/upcast/migration/fail-safe behavior as applicable; old rows MUST NOT be silently reinterpreted.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-107`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: on any persisted JSON/discriminator change, seed previous-version rows and prove dual-read/upcast/migration/fail-safe behavior and rollback/forward-fix.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-108 — Concurrency constraints match semantic identities

### Traceability

- Requirement: `AIREQ108` — Concurrency constraints match semantic identities
- PLAN: `AI-11-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Database indexes/versions/conditional writes MUST enforce logical uniqueness such as one execution per (Rule, trigger occurrence), connection/provider/binding uniqueness and webhook receipt dedup where required.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-108`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-109 — Soft-delete/retention behavior is explicit

### Traceability

- Requirement: `AIREQ109` — Soft-delete/retention behavior is explicit
- PLAN: `AI-11-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Soft deletion, restore and retention for Rules, Templates, Connections, Calendar mappings and related history MUST preserve required audit/history while excluding deleted state from normal operations.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-109`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-110 — No cross-context cascade semantics by database convenience

### Traceability

- Requirement: `AIREQ110` — No cross-context cascade semantics by database convenience
- PLAN: `AI-11-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
a clean PostgreSQL database and a supported previous baseline contain representative Automation/Integration rows, RLS policies and concurrency identities.

**When**  
migrations/upgrades, tenant-isolated requests/workers, duplicate writes, soft-delete/restore and schema evolution are executed.

**Then**  
persistence remains owner-local, isolated and migration-safe; specifically: Foreign keys/cascades MUST NOT make P5 delete or mutate resources owned by WorkManagement/Documents/Collaboration/Identity/Billing. Cross-context lifecycle is an explicit contract.

**Expected result:** no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs.

### Verification binding

- Test ID: `AI-TST-AIREQ-110`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; `Notrelix.Infrastructure.Tests`; `Notrelix.Integration.Tests/Data|Integration`
- Source under test: Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence
- Existing evidence: `RlsPolicyVerificationTests.cs`; `RlsRuntimeEnforcementTests.cs`; `MigrationSmokeTests.cs`; `MigrationResiliencyTests.cs`; `ExpectedVersionConcurrencyIntegrationTests.cs`
- Required proof target: In `MigrationSmokeTests.cs` + `MigrationResiliencyTests.cs` + RLS runtime/architecture suites (extend), execute the scenario's exact Given/When through Automation/Integration EF configurations, migrations, RLS scripts, `ResourceLocator`, and owned persistence; assert the complete Then contract and durable result: no cross-tenant/private-context access, silent reinterpretation, invalid duplicate identity or convenience cascade occurs. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: PostgreSQL/RLS/migration gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-OBS-001 — Correlation traces the semantic chain without conflating identities

### Traceability

- Requirement: `AIREQ111` — Correlation traces the semantic chain without conflating identities
- PLAN: `AI-12-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: Observability MUST relate source event, Rule, Execution, Action, target operation, Connection/provider operation, webhook delivery and worker attempt while preserving their distinct identities.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-OBS-001`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationOperationsRecoveryIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`; assert the complete Then contract and durable result: backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-MSG-001 — Message/provider/operation/correlation identities remain distinct

### Traceability

- Requirement: `AIREQ112` — Message/provider/operation/correlation identities remain distinct
- PLAN: `AI-12-INV-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: EventId, SourceEventId, RuleId, ExecutionId, target OperationId, provider event/operation id, CorrelationId and CausationId MUST not be reused interchangeably.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-MSG-001`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationOperationsRecoveryIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`; assert the complete Then contract and durable result: backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-113 — Telemetry and history redact secrets and unsafe payloads

### Traceability

- Requirement: `AIREQ113` — Telemetry and history redact secrets and unsafe payloads
- PLAN: `AI-12-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: Logs, traces, metrics, execution errors/history and provider diagnostics MUST redact access tokens, webhook secrets, encrypted blobs, provider credentials and sensitive payload data.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-AIREQ-113`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationOperationsRecoveryIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`; assert the complete Then contract and durable result: backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-114 — Operational metrics expose backlog and failure semantics

### Traceability

- Requirement: `AIREQ114` — Operational metrics expose backlog and failure semantics
- PLAN: `AI-12-CORE-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: P5 MUST expose actionable measurements for event/consumer backlog, execution latency/status, retries, poison/terminal outcomes, stale claims/reconciliation, webhook verification, sync lag and provider failures.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-AIREQ-114`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationOperationsRecoveryIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`; assert the complete Then contract and durable result: backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/failure
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-115 — Provider backpressure is isolated and observable

### Traceability

- Requirement: `AIREQ115` — Provider backpressure is isolated and observable
- PLAN: `AI-12-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: A degraded/rate-limited provider or connection MUST not globally block unrelated providers/workspaces; queue growth, retry-after and throttling state MUST be bounded/observable.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-AIREQ-115`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: planned provider backpressure test with one throttled provider/workspace and one healthy provider; healthy path continues and backlog/retry-after metrics are bounded.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-116 — Fanout and recursion have capacity controls

### Traceability

- Requirement: `AIREQ116` — Fanout and recursion have capacity controls
- PLAN: `AI-12-SEC-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: One event matching many Rules, chained Automations and provider operations MUST have explicit throughput/fanout/rate/entitlement guards so retries or loops cannot create unbounded work.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-AIREQ-116`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: planned fanout/loop capacity test enforcing configured max rule matches/chain depth/rate or entitlement bounds.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-POISON-001 — Recovery procedures are governed and auditable

### Traceability

- Requirement: `AIREQ117` — Recovery procedures are governed and auditable
- PLAN: `AI-12-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: Operator repair/reconciliation that changes durable claim/execution/connection/sync state MUST use reviewed procedures/tools, preserve lineage and record what was changed; diagnostics MUST not mutate product state as a shortcut.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-POISON-001`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`
- Existing evidence: `PipelineTelemetryIntegrationTests.cs`; N8n runtime/durability tests; recovery runbook
- Required proof target: In `backend/tests/Notrelix.Integration.Tests/Automation/AutomationOperationsRecoveryIntegrationTests.cs` (add/extend), execute the scenario's exact Given/When through P5 runtime consumers/adapters, observability metrics, readiness dependencies and `docs/operations/recovery-and-data-safety.md`; assert the complete Then contract and durable result: backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-118 — Readiness fails when required dependencies are unavailable

### Traceability

- Requirement: `AIREQ118` — Readiness fails when required dependencies are unavailable
- PLAN: `AI-12-COMPAT-001`, `AI-GATE-003`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED`

### Executable contract

**Given**  
the production P5 runtime is instrumented and mandatory dependencies can be degraded/removed while durable recovery state exists.

**When**  
load/fanout/provider throttling, retry/poison/recovery, secret-safe failure and dependency-unavailable scenarios are exercised.

**Then**  
operators receive bounded, identity-correct and safe diagnostics/recovery behavior; specifically: Required messaging, secret storage/key ring, database/RLS, provider configuration or other mandatory runtime dependencies MUST fail readiness or capability availability honestly rather than report fake success.

**Expected result:** backlog/failure/reconciliation are observable, unrelated consumers remain fair, and repairs are governed/auditable.

### Verification binding

- Test ID: `AI-TST-AIREQ-118`
- Test project: `backend/tests/Notrelix.Integration.Tests`; observability/runtime tests; governed recovery evidence
- Source under test: messaging/database/RLS/key-ring/provider configuration readiness registrations
- Existing evidence: Existing composition/health evidence is reusable, but each mandatory P5 dependency requires a negative readiness test.
- Required proof target: planned readiness tests that remove broker/database/RLS/key-ring/provider mandatory configuration and assert unhealthy/unavailable capability rather than fake success.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: operational/recovery/readiness gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-119 — TAC AI-FLOW-01 through AI-FLOW-07 are reused as canonical cross-boundary flows

### Traceability

- Requirement: `AIREQ119` — TAC AI-FLOW-01 through AI-FLOW-07 are reused as canonical cross-boundary flows
- PLAN: `AI-13-INV-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: The execution package MUST crosswalk existing TAC AI flows to P5 requirements and source evidence instead of inventing parallel flow IDs. Historical disposition text that no longer matches the exact candidate MUST be marked stale rather than copied as current status.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-119`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-120 — Platform flow contracts are inherited, not redefined

### Traceability

- Requirement: `AIREQ120` — Platform flow contracts are inherited, not redefined
- PLAN: `AI-13-CORE-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: Broker consumption, tenant restoration, dedup, outbox, retry/poison/recovery and event compatibility MUST reuse the canonical Platform PF-FLOW and BE-PLT requirements applicable to each P5 consumer.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-120`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AUTHZ-003 — Architecture gates prevent provider and foreign-domain leakage

### Traceability

- Requirement: `AIREQ121` — Architecture gates prevent provider and foreign-domain leakage
- PLAN: `AI-13-CORE-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: Architecture tests MUST enforce Domain purity, bounded-context ownership, public cross-context ports, no provider SDK/model leakage into Automation Domain, and no private foreign persistence access.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AUTHZ-003`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-122 — Verification proves actual runtime owner

### Traceability

- Requirement: `AIREQ122` — Verification proves actual runtime owner
- PLAN: `AI-13-SEC-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `CI_RUNTIME_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: For transaction/retry/RLS/provider semantics, unit tests of a helper are insufficient. Tests MUST execute the production composition owner (pipeline, MassTransit consumer, EF/PostgreSQL/provider adapter boundary as applicable).

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-122`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: MediatR pipeline, MassTransit consumers/filters, PostgreSQL RLS and real provider adapter boundary
- Existing evidence: Existing runtime integration tests are strong anchors; helper/unit tests remain insufficient for transaction/provider guarantees.
- Required proof target: execute production owners: MediatR pipeline, MassTransit endpoint/filter, PostgreSQL RLS/constraints and provider adapter seam; helper/unit evidence alone is insufficient.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-TRG-ORDER-001 — Every critical flow has negative/failure evidence

### Traceability

- Requirement: `AIREQ123` — Every critical flow has negative/failure evidence
- PLAN: `AI-13-COMPAT-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `EXISTING_TEST_EXTEND`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: Authorization denial, tenant mismatch, duplicate/concurrent delivery, invalid config, stale claim, provider unknown outcome, webhook replay/signature failure, migration upgrade and contract drift MUST have explicit failure-path tests.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-TRG-ORDER-001`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-124 — Contract evolution accounts for queued/backlogged/deployed consumers

### Traceability

- Requirement: `AIREQ124` — Contract evolution accounts for queued/backlogged/deployed consumers
- PLAN: `AI-14-INV-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `NEW_TEST_REQUIRED_ON_CONTRACT_CHANGE`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: Changes to source events, dispatch intents, provider callbacks, persisted discriminators and realtime contracts MUST consider old workers, queued/dead-letter messages, replay and independently deployed web/mobile consumers.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-124`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: for each changed event/intent/webhook/realtime schema, run old/new producer-consumer compatibility with queued payload fixtures and independently built web/mobile consumers.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `YES`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-125 — Service extraction is a later governed decision

### Traceability

- Requirement: `AIREQ125` — Service extraction is a later governed decision
- PLAN: `AI-14-CORE-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: Automation and Integrations may be future service candidates, but this workstream MUST first prove explicit contracts, isolated data ownership, failure semantics, tenancy/auth propagation and operational ownership inside the modular monolith.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-125`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-126 — Cross-team handoff is explicit

### Traceability

- Requirement: `AIREQ126` — Cross-team handoff is explicit
- PLAN: `AI-GOV-002`, `AI-14-SEC-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: Every P5 producer/consumer handoff MUST record capability, current/target contract, producer/consumer owners, breaking/additive class, migration, both teams' actions, verification, required readiness and rollback/forward-fix.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-126`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-127 — Readiness is capability- and consumer-specific

### Traceability

- Requirement: `AIREQ127` — Readiness is capability- and consumer-specific
- PLAN: `AI-GOV-002`, `AI-14-SEC-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `STATIC_GOVERNANCE_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: P5 MUST NOT receive one blanket D5. Rule authoring, Work action, N8n provider effect, Calendar connection/webhook, sync, frontend and realtime consumers are certified independently against their actual dependencies and released scope.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-127`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows
- Existing evidence: `AutomationProcessReferenceArchitectureTests.cs`; `CalendarSemanticAuthorityArchitectureTests.cs`; `ProductionGraphTests.cs`; `.github/workflows/backend-ci.yml`; `.github/workflows/container-ci.yml`
- Required proof target: In `backend/tests/Notrelix.Architecture.Tests` + exact production composition/CI evidence (add/extend), execute the scenario's exact Given/When through Canonical TAC/Platform flow docs, architecture tests, exact production composition and CI workflows; assert the complete Then contract and durable result: no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence. Use the actual production composition whenever broker, database/RLS, provider, browser/realtime, restart or key-ring behavior is claimed.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `NO`
- Migration-sensitive: `NO`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## AI-TST-AIREQ-128 — Final certification binds exact source, migrations, tests and CI

### Traceability

- Requirement: `AIREQ128` — Final certification binds exact source, migrations, tests and CI
- PLAN: `AI-GOV-002`, `AI-14-COMPAT-001`, `AI-GATE-004`
- Preparation state: `NOT_EVALUATED`
- Evidence class: `CI_RUNTIME_PROOF`

### Executable contract

**Given**  
the exact candidate includes the canonical TAC AI-FLOW/PF-FLOW contracts, architecture gates, production composition and CI definitions.

**When**  
the verifier crosswalks each released P5 path and runs negative/runtime/compatibility evidence through its actual owner.

**Then**  
architecture and certification remain evidence-driven; specifically: P5 is complete only when SPEC→PLAN→TESTS→source/migrations→runtime execution→CI evidence agree on the exact candidate SHA, all release-scoped blockers are closed/accepted explicitly, and downstream consumers have actionable handoff evidence.

**Expected result:** no helper-only or stale-document evidence is promoted to release readiness, and CERT can trace every PASS to exact execution evidence.

### Verification binding

- Test ID: `AI-TST-AIREQ-128`
- Test project: `backend/tests/Notrelix.Architecture.Tests`; production integration suites; CI evidence
- Source under test: SPEC/PLAN/TESTS/CERT + migrations + executed CI/action evidence at exact candidate
- Existing evidence: Historical CI/source existence is preparation evidence only; final exact candidate must rerun and record evidence.
- Required proof target: record exact candidate SHA, migration head, executed test commands/suite counts, CI run IDs/digests and blocker disposition in CERT; no inferred PASS.
- Positive/negative: positive + negative/absence-of-proof
- Security-sensitive: `YES`
- Migration-sensitive: `YES`
- CI gate: architecture + exact-candidate certification gate
- Exact-candidate rerun: `REQUIRED`
- Execution evidence: `NOT_RECORDED`

## 7. TAC AI-flow evidence routing

Historical flow IDs are reused; no parallel P5 flow namespace is created. Historical source-status prose is not accepted when it conflicts with the exact candidate.

| TAC flow | Primary tests | Existing source/runtime anchor | Evidence rule |
|---|---|---|---|
| `AI-FLOW-01` Rule creation + Billing | `AI-TST-RULE-001`, `AI-TST-RULE-003`, `AI-TST-AIREQ-015`, `AI-TST-AIREQ-018`, `AI-TST-AIREQ-052`, `AI-TST-AIREQ-053` | `CreateAutomationRuleCommand` + Billing capacity + Rule aggregate | full pipeline + idempotency + immutable revision + capacity lifecycle |
| `AI-FLOW-02` Work fact → Automation | `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-EXE-CONC-001`, `AI-TST-AIREQ-024`, `AI-TST-COND-001` | MassTransit Work consumers → evaluator → Execution + outbox intent | producer compatibility + tenant/dedup + condition/vocabulary parity |
| `AI-FLOW-03` Automation → Work action | `AI-TST-ACT-001`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-AUTHZ-004`, `AI-TST-EXE-IDEMP-001` | MoveItem intent → use case → Automation port/ACL → Work public action | production composition; target auth/scope/idempotency required |
| `AI-FLOW-04` Automation → N8n | `AI-TST-OUT-001`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`, `AI-TST-AIREQ-071`, `AI-TST-AIREQ-072`, `AI-TST-AIREQ-073`, `AI-TST-AIREQ-074` | ADR-008 + N8n consumer-owned claim/effect/settle | provider call count + claim/execution residue + governed reconciliation |
| `AI-FLOW-05` Calendar connect | `AI-TST-CONN-001`, `AI-TST-CONN-AUTHZ-001`, `AI-TST-SEC-001`, `AI-TST-SEC-002`, `AI-TST-AIREQ-061` | Connect command → secret reference/version → Connection + Calendar binding | full pipeline + secret round-trip/restart + provider-install scope |
| `AI-FLOW-06` Calendar disconnect | `AI-TST-AIREQ-063`, `AI-TST-SYNC-DISC-001`, `AI-TST-AIREQ-109` | Calendar deactivate → CAL-CONN-001 last-binding policy → cleanup consumer | resource auth + lifecycle + no implicit product deletion |
| `AI-FLOW-07` verified Calendar webhook | `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-WH-AUT-001`, `AI-TST-AIREQ-079`, `AI-TST-AIREQ-081`, `AI-TST-AIREQ-082`, `AI-TST-AIREQ-084` | bounded HTTP → binding/signature → receipt/outbox → tenant consumer → Calendar reconcile | hostile input + exact provider protocol + atomic intake + terminal semantic state |

## 8. Product-rule → requirement → primary-test routing

### 8.1 Automation product rules

| Product rule | AIREQ coverage | Primary tests |
|---|---|---|
| `AUT-001` | AIREQ009, AIREQ017 | `AI-TST-AIREQ-009`, `AI-TST-AIREQ-017` |
| `AUT-002` | AIREQ011 | `AI-TST-RULE-001` |
| `AUT-003` | AIREQ012 | `AI-TST-RULE-002` |
| `AUT-004` | AIREQ013–AIREQ014, AIREQ029 | `AI-TST-AIREQ-013`, `AI-TST-RULE-003`, `AI-TST-AIREQ-029` |
| `AUT-005` | AIREQ022–AIREQ024 | `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-AIREQ-024` |
| `AUT-006` | AIREQ047 | `AI-TST-AIREQ-047` |
| `AUT-007` | AIREQ039–AIREQ042 | `AI-TST-AIREQ-039`, `AI-TST-AIREQ-040`, `AI-TST-AIREQ-041`, `AI-TST-AIREQ-042` |
| `AUT-008` | AIREQ040 | `AI-TST-AIREQ-040` |
| `AUT-009` | AIREQ027–AIREQ028, AIREQ041 | `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-AIREQ-041` |
| `AUT-010` | AIREQ020 | `AI-TST-COND-001` |
| `AUT-011` | AIREQ021 | `AI-TST-COND-002` |
| `AUT-012` | AIREQ025, AIREQ048–AIREQ051 | `AI-TST-AIREQ-025`, `AI-TST-AIREQ-048`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-ACT-001` |
| `AUT-013` | AIREQ017–AIREQ019, AIREQ025 | `AI-TST-AIREQ-017`, `AI-TST-AIREQ-018`, `AI-TST-AIREQ-019`, `AI-TST-AIREQ-025` |
| `AUT-014` | AIREQ026, AIREQ031 | `AI-TST-AIREQ-026`, `AI-TST-AIREQ-031` |
| `AUT-015` | AIREQ002, AIREQ065 | `AI-TST-ACT-ARCH-002`, `AI-TST-AIREQ-065` |
| `AUT-016` | AIREQ027–AIREQ029 | `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-AIREQ-029` |
| `AUT-017` | AIREQ032 | `AI-TST-AIREQ-032` |
| `AUT-018` | AIREQ034–AIREQ036, AIREQ067 | `AI-TST-AIREQ-034`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`, `AI-TST-OUT-001` |
| `AUT-019` | AIREQ038 | `AI-TST-EXE-HIST-001` |
| `AUT-020` | AIREQ049–AIREQ050 | `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002` |
| `AUT-021` | AIREQ038, AIREQ117 | `AI-TST-EXE-HIST-001`, `AI-TST-POISON-001` |
| `AUT-022` | AIREQ037, AIREQ116 | `AI-TST-EXE-REC-001`, `AI-TST-AIREQ-116` |
| `AUT-023` | AIREQ052–AIREQ053 | `AI-TST-AIREQ-052`, `AI-TST-AIREQ-053` |
| `AUT-024` | AIREQ028, AIREQ035 | `AI-TST-EXE-CONC-001`, `AI-TST-EXE-IDEMP-001` |
| `AUT-025` | AIREQ048 | `AI-TST-AIREQ-048` |
| `AUT-026` | AIREQ043–AIREQ044 | `AI-TST-AIREQ-043`, `AI-TST-AIREQ-044` |
| `AUT-027` | AIREQ045–AIREQ046 | `AI-TST-AIREQ-045`, `AI-TST-AIREQ-046` |
| `AUT-028` | AIREQ025 | `AI-TST-AIREQ-025` |
| `AUT-029` | AIREQ060–AIREQ062 | `AI-TST-SEC-001`, `AI-TST-AIREQ-061`, `AI-TST-SEC-002` |
| `AUT-030` | AIREQ027, AIREQ112 | `AI-TST-EXE-001`, `AI-TST-MSG-001` |
| `AUT-031` | AIREQ100–AIREQ101 | `AI-TST-AIREQ-100`, `AI-TST-RT-001` |
| `AUT-032` | AIREQ075–AIREQ084 | `AI-TST-AIREQ-075`, `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-AIREQ-079`, `AI-TST-WH-AUT-001`, `AI-TST-AIREQ-081`, `AI-TST-AIREQ-082`, `AI-TST-AIREQ-083`, `AI-TST-AIREQ-084` |
| `AUT-033` | AIREQ048–AIREQ051, AIREQ121 | `AI-TST-AIREQ-048`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-ACT-001`, `AI-TST-AUTHZ-003` |
| `AUT-034` | AIREQ038, AIREQ088, AIREQ092 | `AI-TST-EXE-HIST-001`, `AI-TST-AIREQ-088`, `AI-TST-SYNC-DISC-001` |
| `AUT-035` | AIREQ038, AIREQ113 | `AI-TST-EXE-HIST-001`, `AI-TST-AIREQ-113` |

### 8.2 Integrations product rules

| Product rule | AIREQ coverage | Primary tests |
|---|---|---|
| `INT-001` | AIREQ002, AIREQ066 | `AI-TST-ACT-ARCH-002`, `AI-TST-PROV-001` |
| `INT-002` | AIREQ055, AIREQ063 | `AI-TST-CONN-001`, `AI-TST-AIREQ-063` |
| `INT-003` | AIREQ056–AIREQ057 | `AI-TST-AIREQ-056`, `AI-TST-CONN-AUTHZ-001` |
| `INT-004` | AIREQ057 | `AI-TST-CONN-AUTHZ-001` |
| `INT-005` | AIREQ060–AIREQ062 | `AI-TST-SEC-001`, `AI-TST-AIREQ-061`, `AI-TST-SEC-002` |
| `INT-006` | AIREQ061–AIREQ064 | `AI-TST-AIREQ-061`, `AI-TST-SEC-002`, `AI-TST-AIREQ-063`, `AI-TST-AIREQ-064` |
| `INT-007` | AIREQ075–AIREQ076 | `AI-TST-AIREQ-075`, `AI-TST-WH-SEC-001` |
| `INT-008` | AIREQ077 | `AI-TST-WH-ROUTE-001` |
| `INT-009` | AIREQ078–AIREQ081 | `AI-TST-WH-IDEMP-001`, `AI-TST-AIREQ-079`, `AI-TST-WH-AUT-001`, `AI-TST-AIREQ-081` |
| `INT-010` | AIREQ078, AIREQ112 | `AI-TST-WH-IDEMP-001`, `AI-TST-MSG-001` |
| `INT-011` | AIREQ085 | `AI-TST-SYNC-001` |
| `INT-012` | AIREQ085, AIREQ089 | `AI-TST-SYNC-001`, `AI-TST-AIREQ-089` |
| `INT-013` | AIREQ086 | `AI-TST-SYNC-CUR-001` |
| `INT-014` | AIREQ086 | `AI-TST-SYNC-CUR-001` |
| `INT-015` | AIREQ064 | `AI-TST-AIREQ-064` |
| `INT-016` | AIREQ088 | `AI-TST-AIREQ-088` |
| `INT-017` | AIREQ063, AIREQ092 | `AI-TST-AIREQ-063`, `AI-TST-SYNC-DISC-001` |
| `INT-018` | AIREQ092 | `AI-TST-SYNC-DISC-001` |
| `INT-019` | AIREQ067 | `AI-TST-OUT-001` |
| `INT-020` | AIREQ070, AIREQ073 | `AI-TST-OUT-UNK-001`, `AI-TST-AIREQ-073` |
| `INT-021` | AIREQ068–AIREQ069, AIREQ115 | `AI-TST-AIREQ-068`, `AI-TST-OUT-IDEMP-001`, `AI-TST-AIREQ-115` |
| `INT-022` | AIREQ087 | `AI-TST-WH-ORDER-001` |
| `INT-023` | AIREQ089 | `AI-TST-AIREQ-089` |
| `INT-024` | AIREQ089 | `AI-TST-AIREQ-089` |
| `INT-025` | AIREQ057–AIREQ059 | `AI-TST-CONN-AUTHZ-001`, `AI-TST-AIREQ-058`, `AI-TST-AIREQ-059` |
| `INT-026` | AIREQ075, AIREQ082 | `AI-TST-AIREQ-075`, `AI-TST-AIREQ-082` |
| `INT-027` | AIREQ067–AIREQ069 | `AI-TST-OUT-001`, `AI-TST-AIREQ-068`, `AI-TST-OUT-IDEMP-001` |
| `INT-028` | AIREQ051, AIREQ103, AIREQ110 | `AI-TST-ACT-001`, `AI-TST-ACT-ARCH-001`, `AI-TST-AIREQ-110` |
| `INT-029` | AIREQ049 | `AI-TST-AUTHZ-001` |
| `INT-030` | AIREQ052 | `AI-TST-AIREQ-052` |
| `INT-031` | AIREQ066, AIREQ124 | `AI-TST-PROV-001`, `AI-TST-AIREQ-124` |
| `INT-032` | AIREQ100–AIREQ101 | `AI-TST-AIREQ-100`, `AI-TST-RT-001` |
| `INT-033` | AIREQ063 | `AI-TST-AIREQ-063` |
| `INT-034` | AIREQ092 | `AI-TST-SYNC-DISC-001` |
| `INT-035` | AIREQ057–AIREQ060 | `AI-TST-CONN-AUTHZ-001`, `AI-TST-AIREQ-058`, `AI-TST-AIREQ-059`, `AI-TST-SEC-001` |
| `INT-036` | AIREQ066 | `AI-TST-PROV-001` |
| `INT-037` | AIREQ017, AIREQ107 | `AI-TST-AIREQ-017`, `AI-TST-AIREQ-107` |
| `INT-038` | AIREQ125 | `AI-TST-AIREQ-125` |

## 9. Confirmed gap → proof routing

| Gap | Primary proof scenarios | Closure rule |
|---|---|---|
| `AI-GAP-01` configuration/API drift | `AI-TST-AIREQ-017`, `AI-TST-AIREQ-018`, `AI-TST-AIREQ-019`, `AI-TST-AIREQ-093`, `AI-TST-AIREQ-094`, `AI-TST-AIREQ-095` | producer/consumer config contract and persisted migration converge |
| `AI-GAP-02` immutable Rule revision | `AI-TST-RULE-003`, `AI-TST-AIREQ-013`, `AI-TST-AIREQ-029` | queued execution remains bound to original semantic revision after Rule edit |
| `AI-GAP-03` Condition/multi-action | `AI-TST-COND-001`, `AI-TST-COND-002`, `AI-TST-AIREQ-026`, `AI-TST-AIREQ-031` | real runtime proof if released, otherwise public contract excludes capability |
| `AI-GAP-04` vocabulary > executors | `AI-TST-AIREQ-024`, `AI-TST-AIREQ-025` | every advertised discriminator has a runtime executor/consumer or is hidden/fail-closed |
| `AI-GAP-05` Schedule/Templates/Agents | `AI-TST-AIREQ-039`, `AI-TST-AIREQ-040`, `AI-TST-AIREQ-041`, `AI-TST-AIREQ-042`, `AI-TST-AIREQ-043`, `AI-TST-AIREQ-044`, `AI-TST-AIREQ-045`, `AI-TST-AIREQ-046` | explicit admission before any production-readiness claim |
| `AI-GAP-06` Automation frontend | `AI-TST-AIREQ-093`, `AI-TST-AIREQ-094`, `AI-TST-AIREQ-095`, `AI-TST-AIREQ-096`, `AI-TST-AIREQ-097`, `AI-TST-AIREQ-098`, `AI-TST-AIREQ-099`, `AI-TST-AIREQ-100`, `AI-TST-RT-001` | generated/shared vocabulary, real repositories, no demo fallback, real realtime producer/recovery |
| `AI-GAP-07` permission vocabulary | `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-AIREQ-048`, `AI-TST-ACT-001` | deliberate Governance resource/action mapping and target revalidation |
| `AI-GAP-08` Billing release lifecycle | `AI-TST-AIREQ-015`, `AI-TST-AIREQ-052`, `AI-TST-AIREQ-053` | consume/release/reconsume semantics survive retries/concurrency |
| `AI-GAP-09` provider OAuth | `AI-TST-AIREQ-058`, `AI-TST-AIREQ-059`, `AI-TST-SEC-001` | provider-specific install/auth flow, not raw-token endpoint inference |
| `AI-GAP-10` provider/catalog drift | `AI-TST-AIREQ-056`, `AI-TST-AIREQ-094`, `AI-TST-AIREQ-098` | one producer authority drives backend/frontend released provider surface |
| `AI-GAP-11` N8n reconciliation | `AI-TST-OUT-UNK-001`, `AI-TST-AIREQ-071`, `AI-TST-AIREQ-072`, `AI-TST-AIREQ-073`, `AI-TST-POISON-001` | no blind re-fire plus governed executable recovery with lineage |
| `AI-GAP-12` manual Calendar sync | `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`, `AI-TST-AIREQ-090`, `AI-TST-SYNC-BF-001` | implement and prove or explicitly declare unsupported |
| `AI-GAP-13` generic outbound webhook runtime | `AI-TST-AIREQ-006`, `AI-TST-AIREQ-007`, `AI-TST-AIREQ-083`, `AI-TST-AIREQ-119` | Domain/persistence state cannot be certified as released runtime |
| `AI-GAP-14` provider-specific webhook auth | `AI-TST-WH-SEC-001`, `AI-TST-AIREQ-075`, `AI-TST-AIREQ-084` | per released provider protocol evidence |
| `AI-GAP-15` Domain execution facts exist but public/realtime mapping missing | `AI-TST-AIREQ-100`, `AI-TST-RT-001` | real producer schema + authoritative recovery chain |
| `AI-GAP-16` stale TAC disposition | `AI-TST-AIREQ-001`, `AI-TST-AIREQ-004`, `AI-TST-AIREQ-005`, `AI-TST-AIREQ-006`, `AI-TST-AIREQ-119` | preserve Flow ID/invariants, replace stale source-status evidence |

## 10. Evidence recording schema

For every scenario promoted from `NOT_EVALUATED`, record at minimum:

```text
Test ID:
Requirement:
Candidate SHA:
Branch / PR:
Executed command or CI job:
Test project / suite:
Tests discovered:
Tests executed:
Passed / failed / skipped:
Runtime owner exercised:
Database/provider/container identity where relevant:
Migration baseline/head where relevant:
Evidence artifact / CI run / log locator:
Observed durable post-condition:
Known debt / blocker:
Reviewer:
Decision timestamp:
```

Do not record only a filename or a historical test name as execution evidence.

## 11. Certification handoff

CERTIFICATION consumes this file as the executable proof catalog. It MUST NOT:

- infer PASS from `Existing evidence`;
- treat `ADMISSION_DECISION_PROOF` as a released runtime capability;
- certify one provider from another provider's tests;
- certify frontend realtime from fixtures without backend producer/recovery;
- certify Calendar sync while the released command remains throw-only;
- certify N8n D5 while required operator/reconciliation behavior has no governed executable proof;
- certify transaction/RLS/provider semantics from helper-level tests that bypass the production owner.
