---
document_id: WRK-PLAN-AUTOMATION-INTEGRATIONS
document_type: workstream-plan
status: active
owner: automation-integrations-team
applies_to:
  - backend
  - frontend
  - automation
  - integrations
  - p5
  - provider-effects
  - inbound-webhooks
  - calendar-sync
  - generated-contracts
  - runtime-evidence
evidence:
  - docs/workstreams/executions/automation-integrations/automation-integrations.spec.md
  - docs/product/automation.md
  - docs/product/integrations.md
  - docs/workstreams/teams/automation-integrations.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/contract-first-delivery.md
  - docs/workstreams/executions/backend-team-architecture-closure/
review_on:
  - automation-contract-change
  - rule-revision-change
  - trigger-action-vocabulary-change
  - provider-connection-change
  - provider-outcome-change
  - webhook-trust-boundary-change
  - calendar-sync-change
  - frontend-contract-change
  - messaging-retry-change
  - tenant-rls-change
baseline:
  ref: pull/160-head
  sha: 902bc9c5b39a003df3dce6673edc124984d2b398
---

# PLAN — P5 Automation & Integrations

## 1. Purpose

This PLAN executes all 128 requirements from `automation-integrations.spec.md` as a brownfield hardening and contract-convergence program. It does **not** authorize a greenfield rewrite of Automation or Integrations and it does not treat Domain types, mocks, fixtures, comments, or historical TAC status as production readiness.

The execution rule is:

```text
freeze exact candidate
→ resolve authority/source conflicts
→ classify production reachability
→ retain proven current mechanisms
→ close release-scoped gaps with the smallest owner-correct change
→ migrate stored/public contracts deliberately
→ run focused + production-runtime + negative evidence
→ certify each capability/consumer independently
```

Current source already contains real Rule/Execution paths, WorkManagement-triggered Automation, MoveItem target execution, N8n provider-effect settlement, Calendar Connection/secret handling, verified inbound Calendar webhook intake and Calendar reconciliation. The plan therefore hardens those paths and explicitly defers source areas that are not production-reachable.

## 2. Non-negotiable scope rules

- Automation and Integrations remain separate bounded contexts even though one team owns the workstream.
- Platform owns generic delivery/dedup/retry/tenant restoration/realtime transport; P5 consumes those contracts.
- Governance owns authorization policy; Billing owns capability/capacity semantics; target contexts own their mutations.
- No direct foreign-context EF/table/aggregate mutation is introduced.
- No provider SDK/model leaks into Automation Domain.
- No blind re-fire of an external provider effect after an unknown outcome.
- No Domain-only Schedule/Template/Agent/WebhookDelivery capability is presented as released merely because source/tests exist.
- No new service/project/workflow engine/global secret architecture is introduced without escalation.
- All evidence is exact-candidate evidence; source/test existence alone is not D4/D5.

## 2.1. Frozen implementation decisions

The implementation agent does not choose among alternatives for the following P5 contracts:

1. **Rule revision:** persist a semantic revision/config snapshot at Execution creation. Existing queued/running Executions continue their captured semantics; edits affect future Executions.
2. **Rule management surface:** implement create/list/detail/update/enable/disable/archive/soft-delete/restore. Disable retains Rule capacity; archive/delete release one slot; restore atomically re-consumes capacity.
3. **Conditions:** implement the production evaluator in P5.
4. **Multi-action:** implement ordered sequential Actions with durable `AutomationExecutionStep`; terminal failure stops dependent later Steps; retries resume the same Step/operation identity; no arbitrary DAG/parallel execution in P5.
5. **Release trigger set:** `ItemAssigned`, `ItemMovedToGroup`, `ItemCreated`. Other trigger discriminators remain unavailable.
6. **Release Action set:** `MoveItem` and ADR-008 N8n/Webhook provider effect. Other Action discriminators remain unavailable until separately admitted with executor evidence.
7. **Schedules/Templates/AI Agents/generic outbound WebhookSubscription product surface:** deferred from P5; enforce no API/frontend/runtime advertisement.
8. **Calendar provider surface:** Google and Microsoft/Outlook only. Implement provider-auth/install and callback authenticity using each real provider contract; request least-privilege scopes.
9. **Calendar manual sync:** implement; do not remove the requirement or retain a `NotImplementedException`.
10. **Realtime:** reuse existing Automation Execution Domain lifecycle facts and map them into a public/realtime schema; do not create a competing lifecycle. Both Automation and Integration progress recover from durable state after gaps.

Changing these decisions requires authority review and synchronized SPEC/PLAN/TESTS/CERT changes.

## 3. Canonical execution waves

```text
Wave 0 — authority + release-scope normalization
  AI-INV-001 / AI-INV-002 / AI-GOV-001
  classify stale TAC text, Domain-only capability and frontend/backend drift

Wave 1 — Rule/config/execution correctness
  AI-01 Rule lifecycle + immutable revision
  AI-02 trigger/condition/action contract
  AI-03 execution/idempotency/retry/history
  AI-05 target authorization + Billing handoffs

Wave 2 — Connection and provider-effect foundation
  AI-06 Connection/OAuth/secrets
  AI-07 outbound provider / N8n

Wave 3 — inbound provider + Calendar synchronization
  AI-08 webhook trust/dedup/tenant processing
  AI-09 sync/cursor/conflict/manual-sync admission

Wave 4 — consumer contract convergence
  AI-10 API/frontend/realtime
  AI-11 persistence/RLS/migrations
  AI-12 observability/recovery/readiness

Wave 5 — architecture + compatibility + certification
  AI-13 TAC/runtime-owner/negative evidence
  AI-14 mixed-version/handoff/final certification
  AI-GATE-001 .. AI-GATE-004
```

`AI-04` (Schedules/Templates/AI Agents) is a **non-reachability/defer track for P5**. The required work is to prove these Domain-only capabilities are not exposed by API/frontend/runtime and to record a future admission boundary. P5 does not implement them.

## 4. Current preparation posture

| Capability | Exact-candidate posture | PLAN treatment |
|---|---|---|
| Rule create/list/enable/disable | `PRODUCTION_REACHABLE / PARTIAL_GAP` | retain current path; normalize request/config contract, immutable revision and lifecycle surface |
| Rule update/archive/delete/restore | `DOMAIN_ONLY / PARTIAL_GAP` | implement the frozen P5 management surface; archive/delete release capacity, restore re-consumes capacity |
| Work event triggers | `PRODUCTION_REACHABLE / PARTIAL_GAP` | retain consumer/evaluator; close vocabulary/condition/revision gaps |
| Conditions | `DOMAIN_ONLY` | implement deterministic production evaluation in P5; false/skip remains distinct from evaluation failure |
| MoveItem action | `PRODUCTION_REACHABLE` | reference target-action pattern; retain port/ACL/target revalidation |
| N8n/Webhook action | `PRODUCTION_REACHABLE / OPERATIONAL_GAP` | retain ADR-008 protocol; close executable reconciliation/operator path |
| Schedule/Templates/AI Agents | `DOMAIN_ONLY / EXPLICIT_ADMISSION_REQUIRED` | DEFER from P5 and enforce non-reachability; future admission requires a new governed execution |
| Billing Rule capacity | `PRODUCTION_REACHABLE / ACCEPTED_DEBT` | retain transactional consume; resolve/accept release lifecycle debt |
| Calendar connect/disconnect | `PRODUCTION_REACHABLE / AUTH_GAP` | retain Connection/secret/binding semantics and implement real Google/Microsoft provider authorization with least-privilege scopes |
| Calendar verified webhook | `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT` | retain trust/dedup/tenant/reconcile chain; certify only real provider protocols implemented |
| Manual Calendar sync | `GAP_CONFIRMED` | implement the real command/runtime path; throw-only or fake-success final state is forbidden |
| Generic outbound webhook runtime | `DOMAIN_ONLY / LEGACY_BOUNDARY` | no release claim until explicit Application/API/worker admission |
| Automation frontend | `CONTRACT_DRIFT / PARTIAL` | align vocab/DTOs/repositories/demo state/realtime producer |
| Integrations frontend | `CONTRACT_DRIFT / PARTIAL` | align provider/status/endpoints to released backend surface |
| Automation realtime | `PARTIAL_PUBLIC_MAPPING_GAP` | map existing Domain lifecycle facts to the public/realtime contract and implement durable recovery |

## 5. Exact dependency/readiness targets

| P5 capability | Required dependency target |
|---|---|
| Rule management | Governance authorization D5; tenant/RLS D5; Billing capacity D4+ where used |
| event trigger consumption | producer event D4+; Platform messaging/dedup D5 |
| immutable Execution semantics | Rule/config contract D5; persistence/migration D5 |
| Work target action | WorkManagement public action D5; auth/idempotency D5 |
| N8n provider effect | messaging/dedup D5; ADR-008 provider classification D5; reconciliation operational path D4+ |
| Connection/secret | ManageIntegrations D5; secret/key-ring durability D5 |
| provider OAuth | session/security foundation D5; provider protocol D5 |
| Calendar webhook | exact provider authenticity protocol D5; RLS/messaging D5 |
| Calendar sync | Connection/binding D5; direction/conflict/cursor D4+ per provider |
| frontend authoring/management | producer contract D5; consumer integration D4+ |
| realtime execution | producer schema + Platform recovery D4+ per consumer |
| scheduled/template/agent | explicit product admission plus capability-specific dependency targets |

A blocked dependency keeps only that consumer/capability blocked; it does not lower or inflate unrelated P5 readiness.

## 6. Legacy TEST ID preservation contract

The canonical TESTS file retains the following pre-existing stable IDs and attaches them to the AIREQ traceability instead of deleting or silently renaming them:

```text
AI-TST-RULE-001
AI-TST-RULE-002
AI-TST-RULE-003
AI-TST-TRG-001
AI-TST-TRG-CON-001
AI-TST-TRG-ORDER-001
AI-TST-COND-001
AI-TST-COND-002
AI-TST-ACT-001
AI-TST-ACT-ARCH-001
AI-TST-ACT-ARCH-002
AI-TST-EXE-001
AI-TST-EXE-CONC-001
AI-TST-EXE-IDEMP-001
AI-TST-EXE-UNK-001
AI-TST-EXE-REC-001
AI-TST-EXE-HIST-001
AI-TST-AUTHZ-001
AI-TST-AUTHZ-002
AI-TST-AUTHZ-003
AI-TST-AUTHZ-004
AI-TST-CONN-001
AI-TST-CONN-AUTHZ-001
AI-TST-SEC-001
AI-TST-SEC-002
AI-TST-OUT-001
AI-TST-OUT-IDEMP-001
AI-TST-OUT-UNK-001
AI-TST-PROV-001
AI-TST-WH-SEC-001
AI-TST-WH-ROUTE-001
AI-TST-WH-IDEMP-001
AI-TST-WH-ORDER-001
AI-TST-WH-AUT-001
AI-TST-SYNC-001
AI-TST-SYNC-CUR-001
AI-TST-SYNC-BF-001
AI-TST-SYNC-DISC-001
AI-TST-MSG-001
AI-TST-POISON-001
AI-TST-OBS-001
AI-TST-RT-001
```

Additional tests may be added for uncovered AIREQ requirements. Existing IDs do not imply current PASS; they are stable trace identifiers only.

## 7. TAC AI-flow reuse matrix

| TAC flow | Current exact-candidate source path | PLAN work units | Preparation state | Exact-candidate rule |
|---|---|---|---|---|
| `AI-FLOW-01` Rule creation + Billing capacity | `CreateAutomationRuleCommand` → Billing capability/capacity → `AutomationRule.Create` | AI-01, AI-02, AI-05 | `REUSE_WITH_GAPS` | rerun config, auth, idempotency, capacity and revision proof |
| `AI-FLOW-02` Work fact → Automation process | Work integration event → MassTransit tenant/dedup → Automation consumer/evaluator → Execution + dispatch intent | AI-02, AI-03, AI-05 | `REUSE_WITH_GAPS` | rerun producer compatibility, duplicate/concurrency and condition/vocabulary proof |
| `AI-FLOW-03` Automation → Work target action | `AutomationMoveItemRequestedV1` → MoveItem consumer/use case → `IWorkActionPort` → target public capability | AI-03, AI-05, AI-13 | `PRODUCTION_REFERENCE` | rerun auth/scope/idempotency/technical-retry chain in production composition |
| `AI-FLOW-04` Automation → N8n | N8n dispatch intent → consumer-owned claim → prepare/effect/settle → N8n client | AI-03, AI-07, AI-12, AI-13 | `PRODUCTION_REFERENCE_WITH_OP_GAP` | preserve ADR-008 and prove stale/unknown reconciliation behavior |
| `AI-FLOW-05` Calendar connect | `ConnectCalendarCommand` → secret reference/version → Connection → Calendar binding | AI-06, AI-11, AI-13 | `PRODUCTION_REACHABLE`; historical status stale | rerun full pipeline, secret round-trip and restart/key-ring evidence |
| `AI-FLOW-06` Calendar disconnect | `DisconnectCalendarCommand` → binding deactivate → CAL-CONN-001 connection policy | AI-06, AI-09, AI-13 | `PRODUCTION_REACHABLE`; historical status stale | rerun resource auth, last-binding policy and cleanup outcome proof |
| `AI-FLOW-07` verified Calendar webhook | bounded HTTP → binding → signature → derived tenant → receipt/outbox → tenant consumer → Calendar reconcile | AI-08, AI-09, AI-11, AI-13 | `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT`; historical status stale | rerun hostile-input, provider-auth, dedup, RLS and semantic terminal proof |

Historical TAC evidence is reusable baseline only. Source-status text that says AI-FLOW-05/06/07 are still throw-only is `DOC_STALE` for this candidate.

## 8. Work-unit topology

The PLAN has **64 stable work-unit IDs**:

```text
4  inventory/governance units
56 capability units (14 lanes × INV/CORE/SEC/COMPAT)
4  aggregate release gates
```

Each AIREQ is owned by at least one work unit; gate work units intentionally overlap critical ranges.

## 9. Requirement → work-unit coverage

- `AI-INV-001` — AIREQ001–AIREQ004 — Freeze authority, source graph and bounded-context ownership
- `AI-INV-002` — AIREQ005–AIREQ007 — Classify production reachability and release scope
- `AI-GOV-001` — AIREQ008 — Enforce architecture-expansion stop gate
- `AI-GOV-002` — AIREQ126–AIREQ128 — Prepare cross-team handoff and exact-candidate certification contract
- `AI-01-INV-001` — AIREQ009–AIREQ010 — Rule lifecycle and immutable semantics — Rule identity, lifecycle and released surface
- `AI-01-CORE-001` — AIREQ011–AIREQ014 — Rule lifecycle and immutable semantics — Enable/edit/execution-revision semantics
- `AI-01-SEC-001` — AIREQ015 — Rule lifecycle and immutable semantics — Delete/archive/restore and Billing capacity lifecycle
- `AI-01-COMPAT-001` — AIREQ016 — Rule lifecycle and immutable semantics — Released Rule CRUD/API contract
- `AI-02-INV-001` — AIREQ017–AIREQ019 — Configuration, triggers, conditions and actions — Typed/versioned persisted configuration
- `AI-02-CORE-001` — AIREQ020–AIREQ021 — Configuration, triggers, conditions and actions — Condition runtime semantics
- `AI-02-SEC-001` — AIREQ022–AIREQ025 — Configuration, triggers, conditions and actions — Producer trigger and action executor parity
- `AI-02-COMPAT-001` — AIREQ026 — Configuration, triggers, conditions and actions — Multi-action model admission
- `AI-03-INV-001` — AIREQ027–AIREQ029 — Execution, idempotency, retry and history — Execution identity and provenance
- `AI-03-CORE-001` — AIREQ030–AIREQ032 — Execution, idempotency, retry and history — Execution state and durable success
- `AI-03-SEC-001` — AIREQ033–AIREQ036 — Execution, idempotency, retry and history — Failure taxonomy, retry budget and unknown outcomes
- `AI-03-COMPAT-001` — AIREQ037–AIREQ038 — Execution, idempotency, retry and history — Recursion bounds and product-safe history
- `AI-04-INV-001` — AIREQ039–AIREQ040 — Schedules, templates and AI-agent admission — Schedule semantic admission
- `AI-04-CORE-001` — AIREQ041–AIREQ042 — Schedules, templates and AI-agent admission — Scheduled occurrence identity and runtime reachability
- `AI-04-SEC-001` — AIREQ043–AIREQ044 — Schedules, templates and AI-agent admission — Template semantics and compatibility
- `AI-04-COMPAT-001` — AIREQ045–AIREQ046 — Schedules, templates and AI-agent admission — AI-agent capability admission
- `AI-05-INV-001` — AIREQ047–AIREQ048 — Cross-context actions, authorization and Billing — Async source fact and target revalidation boundary
- `AI-05-CORE-001` — AIREQ049–AIREQ051 — Cross-context actions, authorization and Billing — Background principal and permission revocation
- `AI-05-SEC-001` — AIREQ052–AIREQ053 — Cross-context actions, authorization and Billing — Billing capability/capacity lifecycle
- `AI-05-COMPAT-001` — AIREQ054 — Cross-context actions, authorization and Billing — Cross-context handoff lineage
- `AI-06-INV-001` — AIREQ055–AIREQ057 — Connections, provider authorization and secrets — Connection lifecycle and provider catalog
- `AI-06-CORE-001` — AIREQ058–AIREQ060 — Connections, provider authorization and secrets — Provider OAuth/install security
- `AI-06-SEC-001` — AIREQ061–AIREQ063 — Connections, provider authorization and secrets — Secret durability/versioning and restart safety
- `AI-06-COMPAT-001` — AIREQ064 — Connections, provider authorization and secrets — Connection state versus health/sync status
- `AI-07-INV-001` — AIREQ065–AIREQ067 — Outbound provider effects and N8n reference — Integration-owned outbound contract and operation identity
- `AI-07-CORE-001` — AIREQ068–AIREQ070 — Outbound provider effects and N8n reference — Provider outcome classification and retry ownership
- `AI-07-SEC-001` — AIREQ071–AIREQ073 — Outbound provider effects and N8n reference — Provider-effect claim lifecycle and stale residue
- `AI-07-COMPAT-001` — AIREQ074 — Outbound provider effects and N8n reference — Provider effect outside DB transaction
- `AI-08-INV-001` — AIREQ075–AIREQ076 — Inbound webhook trust boundary and reconciliation — Bounded raw HTTP and signature bytes
- `AI-08-CORE-001` — AIREQ077–AIREQ079 — Inbound webhook trust boundary and reconciliation — Trusted tenant routing and inbound identity claim
- `AI-08-SEC-001` — AIREQ080–AIREQ082 — Inbound webhook trust boundary and reconciliation — Tenant processing and terminal receipt semantics
- `AI-08-COMPAT-001` — AIREQ083–AIREQ084 — Inbound webhook trust boundary and reconciliation — Inbound/outbound boundary and provider-specific authenticity
- `AI-09-INV-001` — AIREQ085–AIREQ086 — Calendar sync, cursor, mapping and reconciliation — Sync direction and cursor commit semantics
- `AI-09-CORE-001` — AIREQ087–AIREQ089 — Calendar sync, cursor, mapping and reconciliation — Conflict policy and identity mapping
- `AI-09-SEC-001` — AIREQ090 — Calendar sync, cursor, mapping and reconciliation — Manual sync admission / remove throw path
- `AI-09-COMPAT-001` — AIREQ091–AIREQ092 — Calendar sync, cursor, mapping and reconciliation — Backfill/live overlap and disconnect semantics
- `AI-10-INV-001` — AIREQ093–AIREQ095 — API, frontend authoring and realtime consumers — Producer API authority and vocabulary normalization
- `AI-10-CORE-001` — AIREQ096–AIREQ098 — API, frontend authoring and realtime consumers — Production frontend repositories and supported Integrations surface
- `AI-10-SEC-001` — AIREQ099–AIREQ101 — API, frontend authoring and realtime consumers — Scoped query keys and realtime producer/recovery
- `AI-10-COMPAT-001` — AIREQ102 — API, frontend authoring and realtime consumers — OpenAPI/codegen and mixed-version rollout
- `AI-11-INV-001` — AIREQ103–AIREQ104 — Persistence, RLS, migrations and retention — Data ownership and RLS scope
- `AI-11-CORE-001` — AIREQ105–AIREQ106 — Persistence, RLS, migrations and retention — Technical secret/receipt state and migration proof
- `AI-11-SEC-001` — AIREQ107–AIREQ108 — Persistence, RLS, migrations and retention — Persisted JSON/discriminator and semantic uniqueness
- `AI-11-COMPAT-001` — AIREQ109–AIREQ110 — Persistence, RLS, migrations and retention — Soft delete, retention and no foreign cascades
- `AI-12-INV-001` — AIREQ111–AIREQ112 — Observability, backpressure and recovery operations — Semantic identity correlation
- `AI-12-CORE-001` — AIREQ113–AIREQ114 — Observability, backpressure and recovery operations — Redaction and actionable operational metrics
- `AI-12-SEC-001` — AIREQ115–AIREQ116 — Observability, backpressure and recovery operations — Provider isolation, fanout and recursion controls
- `AI-12-COMPAT-001` — AIREQ117–AIREQ118 — Observability, backpressure and recovery operations — Governed recovery and readiness truth
- `AI-13-INV-001` — AIREQ119 — Architecture, TAC and production-runtime proof — Reuse TAC AI-FLOW-01..07 with current source posture
- `AI-13-CORE-001` — AIREQ120–AIREQ121 — Architecture, TAC and production-runtime proof — Inherit Platform messaging/recovery contracts and architecture gates
- `AI-13-SEC-001` — AIREQ122 — Architecture, TAC and production-runtime proof — Production-runtime-owner verification
- `AI-13-COMPAT-001` — AIREQ123 — Architecture, TAC and production-runtime proof — Negative/failure evidence gate
- `AI-14-INV-001` — AIREQ124 — Compatibility, extraction, handoff and final certification — Queued/backlog/deployed-consumer compatibility
- `AI-14-CORE-001` — AIREQ125 — Compatibility, extraction, handoff and final certification — Service extraction remains deferred decision
- `AI-14-SEC-001` — AIREQ126–AIREQ127 — Compatibility, extraction, handoff and final certification — Cross-team handoff and readiness records
- `AI-14-COMPAT-001` — AIREQ128 — Compatibility, extraction, handoff and final certification — Exact candidate final certification
- `AI-GATE-001` — AIREQ009–AIREQ038 — Rule → trigger/action → execution release gate
- `AI-GATE-002` — AIREQ055–AIREQ084 — Connection → N8n → webhook trust/reliability gate
- `AI-GATE-003` — AIREQ085–AIREQ118 — Sync → frontend → data/ops convergence gate
- `AI-GATE-004` — AIREQ119–AIREQ128 — Architecture, compatibility and final exact-candidate certification gate

# Detailed execution work units

## AI-INV-001 — Freeze authority, source graph and bounded-context ownership

**Requirements:** `AIREQ001` — Authority precedence and conflict handling; `AIREQ002` — Automation and Integrations remain separate bounded contexts; `AIREQ003` — Platform mechanisms do not become P5 semantics; `AIREQ004` — Brownfield-first implementation

**Preparation posture:** `BASELINE_REQUIRED`  
**Execution disposition:** `RETAIN_AND_NORMALIZE`

**Known gaps/debts:** `AI-GAP-16` (Historical TAC dispositions stale)

### Objective

Pin the exact candidate and resolve authority/source conflicts before any implementation change.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Record candidate SHA, branch/PR reference and the active product/system/team authorities.
2. Inventory Automation, Integrations, Platform, Governance, Billing and target-context state/mutation ownership.
3. Mark historical TAC source-status statements that disagree with the candidate as DOC_STALE without changing the frozen flow IDs/invariants.
4. Reject duplicate abstractions when a current aggregate/port/consumer/adapter already owns the mechanism.

### Data / migration impact

None by default; this unit is inventory/governance only.

### Compatibility / rollout

No public contract change. Any discovered conflict becomes a classified work item before code moves.

### Dependencies / readiness

All later work units.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-ACT-ARCH-001`, `AI-TST-ACT-ARCH-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-001`, `AI-TST-ACT-ARCH-002`, `AI-TST-AIREQ-003`, `AI-TST-AIREQ-004`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-INV-002 — Classify production reachability and release scope

**Requirements:** `AIREQ005` — Exact-candidate evidence; `AIREQ006` — Production reachability classification; `AIREQ007` — Domain-only capability does not silently enter release scope

**Preparation posture:** `MIXED`  
**Execution disposition:** `CLASSIFY_AND_GATE`

**Known gaps/debts:** `AI-GAP-05` (Schedule/Templates/AI Agents domain-only), `AI-GAP-13` (Generic outbound webhook runtime uncertified)

### Objective

Create an exact-candidate capability inventory that distinguishes production-reachable behavior from Domain-only and deferred source.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Classify every Rule/Execution/Schedule/Template/Agent/Connection/provider/webhook/sync/frontend/realtime capability using the SPEC posture vocabulary.
2. For DOMAIN_ONLY or EXPLICIT_ADMISSION_REQUIRED capability, record explicit release exclusion and entry criteria.
3. Bind evidence to source/runtime/test execution rather than type or fixture existence.
4. Prevent deferred capabilities from contaminating release claims or frontend catalogs.

### Data / migration impact

None unless a deferred capability is intentionally admitted later.

### Compatibility / rollout

Release scope is additive only after admission; do not expose new API/event/provider values during inventory.

### Dependencies / readiness

AI-INV-001.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-RULE-001`, `AI-TST-SYNC-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-005`, `AI-TST-AIREQ-006`, `AI-TST-AIREQ-007`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-GOV-001 — Enforce architecture-expansion stop gate

**Requirements:** `AIREQ008` — No architecture expansion by convenience

**Preparation posture:** `GOVERNED`  
**Execution disposition:** `ESCALATE_ON_CHANGE`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Prevent convenience-driven service/framework/security architecture changes inside P5.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Treat new service/project, workflow/scripting engine, global provider SDK dependency, secret architecture or actor model as escalation.
2. Require an ADR/architecture approval before such change enters implementation.
3. Prefer existing modular-monolith boundaries and ports/adapters until extraction criteria are proven.

### Data / migration impact

Depends on the approved architecture decision if escalation occurs.

### Compatibility / rollout

Breaking architecture change is outside a local work unit and needs explicit rollout/rollback design.

### Dependencies / readiness

AI-INV-001.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-ACT-ARCH-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-008`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-GOV-002 — Prepare cross-team handoff and exact-candidate certification contract

**Requirements:** `AIREQ126` — Cross-team handoff is explicit; `AIREQ127` — Readiness is capability- and consumer-specific; `AIREQ128` — Final certification binds exact source, migrations, tests and CI

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `DEFINE_EVIDENCE_CONTRACT`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Define the evidence/handoff schema before implementation so no lane can self-certify from source existence.

### Current source authority / source under change

- `docs/delivery/contract-first-delivery.md`
- `docs/delivery/change-classification.md`
- `docs/delivery/migration-policy.md`
- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Use capability-specific readiness rather than one blanket P5 maturity value.
2. For every producer/consumer handoff capture current/target contract, owners, compatibility class, migration, actions, verification, readiness and rollback/forward-fix.
3. Require exact candidate SHA, migration head, runtime owner, executed test/CI evidence and unresolved blocker inventory.
4. Keep final status NOT_EVALUATED until TESTS and exact-candidate executions exist.

### Data / migration impact

Certification-only unless a lane requires migration.

### Compatibility / rollout

Handoffs must cover mixed-version consumers and queued messages when contract-affecting.

### Dependencies / readiness

All release gates.

### Existing evidence anchors

- `backend/tests/ci-proofs.json`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-126`, `AI-TST-AIREQ-127`, `AI-TST-AIREQ-128`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-01-INV-001 — Rule lifecycle and immutable semantics — Rule identity, lifecycle and released surface

**Requirements:** `AIREQ009` — Automation Rule stable identity and tenant scope; `AIREQ010` — Rule lifecycle is explicit

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Retain stable tenant-scoped Rule identity and make lifecycle semantics explicit.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`
- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRuleStatus.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationRuleValidator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/SetAutomationRuleEnabled/SetAutomationRuleEnabled.cs`
- `backend/src/Notrelix.API/Endpoints/Automation/Rules/MapRuleEndpoints.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain AccountId/WorkspaceId/RuleId as immutable ownership identity.
2. Define exact product meaning for Draft/Active/Disabled/Archived plus soft-delete/restore.
3. Inventory API/Application reachability for list/create/enable/disable versus update/archive/delete/restore/test; mark unsupported operations explicitly.
4. Preserve execution history across lifecycle changes.

### Data / migration impact

LIKELY for immutable revision/snapshot and lifecycle state if schema changes.

### Compatibility / rollout

Preserve current create/list/enable clients unless an explicit versioned migration is approved.

### Dependencies / readiness

Governance authorization D5; Billing capacity contract D4+; persistence/RLS available.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleLifecycleTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleActivationInvariantTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/CreateAutomationRulePipelineTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Billing/BillingCapacityFlowIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-RULE-001`, `AI-TST-RULE-002`, `AI-TST-RULE-003`.

Primary canonical TEST IDs: `AI-TST-AIREQ-009`, `AI-TST-AIREQ-010`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-01-CORE-001 — Rule lifecycle and immutable semantics — Enable/edit/execution-revision semantics

**Requirements:** `AIREQ011` — Enable is authoritative server validation; `AIREQ012` — Disable stops new executions without erasing history; `AIREQ013` — Edit behavior for queued/running executions is defined; `AIREQ014` — Execution binds to immutable rule semantics

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Close the mutable-Rule-at-dispatch gap so queued/running executions have deterministic semantics.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`
- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRuleStatus.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationRuleValidator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/SetAutomationRuleEnabled/SetAutomationRuleEnabled.cs`
- `backend/src/Notrelix.API/Endpoints/Automation/Rules/MapRuleEndpoints.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep server-side activation validation authoritative.
2. Introduce an immutable Rule semantic revision/snapshot contract distinct from aggregate optimistic Version.
3. Persist revision/snapshot identity on AutomationExecution at creation.
4. Change runtime action paths so execution semantics come from the execution-bound revision/snapshot, not a later mutable Rule edit.
5. Define disable/edit behavior for already-created executions and prove it.

### Data / migration impact

LIKELY for immutable revision/snapshot and lifecycle state if schema changes.

### Compatibility / rollout

Preserve current create/list/enable clients unless an explicit versioned migration is approved.

### Dependencies / readiness

Governance authorization D5; Billing capacity contract D4+; persistence/RLS available.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleLifecycleTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleActivationInvariantTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/CreateAutomationRulePipelineTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Billing/BillingCapacityFlowIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-RULE-001`, `AI-TST-RULE-002`, `AI-TST-RULE-003`.

Primary canonical TEST IDs: `AI-TST-RULE-001`, `AI-TST-RULE-002`, `AI-TST-AIREQ-013`, `AI-TST-RULE-003`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-01-SEC-001 — Rule lifecycle and immutable semantics — Delete/archive/restore and Billing capacity lifecycle

**Requirements:** `AIREQ015` — Rule deletion/archive and capacity release are coordinated

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Resolve or formally retain the accepted capacity-release debt without inventing plan logic in Automation.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`
- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRuleStatus.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationRuleValidator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/SetAutomationRuleEnabled/SetAutomationRuleEnabled.cs`
- `backend/src/Notrelix.API/Endpoints/Automation/Rules/MapRuleEndpoints.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Choose released delete/archive semantics.
2. Invoke Billing-owned capacity release/re-consumption contract using stable logical operation identity when lifecycle consumes/releases capacity.
3. Keep Billing as commercial decision owner; no PlanTier branching.
4. Define behavior for queued executions and references to deleted/archived Rules.

### Data / migration impact

LIKELY for immutable revision/snapshot and lifecycle state if schema changes.

### Compatibility / rollout

Preserve current create/list/enable clients unless an explicit versioned migration is approved.

### Dependencies / readiness

Governance authorization D5; Billing capacity contract D4+; persistence/RLS available.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleLifecycleTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleActivationInvariantTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/CreateAutomationRulePipelineTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Billing/BillingCapacityFlowIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-RULE-001`, `AI-TST-RULE-002`, `AI-TST-RULE-003`.

Primary canonical TEST IDs: `AI-TST-AIREQ-015`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-01-COMPAT-001 — Rule lifecycle and immutable semantics — Released Rule CRUD/API contract

**Requirements:** `AIREQ016` — Released Rule CRUD surface is deliberate and complete

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Make the actual released Rule surface explicit and producer-authoritative.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRule.cs`
- `backend/src/Notrelix.Domain/Automation/Rules/AutomationRuleStatus.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationRuleValidator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/SetAutomationRuleEnabled/SetAutomationRuleEnabled.cs`
- `backend/src/Notrelix.API/Endpoints/Automation/Rules/MapRuleEndpoints.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Align endpoint/resource DTOs with the deliberately supported operations.
2. Generate/update OpenAPI only for released operations.
3. Do not expose Domain methods as implied API features.
4. Record compatibility for existing create/list/enable clients.

### Data / migration impact

LIKELY for immutable revision/snapshot and lifecycle state if schema changes.

### Compatibility / rollout

Preserve current create/list/enable clients unless an explicit versioned migration is approved.

### Dependencies / readiness

Governance authorization D5; Billing capacity contract D4+; persistence/RLS available.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleLifecycleTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Rules/AutomationRuleActivationInvariantTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/CreateAutomationRulePipelineTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Billing/BillingCapacityFlowIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-RULE-001`, `AI-TST-RULE-002`, `AI-TST-RULE-003`.

Primary canonical TEST IDs: `AI-TST-AIREQ-016`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-02-INV-001 — Configuration, triggers, conditions and actions — Typed/versioned persisted configuration

**Requirements:** `AIREQ017` — Persisted automation configuration is typed and versioned; `AIREQ018` — Trigger and action configuration are independent contracts; `AIREQ019` — Persisted discriminator evolution is a contract and data change

**Preparation posture:** `PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-04` (Trigger/action vocabulary exceeds executors)

### Objective

Replace the semantic ambiguity of one shared Configuration blob with versioned trigger/condition/action contracts.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationTriggerDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationActionDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConditionDefinition.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRuleCommandValidator.cs`
- `backend/src/Notrelix.API/Contracts/Automation/Rules/Requests/CreateAutomationRuleRequest.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Define independent triggerConfig, optional condition config and actionConfig schema boundaries.
2. Preserve stable discriminators and explicit schemaVersion.
3. Add migration/reader policy for current persisted JSON.
4. Update create/update validators to validate each discriminator against its own schema.
5. Treat discriminator renames/default changes as data+contract migration.

### Data / migration impact

REQUIRED for request/persisted config schema or discriminator changes.

### Compatibility / rollout

Old stored rules and deployed web/mobile must remain readable during rollout; no silent reinterpretation.

### Dependencies / readiness

Rule lifecycle contract; producer event contracts for released triggers; target action contracts.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Configuration/AutomationConfigurationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Conditions/AutomationConditionDefinitionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationTriggerMatrixIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-COND-001`, `AI-TST-COND-002`, `AI-TST-ACT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-017`, `AI-TST-AIREQ-018`, `AI-TST-AIREQ-019`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-02-CORE-001 — Configuration, triggers, conditions and actions — Condition runtime semantics

**Requirements:** `AIREQ020` — Condition evaluation is deterministic; `AIREQ021` — Condition false is distinct from condition failure

**Preparation posture:** `PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-04` (Trigger/action vocabulary exceeds executors)

### Objective

Implement the released Condition evaluator end to end and prove false/skip, invalid configuration, unavailable facts, authorization failure and transient evaluation failure as distinct outcomes.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationTriggerDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationActionDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConditionDefinition.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRuleCommandValidator.cs`
- `backend/src/Notrelix.API/Contracts/Automation/Rules/Requests/CreateAutomationRuleRequest.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Define operators/value types/null/missing semantics and consistency point.
2. Build condition input only from trigger facts plus explicit fact-loading contracts.
3. Represent false as non-error skip; keep invalid/transient/auth failures distinct.
4. Add production evaluator integration before advertising Conditions.

### Data / migration impact

REQUIRED for request/persisted config schema or discriminator changes.

### Compatibility / rollout

Old stored rules and deployed web/mobile must remain readable during rollout; no silent reinterpretation.

### Dependencies / readiness

Rule lifecycle contract; producer event contracts for released triggers; target action contracts.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Configuration/AutomationConfigurationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Conditions/AutomationConditionDefinitionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationTriggerMatrixIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-COND-001`, `AI-TST-COND-002`, `AI-TST-ACT-001`.

Primary canonical TEST IDs: `AI-TST-COND-001`, `AI-TST-COND-002`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-02-SEC-001 — Configuration, triggers, conditions and actions — Producer trigger and action executor parity

**Requirements:** `AIREQ022` — Trigger identity follows producer contract; `AIREQ023` — Producer evolution includes Automation as a consumer; `AIREQ024` — Released trigger vocabulary matches runtime reachability; `AIREQ025` — Released action vocabulary matches an executor

**Preparation posture:** `PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-04` (Trigger/action vocabulary exceeds executors)

### Objective

Bind advertised vocabulary to real producer contracts and runtime executors.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationTriggerDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationActionDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConditionDefinition.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRuleCommandValidator.cs`
- `backend/src/Notrelix.API/Contracts/Automation/Rules/Requests/CreateAutomationRuleRequest.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Inventory every released trigger with producer event identity/version/scope.
2. Keep Automation in producer compatibility/backlog inventory.
3. Restrict advertised trigger catalog to production-reachable consumer/evaluator paths.
4. Restrict advertised action catalog to actions with config schema, target owner, authorization/idempotency semantics and real executor.
5. Preserve fail-closed behavior for unsupported action types until parity closes.

### Data / migration impact

REQUIRED for request/persisted config schema or discriminator changes.

### Compatibility / rollout

Old stored rules and deployed web/mobile must remain readable during rollout; no silent reinterpretation.

### Dependencies / readiness

Rule lifecycle contract; producer event contracts for released triggers; target action contracts.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Configuration/AutomationConfigurationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Conditions/AutomationConditionDefinitionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationTriggerMatrixIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-COND-001`, `AI-TST-COND-002`, `AI-TST-ACT-001`.

Primary canonical TEST IDs: `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-AIREQ-024`, `AI-TST-AIREQ-025`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-02-COMPAT-001 — Configuration, triggers, conditions and actions — Multi-action model admission

**Requirements:** `AIREQ026` — Multi-action ordering/dependency semantics are explicit

**Preparation posture:** `PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-04` (Trigger/action vocabulary exceeds executors)

### Objective

Prevent single-action source plus detached ExecutionStep entity from being misrepresented as workflow orchestration.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationTriggerDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationActionDefinition.cs`
- `backend/src/Notrelix.Domain/Automation/RulesEngine/AutomationConditionDefinition.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRuleCommandValidator.cs`
- `backend/src/Notrelix.API/Contracts/Automation/Rules/Requests/CreateAutomationRuleRequest.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep current single-action contract if multi-action is not product-admitted.
2. Only in a future separately admitted workstream, define ordered/parallel/dependency graph semantics before schema/API changes.
3. Attach step creation/order/state to production execution and define retry of prior successful steps.
4. Migrate stored Rule config only under explicit compatibility plan.

### Data / migration impact

REQUIRED for request/persisted config schema or discriminator changes.

### Compatibility / rollout

Old stored rules and deployed web/mobile must remain readable during rollout; no silent reinterpretation.

### Dependencies / readiness

Rule lifecycle contract; producer event contracts for released triggers; target action contracts.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Configuration/AutomationConfigurationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Conditions/AutomationConditionDefinitionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationTriggerMatrixIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-COND-001`, `AI-TST-COND-002`, `AI-TST-ACT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-026`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-03-INV-001 — Execution, idempotency, retry and history — Execution identity and provenance

**Requirements:** `AIREQ027` — Execution identity is logical and stable; `AIREQ028` — Concurrent duplicate creation is constrained durably; `AIREQ029` — Execution records semantic provenance

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Make one logical trigger occurrence produce one durable execution with sufficient immutable provenance.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecution.cs`
- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecutionStatus.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain/verify unique (RuleId, TriggerId) for event-triggered execution identity.
2. Handle concurrent duplicate creation by database authority, not pre-check alone.
3. Persist Rule revision/snapshot identity, source occurrence, scope, correlation/causation and safe input provenance.
4. Do not store provider secrets in payload/history.

### Data / migration impact

REQUIRED if execution revision/provenance/step schema changes.

### Compatibility / rollout

Queued intents and historical executions must remain interpretable by new workers.

### Dependencies / readiness

Platform messaging/idempotency D5 for async paths; target/provider contract readiness.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionRetryTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`, `AI-TST-EXE-REC-001`, `AI-TST-EXE-HIST-001`.

Primary canonical TEST IDs: `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-AIREQ-029`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-03-CORE-001 — Execution, idempotency, retry and history — Execution state and durable success

**Requirements:** `AIREQ030` — Execution lifecycle transitions are validated; `AIREQ031` — Action-step state is real product state only when attached to execution; `AIREQ032` — Success follows durable effect evidence

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Keep lifecycle transitions explicit and settle success only after the owning effect is durable.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecution.cs`
- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecutionStatus.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Preserve validated Queued→Running→terminal transitions.
2. If steps are released, make them created/ordered/persisted by production path.
3. For Work target action, settle only after target use case succeeds durably.
4. For provider actions, use provider outcome contract/claim settlement before success.

### Data / migration impact

REQUIRED if execution revision/provenance/step schema changes.

### Compatibility / rollout

Queued intents and historical executions must remain interpretable by new workers.

### Dependencies / readiness

Platform messaging/idempotency D5 for async paths; target/provider contract readiness.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionRetryTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`, `AI-TST-EXE-REC-001`, `AI-TST-EXE-HIST-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-030`, `AI-TST-AIREQ-031`, `AI-TST-AIREQ-032`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-03-SEC-001 — Execution, idempotency, retry and history — Failure taxonomy, retry budget and unknown outcomes

**Requirements:** `AIREQ033` — Target/business failure and technical failure remain distinct; `AIREQ034` — Transport retry and Automation business retry have one coherent budget; `AIREQ035` — Retry preserves operation identity; `AIREQ036` — Unknown external outcome is never blindly re-fired

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Prevent duplicate effects and retry storms across target, Automation and Platform layers.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecution.cs`
- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecutionStatus.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Normalize business rejection, auth/not-found/conflict, technical retryable and provider unknown outcomes separately.
2. Assign one end-to-end retry owner/budget per path.
3. Reuse stable operation identity on every safe retry.
4. Never convert timeout/reset/indeterminate outcome into safe retry without provider proof or reconciliation.

### Data / migration impact

REQUIRED if execution revision/provenance/step schema changes.

### Compatibility / rollout

Queued intents and historical executions must remain interpretable by new workers.

### Dependencies / readiness

Platform messaging/idempotency D5 for async paths; target/provider contract readiness.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionRetryTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`, `AI-TST-EXE-REC-001`, `AI-TST-EXE-HIST-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-033`, `AI-TST-AIREQ-034`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-03-COMPAT-001 — Execution, idempotency, retry and history — Recursion bounds and product-safe history

**Requirements:** `AIREQ037` — Causation and recursion are bounded; `AIREQ038` — Execution history is safe product evidence

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Make chained automation bounded and execution history stable, actionable and redacted.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecution.cs`
- `backend/src/Notrelix.Domain/Automation/Executions/AutomationExecutionStatus.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Application/Features/Automation/Events/N8nAutomationRuleEvaluator.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Propagate causation/origin/depth identities.
2. Define max depth/self-trigger/rule-chain suppression policy.
3. Store product-semantic history distinct from broker attempts.
4. Redact sensitive payload/provider details from user-visible history.

### Data / migration impact

REQUIRED if execution revision/provenance/step schema changes.

### Compatibility / rollout

Queued intents and historical executions must remain interpretable by new workers.

### Dependencies / readiness

Platform messaging/idempotency D5 for async paths; target/provider contract readiness.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Executions/AutomationExecutionRetryTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`, `AI-TST-EXE-REC-001`, `AI-TST-EXE-HIST-001`.

Primary canonical TEST IDs: `AI-TST-EXE-REC-001`, `AI-TST-EXE-HIST-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-04-INV-001 — Schedules, templates and AI-agent admission — Schedule semantic admission

**Requirements:** `AIREQ039` — Schedule intent is Automation product state; `AIREQ040` — Timezone, DST and missed-fire policy are explicit

**Preparation posture:** `DOMAIN_ONLY / EXPLICIT_ADMISSION_REQUIRED`  
**Execution disposition:** `DEFER_AND_ENFORCE_NON_REACHABILITY`

**Known gaps/debts:** `AI-GAP-05` (Schedule/Templates/AI Agents domain-only)

### Objective

Keep this capability outside P5 production reachability. Preserve Domain/persistence compatibility, add architecture/API/frontend guards preventing advertisement, and record a separate future admission dependency rather than building runtime behavior in this package.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduledJob.cs`
- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduleDefinition.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/ScheduledJobConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/Templates/AutomationTemplate.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgent.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgentRun.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Do NOT add Application/API/frontend/background-worker reachability in P5.
2. Add/retain architecture tests proving the Domain-only capability is not registered or advertised as a released P5 feature.
3. Keep persisted/source compatibility for existing rows/types; do not delete Domain code merely to make the release surface smaller.
4. Record a future admission handoff naming product semantics, authorization/background principal, runtime owner, migrations and required tests before any later implementation starts.

### Data / migration impact

NONE while deferred; REQUIRED if capability is admitted.

### Compatibility / rollout

No public compatibility promise until admitted; existing persisted rows still need safe reading.

### Dependencies / readiness

Explicit product admission plus Platform scheduling/AI/provider readiness as applicable.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduleDefinitionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduledJobTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Templates/AutomationTemplateTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Agents/AiAgentTests.cs`

Primary canonical TEST IDs: `AI-TST-AIREQ-039`, `AI-TST-AIREQ-040`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-04-CORE-001 — Schedules, templates and AI-agent admission — Scheduled occurrence identity and runtime reachability

**Requirements:** `AIREQ041` — Scheduled occurrence identity prevents duplicate execution; `AIREQ042` — Scheduler production reachability is required before release

**Preparation posture:** `DOMAIN_ONLY / EXPLICIT_ADMISSION_REQUIRED`  
**Execution disposition:** `DEFER_AND_ENFORCE_NON_REACHABILITY`

**Known gaps/debts:** `AI-GAP-05` (Schedule/Templates/AI Agents domain-only)

### Objective

Keep this capability outside P5 production reachability. Preserve Domain/persistence compatibility, add architecture/API/frontend guards preventing advertisement, and record a separate future admission dependency rather than building runtime behavior in this package.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduledJob.cs`
- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduleDefinition.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/ScheduledJobConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/Templates/AutomationTemplate.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgent.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgentRun.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Do NOT add Application/API/frontend/background-worker reachability in P5.
2. Add/retain architecture tests proving the Domain-only capability is not registered or advertised as a released P5 feature.
3. Keep persisted/source compatibility for existing rows/types; do not delete Domain code merely to make the release surface smaller.
4. Record a future admission handoff naming product semantics, authorization/background principal, runtime owner, migrations and required tests before any later implementation starts.

### Data / migration impact

NONE while deferred; REQUIRED if capability is admitted.

### Compatibility / rollout

No public compatibility promise until admitted; existing persisted rows still need safe reading.

### Dependencies / readiness

Explicit product admission plus Platform scheduling/AI/provider readiness as applicable.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduleDefinitionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduledJobTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Templates/AutomationTemplateTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Agents/AiAgentTests.cs`

Primary canonical TEST IDs: `AI-TST-AIREQ-041`, `AI-TST-AIREQ-042`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-04-SEC-001 — Schedules, templates and AI-agent admission — Template semantics and compatibility

**Requirements:** `AIREQ043` — Template is creation input, not live shared authority; `AIREQ044` — Template schemas participate in compatibility

**Preparation posture:** `DOMAIN_ONLY / EXPLICIT_ADMISSION_REQUIRED`  
**Execution disposition:** `DEFER_AND_ENFORCE_NON_REACHABILITY`

**Known gaps/debts:** `AI-GAP-05` (Schedule/Templates/AI Agents domain-only)

### Objective

Keep this capability outside P5 production reachability. Preserve Domain/persistence compatibility, add architecture/API/frontend guards preventing advertisement, and record a separate future admission dependency rather than building runtime behavior in this package.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduledJob.cs`
- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduleDefinition.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/ScheduledJobConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/Templates/AutomationTemplate.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgent.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgentRun.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Do NOT add Application/API/frontend/background-worker reachability in P5.
2. Add/retain architecture tests proving the Domain-only capability is not registered or advertised as a released P5 feature.
3. Keep persisted/source compatibility for existing rows/types; do not delete Domain code merely to make the release surface smaller.
4. Record a future admission handoff naming product semantics, authorization/background principal, runtime owner, migrations and required tests before any later implementation starts.

### Data / migration impact

NONE while deferred; REQUIRED if capability is admitted.

### Compatibility / rollout

No public compatibility promise until admitted; existing persisted rows still need safe reading.

### Dependencies / readiness

Explicit product admission plus Platform scheduling/AI/provider readiness as applicable.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduleDefinitionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduledJobTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Templates/AutomationTemplateTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Agents/AiAgentTests.cs`

Primary canonical TEST IDs: `AI-TST-AIREQ-043`, `AI-TST-AIREQ-044`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-04-COMPAT-001 — Schedules, templates and AI-agent admission — AI-agent capability admission

**Requirements:** `AIREQ045` — AI Agent requires explicit product admission; `AIREQ046` — No hidden AI runtime capability

**Preparation posture:** `DOMAIN_ONLY / EXPLICIT_ADMISSION_REQUIRED`  
**Execution disposition:** `DEFER_AND_ENFORCE_NON_REACHABILITY`

**Known gaps/debts:** `AI-GAP-05` (Schedule/Templates/AI Agents domain-only)

### Objective

Keep this capability outside P5 production reachability. Preserve Domain/persistence compatibility, add architecture/API/frontend guards preventing advertisement, and record a separate future admission dependency rather than building runtime behavior in this package.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduledJob.cs`
- `backend/src/Notrelix.Domain/Automation/Scheduled/ScheduleDefinition.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/ScheduledJobConfiguration.cs`
- `backend/src/Notrelix.Domain/Automation/Templates/AutomationTemplate.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgent.cs`
- `backend/src/Notrelix.Domain/Automation/Agents/AiAgentRun.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Do NOT add Application/API/frontend/background-worker reachability in P5.
2. Add/retain architecture tests proving the Domain-only capability is not registered or advertised as a released P5 feature.
3. Keep persisted/source compatibility for existing rows/types; do not delete Domain code merely to make the release surface smaller.
4. Record a future admission handoff naming product semantics, authorization/background principal, runtime owner, migrations and required tests before any later implementation starts.

### Data / migration impact

NONE while deferred; REQUIRED if capability is admitted.

### Compatibility / rollout

No public compatibility promise until admitted; existing persisted rows still need safe reading.

### Dependencies / readiness

Explicit product admission plus Platform scheduling/AI/provider readiness as applicable.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduleDefinitionTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Scheduled/ScheduledJobTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Templates/AutomationTemplateTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Automation/Agents/AiAgentTests.cs`

Primary canonical TEST IDs: `AI-TST-AIREQ-045`, `AI-TST-AIREQ-046`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-05-INV-001 — Cross-context actions, authorization and Billing — Async source fact and target revalidation boundary

**Requirements:** `AIREQ047` — Source transactions do not execute Automation side effects inline; `AIREQ048` — Target bounded context revalidates every action

**Preparation posture:** `PRODUCTION_REACHABLE / REVIEW_REQUIRED`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-07` (Automation permission vocabulary coarse/inconsistent), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Preserve source commit independence and target-context authority.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Ports/WorkManagement/IWorkActionPort.cs`
- `backend/src/Notrelix.Application/Features/Automation/CrossContext/WorkManagement/WorkActionAcl.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Automation/WorkManagement/WorkItemActionAdapter.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep WorkManagement committed fact → Automation consumer asynchronous through outbox/broker.
2. Never execute provider/target side effects inside source aggregate transaction.
3. Target actions must revalidate current target state, authorization, scope and concurrency.
4. Use current MoveItem port/adapter path as the reference.

### Data / migration impact

NONE unless permission/resource/action or Billing operation contract changes.

### Compatibility / rollout

Target/public contracts and producer events require mixed-version review.

### Dependencies / readiness

Governance authorization; WorkManagement public action; Billing capacity; Platform messaging.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationWorkActionChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-ACT-ARCH-001`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-AUTHZ-003`, `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AIREQ-047`, `AI-TST-AIREQ-048`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-05-CORE-001 — Cross-context actions, authorization and Billing — Background principal and permission revocation

**Requirements:** `AIREQ049` — Background Automation principal is explicit; `AIREQ050` — Permission revocation is honored at execution time; `AIREQ051` — Cross-context actions use public capability contracts

**Preparation posture:** `PRODUCTION_REACHABLE / REVIEW_REQUIRED`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-07` (Automation permission vocabulary coarse/inconsistent), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Normalize Automation principal/resource/action semantics instead of incidental permission reuse.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Ports/WorkManagement/IWorkActionPort.cs`
- `backend/src/Notrelix.Application/Features/Automation/CrossContext/WorkManagement/WorkActionAcl.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Automation/WorkManagement/WorkItemActionAdapter.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Define principal model per released action: initiating actor, bounded service principal or delegated capability.
2. Review current ManageWorkspaceSettings/ViewBoard use against Governance product actions and create specific action vocabulary only if product authority requires it.
3. Honor permission/membership/resource revocation at action time.
4. Keep target mutations behind public Application contracts/ports.

### Data / migration impact

NONE unless permission/resource/action or Billing operation contract changes.

### Compatibility / rollout

Target/public contracts and producer events require mixed-version review.

### Dependencies / readiness

Governance authorization; WorkManagement public action; Billing capacity; Platform messaging.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationWorkActionChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-ACT-ARCH-001`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-AUTHZ-003`, `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-ACT-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-05-SEC-001 — Cross-context actions, authorization and Billing — Billing capability/capacity lifecycle

**Requirements:** `AIREQ052` — Billing supplies capability/capacity facts, not plan branching; `AIREQ053` — Capacity consumption and release semantics are operation-safe

**Preparation posture:** `PRODUCTION_REACHABLE / REVIEW_REQUIRED`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-07` (Automation permission vocabulary coarse/inconsistent), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Keep Billing ownership while making Rule capacity operations retry/lifecycle safe.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Ports/WorkManagement/IWorkActionPort.cs`
- `backend/src/Notrelix.Application/Features/Automation/CrossContext/WorkManagement/WorkActionAcl.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Automation/WorkManagement/WorkItemActionAdapter.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Continue querying Billing capability facts rather than PlanTier.
2. Keep create consume in the request transaction with stable logical operation identity.
3. Wire release/re-consume only when Rule lifecycle semantics require it.
4. Test concurrent capacity race and lifecycle replay.

### Data / migration impact

NONE unless permission/resource/action or Billing operation contract changes.

### Compatibility / rollout

Target/public contracts and producer events require mixed-version review.

### Dependencies / readiness

Governance authorization; WorkManagement public action; Billing capacity; Platform messaging.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationWorkActionChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-ACT-ARCH-001`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-AUTHZ-003`, `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AIREQ-052`, `AI-TST-AIREQ-053`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-05-COMPAT-001 — Cross-context actions, authorization and Billing — Cross-context handoff lineage

**Requirements:** `AIREQ054` — Cross-context handoffs preserve source identity and ownership

**Preparation posture:** `PRODUCTION_REACHABLE / REVIEW_REQUIRED`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-07` (Automation permission vocabulary coarse/inconsistent), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

Preserve source/target identity and ownership across every P5 edge.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/AutomationMoveItemUseCase.cs`
- `backend/src/Notrelix.Application/Features/Automation/Ports/WorkManagement/IWorkActionPort.cs`
- `backend/src/Notrelix.Application/Features/Automation/CrossContext/WorkManagement/WorkActionAcl.cs`
- `backend/src/Notrelix.Infrastructure/CrossContext/Automation/WorkManagement/WorkItemActionAdapter.cs`
- `backend/src/Notrelix.Application/Features/Automation/Rules/Commands/CreateAutomationRule/CreateAutomationRule.cs`
- `backend/src/Notrelix.Application/Features/Governance/Authorization/AccessPolicyEngine.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Carry source event/correlation/causation and stable execution/operation identities.
2. Document target contract owner and consumer port.
3. Avoid leaking target aggregate/EF/provider types into Automation.
4. Record readiness and compatibility for each producer/action pair.

### Data / migration impact

NONE unless permission/resource/action or Billing operation contract changes.

### Compatibility / rollout

Target/public contracts and producer events require mixed-version review.

### Dependencies / readiness

Governance authorization; WorkManagement public action; Billing capacity; Platform messaging.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationWorkActionChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationMoveItemExecutorCompositionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Governance/GovernanceResourcePermissionFlowTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-ACT-ARCH-001`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-AUTHZ-003`, `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AIREQ-054`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-06-INV-001 — Connections, provider authorization and secrets — Connection lifecycle and provider catalog

**Requirements:** `AIREQ055` — Integration Connection has explicit lifecycle; `AIREQ056` — Provider catalog is canonical and consumer-compatible; `AIREQ057` — Connection installation is Account/Workspace scoped and governed

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-09` (Provider OAuth/install flow incomplete), `AI-GAP-10` (Provider/catalog vocabulary drift)

### Objective

Make Connection/provider vocabulary canonical and tenant/governance scoped.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnection.cs`
- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnectionStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/DisconnectCalendar/DisconnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Public/Secrets/IIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Security/Secrets/DataProtectionIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain IntegrationConnection lifecycle semantics and Calendar binding distinction.
2. Define a canonical released provider catalog with capability metadata.
3. Reconcile generic provider enum, Calendar provider enum and frontend catalog; do not publish unsupported values.
4. Keep ManageIntegrations authorization over workspace/binding resource.

### Data / migration impact

POSSIBLE for connection/secret/provider catalog changes; OAuth state likely new persistence only in a future separately admitted workstream.

### Compatibility / rollout

Existing Calendar connections/secrets must remain readable; provider catalog changes are public contract changes.

### Dependencies / readiness

Governance ManageIntegrations; secret/key-ring mechanism; provider contract.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationConnectionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarConnectionFlowIntegrationTests.cs`
- `backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/IntegrationConnectionRevokedConsumerTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-CONN-001`, `AI-TST-CONN-AUTHZ-001`, `AI-TST-SEC-001`, `AI-TST-SEC-002`.

Primary canonical TEST IDs: `AI-TST-CONN-001`, `AI-TST-AIREQ-056`, `AI-TST-CONN-AUTHZ-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-06-CORE-001 — Connections, provider authorization and secrets — Provider OAuth/install security

**Requirements:** `AIREQ058` — Integration OAuth is separate from Identity OAuth; `AIREQ059` — Provider authorization flow has state/PKCE/callback security where required; `AIREQ060` — Reusable secrets live behind opaque references

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-09` (Provider OAuth/install flow incomplete), `AI-GAP-10` (Provider/catalog vocabulary drift)

### Objective

Replace raw-token install as a broad product claim with a truthful provider authorization contract.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnection.cs`
- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnectionStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/DisconnectCalendar/DisconnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Public/Secrets/IIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Security/Secrets/DataProtectionIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep Integration OAuth separate from Identity OAuth.
2. For providers requiring OAuth, define state, PKCE, redirect/callback, expiry, actor/workspace binding and token exchange.
3. Until implemented, classify raw access-token Calendar connect as internal/bootstrap contract, not complete OAuth.
4. Store reusable secret through IIntegrationSecretStore opaque reference only.

### Data / migration impact

POSSIBLE for connection/secret/provider catalog changes; OAuth state likely new persistence only in a future separately admitted workstream.

### Compatibility / rollout

Existing Calendar connections/secrets must remain readable; provider catalog changes are public contract changes.

### Dependencies / readiness

Governance ManageIntegrations; secret/key-ring mechanism; provider contract.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationConnectionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarConnectionFlowIntegrationTests.cs`
- `backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/IntegrationConnectionRevokedConsumerTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-CONN-001`, `AI-TST-CONN-AUTHZ-001`, `AI-TST-SEC-001`, `AI-TST-SEC-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-058`, `AI-TST-AIREQ-059`, `AI-TST-SEC-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-06-SEC-001 — Connections, provider authorization and secrets — Secret durability/versioning and restart safety

**Requirements:** `AIREQ061` — Physical secret storage is restart-safe; `AIREQ062` — Secret versions and current pointers are durable authority; `AIREQ063` — Disconnect, local secret retirement and provider revocation are distinct

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-09` (Provider OAuth/install flow incomplete), `AI-GAP-10` (Provider/catalog vocabulary drift)

### Objective

Prove secret authority survives restart and cleanup semantics are separate from Connection lifecycle.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnection.cs`
- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnectionStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/DisconnectCalendar/DisconnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Public/Secrets/IIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Security/Secrets/DataProtectionIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Persist DataProtection key ring durably in production or use approved external secret mechanism.
2. Keep IntegrationSecretVersion as durable reference/version authority and CurrentSecretRef as non-persisted pointer.
3. Prove reconnect/rotation increments version and round-trips secret reference.
4. Keep local revoke/provider remote revoke/connection disconnect outcome-classified and idempotent.

### Data / migration impact

POSSIBLE for connection/secret/provider catalog changes; OAuth state likely new persistence only in a future separately admitted workstream.

### Compatibility / rollout

Existing Calendar connections/secrets must remain readable; provider catalog changes are public contract changes.

### Dependencies / readiness

Governance ManageIntegrations; secret/key-ring mechanism; provider contract.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationConnectionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarConnectionFlowIntegrationTests.cs`
- `backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/IntegrationConnectionRevokedConsumerTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-CONN-001`, `AI-TST-CONN-AUTHZ-001`, `AI-TST-SEC-001`, `AI-TST-SEC-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-061`, `AI-TST-SEC-002`, `AI-TST-AIREQ-063`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-06-COMPAT-001 — Connections, provider authorization and secrets — Connection state versus health/sync status

**Requirements:** `AIREQ064` — Connection state and sync/provider health remain distinct

**Preparation posture:** `PRODUCTION_REACHABLE / PARTIAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-09` (Provider OAuth/install flow incomplete), `AI-GAP-10` (Provider/catalog vocabulary drift)

### Objective

Prevent provider/transient sync state from corrupting product Connection lifecycle.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnection.cs`
- `backend/src/Notrelix.Domain/Integrations/Connections/IntegrationConnectionStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/ConnectCalendar/ConnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/DisconnectCalendar/DisconnectCalendar.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Public/Secrets/IIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Security/Secrets/DataProtectionIntegrationSecretStore.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep Active/Expired/Revoked/Error semantics deliberate.
2. Do not mark a connection revoked from one transient provider failure.
3. Represent sync lag/provider availability through separate health/operational facts where needed.
4. Align UI status vocabulary to canonical backend meaning.

### Data / migration impact

POSSIBLE for connection/secret/provider catalog changes; OAuth state likely new persistence only in a future separately admitted workstream.

### Compatibility / rollout

Existing Calendar connections/secrets must remain readable; provider catalog changes are public contract changes.

### Dependencies / readiness

Governance ManageIntegrations; secret/key-ring mechanism; provider contract.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationConnectionTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarConnectionFlowIntegrationTests.cs`
- `backend/tests/Notrelix.Infrastructure.Tests/Messaging/Consumers/IntegrationConnectionRevokedConsumerTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-CONN-001`, `AI-TST-CONN-AUTHZ-001`, `AI-TST-SEC-001`, `AI-TST-SEC-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-064`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-07-INV-001 — Outbound provider effects and N8n reference — Integration-owned outbound contract and operation identity

**Requirements:** `AIREQ065` — Outbound provider operation uses an Integration-owned contract; `AIREQ066` — Provider anti-corruption layer translates types and errors; `AIREQ067` — Outbound operation has stable logical idempotency identity

**Preparation posture:** `PRODUCTION_REACHABLE / OPERATIONAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Keep Automation intent separate from provider adapter semantics and give every external effect stable identity.

### Current source authority / source under change

- `backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/IProviderEffectClaimStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs`
- `docs/operations/recovery-and-data-safety.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain Automation→Integrations public action boundary for N8n.
2. Keep provider DTO/error types inside Integrations/Infrastructure ACL.
3. Use ExecutionId or another explicit business operation id as provider action identity; never worker attempt id.
4. Document provider idempotency support or reconciliation fallback.

### Data / migration impact

NONE for ADR-008-preserving hardening; REQUIRED if durable claim/execution shape changes.

### Compatibility / rollout

Older queued N8n intents must preserve ADR-008 settlement semantics.

### Dependencies / readiness

Platform messaging/dedup; Integrations provider adapter; ADR-008.

### Existing evidence anchors

- `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`
- `backend/tests/Notrelix.Application.Tests/Features/Automation/Executions/N8nDispatchUseCaseTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OUT-001`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`, `AI-TST-PROV-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-065`, `AI-TST-PROV-001`, `AI-TST-OUT-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-07-CORE-001 — Outbound provider effects and N8n reference — Provider outcome classification and retry ownership

**Requirements:** `AIREQ068` — Timeout/rate-limit behavior follows provider evidence; `AIREQ069` — One end-to-end retry budget owns provider re-attempts; `AIREQ070` — Unknown provider outcome has an executable reconciliation path

**Preparation posture:** `PRODUCTION_REACHABLE / OPERATIONAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Make safe-to-retry versus unknown evidence executable and operational.

### Current source authority / source under change

- `backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/IProviderEffectClaimStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs`
- `docs/operations/recovery-and-data-safety.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep ADR-008 classification matrix authoritative for N8n.
2. Preserve one provider attempt per consumer call; no adapter-internal retry.
3. Treat 408/429/5xx/reset/timeout according to current conservative provider evidence.
4. Route UnknownOutcome to durable reconciliation-required terminal state; safe RetryableFailure alone may redeliver.
5. Define executable reconciliation owner/path, not only text.

### Data / migration impact

NONE for ADR-008-preserving hardening; REQUIRED if durable claim/execution shape changes.

### Compatibility / rollout

Older queued N8n intents must preserve ADR-008 settlement semantics.

### Dependencies / readiness

Platform messaging/dedup; Integrations provider adapter; ADR-008.

### Existing evidence anchors

- `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`
- `backend/tests/Notrelix.Application.Tests/Features/Automation/Executions/N8nDispatchUseCaseTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OUT-001`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`, `AI-TST-PROV-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-068`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-07-SEC-001 — Outbound provider effects and N8n reference — Provider-effect claim lifecycle and stale residue

**Requirements:** `AIREQ071` — ADR-008 is authoritative for current N8n settlement; `AIREQ072` — Provider-effect claim freshness does not grant re-fire authority; `AIREQ073` — Stale queued provider claim is governed recovery debt

**Preparation posture:** `PRODUCTION_REACHABLE / OPERATIONAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Preserve prepare/effect/settle safety and close operational stale-claim debt without granting blind re-fire.

### Current source authority / source under change

- `backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/IProviderEffectClaimStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs`
- `docs/operations/recovery-and-data-safety.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep consumer-owned Tx1 → out-of-tx effect → Tx2 protocol.
2. Keep fresh Processing duplicate as no-op and stale Running as reconciliation terminal.
3. For stale Queued+Processing, implement/operate a governed sweep decision path that preserves lineage and never auto re-fires provider.
4. Fail closed if claim transition affected-row proof is lost.

### Data / migration impact

NONE for ADR-008-preserving hardening; REQUIRED if durable claim/execution shape changes.

### Compatibility / rollout

Older queued N8n intents must preserve ADR-008 settlement semantics.

### Dependencies / readiness

Platform messaging/dedup; Integrations provider adapter; ADR-008.

### Existing evidence anchors

- `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`
- `backend/tests/Notrelix.Application.Tests/Features/Automation/Executions/N8nDispatchUseCaseTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OUT-001`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`, `AI-TST-PROV-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-071`, `AI-TST-AIREQ-072`, `AI-TST-AIREQ-073`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-07-COMPAT-001 — Outbound provider effects and N8n reference — Provider effect outside DB transaction

**Requirements:** `AIREQ074` — External provider effect runs outside the database transaction

**Preparation posture:** `PRODUCTION_REACHABLE / OPERATIONAL_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Protect database resources and transaction semantics around external IO.

### Current source authority / source under change

- `backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md`
- `backend/src/Notrelix.Application/Features/Automation/Executions/Services/N8nDispatchUseCase.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/IProviderEffectClaimStore.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/MessageDeduplicationStore.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Providers/N8nClient.cs`
- `docs/operations/recovery-and-data-safety.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep provider HTTP call outside database transaction.
2. Persist durable intent before external effect and outcome after effect.
3. Do not hold RLS/data-session transaction across provider latency.
4. Rerun production-composition durability tests after any filter/transaction topology change.

### Data / migration impact

NONE for ADR-008-preserving hardening; REQUIRED if durable claim/execution shape changes.

### Compatibility / rollout

Older queued N8n intents must preserve ADR-008 settlement semantics.

### Dependencies / readiness

Platform messaging/dedup; Integrations provider adapter; ADR-008.

### Existing evidence anchors

- `backend/tests/Notrelix.Infrastructure.Tests/Integrations/Providers/N8nClientTests.cs`
- `backend/tests/Notrelix.Application.Tests/Features/Automation/Executions/N8nDispatchUseCaseTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/AutomationN8nDurabilityIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OUT-001`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`, `AI-TST-PROV-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-074`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-08-INV-001 — Inbound webhook trust boundary and reconciliation — Bounded raw HTTP and signature bytes

**Requirements:** `AIREQ075` — Webhook raw HTTP boundary is bounded before trust; `AIREQ076` — Signature verification uses the exact provider-defined bytes

**Preparation posture:** `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-13` (Generic outbound webhook runtime uncertified), `AI-GAP-14` (Provider-specific webhook authenticity not proven)

### Objective

Keep the webhook endpoint as a hostile-input boundary before tenant/business trust.

### Current source authority / source under change

- `backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs`
- `backend/src/Notrelix.API/Middleware/WebhookBoundedBodyReader.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookBindingResolver.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookVerifier.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookIntake.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain media-type allowlist, bounded exact raw-byte read, empty/malformed rejection and timeout behavior.
2. Verify signatures over exact provider-defined bytes and freshness window before payload trust.
3. Do not log raw secret/body on rejection.
4. Keep provider callback anonymous at session layer but signature-authenticated at provider boundary.

### Data / migration impact

POSSIBLE for receipt/provider binding/schema changes.

### Compatibility / rollout

Provider callbacks and queued processing messages must remain compatible through rollout.

### Dependencies / readiness

API hostile-input controls; trusted binding; RLS; Platform outbox/consumer delivery.

### Existing evidence anchors

- `backend/tests/Notrelix.API.Tests/Contracts/CalendarWebhookHttpContractTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookIntakeIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookDataSessionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-WH-ORDER-001`, `AI-TST-WH-AUT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-075`, `AI-TST-WH-SEC-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-08-CORE-001 — Inbound webhook trust boundary and reconciliation — Trusted tenant routing and inbound identity claim

**Requirements:** `AIREQ077` — Tenant routing derives from trusted binding, never payload; `AIREQ078` — Inbound provider delivery identity is scoped correctly; `AIREQ079` — Inbound claim and durable processing intent are atomic

**Preparation posture:** `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-13` (Generic outbound webhook runtime uncertified), `AI-GAP-14` (Provider-specific webhook authenticity not proven)

### Objective

Derive tenant from trusted binding and claim provider delivery atomically with downstream intent.

### Current source authority / source under change

- `backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs`
- `backend/src/Notrelix.API/Middleware/WebhookBoundedBodyReader.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookBindingResolver.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookVerifier.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookIntake.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Resolve active CalendarIntegration binding by unguessable/stable webhook path and provider match.
2. Never accept AccountId/WorkspaceId/ConnectionId from webhook JSON as authority.
3. Dedup by the provider contract scope: current Calendar path uses (connection, provider, external event id).
4. Persist Captured receipt and enqueue processing intent in the same request transaction/outbox.

### Data / migration impact

POSSIBLE for receipt/provider binding/schema changes.

### Compatibility / rollout

Provider callbacks and queued processing messages must remain compatible through rollout.

### Dependencies / readiness

API hostile-input controls; trusted binding; RLS; Platform outbox/consumer delivery.

### Existing evidence anchors

- `backend/tests/Notrelix.API.Tests/Contracts/CalendarWebhookHttpContractTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookIntakeIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookDataSessionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-WH-ORDER-001`, `AI-TST-WH-AUT-001`.

Primary canonical TEST IDs: `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-AIREQ-079`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-08-SEC-001 — Inbound webhook trust boundary and reconciliation — Tenant processing and terminal receipt semantics

**Requirements:** `AIREQ080` — Tenant-scoped semantic processing occurs after commit; `AIREQ081` — Receipt becomes processed only after semantic reconciliation; `AIREQ082` — Malformed/conflicting webhook payload fails closed

**Preparation posture:** `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-13` (Generic outbound webhook runtime uncertified), `AI-GAP-14` (Provider-specific webhook authenticity not proven)

### Objective

Process only after commit under restored tenant/RLS and fail closed on invalid/conflicting semantic mappings.

### Current source authority / source under change

- `backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs`
- `backend/src/Notrelix.API/Middleware/WebhookBoundedBodyReader.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookBindingResolver.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookVerifier.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookIntake.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Restore tenant from trusted envelope before decrypt/read/reconcile.
2. Validate verified external event identity against decrypted payload.
3. Reconcile CalendarEvent/Link idempotently.
4. Mark receipt Processed only after semantic commit; malformed/conflict becomes terminal failure, not success.

### Data / migration impact

POSSIBLE for receipt/provider binding/schema changes.

### Compatibility / rollout

Provider callbacks and queued processing messages must remain compatible through rollout.

### Dependencies / readiness

API hostile-input controls; trusted binding; RLS; Platform outbox/consumer delivery.

### Existing evidence anchors

- `backend/tests/Notrelix.API.Tests/Contracts/CalendarWebhookHttpContractTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookIntakeIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookDataSessionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-WH-ORDER-001`, `AI-TST-WH-AUT-001`.

Primary canonical TEST IDs: `AI-TST-WH-AUT-001`, `AI-TST-AIREQ-081`, `AI-TST-AIREQ-082`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-08-COMPAT-001 — Inbound webhook trust boundary and reconciliation — Inbound/outbound boundary and provider-specific authenticity

**Requirements:** `AIREQ083` — Inbound receipt is not outbound WebhookDelivery; `AIREQ084` — Provider-specific verification support is truthful

**Preparation posture:** `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-13` (Generic outbound webhook runtime uncertified), `AI-GAP-14` (Provider-specific webhook authenticity not proven)

### Objective

Avoid semantic misclassification and overclaiming Google/Microsoft protocol support.

### Current source authority / source under change

- `backend/src/Notrelix.API/Endpoints/Integrations/Calendar/CalendarEndpoints.cs`
- `backend/src/Notrelix.API/Middleware/WebhookBoundedBodyReader.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/HandleCalendarWebhook/HandleCalendarWebhook.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookBindingResolver.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookVerifier.cs`
- `backend/src/Notrelix.Infrastructure/Integrations/Webhooks/CalendarWebhookIntake.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep Infrastructure inbound receipt separate from Domain outbound WebhookDelivery.
2. Keep InboundWebhookEvent legacy/no-growth unless product authority promotes it.
3. For each advertised provider, implement its real verification/subscription contract or label current HMAC as internal/test provider contract.
4. Add provider-specific fixtures/contract tests before certification.

### Data / migration impact

POSSIBLE for receipt/provider binding/schema changes.

### Compatibility / rollout

Provider callbacks and queued processing messages must remain compatible through rollout.

### Dependencies / readiness

API hostile-input controls; trusted binding; RLS; Platform outbox/consumer delivery.

### Existing evidence anchors

- `backend/tests/Notrelix.API.Tests/Contracts/CalendarWebhookHttpContractTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookIntakeIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Integrations/CalendarWebhookDataSessionIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-WH-ORDER-001`, `AI-TST-WH-AUT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-083`, `AI-TST-AIREQ-084`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-09-INV-001 — Calendar sync, cursor, mapping and reconciliation — Sync direction and cursor commit semantics

**Requirements:** `AIREQ085` — Sync direction and source-of-truth are explicit; `AIREQ086` — Sync cursor advances after durable successful processing

**Preparation posture:** `PARTIAL / MANUAL_SYNC_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-12` (Manual Calendar sync stub)

### Objective

Make source-of-truth/direction and progress cursor behavior explicit.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarIntegration.cs`
- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarEvent.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/IntegrationSyncCursor.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/SyncStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/TriggerCalendarSync/TriggerCalendarSync.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain CalendarSyncDirection as Integration semantics.
2. Define push/pull/two-way authority and unsupported directions per provider.
3. Advance IntegrationSyncCursor only after durable successful batch/effect.
4. Do not use cursor as business truth.

### Data / migration impact

REQUIRED if cursor/mapping/manual-sync persistence changes.

### Compatibility / rollout

Existing links/cursors/backfill state must survive deployment and restart.

### Dependencies / readiness

Connection/binding; provider mapping; durable DB/RLS; explicit conflict policy.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Calendar/CalendarIntegrationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationSyncCursorTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`, `AI-TST-SYNC-BF-001`, `AI-TST-SYNC-DISC-001`.

Primary canonical TEST IDs: `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-09-CORE-001 — Calendar sync, cursor, mapping and reconciliation — Conflict policy and identity mapping

**Requirements:** `AIREQ087` — Sync conflict policy is product/provider semantics; `AIREQ088` — Mappings preserve internal and external identities; `AIREQ089` — Calendar integration is mapping, not WorkManagement calendar truth

**Preparation posture:** `PARTIAL / MANUAL_SYNC_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-12` (Manual Calendar sync stub)

### Objective

Preserve Notrelix ownership while mapping external calendar identities.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarIntegration.cs`
- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarEvent.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/IntegrationSyncCursor.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/SyncStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/TriggerCalendarSync/TriggerCalendarSync.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Define conflicts using stable internal ResourceRef + external event id + ETag/fingerprint.
2. Preserve both identities in CalendarEvent/CalendarEventLink.
3. Never make external Calendar provider owner of WorkManagement data by mapping convenience.
4. Keep calendar integration as mapping/projection boundary.

### Data / migration impact

REQUIRED if cursor/mapping/manual-sync persistence changes.

### Compatibility / rollout

Existing links/cursors/backfill state must survive deployment and restart.

### Dependencies / readiness

Connection/binding; provider mapping; durable DB/RLS; explicit conflict policy.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Calendar/CalendarIntegrationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationSyncCursorTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`, `AI-TST-SYNC-BF-001`, `AI-TST-SYNC-DISC-001`.

Primary canonical TEST IDs: `AI-TST-WH-ORDER-001`, `AI-TST-AIREQ-088`, `AI-TST-AIREQ-089`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-09-SEC-001 — Calendar sync, cursor, mapping and reconciliation — Manual sync admission / remove throw path

**Requirements:** `AIREQ090` — Manual Calendar sync is production-reachable

**Preparation posture:** `PARTIAL / MANUAL_SYNC_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-12` (Manual Calendar sync stub)

### Objective

Do not advertise a command path that still throws NotImplementedException.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarIntegration.cs`
- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarEvent.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/IntegrationSyncCursor.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/SyncStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/TriggerCalendarSync/TriggerCalendarSync.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Implement the manual-sync command through a real sync service/job with canonical authorization and tenant scope.
2. Assign a stable logical sync operation identity and enforce one retry owner.
3. Apply declared sync direction/conflict policy and advance the durable cursor only after successful durable reconciliation.
4. Persist/query explicit retryable versus terminal outcomes; remove `NotImplementedException`.
5. No fake success or unsupported final branch.

### Data / migration impact

REQUIRED if cursor/mapping/manual-sync persistence changes.

### Compatibility / rollout

Existing links/cursors/backfill state must survive deployment and restart.

### Dependencies / readiness

Connection/binding; provider mapping; durable DB/RLS; explicit conflict policy.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Calendar/CalendarIntegrationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationSyncCursorTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`, `AI-TST-SYNC-BF-001`, `AI-TST-SYNC-DISC-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-090`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-09-COMPAT-001 — Calendar sync, cursor, mapping and reconciliation — Backfill/live overlap and disconnect semantics

**Requirements:** `AIREQ091` — Backfill and live processing converge without duplication; `AIREQ092` — Provider deletion/disconnect semantics do not delete Notrelix resources implicitly

**Preparation posture:** `PARTIAL / MANUAL_SYNC_GAP`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-12` (Manual Calendar sync stub)

### Objective

Make resync/backfill and disconnect safe against duplication or foreign resource deletion.

### Current source authority / source under change

- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarIntegration.cs`
- `backend/src/Notrelix.Domain/Integrations/Calendar/CalendarEvent.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/IntegrationSyncCursor.cs`
- `backend/src/Notrelix.Domain/Integrations/Sync/SyncStatus.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Commands/TriggerCalendarSync/TriggerCalendarSync.cs`
- `backend/src/Notrelix.Application/Features/Integrations/Calendar/Processing/CalendarWebhookProcessingUseCase.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Use stable mapping/operation identity when backfill overlaps webhook/live changes.
2. Define conflict resolution and cutover.
3. Disconnect/deactivate integration must not delete Notrelix-owned business resources.
4. Queued work after disconnect must fail/skip/reconcile under explicit policy.

### Data / migration impact

REQUIRED if cursor/mapping/manual-sync persistence changes.

### Compatibility / rollout

Existing links/cursors/backfill state must survive deployment and restart.

### Dependencies / readiness

Connection/binding; provider mapping; durable DB/RLS; explicit conflict policy.

### Existing evidence anchors

- `backend/tests/Notrelix.Domain.Tests/Integrations/Calendar/CalendarIntegrationTests.cs`
- `backend/tests/Notrelix.Domain.Tests/Integrations/Connections/IntegrationSyncCursorTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`, `AI-TST-SYNC-BF-001`, `AI-TST-SYNC-DISC-001`.

Primary canonical TEST IDs: `AI-TST-SYNC-BF-001`, `AI-TST-SYNC-DISC-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-10-INV-001 — API, frontend authoring and realtime consumers — Producer API authority and vocabulary normalization

**Requirements:** `AIREQ093` — Backend/public contract is the automation/integration API authority; `AIREQ094` — Frontend contract drift is a blocking compatibility defect; `AIREQ095` — Automation authoring vocabulary is generated/shared deliberately

**Preparation posture:** `CONTRACT_DRIFT / PARTIAL`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-06` (Automation frontend not contract-aligned/wired), `AI-GAP-10` (Provider/catalog vocabulary drift), `AI-GAP-15` (Execution lifecycle facts exist but public/realtime mapping is missing)

### Objective

Eliminate independent backend/frontend vocabularies and define one producer-authoritative contract.

### Current source authority / source under change

- `frontend/packages/product/automation/core/src/types/index.ts`
- `frontend/packages/product/automation/state/src/data/repositories.ts`
- `frontend/packages/product/automation/state/src/commands/rule-commands.ts`
- `frontend/packages/product/automation/state/src/query/keys.ts`
- `frontend/packages/product/automation/state/src/realtime/execution-adapter.ts`
- `frontend/packages/product/automation/web/src/components/automations-tab.tsx`
- `frontend/packages/features/integrations/src/core/api/integrations.service.ts`
- `frontend/packages/features/integrations/src/core/query/keys.ts`
- `frontend/packages/features/integrations/src/core/types/integrations.ts`
- `backend/contracts/openapi/notrelix.v1.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Treat backend public API/event schema as semantic producer source.
2. Replace card-era FE unions with generated/shared released trigger/action/status/config metadata.
3. Align Create Rule request with independent trigger/action configuration.
4. Classify compatibility/migration for existing FE source and stored rows; no indefinite ad-hoc translation.

### Data / migration impact

CONTRACT migration; backend DB migration only when producer schema changes.

### Compatibility / rollout

Backend/web/mobile deployment overlap must be explicitly supported.

### Dependencies / readiness

Producer OpenAPI/contracts; query/realtime Platform foundation; app composition.

### Existing evidence anchors

- `frontend/packages/product/automation/core/src/__tests__/automation.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-query-keys.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-state.unit.test.ts`
- `frontend/packages/product/automation/web/src/components/__tests__/automations-tab.interaction.component.test.tsx`
- `frontend/packages/features/integrations/src/__tests__/integrations.unit.test.ts`

Legacy stable TEST IDs to preserve/route: `AI-TST-RT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-093`, `AI-TST-AIREQ-094`, `AI-TST-AIREQ-095`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-10-CORE-001 — API, frontend authoring and realtime consumers — Production frontend repositories and supported Integrations surface

**Requirements:** `AIREQ096` — Frontend Automation repository has a real production adapter; `AIREQ097` — Demo fixture data is not production server state; `AIREQ098` — Integrations frontend exposes only backend-supported capabilities

**Preparation posture:** `CONTRACT_DRIFT / PARTIAL`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-06` (Automation frontend not contract-aligned/wired), `AI-GAP-10` (Provider/catalog vocabulary drift), `AI-GAP-15` (Execution lifecycle facts exist but public/realtime mapping is missing)

### Objective

Wire real production data adapters and remove unsupported/demo capability claims.

### Current source authority / source under change

- `frontend/packages/product/automation/core/src/types/index.ts`
- `frontend/packages/product/automation/state/src/data/repositories.ts`
- `frontend/packages/product/automation/state/src/commands/rule-commands.ts`
- `frontend/packages/product/automation/state/src/query/keys.ts`
- `frontend/packages/product/automation/state/src/realtime/execution-adapter.ts`
- `frontend/packages/product/automation/web/src/components/automations-tab.tsx`
- `frontend/packages/features/integrations/src/core/api/integrations.service.ts`
- `frontend/packages/features/integrations/src/core/query/keys.ts`
- `frontend/packages/features/integrations/src/core/types/integrations.ts`
- `backend/contracts/openapi/notrelix.v1.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Implement/bind AutomationRule/Execution repository adapters to generated API client at app composition.
2. Keep fake repositories only in testing/story composition.
3. Remove component defaultRules fallback from production behavior or confine it to stories.
4. Align Integrations provider/status/endpoints to backend-supported Calendar/connection contracts; do not expose unsupported generic webhook CRUD.

### Data / migration impact

CONTRACT migration; backend DB migration only when producer schema changes.

### Compatibility / rollout

Backend/web/mobile deployment overlap must be explicitly supported.

### Dependencies / readiness

Producer OpenAPI/contracts; query/realtime Platform foundation; app composition.

### Existing evidence anchors

- `frontend/packages/product/automation/core/src/__tests__/automation.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-query-keys.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-state.unit.test.ts`
- `frontend/packages/product/automation/web/src/components/__tests__/automations-tab.interaction.component.test.tsx`
- `frontend/packages/features/integrations/src/__tests__/integrations.unit.test.ts`

Legacy stable TEST IDs to preserve/route: `AI-TST-RT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-096`, `AI-TST-AIREQ-097`, `AI-TST-AIREQ-098`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-10-SEC-001 — API, frontend authoring and realtime consumers — Scoped query keys and realtime producer/recovery

**Requirements:** `AIREQ099` — Automation/Integrations query keys use canonical scope partitioning; `AIREQ100` — Realtime execution status requires a real producer contract; `AIREQ101` — Realtime gap recovery converges from durable execution truth

**Preparation posture:** `CONTRACT_DRIFT / PARTIAL`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-06` (Automation frontend not contract-aligned/wired), `AI-GAP-10` (Provider/catalog vocabulary drift), `AI-GAP-15` (Execution lifecycle facts exist but public/realtime mapping is missing)

### Objective

Make UI tenant-safe and realtime recoverable from durable truth.

### Current source authority / source under change

- `frontend/packages/product/automation/core/src/types/index.ts`
- `frontend/packages/product/automation/state/src/data/repositories.ts`
- `frontend/packages/product/automation/state/src/commands/rule-commands.ts`
- `frontend/packages/product/automation/state/src/query/keys.ts`
- `frontend/packages/product/automation/state/src/realtime/execution-adapter.ts`
- `frontend/packages/product/automation/web/src/components/automations-tab.tsx`
- `frontend/packages/features/integrations/src/core/api/integrations.service.ts`
- `frontend/packages/features/integrations/src/core/query/keys.ts`
- `frontend/packages/features/integrations/src/core/types/integrations.ts`
- `backend/contracts/openapi/notrelix.v1.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Use canonical account/workspace query-key helpers for both Automation and Integrations.
2. Prove Account A→B→A and Workspace switch isolation.
3. Add backend/public producer contract for automation.execution.* only if realtime is released.
4. On gap/reconnect, reload durable execution/history, reconcile sequence checkpoint, then resume delivery.

### Data / migration impact

CONTRACT migration; backend DB migration only when producer schema changes.

### Compatibility / rollout

Backend/web/mobile deployment overlap must be explicitly supported.

### Dependencies / readiness

Producer OpenAPI/contracts; query/realtime Platform foundation; app composition.

### Existing evidence anchors

- `frontend/packages/product/automation/core/src/__tests__/automation.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-query-keys.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-state.unit.test.ts`
- `frontend/packages/product/automation/web/src/components/__tests__/automations-tab.interaction.component.test.tsx`
- `frontend/packages/features/integrations/src/__tests__/integrations.unit.test.ts`

Legacy stable TEST IDs to preserve/route: `AI-TST-RT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-099`, `AI-TST-AIREQ-100`, `AI-TST-RT-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-10-COMPAT-001 — API, frontend authoring and realtime consumers — OpenAPI/codegen and mixed-version rollout

**Requirements:** `AIREQ102` — OpenAPI/codegen and mixed-version behavior are gated

**Preparation posture:** `CONTRACT_DRIFT / PARTIAL`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-06` (Automation frontend not contract-aligned/wired), `AI-GAP-10` (Provider/catalog vocabulary drift), `AI-GAP-15` (Execution lifecycle facts exist but public/realtime mapping is missing)

### Objective

Gate producer/consumer drift across backend, web, mobile and workers.

### Current source authority / source under change

- `frontend/packages/product/automation/core/src/types/index.ts`
- `frontend/packages/product/automation/state/src/data/repositories.ts`
- `frontend/packages/product/automation/state/src/commands/rule-commands.ts`
- `frontend/packages/product/automation/state/src/query/keys.ts`
- `frontend/packages/product/automation/state/src/realtime/execution-adapter.ts`
- `frontend/packages/product/automation/web/src/components/automations-tab.tsx`
- `frontend/packages/features/integrations/src/core/api/integrations.service.ts`
- `frontend/packages/features/integrations/src/core/query/keys.ts`
- `frontend/packages/features/integrations/src/core/types/integrations.ts`
- `backend/contracts/openapi/notrelix.v1.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Regenerate deterministic OpenAPI/generated clients after contract changes.
2. Run semantic consumer tests, not compile-only.
3. Define old/new backend-web/mobile/worker compatibility for discriminator/status/request changes.
4. Do not patch generated frontend output manually.

### Data / migration impact

CONTRACT migration; backend DB migration only when producer schema changes.

### Compatibility / rollout

Backend/web/mobile deployment overlap must be explicitly supported.

### Dependencies / readiness

Producer OpenAPI/contracts; query/realtime Platform foundation; app composition.

### Existing evidence anchors

- `frontend/packages/product/automation/core/src/__tests__/automation.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-query-keys.unit.test.ts`
- `frontend/packages/product/automation/state/src/__tests__/automation-state.unit.test.ts`
- `frontend/packages/product/automation/web/src/components/__tests__/automations-tab.interaction.component.test.tsx`
- `frontend/packages/features/integrations/src/__tests__/integrations.unit.test.ts`

Legacy stable TEST IDs to preserve/route: `AI-TST-RT-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-102`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-11-INV-001 — Persistence, RLS, migrations and retention — Data ownership and RLS scope

**Requirements:** `AIREQ103` — Automation and Integrations own only their persistence; `AIREQ104` — Workspace-scoped tables remain under RLS defense-in-depth

**Preparation posture:** `IMPLEMENTED_UNCERTIFIED / MIGRATION_SENSITIVE`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Keep P5 writes inside owned schemas and prove tenant/RLS enforcement in request and worker paths.

### Current source authority / source under change

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationSyncCursorConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookSubscriptionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookDeliveryConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Integrations/InboundWebhookReceipt.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Retain automation/integration schema ownership.
2. Keep target product mutations behind public contracts rather than EF navigation.
3. Verify RLS policies for workspace-scoped Automation/Integrations tables.
4. Exercise worker tenant restoration before EF work.

### Data / migration impact

REQUIRED by definition for schema/index/RLS/retention changes.

### Compatibility / rollout

Schema rollout must preserve old/new reader safety as classified.

### Dependencies / readiness

Migration/RLS foundation; exact supported baseline.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-ACT-ARCH-001`, `AI-TST-AUTHZ-004`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-11-CORE-001 — Persistence, RLS, migrations and retention — Technical secret/receipt state and migration proof

**Requirements:** `AIREQ105` — Technical secret/receipt state has constrained access paths; `AIREQ106` — Migration evidence includes clean database and supported upgrade

**Preparation posture:** `IMPLEMENTED_UNCERTIFIED / MIGRATION_SENSITIVE`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Constrain technical state and make schema changes deployable.

### Current source authority / source under change

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationSyncCursorConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookSubscriptionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookDeliveryConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Integrations/InboundWebhookReceipt.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep secret blob and inbound receipt accessible only through trusted opaque references/bindings.
2. No general API/query surface over protected payload or raw secret blob.
3. For every schema change, prove clean DB and supported upgrade from declared baseline.
4. Run pending-model-drift check against production design-time model.

### Data / migration impact

REQUIRED by definition for schema/index/RLS/retention changes.

### Compatibility / rollout

Schema rollout must preserve old/new reader safety as classified.

### Dependencies / readiness

Migration/RLS foundation; exact supported baseline.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AIREQ-105`, `AI-TST-AIREQ-106`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-11-SEC-001 — Persistence, RLS, migrations and retention — Persisted JSON/discriminator and semantic uniqueness

**Requirements:** `AIREQ107` — Persisted JSON/discriminator changes are migration-sensitive; `AIREQ108` — Concurrency constraints match semantic identities

**Preparation posture:** `IMPLEMENTED_UNCERTIFIED / MIGRATION_SENSITIVE`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Make stored contracts migratable and enforce logical uniqueness in the database.

### Current source authority / source under change

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationSyncCursorConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookSubscriptionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookDeliveryConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Integrations/InboundWebhookReceipt.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Version/migrate Rule config and provider mapping keys deliberately.
2. Use dual-reader/upcast/fail-safe behavior during mixed-version windows as required.
3. Preserve execution unique Rule+Trigger identity and webhook receipt conditional insert authority.
4. Add connection/binding uniqueness constraints only when product semantics require one-of-many/one-per-provider behavior.

### Data / migration impact

REQUIRED by definition for schema/index/RLS/retention changes.

### Compatibility / rollout

Schema rollout must preserve old/new reader safety as classified.

### Dependencies / readiness

Migration/RLS foundation; exact supported baseline.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AIREQ-107`, `AI-TST-AIREQ-108`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-11-COMPAT-001 — Persistence, RLS, migrations and retention — Soft delete, retention and no foreign cascades

**Requirements:** `AIREQ109` — Soft-delete/retention behavior is explicit; `AIREQ110` — No cross-context cascade semantics by database convenience

**Preparation posture:** `IMPLEMENTED_UNCERTIFIED / MIGRATION_SENSITIVE`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Keep lifecycle deletion local and preserve required history/audit.

### Current source authority / source under change

- `backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationExecutionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Automation/AutomationRuleConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationConnectionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/IntegrationSyncCursorConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookSubscriptionConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Configurations/Integrations/WebhookDeliveryConfiguration.cs`
- `backend/src/Notrelix.Infrastructure/Data/Integrations/InboundWebhookReceipt.cs`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Define retention for rules/executions/templates/connections/calendar mappings.
2. Ensure deleted state excluded from normal operations but history kept as required.
3. Review FK delete behaviors so P5 cannot cascade into foreign-context business state.
4. Cross-context cleanup is an explicit command/event/port, never DB cascade convenience.

### Data / migration impact

REQUIRED by definition for schema/index/RLS/retention changes.

### Compatibility / rollout

Schema rollout must preserve old/new reader safety as classified.

### Dependencies / readiness

Migration/RLS foundation; exact supported baseline.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/RlsRuntimeEnforcementTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Resiliency/MigrationResiliencyTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-AUTHZ-004`.

Primary canonical TEST IDs: `AI-TST-AIREQ-109`, `AI-TST-AIREQ-110`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-12-INV-001 — Observability, backpressure and recovery operations — Semantic identity correlation

**Requirements:** `AIREQ111` — Correlation traces the semantic chain without conflating identities; `AIREQ112` — Message/provider/operation/correlation identities remain distinct

**Preparation posture:** `PARTIAL / HARDEN_EXISTING`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Make the source→execution→target/provider chain traceable without identity conflation.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Integrations/CalendarWebhookProcessingRequestedConsumer.cs`
- `docs/operations/recovery-and-data-safety.md`
- `docs/operations/observability.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Propagate EventId/SourceEventId/RuleId/ExecutionId/OperationId/ConnectionId/CorrelationId/CausationId distinctly.
2. Emit stable low-cardinality dimensions where feasible.
3. Do not reuse provider event id as broker event id or worker attempt id.
4. Preserve lineage in recovery tooling.

### Data / migration impact

NONE unless new durable recovery state is introduced.

### Compatibility / rollout

Telemetry field changes must not become semantic contract substitutes.

### Dependencies / readiness

Observability infrastructure; runbook/recovery ownership.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/PipelineTelemetryIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`, `AI-TST-POISON-001`.

Primary canonical TEST IDs: `AI-TST-OBS-001`, `AI-TST-MSG-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-12-CORE-001 — Observability, backpressure and recovery operations — Redaction and actionable operational metrics

**Requirements:** `AIREQ113` — Telemetry and history redact secrets and unsafe payloads; `AIREQ114` — Operational metrics expose backlog and failure semantics

**Preparation posture:** `PARTIAL / HARDEN_EXISTING`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Expose useful P5 diagnostics without leaking credentials or raw unsafe payloads.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Integrations/CalendarWebhookProcessingRequestedConsumer.cs`
- `docs/operations/recovery-and-data-safety.md`
- `docs/operations/observability.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Centralize redaction of access tokens, webhook secrets, protected payloads and provider credentials.
2. Measure execution latency/status/retry, messaging backlog, stale claim/reconciliation, webhook verification, sync lag and provider failures.
3. Keep product history distinct from transport logs.
4. Add alerts/SLO hooks only for released capabilities.

### Data / migration impact

NONE unless new durable recovery state is introduced.

### Compatibility / rollout

Telemetry field changes must not become semantic contract substitutes.

### Dependencies / readiness

Observability infrastructure; runbook/recovery ownership.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/PipelineTelemetryIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`, `AI-TST-POISON-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-113`, `AI-TST-AIREQ-114`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-12-SEC-001 — Observability, backpressure and recovery operations — Provider isolation, fanout and recursion controls

**Requirements:** `AIREQ115` — Provider backpressure is isolated and observable; `AIREQ116` — Fanout and recursion have capacity controls

**Preparation posture:** `PARTIAL / HARDEN_EXISTING`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Bound workload amplification and provider-specific degradation.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Integrations/CalendarWebhookProcessingRequestedConsumer.cs`
- `docs/operations/recovery-and-data-safety.md`
- `docs/operations/observability.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Partition retry/backpressure by provider/connection/workspace where supported.
2. Honor Retry-After only under provider contract without global stalls.
3. Bound rules matched per event, recursion depth, chain length and provider call amplification.
4. Integrate entitlement/rate safeguards without moving Billing/provider ownership.

### Data / migration impact

NONE unless new durable recovery state is introduced.

### Compatibility / rollout

Telemetry field changes must not become semantic contract substitutes.

### Dependencies / readiness

Observability infrastructure; runbook/recovery ownership.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/PipelineTelemetryIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`, `AI-TST-POISON-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-115`, `AI-TST-AIREQ-116`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-12-COMPAT-001 — Observability, backpressure and recovery operations — Governed recovery and readiness truth

**Requirements:** `AIREQ117` — Recovery procedures are governed and auditable; `AIREQ118` — Readiness fails when required dependencies are unavailable

**Preparation posture:** `PARTIAL / HARDEN_EXISTING`  
**Execution disposition:** `IMPLEMENT_AND_HARDEN`

**Known gaps/debts:** `AI-GAP-11` (N8n reconciliation safe but operationally incomplete)

### Objective

Make operator changes auditable and runtime dependency failure visible.

### Current source authority / source under change

- `backend/src/Notrelix.Application/Common/Behaviors/ApplicationTracingBehavior.cs`
- `backend/src/Notrelix.Infrastructure/Observability/Metrics/MetricsService.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Automation/N8nDispatchConsumer.cs`
- `backend/src/Notrelix.Infrastructure/Messaging/Consumers/Integrations/CalendarWebhookProcessingRequestedConsumer.cs`
- `docs/operations/recovery-and-data-safety.md`
- `docs/operations/observability.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Turn ADR/runbook recovery into reviewed executable procedure/tooling where product readiness requires it.
2. Record who/what changed claim/execution/connection/sync state and preserve lineage.
3. Required DB/RLS/key ring/provider/messaging dependencies must fail readiness/capability availability honestly.
4. No diagnostic endpoint may mutate durable state as a shortcut.

### Data / migration impact

NONE unless new durable recovery state is introduced.

### Compatibility / rollout

Telemetry field changes must not become semantic contract substitutes.

### Dependencies / readiness

Observability infrastructure; runbook/recovery ownership.

### Existing evidence anchors

- `backend/tests/Notrelix.Integration.Tests/Integration/PipelineTelemetryIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Automation/N8nDispatchRuntimeChainIntegrationTests.cs`
- `backend/tests/Notrelix.Integration.Tests/Messaging/CalendarWebhookProcessingRuntimeTests.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`, `AI-TST-POISON-001`.

Primary canonical TEST IDs: `AI-TST-POISON-001`, `AI-TST-AIREQ-118`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-13-INV-001 — Architecture, TAC and production-runtime proof — Reuse TAC AI-FLOW-01..07 with current source posture

**Requirements:** `AIREQ119` — TAC AI-FLOW-01 through AI-FLOW-07 are reused as canonical cross-boundary flows

**Preparation posture:** `REUSE_BASELINE / EXACT_SHA_RERUN_REQUIRED`  
**Execution disposition:** `VERIFY_PRODUCTION_RUNTIME_AND_CLOSE_DOC_DRIFT`

**Known gaps/debts:** `AI-GAP-16` (Historical TAC dispositions stale)

### Objective

Keep canonical flow identities/invariants while correcting stale source-status descriptions.

### Current source authority / source under change

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`
- `backend/docs/architecture/platform-and-messaging.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Map each flow to current entrypoint/runtime owner/source/test evidence.
2. Do not invent P5 replacement flow IDs.
3. Mark old ImplementMissing descriptions stale when candidate source now implements the flow.
4. Rerun affected flow evidence on exact candidate.

### Data / migration impact

NONE unless enforcement requires source/test manifest changes.

### Compatibility / rollout

No public behavior change unless the owning lane says so.

### Dependencies / readiness

TAC/Platform architecture authority and production DI graph.

### Existing evidence anchors

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-MSG-001`, `AI-TST-ACT-ARCH-001`, `AI-TST-ACT-ARCH-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-119`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-13-CORE-001 — Architecture, TAC and production-runtime proof — Inherit Platform messaging/recovery contracts and architecture gates

**Requirements:** `AIREQ120` — Platform flow contracts are inherited, not redefined; `AIREQ121` — Architecture gates prevent provider and foreign-domain leakage

**Preparation posture:** `REUSE_BASELINE / EXACT_SHA_RERUN_REQUIRED`  
**Execution disposition:** `VERIFY_PRODUCTION_RUNTIME_AND_CLOSE_DOC_DRIFT`

**Known gaps/debts:** `AI-GAP-16` (Historical TAC dispositions stale)

### Objective

Use Platform mechanisms without redefining delivery or allowing boundary leakage.

### Current source authority / source under change

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`
- `backend/docs/architecture/platform-and-messaging.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Bind each broker consumer to PF-FLOW tenant/dedup/retry semantics.
2. Keep provider SDK/models out of Automation Domain and private foreign persistence out of P5.
3. Preserve ResourceLocator/AccessPolicy/port-adapter architecture guards.
4. Any Platform guarantee change reopens Platform certification.

### Data / migration impact

NONE unless enforcement requires source/test manifest changes.

### Compatibility / rollout

No public behavior change unless the owning lane says so.

### Dependencies / readiness

TAC/Platform architecture authority and production DI graph.

### Existing evidence anchors

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-MSG-001`, `AI-TST-ACT-ARCH-001`, `AI-TST-ACT-ARCH-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-120`, `AI-TST-AUTHZ-003`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-13-SEC-001 — Architecture, TAC and production-runtime proof — Production-runtime-owner verification

**Requirements:** `AIREQ122` — Verification proves actual runtime owner

**Preparation posture:** `REUSE_BASELINE / EXACT_SHA_RERUN_REQUIRED`  
**Execution disposition:** `VERIFY_PRODUCTION_RUNTIME_AND_CLOSE_DOC_DRIFT`

**Known gaps/debts:** `AI-GAP-16` (Historical TAC dispositions stale)

### Objective

Prove transaction/RLS/retry/provider behavior through actual production composition.

### Current source authority / source under change

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`
- `backend/docs/architecture/platform-and-messaging.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Use ISender request pipeline for auth/data-session tests.
2. Use MassTransit consumer graph for messaging/dedup/retry tests.
3. Use PostgreSQL/RLS for tenant policy tests where semantics depend on DB.
4. Use real provider adapter boundary/fake HTTP transport only at external edge; helper unit tests alone are insufficient.

### Data / migration impact

NONE unless enforcement requires source/test manifest changes.

### Compatibility / rollout

No public behavior change unless the owning lane says so.

### Dependencies / readiness

TAC/Platform architecture authority and production DI graph.

### Existing evidence anchors

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-MSG-001`, `AI-TST-ACT-ARCH-001`, `AI-TST-ACT-ARCH-002`.

Primary canonical TEST IDs: `AI-TST-AIREQ-122`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-13-COMPAT-001 — Architecture, TAC and production-runtime proof — Negative/failure evidence gate

**Requirements:** `AIREQ123` — Every critical flow has negative/failure evidence

**Preparation posture:** `REUSE_BASELINE / EXACT_SHA_RERUN_REQUIRED`  
**Execution disposition:** `VERIFY_PRODUCTION_RUNTIME_AND_CLOSE_DOC_DRIFT`

**Known gaps/debts:** `AI-GAP-16` (Historical TAC dispositions stale)

### Objective

Make failure behavior first-class in every critical flow.

### Current source authority / source under change

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`
- `backend/docs/architecture/platform-and-messaging.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Require explicit tests for auth denial, cross-tenant access, duplicate/concurrent delivery, invalid config, stale claim, provider unknown outcome, bad webhook signature/replay, migration upgrade and contract drift.
2. A happy-path-only flow cannot be certified D4/D5.

### Data / migration impact

NONE unless enforcement requires source/test manifest changes.

### Compatibility / rollout

No public behavior change unless the owning lane says so.

### Dependencies / readiness

TAC/Platform architecture authority and production DI graph.

### Existing evidence anchors

- `backend/tests/Notrelix.Architecture.Tests/Integrations/CalendarSemanticAuthorityArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DomainPurity/DomainHardeningArchitectureTests.cs`
- `backend/tests/Notrelix.Architecture.Tests/DataAccess/ResourceScopeResolutionChecks.cs`

Legacy stable TEST IDs to preserve/route: `AI-TST-MSG-001`, `AI-TST-ACT-ARCH-001`, `AI-TST-ACT-ARCH-002`.

Primary canonical TEST IDs: `AI-TST-TRG-ORDER-001`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-14-INV-001 — Compatibility, extraction, handoff and final certification — Queued/backlog/deployed-consumer compatibility

**Requirements:** `AIREQ124` — Contract evolution accounts for queued/backlogged/deployed consumers

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `VERIFY_COMPATIBILITY_AND_CERTIFY`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Treat public events, dispatch intents, persisted discriminators and realtime contracts as independently deployed contracts.

### Current source authority / source under change

- `docs/delivery/contract-first-delivery.md`
- `docs/delivery/change-classification.md`
- `docs/delivery/migration-policy.md`
- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Inventory queued/dead-letter messages and old workers for event/schema changes.
2. Inventory web/mobile lag for API/status/discriminator changes.
3. Use additive/dual-reader/consumer-first sequencing as required.
4. Define removal conditions for compatibility shims.

### Data / migration impact

NONE except contract/migration work owned by upstream lane.

### Compatibility / rollout

All independently deployed consumers/backlog considered before removal.

### Dependencies / readiness

All release-scoped lanes and downstream consumers.

### Existing evidence anchors

- `backend/tests/ci-proofs.json`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-124`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-14-CORE-001 — Compatibility, extraction, handoff and final certification — Service extraction remains deferred decision

**Requirements:** `AIREQ125` — Service extraction is a later governed decision

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `VERIFY_COMPATIBILITY_AND_CERTIFY`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Prove modular boundaries before considering deployment extraction.

### Current source authority / source under change

- `docs/delivery/contract-first-delivery.md`
- `docs/delivery/change-classification.md`
- `docs/delivery/migration-policy.md`
- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Keep Automation and Integrations separate modules/contexts in the modular monolith.
2. Collect evidence of independent data, contracts, failure/tenancy/operational ownership.
3. Escalate any service/project proposal with objective operational reason; team ownership alone is insufficient.

### Data / migration impact

NONE except contract/migration work owned by upstream lane.

### Compatibility / rollout

All independently deployed consumers/backlog considered before removal.

### Dependencies / readiness

All release-scoped lanes and downstream consumers.

### Existing evidence anchors

- `backend/tests/ci-proofs.json`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-125`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-14-SEC-001 — Compatibility, extraction, handoff and final certification — Cross-team handoff and readiness records

**Requirements:** `AIREQ126` — Cross-team handoff is explicit; `AIREQ127` — Readiness is capability- and consumer-specific

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `VERIFY_COMPATIBILITY_AND_CERTIFY`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Give every producer/consumer a concrete readiness contract and migration responsibility.

### Current source authority / source under change

- `docs/delivery/contract-first-delivery.md`
- `docs/delivery/change-classification.md`
- `docs/delivery/migration-policy.md`
- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Complete canonical handoff fields for WorkManagement→Automation, Automation→Work, Automation→Integrations/N8n, provider→Calendar webhook, Billing→Automation and frontend consumers.
2. Record required and actual maturity per released capability.
3. Keep dependency-blocked consumers BLOCKED rather than averaging readiness.

### Data / migration impact

NONE except contract/migration work owned by upstream lane.

### Compatibility / rollout

All independently deployed consumers/backlog considered before removal.

### Dependencies / readiness

All release-scoped lanes and downstream consumers.

### Existing evidence anchors

- `backend/tests/ci-proofs.json`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-126`, `AI-TST-AIREQ-127`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-14-COMPAT-001 — Compatibility, extraction, handoff and final certification — Exact candidate final certification

**Requirements:** `AIREQ128` — Final certification binds exact source, migrations, tests and CI

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `VERIFY_COMPATIBILITY_AND_CERTIFY`

**Known gaps/debts:** none beyond requirement-specific exact-candidate verification.

### Objective

Close SPEC→PLAN→TESTS→source/migrations→runtime→CI traceability on one candidate.

### Current source authority / source under change

- `docs/delivery/contract-first-delivery.md`
- `docs/delivery/change-classification.md`
- `docs/delivery/migration-policy.md`
- `.github/workflows/backend-ci.yml`
- `.github/workflows/container-ci.yml`
- `backend/tests/ci-proofs.json`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Execute all release-scoped canonical tests on exact SHA.
2. Record migration head and clean/upgrade proof.
3. Record runtime-owner evidence and CI job/run locators.
4. List accepted debts with owner/exit condition and ensure no blocker is silently waived.
5. Publish final released trigger/action/provider/frontend/realtime scope.

### Data / migration impact

NONE except contract/migration work owned by upstream lane.

### Compatibility / rollout

All independently deployed consumers/backlog considered before removal.

### Dependencies / readiness

All release-scoped lanes and downstream consumers.

### Existing evidence anchors

- `backend/tests/ci-proofs.json`

Legacy stable TEST IDs to preserve/route: `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-128`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-GATE-001 — Rule → trigger/action → execution release gate

**Requirements:** `AIREQ009` — Automation Rule stable identity and tenant scope; `AIREQ010` — Rule lifecycle is explicit; `AIREQ011` — Enable is authoritative server validation; `AIREQ012` — Disable stops new executions without erasing history; `AIREQ013` — Rule edits affect only future executions; `AIREQ014` — Execution persists immutable Rule revision semantics; `AIREQ015` — Rule archive/delete/restore and Billing capacity have fixed lifecycle semantics; `AIREQ016` — P5 Rule management surface is explicit; `AIREQ017` — Persisted automation configuration is typed and versioned; `AIREQ018` — Trigger and action configuration are independent contracts; `AIREQ019` — Persisted discriminator and referenced-schema evolution is explicit; `AIREQ020` — Condition evaluation is deterministic; `AIREQ021` — Condition false is distinct from condition failure; `AIREQ022` — Trigger identity follows producer contract; `AIREQ023` — Producer evolution includes Automation as a consumer; `AIREQ024` — Released trigger vocabulary matches runtime reachability; `AIREQ025` — Released action vocabulary matches an executor; `AIREQ026` — P5 multi-action execution is ordered and step-backed; `AIREQ027` — Execution identity is logical and stable; `AIREQ028` — Concurrent duplicate creation is constrained durably; `AIREQ029` — Execution records semantic provenance; `AIREQ030` — Execution lifecycle transitions are validated; `AIREQ031` — Action-step state is real product state only when attached to execution; `AIREQ032` — Success follows durable effect evidence; `AIREQ033` — Target/business failure and technical failure remain distinct; `AIREQ034` — Transport retry and Automation business retry have one coherent budget; `AIREQ035` — Retry preserves operation identity; `AIREQ036` — Unknown external outcome is never blindly re-fired; `AIREQ037` — Causation and recursion are bounded; `AIREQ038` — Execution history is safe product evidence and remains distinct from Audit; `AIREQ039` — Schedule intent is Automation product state; `AIREQ040` — Timezone, DST and missed-fire policy are explicit; `AIREQ041` — Scheduled occurrence identity prevents duplicate execution; `AIREQ042` — Scheduler production reachability is required before release; `AIREQ043` — Template is creation input, not live shared authority; `AIREQ044` — Template schemas participate in compatibility; `AIREQ045` — AI Agent requires explicit product admission; `AIREQ046` — No hidden AI runtime capability; `AIREQ047` — Source transactions do not execute Automation side effects inline; `AIREQ048` — Target bounded context revalidates every action; `AIREQ049` — Background Automation principal is explicit; `AIREQ050` — Permission revocation is honored at execution time; `AIREQ051` — Cross-context actions use public capability contracts; `AIREQ052` — Billing supplies capability/capacity facts, not plan branching; `AIREQ053` — Capacity consumption and release semantics are operation-safe; `AIREQ054` — Cross-context handoffs preserve source identity and ownership

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `BLOCKING_GATE`

**Known gaps/debts:** `AI-GAP-01` (Rule/API configuration contract drift), `AI-GAP-02` (Immutable Rule revision/config snapshot missing), `AI-GAP-03` (Condition and multi-action runtime not production-complete), `AI-GAP-04` (Trigger/action vocabulary exceeds executors), `AI-GAP-05` (Schedule/Templates/AI Agents must remain deferred/non-reachable), `AI-GAP-07` (authorization vocabulary), `AI-GAP-08` (Billing capacity release lifecycle)

### Objective

No Rule/Automation execution capability is release-ready until Rule/config/revision/condition/action/execution semantics compose with target authorization and Billing; deferred Schedule/Templates/AI-Agent capabilities must also be proven non-reachable rather than silently omitted.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Execute Rule create/enable/edit/revision and duplicate source occurrence tests through real request/consumer paths.
2. Prove unsupported vocabulary remains hidden or fails closed.
3. Prove Work target action revalidates current authorization and exactly-one operation semantics.
4. Record released trigger/action set explicitly.

### Data / migration impact

Aggregates migrations from AI-01..03.

### Compatibility / rollout

Gate must remain backward-compatible with supported clients/backlog or stay BLOCKED.

### Dependencies / readiness

AI-01..03 plus AI-05.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-RULE-003`, `AI-TST-EXE-CONC-001`, `AI-TST-EXE-IDEMP-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-009`, `AI-TST-AIREQ-010`, `AI-TST-RULE-001`, `AI-TST-RULE-002`, `AI-TST-AIREQ-013`, `AI-TST-RULE-003`, `AI-TST-AIREQ-015`, `AI-TST-AIREQ-016`, `AI-TST-AIREQ-017`, `AI-TST-AIREQ-018`, `AI-TST-AIREQ-019`, `AI-TST-COND-001`, `AI-TST-COND-002`, `AI-TST-TRG-001`, `AI-TST-TRG-CON-001`, `AI-TST-AIREQ-024`, `AI-TST-AIREQ-025`, `AI-TST-AIREQ-026`, `AI-TST-EXE-001`, `AI-TST-EXE-CONC-001`, `AI-TST-AIREQ-029`, `AI-TST-AIREQ-030`, `AI-TST-AIREQ-031`, `AI-TST-AIREQ-032`, `AI-TST-AIREQ-033`, `AI-TST-AIREQ-034`, `AI-TST-EXE-IDEMP-001`, `AI-TST-EXE-UNK-001`, `AI-TST-EXE-REC-001`, `AI-TST-EXE-HIST-001`, `AI-TST-AIREQ-039`, `AI-TST-AIREQ-040`, `AI-TST-AIREQ-041`, `AI-TST-AIREQ-042`, `AI-TST-AIREQ-043`, `AI-TST-AIREQ-044`, `AI-TST-AIREQ-045`, `AI-TST-AIREQ-046`, `AI-TST-AIREQ-047`, `AI-TST-AIREQ-048`, `AI-TST-AUTHZ-001`, `AI-TST-AUTHZ-002`, `AI-TST-ACT-001`, `AI-TST-AIREQ-052`, `AI-TST-AIREQ-053`, `AI-TST-AIREQ-054`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-GATE-002 — Connection → N8n → webhook trust/reliability gate

**Requirements:** `AIREQ055` — Integration Connection has explicit lifecycle; `AIREQ056` — Provider catalog and provider-specific connection configuration are canonical and typed; `AIREQ057` — Connection installation is Account/Workspace scoped and governed; `AIREQ058` — Integration provider identity is separate from Identity login and Workspace membership; `AIREQ059` — Provider authorization is state/PKCE secured and least-privilege; `AIREQ060` — Reusable secrets live behind opaque references; `AIREQ061` — Physical secret storage is restart-safe; `AIREQ062` — Secret versions and current pointers are durable authority; `AIREQ063` — Disconnect, secret retirement, provider revocation and provider-held copies are distinct; `AIREQ064` — Connection state and sync/provider health remain distinct; `AIREQ065` — Outbound provider operation uses an Integration-owned contract; `AIREQ066` — Provider anti-corruption layer translates types and errors; `AIREQ067` — Outbound operation has stable logical idempotency identity; `AIREQ068` — Timeout/rate-limit behavior follows provider evidence; `AIREQ069` — One end-to-end retry budget owns provider re-attempts; `AIREQ070` — Unknown provider outcome has an executable reconciliation path; `AIREQ071` — ADR-008 is authoritative for current N8n settlement; `AIREQ072` — Provider-effect claim freshness does not grant re-fire authority; `AIREQ073` — Stale queued provider claim is governed recovery debt; `AIREQ074` — External provider effect runs outside the database transaction; `AIREQ075` — Webhook raw HTTP boundary is bounded before trust; `AIREQ076` — Signature verification uses the exact provider-defined bytes; `AIREQ077` — Tenant routing derives from trusted binding, never payload; `AIREQ078` — Inbound provider delivery identity is scoped correctly; `AIREQ079` — Inbound claim and durable processing intent are atomic; `AIREQ080` — Tenant-scoped semantic processing occurs after commit; `AIREQ081` — Receipt becomes processed only after semantic reconciliation; `AIREQ082` — Malformed/conflicting webhook payload fails closed; `AIREQ083` — Inbound receipt is not outbound WebhookDelivery; `AIREQ084` — Provider-specific verification support is truthful

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `BLOCKING_GATE`

**Known gaps/debts:** `AI-GAP-09` (Provider OAuth/install flow incomplete), `AI-GAP-10` (Provider/catalog vocabulary drift), `AI-GAP-11` (N8n reconciliation safe but operationally incomplete), `AI-GAP-14` (Provider-specific webhook authenticity not proven)

### Objective

No provider path is D5 until connection/secret lifecycle, outbound effect safety and inbound trust/tenant semantics are proven together.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Run Calendar connection and secret round-trip/restart evidence.
2. Run N8n prepare/effect/settle crash/residue matrix.
3. Run raw webhook signature/replay/tenant/dedup/semantic processing through production composition.
4. Certify only actual provider protocols implemented; do not extrapolate from internal HMAC.

### Data / migration impact

Aggregates migrations from AI-06..08.

### Compatibility / rollout

Provider-specific compatibility and queued messages/callbacks must be addressed.

### Dependencies / readiness

AI-06..08 plus Platform messaging/RLS.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-SEC-001`, `AI-TST-OUT-UNK-001`, `AI-TST-WH-SEC-001`, `AI-TST-WH-IDEMP-001`.

Primary canonical TEST IDs: `AI-TST-CONN-001`, `AI-TST-AIREQ-056`, `AI-TST-CONN-AUTHZ-001`, `AI-TST-AIREQ-058`, `AI-TST-AIREQ-059`, `AI-TST-SEC-001`, `AI-TST-AIREQ-061`, `AI-TST-SEC-002`, `AI-TST-AIREQ-063`, `AI-TST-AIREQ-064`, `AI-TST-AIREQ-065`, `AI-TST-PROV-001`, `AI-TST-OUT-001`, `AI-TST-AIREQ-068`, `AI-TST-OUT-IDEMP-001`, `AI-TST-OUT-UNK-001`, `AI-TST-AIREQ-071`, `AI-TST-AIREQ-072`, `AI-TST-AIREQ-073`, `AI-TST-AIREQ-074`, `AI-TST-AIREQ-075`, `AI-TST-WH-SEC-001`, `AI-TST-WH-ROUTE-001`, `AI-TST-WH-IDEMP-001`, `AI-TST-AIREQ-079`, `AI-TST-WH-AUT-001`, `AI-TST-AIREQ-081`, `AI-TST-AIREQ-082`, `AI-TST-AIREQ-083`, `AI-TST-AIREQ-084`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-GATE-003 — Sync → frontend → data/ops convergence gate

**Requirements:** `AIREQ085` — Sync direction and source-of-truth are explicit; `AIREQ086` — Sync cursor advances after durable successful processing; `AIREQ087` — Sync conflict policy is product/provider semantics; `AIREQ088` — Mappings preserve internal and external identities; `AIREQ089` — Calendar mapping preserves ownership and date/time meaning; `AIREQ090` — Manual Calendar sync is production-reachable; `AIREQ091` — Backfill and live processing converge without duplication; `AIREQ092` — Provider deletion/disconnect semantics do not delete Notrelix resources implicitly; `AIREQ093` — Backend/public contract is the automation/integration API authority; `AIREQ094` — Frontend contract drift is a blocking compatibility defect; `AIREQ095` — Automation authoring vocabulary is generated/shared deliberately; `AIREQ096` — Frontend Automation repository has a real production adapter; `AIREQ097` — Demo fixture data is not production server state; `AIREQ098` — Integrations frontend exposes only backend-supported capabilities; `AIREQ099` — Automation/Integrations query keys use canonical scope partitioning; `AIREQ100` — Realtime execution status reuses Domain lifecycle facts through a real public producer; `AIREQ101` — Automation and Integration realtime recovery converge from durable truth; `AIREQ102` — OpenAPI/codegen and mixed-version behavior are gated; `AIREQ103` — Automation and Integrations own only their persistence; `AIREQ104` — Workspace-scoped tables remain under RLS defense-in-depth; `AIREQ105` — Technical secret/receipt state has constrained access paths; `AIREQ106` — Migration evidence includes clean database and supported upgrade; `AIREQ107` — Persisted JSON/discriminator changes are migration-sensitive; `AIREQ108` — Concurrency constraints match semantic identities; `AIREQ109` — Soft-delete/retention behavior is explicit; `AIREQ110` — No cross-context cascade semantics by database convenience; `AIREQ111` — Correlation traces the semantic chain without conflating identities; `AIREQ112` — Message/provider/operation/correlation identities remain distinct; `AIREQ113` — Telemetry and history redact secrets and unsafe payloads; `AIREQ114` — Operational metrics expose backlog and failure semantics; `AIREQ115` — Provider backpressure is isolated and observable; `AIREQ116` — Fanout and recursion have capacity controls; `AIREQ117` — Recovery procedures are governed and auditable; `AIREQ118` — Readiness fails when required dependencies are unavailable

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `BLOCKING_GATE`

**Known gaps/debts:** `AI-GAP-06` (Automation frontend not contract-aligned/wired), `AI-GAP-10` (Provider/catalog vocabulary drift), `AI-GAP-12` (Manual Calendar sync stub), `AI-GAP-15` (Execution lifecycle facts exist but public/realtime mapping is missing)

### Objective

Released Calendar sync and client surfaces must converge on durable backend truth with tenant-safe state and observable recovery.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Implement manual Calendar sync through the canonical Application/provider path; remove `NotImplementedException` and prove durable cursor/conflict semantics.
2. Align generated frontend contract/repositories/provider catalog/query keys with backend.
3. Map existing Automation Execution lifecycle Domain facts into the versioned public/realtime contract and prove gap recovery from durable execution/history plus durable Integration connection/sync/error state.
4. Run RLS/migration/observability/recovery readiness evidence.

### Data / migration impact

Aggregates migrations from AI-09..12.

### Compatibility / rollout

Backend/web/mobile overlap and stored rows/cursors must remain safe.

### Dependencies / readiness

AI-09..12.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-SYNC-BF-001`, `AI-TST-RT-001`, `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-SYNC-001`, `AI-TST-SYNC-CUR-001`, `AI-TST-WH-ORDER-001`, `AI-TST-AIREQ-088`, `AI-TST-AIREQ-089`, `AI-TST-AIREQ-090`, `AI-TST-SYNC-BF-001`, `AI-TST-SYNC-DISC-001`, `AI-TST-AIREQ-093`, `AI-TST-AIREQ-094`, `AI-TST-AIREQ-095`, `AI-TST-AIREQ-096`, `AI-TST-AIREQ-097`, `AI-TST-AIREQ-098`, `AI-TST-AIREQ-099`, `AI-TST-AIREQ-100`, `AI-TST-RT-001`, `AI-TST-AIREQ-102`, `AI-TST-ACT-ARCH-001`, `AI-TST-AUTHZ-004`, `AI-TST-AIREQ-105`, `AI-TST-AIREQ-106`, `AI-TST-AIREQ-107`, `AI-TST-AIREQ-108`, `AI-TST-AIREQ-109`, `AI-TST-AIREQ-110`, `AI-TST-OBS-001`, `AI-TST-MSG-001`, `AI-TST-AIREQ-113`, `AI-TST-AIREQ-114`, `AI-TST-AIREQ-115`, `AI-TST-AIREQ-116`, `AI-TST-POISON-001`, `AI-TST-AIREQ-118`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.

## AI-GATE-004 — Architecture, compatibility and final exact-candidate certification gate

**Requirements:** `AIREQ119` — TAC AI-FLOW-01 through AI-FLOW-07 are reused as canonical cross-boundary flows; `AIREQ120` — Platform flow contracts are inherited, not redefined; `AIREQ121` — Architecture gates prevent provider and foreign-domain leakage; `AIREQ122` — Verification proves actual runtime owner; `AIREQ123` — Every critical flow has negative/failure evidence; `AIREQ124` — Contract evolution accounts for queued/backlogged/deployed consumers; `AIREQ125` — Service extraction is a later governed decision; `AIREQ126` — Cross-team handoff is explicit; `AIREQ127` — Readiness is capability- and consumer-specific; `AIREQ128` — Final certification binds exact source, migrations, tests and CI

**Preparation posture:** `NOT_EVALUATED`  
**Execution disposition:** `FINAL_GATE`

**Known gaps/debts:** `AI-GAP-16` (Historical TAC dispositions stale)

### Objective

Publish P5 only when canonical flows, production runtime owners, negative evidence, compatibility and consumer handoffs agree on one exact candidate.

### Current source authority / source under change

- `docs/product/automation.md`
- `docs/product/integrations.md`
- `docs/workstreams/teams/automation-integrations.md`
- `docs/workstreams/cross-team-dependencies.md`
- `backend/docs/architecture/platform-and-messaging.md`
- `backend/docs/architecture/application-model.md`
- `backend/docs/architecture/security-tenancy-authorization.md`
- `docs/architecture/events-realtime-and-delivery-boundary.md`
- `docs/delivery/contract-first-delivery.md`
- `docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md`

These are current evidence/likely change surfaces, not permission to modify every listed file. Reuse current owner paths and make the smallest change that closes the requirement.

### Implementation actions

1. Rerun affected TAC AI-FLOW-01..07 evidence against current source.
2. Execute architecture and runtime-owner tests, not helper-only proxies.
3. Record exact SHA, migration head, test/CI locators, release scope and accepted debt.
4. Keep blocked/deferred capabilities out of the release declaration.

### Data / migration impact

No new migration beyond lane-owned changes.

### Compatibility / rollout

Final removal/cutover only after consumer/backlog proof.

### Dependencies / readiness

All units and gates.

### Existing evidence anchors

- No existing test path is sufficient by itself; use source/admission evidence and add TESTS coverage when implementation enters release scope.

Legacy stable TEST IDs to preserve/route: `AI-TST-MSG-001`, `AI-TST-POISON-001`, `AI-TST-OBS-001`.

Primary canonical TEST IDs: `AI-TST-AIREQ-119`, `AI-TST-AIREQ-120`, `AI-TST-AUTHZ-003`, `AI-TST-AIREQ-122`, `AI-TST-TRG-ORDER-001`, `AI-TST-AIREQ-124`, `AI-TST-AIREQ-125`, `AI-TST-AIREQ-126`, `AI-TST-AIREQ-127`, `AI-TST-AIREQ-128`.

### Required proof before PASS

- Execute the relevant canonical TESTS scenario(s) on the exact candidate SHA; `source exists` or `test exists` is not PASS.
- Include the negative/failure path whenever authorization, tenancy, duplicate delivery, retry, migration, provider outcome, webhook trust, or compatibility is part of the requirement.
- Record actual runtime owner: request pipeline, MassTransit consumer, EF/PostgreSQL/RLS, frontend production composition, or provider adapter as applicable.
- Record exact command/suite, result, non-zero test count, candidate SHA and CI/run locator in CERTIFICATION.

### Stop / escalation condition

Stop before implementation if closing this unit requires a new cross-cutting framework/service, changes Platform delivery guarantees, weakens authorization/tenant isolation, exposes reusable secrets, introduces direct foreign persistence, or changes product/provider semantics without the owning authority.


# Sequencing and safe change sets

## 10. Preferred change-set order

These are review/deployment units, not mandatory git branch names:

| Change set | Contents | Must be green before next dependent set |
|---|---|---|
| `AI-CS-00` | authority/reachability inventory + stale-doc corrections | no unresolved owner conflict |
| `AI-CS-01` | Rule request/config contract + persisted schema compatibility | producer contract and migration proof |
| `AI-CS-02` | immutable Rule revision/snapshot + Execution provenance | queued execution determinism + migration proof |
| `AI-CS-03` | trigger/condition/action runtime parity + released catalog | producer/action matrix and negative tests |
| `AI-CS-04` | target action auth/idempotency/Billing lifecycle | Work/Governance/Billing handoff proof |
| `AI-CS-05` | Connection/provider catalog/OAuth/secret hardening | connection/secret lifecycle D5 for released provider |
| `AI-CS-06` | N8n reconciliation operational closure | ADR-008 runtime/crash/residue proof |
| `AI-CS-07` | provider webhook authenticity + Calendar inbound hardening | exact provider protocol trust/dedup/tenant proof |
| `AI-CS-08` | Calendar sync/manual-sync/backfill conflict | D4+ per released direction/provider |
| `AI-CS-09` | frontend generated contract/repositories/provider catalog | backend-web/mobile compatibility proof |
| `AI-CS-10` | realtime public mapping + durable recovery | durable recovery convergence proof |
| `AI-CS-11` | persistence/RLS/migration/observability/recovery hardening | clean/upgrade/runtime-owner evidence |
| `AI-CS-12` | final architecture/compatibility/certification | all release-scoped blockers closed/accepted |

Do not combine all sets into one broad cleanup PR merely because the repo is a monorepo. Split where compatibility and rollback improve, but every intermediate state must compile and satisfy its declared mixed-version contract.

## 11. Parallelization

Safe after contracts are frozen:

- Rule revision persistence can proceed alongside provider OAuth design if they touch independent schemas/contracts.
- Frontend repository wiring can proceed against the frozen OpenAPI/public contract while backend runtime tests are hardened.
- Provider-specific webhook verification adapters can proceed behind the already-frozen inbound trust boundary.
- Observability metrics/redaction can proceed in parallel if they do not change semantic outcomes.
- Schedule/Templates/AI-agent admission analysis can proceed without forcing their implementation.

Unsafe parallelization:

- frontend vocabulary changes before producer discriminator/config contract is frozen;
- Rule revision migration and worker dispatch changes designed independently;
- provider retry logic modified separately in adapter, consumer and Platform without one retry budget;
- webhook provider verification and tenant binding redesigned by different changes without one raw-byte/trusted-routing contract;
- Billing capacity release implemented before Rule delete/archive semantics are fixed;
- service extraction before source/module boundaries are certified.

# Cross-cutting implementation contracts

## 12. Rule revision migration target

Target sequence:

```text
existing stored Rule config v1
→ compatible reader exists
→ new Rule revision/snapshot representation introduced
→ new executions bind revision/snapshot
→ queued old executions have an explicit policy/migration
→ runtime dispatch stops loading mutable latest Rule semantics
→ old compatibility path removed only after backlog/consumer proof
```

The exact physical representation (revision row, immutable JSON snapshot, or equivalent) is an implementation detail only if it preserves immutable semantic identity, migration, auditability and efficient dispatch. Aggregate optimistic `Version` alone is not enough.

## 13. Configuration contract migration target

The current request shape:

```text
Name
TriggerEvent
ActionType
Configuration
```

must converge to a contract that represents trigger/action configuration independently, and condition configuration when released. The migration must define:

```text
old API consumer behavior
new API shape
stored JSON reader/version
frontend generated type migration
queued/existing Rule behavior
removal condition for legacy shape
```

No implementation may simply duplicate the same JSON into two fields and call the contract fixed.

## 14. N8n provider-effect invariant

The accepted current invariant remains:

```text
Tx1: durable claim + execution prepare
commit
provider effect: no DB transaction
Tx2: durable outcome + claim Succeeded OR retry evidence + claim release
```

A fresh Processing claim means an active attempt and grants no mutation to a duplicate. A stale claim grants only reconciliation authority, never automatic provider re-fire. A stale Queued+Processing residue must be governed by a reviewed recovery path/tool or remain a release debt with explicit readiness impact.

## 15. Inbound webhook invariant

```text
bounded exact raw bytes
→ provider authenticity/freshness verification
→ trusted binding lookup / provider match
→ derived Account/Workspace tenant
→ durable connection-scoped receipt claim
→ outbox processing intent in same transaction
→ tenant-restored consumer/RLS
→ semantic Calendar reconciliation
→ terminal receipt state
```

Provider JSON never supplies trusted tenant identity. Outbound `WebhookDelivery` is never reused as inbound receipt state.

## 16. Frontend convergence target

Production web/mobile composition must not carry a second product vocabulary. The target is:

```text
backend/public semantic contract
→ OpenAPI / generated metadata or approved shared public contract
→ production repository adapter
→ canonical account/workspace query keys
→ UI state
```

Demo/default rules belong only to story/test/demo composition. Realtime is optional; if present it is freshness and recovers from durable query state.

# TESTS construction contract

## 17. TESTS file requirements

The next canonical file, `automation-integrations.tests.md`, MUST:

1. preserve every legacy `AI-TST-*` ID listed in §6;
2. map **all 128 AIREQ requirements** to executable scenarios;
3. add new stable test IDs where the old checklist does not cover a requirement;
4. distinguish `EXISTING_TEST_REUSE`, `EXISTING_TEST_EXTEND`, `NEW_TEST_REQUIRED`, `STATIC_GOVERNANCE_PROOF`, `CI_RUNTIME_PROOF`, and `NOT_APPLICABLE_UNLESS_ADMITTED`;
5. record `Given / When / Then`, expected result, exact test project, source under test, positive/negative, security-sensitive, migration-sensitive, CI gate and execution evidence;
6. route each `AI-FLOW-01..07` to exact source/runtime tests;
7. never use Domain/unit tests as substitute for production composition when transaction/RLS/messaging/provider semantics are being certified.

High-priority new/extended scenarios include:

- Rule R1 execution created → Rule edited to R2 → queued execution cannot silently run R2;
- old single-Configuration Rule/API row/client → migrated independent trigger/action config behavior;
- duplicate Work event arriving concurrently → exactly one Execution and one target operation;
- unsupported advertised trigger/action cannot reach production UI/catalog;
- permission revoked after Rule creation → target action denied at execution time;
- Billing Rule capacity release/reconsume lifecycle if delete/archive is released;
- N8n stale Queued+Processing → governed recovery without provider re-fire;
- N8n fresh duplicate → no execution/claim/provider mutation;
- provider UnknownOutcome → no automatic second provider call;
- webhook invalid signature/timestamp/content-type/body size → no tenant adoption/no durable semantic processing;
- webhook trusted binding for Workspace A + payload claiming Workspace B → remains Workspace A or rejects; never trusts payload;
- duplicate/reordered Calendar callbacks → no duplicate/regressive mapping;
- `TriggerCalendarSyncCommand` cannot be advertised while throw-only;
- backend/frontend trigger/action/provider vocabularies compile and semantically agree;
- Automation repository production adapter is actually bound;
- demo default rules cannot appear as production fallback;
- realtime producer → gap → durable reload → checkpoint reconcile → normal next sequence;
- clean DB + supported upgrade for revision/config/provider schema changes;
- RLS cross-tenant negative proof through actual PostgreSQL production path.

# Handoff contracts

The two handoff schemas below are inherited from `docs/workstreams/teams/automation-integrations.md` and MUST NOT be replaced by a generic Platform/P5 template.

## 18. Source-event → Automation — team-authoritative schema

```text
Source context:
Event:
Semantic owner:
Automation trigger:
Required payload:
Ordering:
Idempotency:
Current readiness:
Required readiness:
Compatibility:
Tests:
```

For the P5 WorkManagement trigger set, create one record per released event (`ItemAssigned`, `ItemMovedToGroup`, `ItemCreated`) and append:

```text
Candidate SHA:
Evidence locator:
Known debt/blocker:
Invalidation trigger:
Rollback/forward-fix:
```

## 19. Automation → Integrations — team-authoritative schema

```text
Action type:
Automation owner:
Integration operation:
Connection requirement:
Credential requirement:
Retry classification:
Idempotency:
Provider failure mapping:
Tests:
```

For the released N8n/Webhook action, the record MUST bind ADR-008 classification, stable Execution/operation identity, provider-effect claim lifecycle and governed reconciliation. Append:

```text
Candidate SHA:
Breaking/additive:
Migration/rollout strategy:
Evidence locator:
Known debt/blocker:
Invalidation trigger:
Rollback/forward-fix:
```

## 20. Automation → WorkManagement target action

```text
Source context: Automation
Action type: MoveItem
Automation owner: Execution lifecycle + stable operation identity
Target semantic owner: WorkManagement
Target public capability: IWorkItemActions / public ItemMovement contract
Required scope: Account + Workspace + actor propagated from trusted trigger fact
Authorization: target revalidates current permission/invariants at action time
Idempotency: ExecutionId is the logical operation identity
Failure mapping: business/authorization/conflict terminal; technical failure retryable under one budget
Compatibility: version public target contract before breaking queued executions
Tests: production action chain + denial + cross-tenant + conflict + retry
Candidate SHA:
Evidence locator:
```

## 21. Provider → Calendar Integration

```text
Provider: Google | Microsoft/Outlook
Integration operation: Calendar callback intake + semantic reconcile
Connection requirement: active matching CalendarIntegration + IntegrationConnection
Credential/authenticity requirement: actual provider callback/subscription protocol; generic HMAC alone is insufficient
Tenant derivation: trusted WebhookPath/binding → Connection → Account/Workspace; never payload
Delivery identity: provider delivery/event identity scoped by Connection
Idempotency: durable receipt unique identity
Retry classification: intake capture separate from tenant semantic processing
Provider failure mapping: rejected / duplicate / captured / processed / blocked / failed
Tests: real-provider fixture/protocol + HTTP boundary + receipt/dedup + tenant consumer + semantic terminal state
Candidate SHA:
Evidence locator:
```

## 22. Billing → Automation

```text
Capability: Automation Rule capacity
Current contract: transactional consume on Rule create
Target lifecycle: disable retains; archive/delete release once; restore re-consumes atomically
Semantic owner: Billing owns capacity truth; Automation owns Rule lifecycle
Idempotency: stable lifecycle operation identity
Authorization: Automation lifecycle operation passes canonical Governance pipeline
Compatibility: historical counts/reconciliation defined before rollout if existing rows diverge
Tests: concurrent last-slot + rollback + release + duplicate release + restore no-headroom
Candidate SHA:
Evidence locator:
```

## 23. Backend → frontend Automation/Integrations

```text
Producer authority: OpenAPI/public Application + public realtime contracts
Automation triggers: ItemAssigned / ItemMovedToGroup / ItemCreated
Automation actions: MoveItem / N8n-WebHook
Calendar providers: Google / Microsoft-Outlook
Frontend adapter: real production repository/client; no demo fallback as server truth
Realtime: mapped from owned backend lifecycle facts; durable refetch/recovery after gap
Compatibility: compatible backend expansion → consumer migration → old shape removal
Tests: OpenAPI/codegen drift + semantic web/mobile + production composition + realtime recovery
Candidate SHA:
Evidence locator:
```

# Escalation and completion

## 24. Mandatory escalation

Stop and escalate when implementation would:

- add a new production service/project or generic workflow engine;
- add arbitrary user code/expression execution without sandbox/product approval;
- change background actor/service-principal architecture;
- change generic messaging delivery/dedup/retry guarantees;
- weaken provider signature, CSRF, authorization, RLS or tenant isolation;
- introduce a new secret-storage architecture or expose reusable secrets;
- require direct private persistence access across bounded contexts;
- require a breaking source event without producer-owner coordination;
- change provider UnknownOutcome into automatic retry without provider idempotency/reconciliation evidence;
- make a provider SDK a repo-wide/domain dependency;
- extract Automation/Integrations into services for team-organizational convenience alone.

## 25. Final execution definition of done

P5 implementation is ready for final certification only when:

- all 128 AIREQ requirements have an owning PLAN unit and TESTS trace;
- every release-scoped gap is `CLOSED`, `ACCEPTED_DEBT` with owner/exit condition, or explicit `NOT_APPLICABLE/DEFERRED`;
- released Rule/trigger/action/provider vocabularies match actual runtime reachability;
- immutable Rule execution semantics are proven;
- Work action and provider action retries are exactly-once-effect-safe according to their contracts;
- Connection/secret lifecycle is restart-safe and secret-safe;
- webhook trust precedes tenant adoption and semantic processing;
- manual Calendar sync is implemented or not exposed;
- frontend production composition consumes canonical contracts rather than demo/handwritten divergent state;
- RLS/migrations/clean+upgrade evidence passes on the candidate;
- operational reconciliation/backpressure/fanout signals are actionable;
- `AI-FLOW-01..07` exact-candidate evidence is current;
- architecture/runtime-owner/negative tests and required CI jobs are green with non-zero execution;
- consumer-specific handoffs/readiness are recorded;
- final CERTIFICATION names the exact SHA and does not infer PASS from source existence.

## 26. PLAN structural QA target

This file is expected to maintain:

```text
Work units:          64 / 64 unique
AIREQ coverage:     128 / 128
AI-FLOW routing:      7 / 7
legacy AI-TST IDs:   preserved / routed forward
known gaps:          AI-GAP-01 .. AI-GAP-16 represented
exact baseline SHA:  required
```
