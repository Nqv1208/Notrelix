---
document_id: WRK-SPEC-AUTOMATION-INTEGRATIONS
document_type: workstream-specification
status: active
owner: automation-integrations-team
applies_to:
  - backend
  - frontend
  - automation
  - integrations
  - p5
  - rules
  - triggers
  - conditions
  - actions
  - executions
  - scheduling
  - templates
  - ai-agents
  - provider-connections
  - provider-auth
  - secrets
  - outbound-provider-effects
  - inbound-webhooks
  - calendar-sync
  - realtime
  - migrations
  - tenant-isolation
  - cross-context-contracts
evidence:
  - docs/product/automation.md
  - docs/product/integrations.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/automation-integrations.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/contract-first-delivery.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - backend/docs/decisions/ADR-008-provider-outcome-classification-and-settlement.md
  - docs/workstreams/executions/backend-team-architecture-closure/backend-team-architecture-closure.spec.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/architecture/realtime.md
review_on:
  - automation-product-rule-change
  - integration-product-rule-change
  - trigger-or-action-contract-change
  - execution-lifecycle-change
  - rule-schema-migration
  - schedule-template-agent-admission
  - provider-connection-change
  - provider-auth-or-secret-change
  - provider-outcome-classification-change
  - webhook-contract-change
  - sync-reconciliation-change
  - source-event-change
  - billing-capacity-change
  - authorization-model-change
  - realtime-contract-change
  - frontend-contract-change
  - migration-change
  - ci-gate-change
---

# SPEC — Automation & Integrations

## 1. Purpose

This document is the canonical execution specification for the P5 Automation & Integrations workstream.

It converts current product/architecture authority and the exact brownfield source into one target contract that PLAN, TESTS and CERTIFICATION can execute.

It answers:

```text
What does Automation own?
What does Integrations own?
Which current mechanisms are production-reachable?
Which source areas are only domain/persistence scaffolding?
Which current behaviors are accepted reference mechanisms?
Which gaps must be closed before a capability can be called D4/D5?
Which cross-context/provider contracts are safe to depend on?
What must frontend/web/mobile consume?
What evidence is required before downstream teams rely on P5?
```

This document does **not** declare the implementation complete.

## 2. Exact source baseline

This SPEC was built against:

```text
Repository: Nqv1208/Notrelix
Candidate SHA: 902bc9c5b39a003df3dce6673edc124984d2b398
```

All current-source statements in this file refer to that SHA.

Future implementation/certification MUST rerun evidence on the exact candidate being certified.

## 3. Authority rule

Precedence:

```text
product semantics
→ system / bounded-context architecture
→ backend/frontend architecture
→ accepted ADRs
→ workstream/team delivery authority
→ this execution SPEC
→ source/tests/generated evidence
```

Source can prove that an implementation exists; source does not silently override a higher semantic authority.

When documents describe an older source state, preserve the durable invariant/Flow ID but classify the source-status prose as `DOC_STALE`.

## 4. Namespace rule

This repository currently has two different uses of `AUT-*` / `INT-*`:

```text
docs/product/automation.md
  AUT-001 .. AUT-035
  = canonical product rules

docs/product/integrations.md
  INT-001 .. INT-038
  = canonical product rules

docs/workstreams/teams/automation-integrations.md
  AUT-001 .. AUT-014 / INT-001 .. INT-014
  = team capability labels
```

These are not the same namespace.

This execution package therefore uses:

```text
AIREQ001 .. AIREQ128
AIAC001  .. AIAC018
```

Do not create a third meaning for `AUT-*` or `INT-*`.

## 5. Bounded-context separation

Canonical direction:

```text
source context
  owns committed business fact
        ↓
Automation
  owns Rule / trigger interpretation / conditions / Actions / Execution
        ↓
target product context OR Integrations
  target context owns product mutation
  Integrations owns provider operation / Connection / webhook / sync
        ↓
Platform
  owns delivery / dedup / retry mechanics / tenant restoration / realtime transport
```

Automation is not a generic cross-context transaction coordinator.

Integrations is not Automation's provider-SDK folder.

Platform is not the owner of Rule, provider, Connection, or sync meaning.

## 6. Release-scope model

P5 is capability-gated, not all-or-nothing.

A capability can be released only when its actual producer/consumer dependencies reach the required readiness.

Current broad categories:

```text
CORE / release candidates
- Rule creation/list/enable/disable
- WorkManagement event triggers that are actually wired
- MoveItem target action
- N8n dispatch reference path
- Calendar connect/disconnect
- Calendar verified inbound webhook reconciliation

PARTIAL / must be closed or constrained
- condition execution
- action parity
- immutable Rule revision
- provider OAuth
- manual/scheduled sync
- frontend production wiring
- realtime execution producer/recovery
- operational reconciliation

DEFERRED / explicit admission required
- broad Scheduled Automation
- Templates as released API/product flow
- AI Agents
- generic outbound WebhookSubscription/WebhookDelivery runtime
- providers without real adapter/protocol evidence
```

## 7. Current implementation assessment

| Capability | Current source posture | Current evidence | SPEC interpretation |
|---|---|---|---|
| Rule create/list/enable/disable | `PRODUCTION_REACHABLE / PARTIAL_GAP` | API + Application + Domain + Billing capacity + integration tests | Keep; repair contract/config/lifecycle gaps rather than rebuild |
| Rule update/delete/archive/restore | `DOMAIN_ONLY / PARTIAL_GAP` | Domain mutation exists; no complete released API/Application surface found | Do not claim CRUD complete |
| Trigger model | `PARTIAL_GAP` | RulesEngine discriminator set + three reachable Work trigger families | Vocabulary/runtime parity required |
| Conditions | `DOMAIN_ONLY` | value object/tests; create/evaluator path does not build/evaluate condition | Not a released condition engine |
| Actions | `PARTIAL_GAP` | Action vocabulary is broad; runtime dispatch supports `MoveItem` and `Webhook`; unsupported actions fail closed | Preserve fail-closed behavior; close parity per released type |
| Execution | `PRODUCTION_REACHABLE / PARTIAL_GAP` | durable aggregate, unique Rule+Trigger index, MoveItem/N8n execution paths | Add immutable Rule semantics / real step model if multi-action released |
| Scheduled Jobs | `DOMAIN_ONLY` | aggregate + persistence + tests | Needs scheduler/runtime admission |
| Templates | `DOMAIN_ONLY` | aggregate + persistence + tests | Needs Application/API product path before release |
| AI Agents | `EXPLICIT_ADMISSION_REQUIRED` | rich Domain/tests | Must not be promoted without product/security/model/tool contract |
| Billing capacity | `PRODUCTION_REACHABLE / ACCEPTED_DEBT` | transactional consume on Rule create | release/archive/delete capacity lifecycle remains debt |
| Automation→Work | `PRODUCTION_REACHABLE` | durable dispatch + port/ACL/adapter + target use case + composition tests | Reference target-action boundary |
| N8n provider dispatch | `PRODUCTION_REACHABLE / OPERATIONAL_GAP` | ADR-008, provider client, two-phase consumer, claim store, durability/runtime tests | Strong reference; D5 still requires executable governed reconciliation |
| Calendar Connection | `PRODUCTION_REACHABLE` | connect/disconnect commands, secret store/version, Calendar binding, authorization tests | Keep semantics; provider OAuth remains separate gap |
| Calendar webhook | `PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT` | bounded API intake, binding resolver, HMAC verifier, receipt/outbox, tenant consumer, reconciliation | Strong provider-neutral reference; do not overclaim real provider protocol |
| Calendar manual sync | `GAP_CONFIRMED` | `TriggerCalendarSyncCommandHandler` throws `NotImplementedException` | Not released |
| Generic outbound webhooks | `DOMAIN_ONLY / LEGACY_BOUNDARY` | WebhookSubscription/Delivery domain+persistence tests | Requires explicit runtime/admission; do not reuse as inbound receipt |
| Automation frontend | `CONTRACT_DRIFT / PARTIAL` | state/core/web packages and tests exist | Current type vocabulary/repository wiring/realtime producer do not match backend release path |
| Integrations frontend | `CONTRACT_DRIFT / PARTIAL` | generic feature package exists | Provider/status/endpoint vocabulary differs from backend Calendar surface |
| Automation realtime | `PARTIAL_PUBLIC_MAPPING_GAP` | Domain execution lifecycle facts + FE adapter/fixtures exist | Public/realtime mapper contract + Platform recovery proof required |

## 8. Key source facts that constrain implementation

### 8.1 Current Rule request shape is not target-complete

Backend currently accepts:

```text
Name
TriggerEvent
ActionType
Configuration
```

and passes the same `Configuration` JSON into both Trigger and Action definitions.

Frontend currently models:

```text
triggerConfig
actionConfig
boardId
description
```

These are different contracts.

The target MUST be resolved through an intentional producer contract, not by preserving both divergent shapes indefinitely.

### 8.2 Rule revision is missing from current Execution

Current `AutomationExecution` persists:

```text
RuleId
TriggerId
status
payload
attempt count
...
```

but not an immutable Rule revision/config snapshot.

Current dispatch paths reload the mutable Rule.

Therefore a queued execution can observe a later Rule edit unless the execution contract is hardened.

### 8.3 Runtime trigger/action parity is intentionally incomplete

Current RulesEngine accepts more types than current runtime executes.

Reachable examples include:

```text
Triggers:
- ItemAssigned
- ItemMovedToGroup
- ItemCreated

Actions:
- MoveItem
- Webhook / N8n
```

Unsupported Action types fail the Execution closed with `M8-AUTOMATION-ACTION-PARITY`.

That fail-closed behavior is correct transitional behavior and MUST remain until parity is deliberately implemented.

### 8.4 N8n has a special durable provider-effect protocol

N8n dispatch is not a generic command-owned dedup consumer.

It owns:

```text
Tx1: claim + prepare
no DB transaction: provider effect
Tx2: durable outcome + claim settlement/release
```

with fresh/stale `Processing` claim semantics under ADR-008.

Do not simplify this path into a normal retry filter without re-opening the architecture decision.

### 8.5 Calendar webhook intake is no longer a stub at this candidate

Exact source implements:

```text
bounded raw HTTP
→ trusted binding lookup
→ provider verification
→ derived tenant adoption
→ technical inbound receipt/dedup
→ outbox processing request
→ tenant-scoped Calendar reconciliation
```

Older TAC flow-card text that still says the handler throws is source-status stale, not a reason to regress the implementation.

### 8.6 Manual Calendar sync remains intentionally unimplemented

`TriggerCalendarSyncCommandHandler` still throws `NotImplementedException`.

It is not an AI-FLOW-07 blocker.

It is nevertheless a real gap if manual/provider sync becomes release scope.

### 8.7 Frontend is currently a separate contract island

Automation frontend currently uses older/card-oriented trigger/action names, and Integrations frontend exposes providers/endpoints that do not match the backend Calendar surface.

The workstream MUST fix producer-consumer contract ownership rather than add translation hacks with no removal owner.

# Requirements

## 9. Global governance

### AIREQ001 — Authority precedence and conflict handling

Automation & Integrations MUST follow canonical product semantics first, then system/architecture authority, accepted ADRs, team/workstream ownership, and finally implementation evidence. When authority and source disagree, the execution package MUST classify the difference as `DOC_STALE`, `SOURCE_DEBT`, `TRANSITION`, `CONTRACT_CHANGE`, or `UNRESOLVED` before implementation.

### AIREQ002 — Automation and Integrations remain separate bounded contexts

Automation owns rule, trigger interpretation, condition/action orchestration and execution product state. Integrations owns provider connection, provider translation, credentials, webhook/sync/provider-operation semantics. One delivery team MUST NOT merge their state models or business ownership.

### AIREQ003 — Platform mechanisms do not become P5 semantics

Outbox, broker delivery, deduplication, generic retry plumbing, tenant restoration, realtime transport, secret-encryption mechanics and observability transport remain Platform/Infrastructure mechanisms. P5 owns only the business/provider semantics that consume those mechanisms.

### AIREQ004 — Brownfield-first implementation

Execution MUST begin from the exact current source graph. Existing aggregates, ports, consumers, adapters, migrations, tests and runtime wiring MUST be reused or deliberately changed; missing folders or idealized diagrams are not permission to introduce duplicate abstractions.

### AIREQ005 — Exact-candidate evidence

Every implementation/certification decision MUST name the candidate SHA and distinguish source inspection from executed proof. Source/test existence alone is never sufficient for D4/D5.

### AIREQ006 — Production reachability classification

Each capability MUST be classified as `PRODUCTION_REACHABLE`, `PARTIAL_GAP`, `DOMAIN_ONLY`, `CONTRACT_DRIFT`, `EXPLICIT_ADMISSION_REQUIRED`, or `NOT_APPLICABLE` before it is planned. Domain types and fixtures MUST NOT be represented as released product behavior without a production path.

### AIREQ007 — Domain-only capability does not silently enter release scope

Scheduled Jobs, Templates, AI Agents, generic webhook delivery, sync primitives or other source areas that lack production reachability MUST remain explicit deferred/admission items until product authority, Application/API/runtime paths and tests are defined.

### AIREQ008 — No architecture expansion by convenience

P5 MUST NOT create a new production project, service, workflow engine, scripting engine, repository-wide provider SDK dependency, secret-storage architecture, or background-principal model without the required architecture decision and cross-team approval.

## 9.1. Frozen P5 delivery decisions

The following choices are fixed for this execution package. Implementation MUST NOT reopen them as local options:

```text
Rule revision:
  capture immutable semantic revision/config snapshot when Execution is created
  queued/running Execution continues that snapshot
  later Rule edits affect future Executions only

Released Rule management:
  create / list / detail / update / enable / disable / archive / soft-delete / restore
  no P5 test-preview execution endpoint

Rule execution:
  Conditions are implemented in the production evaluator
  Actions are an ordered sequential list backed by durable Execution Steps
  arbitrary DAG/parallel Action execution is out of P5

Released event triggers:
  ItemAssigned / ItemMovedToGroup / ItemCreated
  additional trigger discriminators remain unavailable until a production consumer is added

Released target/provider actions:
  MoveItem
  N8n/Webhook provider effect under ADR-008
  other Action discriminators remain unavailable until a real executor is admitted

Deferred from P5 release:
  Scheduled Automation
  Automation Templates
  AI Agents
  generic outbound WebhookSubscription/WebhookDelivery product surface
  their Domain/persistence code remains non-reachability evidence only

Calendar provider surface:
  Google and Microsoft/Outlook only
  real provider authorization and callback verification must be implemented/certified
  provider entries without a production adapter remain hidden

Calendar manual sync:
  implement the command/runtime path; a throw-only handler is not an allowed final state

Realtime:
  reuse/map existing Automation execution lifecycle Domain facts into a public/realtime contract
  do not invent a second execution lifecycle
  Automation and Integration progress recover from durable state after gaps
```

Any change to these decisions is an authority/product/architecture change and requires updating SPEC, PLAN, TESTS and CERT in the same change.

## 10. Automation Rule lifecycle

### AIREQ009 — Automation Rule stable identity and tenant scope

Every Automation Rule MUST have stable RuleId, AccountId and WorkspaceId ownership independent of mutable display/configuration fields.

### AIREQ010 — Rule lifecycle is explicit

Rule lifecycle MUST distinguish at least the current supported states `Draft`, `Active`, `Disabled`, and `Archived` where exposed. Delete/soft-delete/restore behavior MUST be explicit and MUST NOT erase required execution history.

### AIREQ011 — Enable is authoritative server validation

A Rule may enter `Active` only after server-side validation of its released trigger, condition and action configuration. Frontend validation is advisory UX, not authority.

### AIREQ012 — Disable stops new executions without erasing history

Disabling a Rule MUST prevent new matching/scheduled executions after the defined effective point while retaining historical executions and audit evidence.

### AIREQ013 — Rule edits affect only future executions

Editing an enabled Rule creates new Rule semantics for future matching occurrences only. An Execution already created in `Queued` or `Running` MUST continue with the immutable Rule revision/configuration captured when that Execution was created. Editing MUST NOT cancel, rewrite, or silently retarget an existing Execution.

### AIREQ014 — Execution persists immutable Rule revision semantics

At Execution creation, Automation MUST persist an immutable semantic revision reference plus the normalized trigger/condition/action configuration snapshot required to reproduce that run. The snapshot/reference is the authority for queued/running execution. Aggregate optimistic-concurrency `Version` remains a mutation guard and MUST NOT substitute for product Rule revision.

### AIREQ015 — Rule archive/delete/restore and Billing capacity have fixed lifecycle semantics

P5 lifecycle semantics are fixed: `Disable` stops new Executions but continues to consume the Automation Rule capacity slot; `Archive` and soft `Delete` stop new Executions and release exactly one slot after the lifecycle mutation is accepted; `Restore` MUST re-consume capacity atomically before the Rule becomes available again and MUST fail closed when capacity is unavailable. Executions already created continue against their immutable Rule snapshot, subject to current target authorization/invariants. History is retained according to policy.

### AIREQ016 — P5 Rule management surface is explicit

The P5 Rule management surface is: create, list, detail, update configuration/name, enable, disable, archive, soft-delete, and restore. A dedicated “test/preview Rule” execution endpoint is NOT part of P5 and MUST NOT be advertised. Domain methods without Application/API paths do not count as released operations; missing target operations above MUST be implemented before Rule-management certification.

## 11. Trigger / Condition / Action contract

### AIREQ017 — Persisted automation configuration is typed and versioned

Trigger, condition and action configuration MUST be represented behind stable discriminators and schema versions. Arbitrary JSON MAY be an encoding but MUST NOT be the semantic authority.

### AIREQ018 — Trigger and action configuration are independent contracts

Trigger configuration and Action configuration MUST be independently represented and validated. The current single `Configuration` request field MUST NOT force the same JSON payload to satisfy two unrelated schemas.

### AIREQ019 — Persisted discriminator and referenced-schema evolution is explicit

Changing trigger/action/condition discriminator names, schema versions, defaults, field/resource identifiers or referenced schema MUST include compatibility/migration behavior for stored Rules and independently deployed consumers. A Rule whose referenced field/resource/schema no longer resolves MUST be marked/surfaced as broken and MUST NOT remain apparently enabled while it cannot execute correctly.

### AIREQ020 — Condition evaluation is deterministic

Released Conditions MUST define operands, operators, types, missing/null behavior, fact-loading consistency point and versioning. Evaluation MUST NOT depend on ambient randomness or hidden mutable process state.

### AIREQ021 — Condition false is distinct from condition failure

A valid Condition evaluating `false` MUST be represented as a non-error skip. Invalid configuration, unavailable facts, authorization failure and transient evaluation failure MUST retain distinct behavior.

### AIREQ022 — Trigger identity follows producer contract

Event triggers MUST bind to stable logical producer event identity/version and required scope, not CLR type, database table name, private namespace or frontend-only names.

### AIREQ023 — Producer evolution includes Automation as a consumer

When a producer event used by Automation changes, Automation MUST be inventoried as a consumer and backlog/replay, additive/breaking compatibility and rollout order MUST be evaluated.

### AIREQ024 — Released trigger vocabulary matches runtime reachability

A trigger type MUST NOT be advertised as executable until a production consumer/evaluator exists and is tested. The current definition/runtime gap (`ValidTriggers` broader than the reachable WorkManagement trigger matrix) MUST remain visible until closed.

### AIREQ025 — Released action vocabulary matches an executor

An Action type MUST have a target semantic owner, config schema, authorization model, idempotency boundary, failure taxonomy and production executor before it is advertised as executable. Unsupported actions MUST continue to fail closed rather than be routed to the wrong executor.

### AIREQ026 — P5 multi-action execution is ordered and step-backed

P5 MUST support an ordered Action list. Actions execute sequentially in Rule order; each Action has a durable `AutomationExecutionStep` with stable step identity and status. A later Action MAY consume only explicitly declared output from an earlier successful Action. Terminal failure stops dependent later Actions; retry resumes the current retryable Step under the same logical operation identity and MUST NOT re-run already committed Steps. Arbitrary DAG/parallel execution is outside P5.

## 12. Execution / retry / history

### AIREQ027 — Execution identity is logical and stable

One logical Rule occurrence MUST resolve to one logical Automation Execution identity independent of worker attempt. Event-driven identity MUST include or derive from stable Rule identity plus source occurrence identity.

### AIREQ028 — Concurrent duplicate creation is constrained durably

Duplicate/concurrent source delivery MUST NOT create two logical executions for one Rule occurrence. Database uniqueness and message deduplication MUST remain compatible with the execution identity contract.

### AIREQ029 — Execution records semantic provenance

An Execution MUST retain Rule revision/configuration identity, trigger/source identity, Account/Workspace, correlation/causation and enough safe input provenance to explain the run without storing provider secrets.

### AIREQ030 — Execution lifecycle transitions are validated

Execution status transitions among `Queued`, `Running`, `Succeeded`, `Failed`, and `Cancelled` MUST be explicit, durable and rejected when invalid.

### AIREQ031 — Action-step state is real product state only when attached to execution

If Action Steps are released, they MUST be created, ordered, persisted and updated by the production execution path. Standalone entity/test existence does not prove step orchestration.

### AIREQ032 — Success follows durable effect evidence

Execution/step success MUST NOT be committed before the target-context or provider effect is durably known successful according to the owning contract.

### AIREQ033 — Target/business failure and technical failure remain distinct

Authorization, not-found, invariant/conflict, invalid configuration, retryable technical failure and unknown external outcome MUST not collapse into one generic retryable exception.

### AIREQ034 — Transport retry and Automation business retry have one coherent budget

Platform delivery retry and Automation retry policy MUST have explicit ownership and MUST NOT multiply into an uncontrolled retry storm.

### AIREQ035 — Retry preserves operation identity

Every retryable target/provider action MUST reuse a stable business operation identity or prove a reconciliation mechanism that makes repetition safe.

### AIREQ036 — Unknown external outcome is never blindly re-fired

Timeout/reset/indeterminate provider outcomes MUST enter reconciliation/terminal semantics according to the provider contract; redelivery alone MUST NOT grant permission to repeat the external side effect.

### AIREQ037 — Causation and recursion are bounded

Automation-produced events MUST carry enough causation/origin/depth identity to enforce bounded self-trigger/rule-to-rule recursion and detect runaway chains.

### AIREQ038 — Execution history is safe product evidence and remains distinct from Audit

Execution history MUST expose stable Automation meaning: Rule revision, trigger/source identity, ordered Action-step outcomes, attempts/failures and correlation data without secrets, provider credentials, unsafe payloads or internal stack traces. Execution history explains Automation behavior; governed security/business Audit remains a separate authority and MUST NOT be reconstructed or replaced by Automation history.

## 13. Scheduling / Templates / AI Agents

### AIREQ039 — Schedule intent is Automation product state

Schedule definition, timezone, lifecycle and missed-fire policy belong to Automation semantics; clock/timer/scheduler mechanics belong to Platform/Infrastructure.

### AIREQ040 — Timezone, DST and missed-fire policy are explicit

Recurring schedules MUST define timezone/DST behavior and whether missed occurrences are skipped, fired once, caught up, or bounded.

### AIREQ041 — Scheduled occurrence identity prevents duplicate execution

A logical scheduled occurrence MUST have durable identity suitable for scale-out, restart and duplicate scheduler delivery.

### AIREQ042 — Scheduler production reachability is required before release

`ScheduledJob` persistence/domain state alone does not certify scheduling. A released schedule capability requires a production scheduler/claim/delivery path and crash/restart proof.

### AIREQ043 — Template is creation input, not live shared authority

Automation Template application MUST copy/instantiate validated Rule configuration according to explicit semantics; later template edits MUST NOT silently mutate existing Rules.

### AIREQ044 — Template schemas participate in compatibility

Published Template trigger/action/config schema versions MUST follow the same discriminator/migration rules as Rules.

### AIREQ045 — AI Agent requires explicit product admission

`AiAgent`/`AiAgentRun` source types MUST remain `EXPLICIT_ADMISSION_REQUIRED` until product semantics, model/provider policy, tool permissions, authorization, data handling, cost/entitlement, audit and execution boundaries are approved.

### AIREQ046 — No hidden AI runtime capability

Tests/domain objects for AI Agents MUST NOT be interpreted as a production AI automation feature in API/frontend/certification until a complete runtime path exists.

## 14. Cross-context authorization / Billing / target actions

### AIREQ047 — Source transactions do not execute Automation side effects inline

WorkManagement/Documents/Collaboration producer transactions MUST publish committed facts; Automation reactions occur after durable fact publication unless an explicit synchronous invariant is approved.

### AIREQ048 — Target bounded context revalidates every action

Automation Conditions or prior authorization do not lock foreign state. The target capability MUST validate current authorization, scope, lifecycle, concurrency and invariants when the Action executes.

### AIREQ049 — Background Automation principal is explicit

Each Action MUST execute under an explicit trusted actor/delegation/service-principal model. `background` or `system` MUST NOT mean unrestricted authority.

### AIREQ050 — Permission revocation is honored at execution time

A Rule authorized at creation MUST NOT retain stale permission forever. Revoked membership/permission/resource access MUST cause future Actions to fail safely according to current policy.

### AIREQ051 — Cross-context actions use public capability contracts

Automation→WorkManagement/Documents/Collaboration/etc. MUST use consumer-owned ports/ACLs and target public Application contracts, never private DbContext/table access.

### AIREQ052 — Billing supplies capability/capacity facts, not plan branching

Automation/Integrations MUST consume Billing-owned capability/capacity contracts and MUST NOT branch on plan names or duplicate entitlement calculation.

### AIREQ053 — Capacity consumption and release semantics are operation-safe

Capacity consume/release MUST use stable logical operation identity and transaction semantics; retries, delete/archive/restore and failed create flows MUST not leak or double-release capacity.

### AIREQ054 — Cross-context handoffs preserve source identity and ownership

AIX source-event, target-action, provider-operation, entitlement and analytics handoffs MUST name semantic owner, producer/consumer, identity/scope, compatibility, authorization and evidence without moving business ownership.

## 15. Integration Connection / provider auth / secrets

### AIREQ055 — Integration Connection has explicit lifecycle

Connection identity and lifecycle MUST distinguish active, expired, revoked and error states plus reconnect/delete/restore semantics without conflating provider health or sync progress.

### AIREQ056 — Provider catalog and provider-specific connection configuration are canonical and typed

Supported provider identities/capabilities and provider-specific connection configuration MUST come from one canonical/versioned contract or generated source. Provider configuration MAY use flexible persistence, but it MUST remain typed/versioned and MUST NOT become a schema-less generic JSON dumping ground. For the P5 Calendar release, only Google and Microsoft/Outlook provider capabilities proven by backend contracts may be advertised; unrelated frontend/provider enum entries remain unavailable.

### AIREQ057 — Connection installation is Account/Workspace scoped and governed

Connect/reconnect/disconnect/configure operations MUST resolve Account/Workspace from trusted context/resource location and enforce Governance permission through the canonical pipeline.

### AIREQ058 — Integration provider identity is separate from Identity login and Workspace membership

External provider installation/authentication has Integration Connection semantics and MUST NOT reuse user-login linkage as provider connection ownership. Provider users, attendee emails or external identities MUST NOT create or grant Notrelix User/Account/Workspace membership automatically; Identity, Workspaces and Governance remain authoritative.

### AIREQ059 — Provider authorization is state/PKCE secured and least-privilege

Released OAuth/install providers MUST define state, PKCE where applicable, redirect/callback lifetime, initiating actor/scope, token exchange, reconnect and revoke behavior. They MUST request only scopes required by the released feature set; scope expansion requires explicit re-consent/reauthorization. Supplying a raw access token directly is not equivalent to a complete provider authorization flow.

### AIREQ060 — Reusable secrets live behind opaque references

Raw access/refresh tokens and reusable provider secrets MUST NOT be Domain properties, normal read DTOs, logs, events, analytics or frontend persisted state. Aggregates store opaque references/version metadata.

### AIREQ061 — Physical secret storage is restart-safe

The current DataProtection-backed secret store is acceptable only when the production key ring is durable and backup/rotation/recovery behavior is proven. Restart MUST NOT make stored provider credentials undecryptable.

### AIREQ062 — Secret versions and current pointers are durable authority

`IntegrationSecretVersion` is the durable version/reference history; `CurrentSecretVersion` is a pointer. Rotation MUST atomically establish a new reference/version and avoid orphan plaintext/encrypted blobs.

### AIREQ063 — Disconnect, secret retirement, provider revocation and provider-held copies are distinct

Calendar binding deactivation, generic Connection revocation, local secret cleanup, remote token/subscription revocation, and cleanup of provider-held copies MUST have explicit ordering and independently recorded outcomes. Provider-held data cleanup MUST be classified per capability as required, best-effort, user-managed, or impossible after disconnect. Local DB success MUST NOT falsely imply remote provider cleanup or deletion succeeded.

### AIREQ064 — Connection state and sync/provider health remain distinct

A transient provider request, sync failure or rate limit MUST NOT automatically rewrite durable Connection lifecycle without an explicit policy.

## 16. Outbound provider operations / N8n

### AIREQ065 — Outbound provider operation uses an Integration-owned contract

Automation requests provider effects through Integrations-owned ports/contracts; provider SDK/client details MUST remain Infrastructure/adapter concerns.

### AIREQ066 — Provider anti-corruption layer translates types and errors

Provider DTOs, enums, errors, rate-limit semantics and capabilities MUST be translated into stable Integration-owned outcomes rather than propagated through Automation or product domains.

### AIREQ067 — Outbound operation has stable logical idempotency identity

Every retriable provider side effect MUST define logical operation identity, provider idempotency key support where available, duplicate behavior and retention.

### AIREQ068 — Timeout/rate-limit behavior follows provider evidence

Retry classification MUST distinguish reject-before-execution evidence from indeterminate outcomes. Retry-After/backpressure MAY inform scheduling but MUST NOT override unknown-outcome safety.

### AIREQ069 — One end-to-end retry budget owns provider re-attempts

HTTP-client retry, MassTransit retry and Automation retry MUST not independently retry the same provider effect. Adapter-internal retries are forbidden unless the provider operation contract explicitly proves them safe.

### AIREQ070 — Unknown provider outcome has an executable reconciliation path

A failure marked `reconciliation required` MUST have an operationally executable, auditable resolution path and ownership. A log message or runbook-only dead end is insufficient for D5.

### AIREQ071 — ADR-008 is authoritative for current N8n settlement

The N8n prepare/effect/settle protocol, outcome classification and consumer-owned claim lifecycle MUST conform to accepted ADR-008 until superseded by another accepted decision.

### AIREQ072 — Provider-effect claim freshness does not grant re-fire authority

Fresh `Processing` means an active attempt may own the effect; stale `Processing` permits durable-state reconciliation only. Neither condition by itself permits repeating the provider call.

### AIREQ073 — Stale queued provider claim is governed recovery debt

A stale `Processing` claim with a `Queued` execution MUST be resolved by a controlled operator/reconciliation mechanism that records decision/evidence; it MUST NOT remain an undocumented permanent blocker or be auto-deleted.

### AIREQ074 — External provider effect runs outside the database transaction

Provider calls MUST execute outside held database transactions while durable prepare and settle phases preserve tenant/RLS scope, effect identity and crash-recoverable evidence.

## 17. Inbound webhook boundary

### AIREQ075 — Webhook raw HTTP boundary is bounded before trust

Inbound provider callbacks MUST enforce method/route, allowed content type, body-size/read-time bounds, UTF-8/format requirements and abuse/rate controls before business processing.

### AIREQ076 — Signature verification uses the exact provider-defined bytes

Signature/timestamp/replay verification MUST operate on the exact raw request bytes and provider protocol; reserialized JSON MUST NOT be substituted for signed content.

### AIREQ077 — Tenant routing derives from trusted binding, never payload

Webhook AccountId/WorkspaceId/ConnectionId MUST derive from a trusted route/binding/connection resolved independently of provider JSON and only be adopted after authenticity verification.

### AIREQ078 — Inbound provider delivery identity is scoped correctly

Dedup identity MUST use provider-documented uniqueness plus the required Connection/provider scope. Provider delivery identity and downstream product fact identity remain distinct.

### AIREQ079 — Inbound claim and durable processing intent are atomic

Accepted webhook receipt/claim and provider-neutral downstream processing message MUST be committed with one durable fate so an accepted callback cannot be silently lost.

### AIREQ080 — Tenant-scoped semantic processing occurs after commit

Downstream webhook processing MUST restore derived Account/Workspace through the messaging/RLS pipeline and load protected payload/state only within that trusted scope.

### AIREQ081 — Receipt becomes processed only after semantic reconciliation

A callback is not semantically complete merely because signature verification/receipt storage succeeded. Terminal `Processed` requires the Integration-owned reconciliation outcome to commit.

### AIREQ082 — Malformed/conflicting webhook payload fails closed

Malformed schema, mismatched verified event identity, unsupported resource mapping or conflicting identity links MUST produce deterministic terminal failure without mutating unrelated product state.

### AIREQ083 — Inbound receipt is not outbound WebhookDelivery

Infrastructure inbound receipt/dedup state MUST remain distinct from outbound `WebhookDelivery`; legacy `InboundWebhookEvent` MUST not grow into a second unreviewed source of truth.

### AIREQ084 — Provider-specific verification support is truthful

A generic HMAC test adapter MUST NOT be advertised as real Google/Microsoft/other provider webhook support unless the actual provider authenticity/subscription protocol and contract tests are implemented.

## 18. Sync / Calendar reconciliation

### AIREQ085 — Sync direction and source-of-truth are explicit

Every integration sync declares push/pull/bidirectional direction, authoritative owner and which provider facts may update Integration projection versus target product state.

### AIREQ086 — Sync cursor advances after durable successful processing

Cursor/checkpoint/progress MUST advance only after the corresponding mapped changes are durable; restart MUST resume without skipping or double-applying events.

### AIREQ087 — Sync conflict policy is product/provider semantics

ETag/version/timestamp conflicts MUST resolve by an explicit policy; last-write-wins or provider-wins MUST NOT be accidental implementation behavior.

### AIREQ088 — Mappings preserve internal and external identities

Calendar/event mappings MUST retain both Notrelix resource identity and provider external identity/ETag as appropriate, with uniqueness/conflict behavior explicitly tested.

### AIREQ089 — Calendar mapping preserves ownership and date/time meaning

CalendarIntegration/CalendarEvent state is provider mapping/projection, not WorkManagement/Documents lifecycle authority. Mapping MUST preserve the semantic distinction among date-only, instant, local date-time, time zone, all-day event and recurrence. A date-only WorkManagement value MUST NOT be silently converted into a UTC instant, and DST/time-zone conversion MUST be explicit and testable.

### AIREQ090 — Manual Calendar sync is production-reachable

P5 MUST implement manual Calendar sync through the canonical authenticated/authorized Application path and provider adapter. The command MUST create/reuse stable sync operation identity, honor direction/conflict rules, advance durable cursor only after successful durable reconciliation, and expose explicit retry/terminal outcomes. A `NotImplementedException`, fake success, or hidden unsupported branch is not an allowed final P5 state.

### AIREQ091 — Backfill and live processing converge without duplication

When sync/backfill and webhook/live traffic overlap, stable external/internal identities and cursor semantics MUST prevent double application or lost updates.

### AIREQ092 — Provider deletion/disconnect semantics do not delete Notrelix resources implicitly

Remote deletion, Connection revoke or Calendar binding deactivate MUST follow explicit mapping/retention policy and MUST NOT cascade-delete foreign bounded-context business resources by persistence convenience.

## 19. Frontend / API / realtime contracts

### AIREQ093 — Backend/public contract is the automation/integration API authority

OpenAPI/public Application contracts and approved event schemas are producer authority. Frontend handwritten types are consumers and MUST NOT become a second semantic source.

### AIREQ094 — Frontend contract drift is a blocking compatibility defect

Frontend automation/integration vocabularies, routes, DTOs and status values MUST match the released backend contract or be migrated through an explicit compatibility adapter.

### AIREQ095 — Automation authoring vocabulary is generated/shared deliberately

Trigger/action/status/config schema used by authoring UI MUST come from generated/contracted metadata or an approved shared public contract, not independent hard-coded unions that diverge from backend runtime.

### AIREQ096 — Frontend Automation repository has a real production adapter

`AutomationRuleRepository`/Execution/Template ports MUST have a production implementation bound at composition before authoring/history UI can be certified. Test fakes and TypeScript interfaces are not production reachability.

### AIREQ097 — Demo fixture data is not production server state

Default/demo Automation Rules in UI components MUST be confined to story/test/demo composition and MUST NOT be the fallback for real application state.

### AIREQ098 — Integrations frontend exposes only backend-supported capabilities

Provider list, connection statuses, connection/webhook endpoints and configuration forms MUST match actual backend-supported provider/endpoint contracts; generic FE `connections/webhooks` APIs cannot be certified while backend only exposes a different Calendar surface.

### AIREQ099 — Automation/Integrations query keys use canonical scope partitioning

All Account/Workspace server-state keys MUST use the canonical query-key scope helpers and remain safe across Account A→B→A and Workspace transitions.

### AIREQ100 — Realtime execution status reuses Domain lifecycle facts through a real public producer

Automation already raises Domain execution lifecycle facts such as started/succeeded/failed. P5 MUST map the owned lifecycle facts into a versioned public/realtime producer contract consumed by frontend event names/payloads; it MUST NOT create a second competing Execution lifecycle merely to satisfy the UI. Fixture-only frontend events or Domain events with no public/realtime mapping are not production evidence.

### AIREQ101 — Automation and Integration realtime recovery converge from durable truth

Realtime is freshness only. Automation execution status MUST recover from durable execution/history queries, and Integration connection/sync/error progress MUST recover from durable Integration state. On gap/reconnect the consumer MUST suspend or queue later envelopes, refetch authoritative state, reconcile/reset sequence checkpoints, then resume according to the Platform recovery contract. Missing realtime MUST never permanently lose current product state.

### AIREQ102 — OpenAPI/codegen and mixed-version behavior are gated

Contract-affecting changes MUST regenerate deterministic producer artifacts, compile/semantically test consumers and define old/new backend-web/mobile/worker combinations where deployment is non-atomic.

## 20. Persistence / migration / tenant safety

### AIREQ103 — Automation and Integrations own only their persistence

P5 may persist its own Rule/Execution/Schedule/Template/Agent/Connection/secret-reference/webhook/sync/calendar state. It MUST NOT directly write another bounded context's private tables.

### AIREQ104 — Workspace-scoped tables remain under RLS defense-in-depth

Production requests and workers touching Automation/Integrations workspace state MUST apply the canonical tenant/RLS session context. RLS is defense-in-depth and does not replace Application authorization.

### AIREQ105 — Technical secret/receipt state has constrained access paths

Secret blobs and inbound receipt reliability state that intentionally do not model tenant columns MUST be reachable only through trusted opaque references/bindings and must not become general query surfaces.

### AIREQ106 — Migration evidence includes clean database and supported upgrade

Schema/config/discriminator/index changes MUST prove clean database creation, supported upgrade from the declared baseline, migration discipline and no pending model drift at the exact candidate.

### AIREQ107 — Persisted JSON/discriminator changes are migration-sensitive

Rule configuration, provider config, status/discriminator and mapping-key evolution MUST include dual-reader/upcast/migration/fail-safe behavior as applicable; old rows MUST NOT be silently reinterpreted.

### AIREQ108 — Concurrency constraints match semantic identities

Database indexes/versions/conditional writes MUST enforce logical uniqueness such as one execution per (Rule, trigger occurrence), connection/provider/binding uniqueness and webhook receipt dedup where required.

### AIREQ109 — Soft-delete/retention behavior is explicit

Soft deletion, restore and retention for Rules, Templates, Connections, Calendar mappings and related history MUST preserve required audit/history while excluding deleted state from normal operations.

### AIREQ110 — No cross-context cascade semantics by database convenience

Foreign keys/cascades MUST NOT make P5 delete or mutate resources owned by WorkManagement/Documents/Collaboration/Identity/Billing. Cross-context lifecycle is an explicit contract.

## 21. Observability / performance / operations

### AIREQ111 — Correlation traces the semantic chain without conflating identities

Observability MUST relate source event, Rule, Execution, Action, target operation, Connection/provider operation, webhook delivery and worker attempt while preserving their distinct identities.

### AIREQ112 — Message/provider/operation/correlation identities remain distinct

EventId, SourceEventId, RuleId, ExecutionId, target OperationId, provider event/operation id, CorrelationId and CausationId MUST not be reused interchangeably.

### AIREQ113 — Telemetry and history redact secrets and unsafe payloads

Logs, traces, metrics, execution errors/history and provider diagnostics MUST redact access tokens, webhook secrets, encrypted blobs, provider credentials and sensitive payload data.

### AIREQ114 — Operational metrics expose backlog and failure semantics

P5 MUST expose actionable measurements for event/consumer backlog, execution latency/status, retries, poison/terminal outcomes, stale claims/reconciliation, webhook verification, sync lag and provider failures.

### AIREQ115 — Provider backpressure is isolated and observable

A degraded/rate-limited provider or connection MUST not globally block unrelated providers/workspaces; queue growth, retry-after and throttling state MUST be bounded/observable.

### AIREQ116 — Fanout and recursion have capacity controls

One event matching many Rules, chained Automations and provider operations MUST have explicit throughput/fanout/rate/entitlement guards so retries or loops cannot create unbounded work.

### AIREQ117 — Recovery procedures are governed and auditable

Operator repair/reconciliation that changes durable claim/execution/connection/sync state MUST use reviewed procedures/tools, preserve lineage and record what was changed; diagnostics MUST not mutate product state as a shortcut.

### AIREQ118 — Readiness fails when required dependencies are unavailable

Required messaging, secret storage/key ring, database/RLS, provider configuration or other mandatory runtime dependencies MUST fail readiness or capability availability honestly rather than report fake success.

## 22. Architecture / compatibility / certification

### AIREQ119 — TAC AI-FLOW-01 through AI-FLOW-07 are reused as canonical cross-boundary flows

The execution package MUST crosswalk existing TAC AI flows to P5 requirements and source evidence instead of inventing parallel flow IDs. Historical disposition text that no longer matches the exact candidate MUST be marked stale rather than copied as current status.

### AIREQ120 — Platform flow contracts are inherited, not redefined

Broker consumption, tenant restoration, dedup, outbox, retry/poison/recovery and event compatibility MUST reuse the canonical Platform PF-FLOW and BE-PLT requirements applicable to each P5 consumer.

### AIREQ121 — Architecture gates prevent provider and foreign-domain leakage

Architecture tests MUST enforce Domain purity, bounded-context ownership, public cross-context ports, no provider SDK/model leakage into Automation Domain, and no private foreign persistence access.

### AIREQ122 — Verification proves actual runtime owner

For transaction/retry/RLS/provider semantics, unit tests of a helper are insufficient. Tests MUST execute the production composition owner (pipeline, MassTransit consumer, EF/PostgreSQL/provider adapter boundary as applicable).

### AIREQ123 — Every critical flow has negative/failure evidence

Authorization denial, tenant mismatch, duplicate/concurrent delivery, invalid config, stale claim, provider unknown outcome, webhook replay/signature failure, migration upgrade and contract drift MUST have explicit failure-path tests.

### AIREQ124 — Contract evolution accounts for queued/backlogged/deployed consumers

Changes to source events, dispatch intents, provider callbacks, persisted discriminators and realtime contracts MUST consider old workers, queued/dead-letter messages, replay and independently deployed web/mobile consumers.

### AIREQ125 — Service extraction is a later governed decision

Automation and Integrations may be future service candidates, but this workstream MUST first prove explicit contracts, isolated data ownership, failure semantics, tenancy/auth propagation and operational ownership inside the modular monolith.

### AIREQ126 — Cross-team handoff is explicit

Every P5 producer/consumer handoff MUST record capability, current/target contract, producer/consumer owners, breaking/additive class, migration, both teams' actions, verification, required readiness and rollback/forward-fix.

### AIREQ127 — Readiness is capability- and consumer-specific

P5 MUST NOT receive one blanket D5. Rule authoring, Work action, N8n provider effect, Calendar connection/webhook, sync, frontend and realtime consumers are certified independently against their actual dependencies and released scope.

### AIREQ128 — Final certification binds exact source, migrations, tests and CI

P5 is complete only when SPEC→PLAN→TESTS→source/migrations→runtime execution→CI evidence agree on the exact candidate SHA, all release-scoped blockers are closed/accepted explicitly, and downstream consumers have actionable handoff evidence.

# Canonical authority crosswalk
## 23. Product Automation rules

All 35 product Automation rules remain authoritative. This table shows where this execution SPEC operationalizes them.

| Product rule | Execution requirements |
|---|---|
| `AUT-001` | AIREQ009, AIREQ017 |
| `AUT-002` | AIREQ011 |
| `AUT-003` | AIREQ012 |
| `AUT-004` | AIREQ013–AIREQ014, AIREQ029 |
| `AUT-005` | AIREQ022–AIREQ024 |
| `AUT-006` | AIREQ047 |
| `AUT-007` | AIREQ039–AIREQ042 |
| `AUT-008` | AIREQ040 |
| `AUT-009` | AIREQ027–AIREQ028, AIREQ041 |
| `AUT-010` | AIREQ020 |
| `AUT-011` | AIREQ021 |
| `AUT-012` | AIREQ025, AIREQ048–AIREQ051 |
| `AUT-013` | AIREQ017–AIREQ019, AIREQ025 |
| `AUT-014` | AIREQ026, AIREQ031 |
| `AUT-015` | AIREQ002, AIREQ065 |
| `AUT-016` | AIREQ027–AIREQ029 |
| `AUT-017` | AIREQ032 |
| `AUT-018` | AIREQ034–AIREQ036, AIREQ067 |
| `AUT-019` | AIREQ038 |
| `AUT-020` | AIREQ049–AIREQ050 |
| `AUT-021` | AIREQ038 |
| `AUT-022` | AIREQ037, AIREQ116 |
| `AUT-023` | AIREQ052–AIREQ053 |
| `AUT-024` | AIREQ028, AIREQ035 |
| `AUT-025` | AIREQ048 |
| `AUT-026` | AIREQ043–AIREQ044 |
| `AUT-027` | AIREQ045–AIREQ046 |
| `AUT-028` | AIREQ025 |
| `AUT-029` | AIREQ060–AIREQ062 |
| `AUT-030` | AIREQ027, AIREQ112 |
| `AUT-031` | AIREQ100–AIREQ101 |
| `AUT-032` | AIREQ075–AIREQ084 |
| `AUT-033` | AIREQ048–AIREQ051, AIREQ121 |
| `AUT-034` | AIREQ019, AIREQ025 |
| `AUT-035` | AIREQ038, AIREQ113 |

## 24. Product Integrations rules

All 38 product Integrations rules remain authoritative.

| Product rule | Execution requirements |
|---|---|
| `INT-001` | AIREQ002, AIREQ066 |
| `INT-002` | AIREQ055, AIREQ063 |
| `INT-003` | AIREQ056–AIREQ057 |
| `INT-004` | AIREQ056–AIREQ057 |
| `INT-005` | AIREQ060–AIREQ062 |
| `INT-006` | AIREQ061–AIREQ064 |
| `INT-007` | AIREQ075–AIREQ076 |
| `INT-008` | AIREQ077 |
| `INT-009` | AIREQ078–AIREQ081 |
| `INT-010` | AIREQ078, AIREQ112 |
| `INT-011` | AIREQ085 |
| `INT-012` | AIREQ085, AIREQ089 |
| `INT-013` | AIREQ086 |
| `INT-014` | AIREQ086 |
| `INT-015` | AIREQ064 |
| `INT-016` | AIREQ088 |
| `INT-017` | AIREQ063, AIREQ092 |
| `INT-018` | AIREQ092 |
| `INT-019` | AIREQ067 |
| `INT-020` | AIREQ070, AIREQ073 |
| `INT-021` | AIREQ068–AIREQ069, AIREQ115 |
| `INT-022` | AIREQ087 |
| `INT-023` | AIREQ089 |
| `INT-024` | AIREQ089 |
| `INT-025` | AIREQ058 |
| `INT-026` | AIREQ075, AIREQ082 |
| `INT-027` | AIREQ067–AIREQ069 |
| `INT-028` | AIREQ051, AIREQ103, AIREQ110 |
| `INT-029` | AIREQ049 |
| `INT-030` | AIREQ052 |
| `INT-031` | AIREQ066, AIREQ124 |
| `INT-032` | AIREQ100–AIREQ101 |
| `INT-033` | AIREQ063 |
| `INT-034` | AIREQ092 |
| `INT-035` | AIREQ057–AIREQ060 |
| `INT-036` | AIREQ066 |
| `INT-037` | AIREQ017, AIREQ107 |
| `INT-038` | AIREQ125 |

## 25. Team capability labels

These are team capability labels from `docs/workstreams/teams/automation-integrations.md`, not product rule IDs.

| Team capability | Execution requirements |
|---|---|
| Automation `AUT-001 Rule lifecycle` | AIREQ009–AIREQ016 |
| Automation `AUT-002 Trigger model` | AIREQ017–AIREQ024 |
| Automation `AUT-003 Condition model` | AIREQ017–AIREQ021 |
| Automation `AUT-004 Action model` | AIREQ017–AIREQ019, AIREQ025–AIREQ026 |
| Automation `AUT-005 Enable/disable` | AIREQ011–AIREQ015 |
| Automation `AUT-006 Source-event matching` | AIREQ022–AIREQ024, AIREQ047 |
| Automation `AUT-007 Execution creation` | AIREQ027–AIREQ029 |
| Automation `AUT-008 Execution state` | AIREQ030–AIREQ032, AIREQ037–AIREQ038 |
| Automation `AUT-009 Business retry/failure` | AIREQ033–AIREQ036 |
| Automation `AUT-010 History` | AIREQ038 |
| Automation `AUT-011 Authoring frontend` | AIREQ093–AIREQ099 |
| Automation `AUT-012 Realtime status` | AIREQ100–AIREQ101 |
| Automation `AUT-013 Entitlement integration` | AIREQ052–AIREQ053 |
| Automation `AUT-014 Hardening/observability` | AIREQ111–AIREQ128 |
| Integrations `INT-001 Connector catalog` | AIREQ056, AIREQ093–AIREQ098 |
| Integrations `INT-002 Connection lifecycle` | AIREQ055, AIREQ063–AIREQ064 |
| Integrations `INT-003 Provider auth/OAuth` | AIREQ058–AIREQ060 |
| Integrations `INT-004 Connection config` | AIREQ017, AIREQ055–AIREQ064 |
| Integrations `INT-005 Credential contract` | AIREQ060–AIREQ063 |
| Integrations `INT-006 Outbound operation` | AIREQ065–AIREQ074 |
| Integrations `INT-007 Webhook verification` | AIREQ075–AIREQ084 |
| Integrations `INT-008 Inbound mapping` | AIREQ078–AIREQ083 |
| Integrations `INT-009 Connection health` | AIREQ064, AIREQ114–AIREQ118 |
| Integrations `INT-010 Failure normalization` | AIREQ066–AIREQ070 |
| Integrations `INT-011 Management frontend` | AIREQ093–AIREQ099 |
| Integrations `INT-012 Provider limits` | AIREQ068–AIREQ069, AIREQ115 |
| Integrations `INT-013 Entitlements` | AIREQ052–AIREQ053 |
| Integrations `INT-014 Security hardening` | AIREQ057–AIREQ064, AIREQ103–AIREQ128 |

# TAC flow reuse

## 26. AI-FLOW crosswalk

The seven TAC AI flows are reused; no competing flow namespace is created.

| TAC flow | Purpose | Exact-candidate posture | Current production path | Remaining constraint |
|---|---|---|---|---|
| `AI-FLOW-01` | Automation local Rule creation + Billing capacity | PRODUCTION_REACHABLE / PARTIAL_GAP | `CreateAutomationRuleCommand` → Billing capability/capacity → `AutomationRule.Create` under canonical request transaction. | Config request shape is not target-complete; Rule lifecycle lacks released update/delete/archive paths and Billing capacity release lifecycle is unresolved. |
| `AI-FLOW-02` | Work fact → Automation process | PRODUCTION_REACHABLE / PARTIAL_GAP | Work integration events → MassTransit tenant/dedup pipeline → Automation consumers → `N8nAutomationRuleEvaluator` → `AutomationExecution` + durable dispatch intent. | Only a subset of advertised trigger vocabulary is runtime-reachable; condition evaluation is not in the production evaluator. |
| `AI-FLOW-03` | Automation → Work target action | PRODUCTION_REACHABLE | `AutomationMoveItemRequestedV1` → consumer → `AutomationMoveItemUseCase` → `IWorkActionPort` → `WorkItemActionAdapter` → WorkManagement public action. | Must remain the model for target-context revalidation/idempotency; do not replace with foreign DbContext access. |
| `AI-FLOW-04` | Automation → N8n provider operation | PRODUCTION_REACHABLE / OPERATIONAL_GAP | `N8nDispatchRequestedV1` → consumer-owned prepare/effect/settle → Integrations public action → `N8nClient` under ADR-008. | Unknown outcomes are safe from blind retry, but stale Queued+Processing residue still requires an operator/reconciliation process; D5 needs executable governed recovery. |
| `AI-FLOW-05` | Calendar connection + binding creation | PRODUCTION_REACHABLE | `ConnectCalendarCommand` persists/reuses Connection, secret version/blob and CalendarIntegration in one request transaction. | Historical TAC text that still says handler is a stub is stale at this candidate. Provider OAuth is not equivalent to the current raw access-token connect API. |
| `AI-FLOW-06` | Calendar binding disconnect | PRODUCTION_REACHABLE / PARTIAL_EXTERNAL_CLEANUP | `DisconnectCalendarCommand` deactivates CalendarIntegration first and revokes generic Connection only when no active binding remains; revocation event drives secret cleanup. | External provider token/subscription revocation and its unknown/retryable outcomes are separate release work; local cleanup must not overclaim remote cleanup. |
| `AI-FLOW-07` | Verified Calendar inbound webhook | PRODUCTION_REACHABLE / PROVIDER_PROTOCOL_LIMIT | bounded raw HTTP → trusted binding lookup → signature/timestamp verification → derived tenant → atomic receipt + outbox → tenant-scoped semantic Calendar reconciliation. | Current verifier is a generic HMAC contract; real provider-specific webhook authenticity/subscription support must be proven before advertising Google/Microsoft production support. |

## 27. Platform-flow inheritance

P5 messaging consumers inherit applicable Platform contracts, especially:

```text
PF-FLOW-02 DomainEvent → IntegrationEvent → outbox
PF-FLOW-03 broker delivery + tenant restoration + dedup
PF-FLOW-04 delivery retry/failure
PF-FLOW-05 contract evolution/recovery capability
PF-FLOW-06 background actor/security context
PF-FLOW-07 scoped Integration Event tenant envelope
```

P5 TESTS must rerun affected proof on the exact candidate.

`Notrelix.Platform.Tests` or isolated helper tests do not prove MassTransit/Infrastructure production semantics.

# Gap register

## 28. Confirmed current gaps/debts

The following are source findings, not speculative future features.

| Gap | Topic | State | Exact finding | Requirements |
|---|---|---|---|---|
| `AI-GAP-01` | Rule/API configuration contract drift | CONFIRMED | Backend create accepts one `Configuration` string and applies it to Trigger and Action; FE models separate trigger/action config plus different fields/vocabulary. | AIREQ017–AIREQ019, AIREQ093–AIREQ095 |
| `AI-GAP-02` | Immutable Rule revision/config snapshot missing | CONFIRMED | `AutomationExecution` stores RuleId/TriggerId but no immutable rule revision; MoveItem/N8n execution loads current Rule at dispatch time. | AIREQ013–AIREQ014, AIREQ029 |
| `AI-GAP-03` | Condition and multi-action runtime are not production-complete | CONFIRMED | Condition value object exists but create/runtime evaluator does not construct/evaluate it; ExecutionStep exists but production code does not attach steps; configuration is single Action. | AIREQ020–AIREQ026, AIREQ031 |
| `AI-GAP-04` | Trigger/action vocabulary exceeds executors | CONFIRMED | Rule engine accepts more trigger/action discriminators than runtime consumers/executors; unsupported Action currently fails closed with `M8-AUTOMATION-ACTION-PARITY`. | AIREQ024–AIREQ025 |
| `AI-GAP-05` | Schedule/Templates/AI Agents are domain-heavy but not production-reachable | CONFIRMED | Persisted/domain types and tests exist, but no complete Application/API/runtime product path was found for broad release. | AIREQ039–AIREQ046 |
| `AI-GAP-06` | Automation frontend is not contract-aligned/wired | CONFIRMED | FE uses card-era trigger/action vocabulary, repository ports lack a production adapter in source search, component has demo defaults, and FE realtime events have no matching backend producer found. | AIREQ093–AIREQ101 |
| `AI-GAP-07` | Automation permission vocabulary is coarse/inconsistent | REVIEW_REQUIRED | Create/enable use `ManageWorkspaceSettings`; execution history uses `ViewBoard` against `automation.rule`. Product resource/action meaning needs deliberate Governance mapping rather than incidental reuse. | AIREQ048–AIREQ051, AIREQ121 |
| `AI-GAP-08` | Billing capacity release lifecycle | ACCEPTED_DEBT | Create consumes AutomationRule capacity transactionally, but deletion/archive release lifecycle is not wired in current production path (`M9-CAPACITY-RELEASE-LIFECYCLE`). | AIREQ015, AIREQ052–AIREQ053 |
| `AI-GAP-09` | Provider OAuth/install flow incomplete | CONFIRMED | Calendar connect accepts a raw access token; no complete provider OAuth state/PKCE/callback/token-exchange flow is represented by that endpoint. | AIREQ058–AIREQ060 |
| `AI-GAP-10` | Provider/catalog vocabulary drift | CONFIRMED | Backend generic providers, Calendar providers and FE integrations provider union differ materially; FE generic endpoints do not match current backend Calendar endpoints. | AIREQ056, AIREQ093–AIREQ098 |
| `AI-GAP-11` | N8n reconciliation is safe but operationally incomplete | PARTIAL_GAP | ADR-008 prevents blind re-fire; stale Queued+Processing claim intentionally requires operator sweep and unknown outcomes require reconciliation. Current proof is runbook/tests rather than an automated product recovery workflow. | AIREQ070–AIREQ073, AIREQ117 |
| `AI-GAP-12` | Manual Calendar sync is still a stub | CONFIRMED | `TriggerCalendarSyncCommandHandler` throws `NotImplementedException`; it is intentionally not an AI-FLOW-07 blocker but means manual sync is not released. | AIREQ085–AIREQ091 |
| `AI-GAP-13` | Generic outbound webhook domain is not a certified runtime | CONFIRMED | `WebhookSubscription`/`WebhookDelivery` domain+persistence exist, but TAC classifies outbound delivery as a legacy/operational boundary and no complete released Application/API/worker flow was found. | AIREQ006–AIREQ007, AIREQ083, AIREQ119 |
| `AI-GAP-14` | Provider-specific webhook authenticity not proven | CONFIRMED | Calendar endpoint/provider enum names Google/Microsoft while verifier currently uses configurable HMAC over `{timestamp}.{rawBody}`; that is a valid internal contract but not proof of each external provider's real protocol. | AIREQ075–AIREQ084 |
| `AI-GAP-15` | Execution lifecycle facts exist but public/realtime mapping is missing | CONFIRMED | Domain raises `automation.automation-execution-*` lifecycle facts, while FE consumes `automation.execution.*`; no production public/realtime mapper/producer contract connecting those vocabularies was found. | AIREQ100–AIREQ101 |
| `AI-GAP-16` | Historical TAC flow dispositions contain stale source-status text | DOC_STALE | AI-FLOW-05/06/07 sections contain older `NotImplementedException` descriptions although exact candidate source implements connect/disconnect/webhook. Flow IDs/invariants remain reusable; source-status text must not be copied. | AIREQ001, AIREQ004–AIREQ006, AIREQ119 |

## 29. Gap handling rule

A gap may be closed by:

```text
IMPLEMENT
HARDEN_EXISTING
CONTRACT_MIGRATION
DOC_STALE_CORRECTION
EXPLICIT_DEFER / NOT_APPLICABLE
ACCEPTED_DEBT with owner + exit condition
```

It MUST NOT be closed by:

```text
source file exists
unit test exists
frontend mock exists
Domain type exists
comment says TODO/debt
CI is green but the scenario is not executed
```

# Readiness

## 30. Consumer-specific readiness targets

| Capability | Required readiness before broad consumer reliance |
|---|---|
| Rule identity/lifecycle + released CRUD | D5 |
| released trigger/action/config contract | D5 |
| execution identity + immutable semantics | D5 |
| WorkManagement target-action reference | D5 |
| Billing capacity consume/release for released lifecycle | D4–D5 according to operation |
| N8n provider-effect reference | D5 for the released provider operation |
| Connection + secret reference lifecycle | D5 |
| released provider OAuth/install flow | D5 |
| Calendar verified webhook | D5 for the actual provider protocol being advertised |
| Calendar sync | D4+ per released direction/provider |
| Automation frontend authoring/management contract | D4+; producer contract D5 |
| Automation realtime status | D4+ per consumer; durable query remains authority |
| Scheduled Automation | D4+ only if release-scoped |
| Templates | D4+ only if release-scoped |
| AI Agents | explicit admission; no implied readiness from Domain source |

No single blanket P5 readiness value may hide a blocked released capability.

# Acceptance criteria

## 31. Functional and architectural acceptance


### AIAC001 — Ownership separation

Automation, Integrations, Platform, Governance, Billing and target contexts retain the ownership boundaries defined in this SPEC; architecture tests find no foreign private persistence or provider leakage.

### AIAC002 — Released Rule lifecycle

Released Rule operations are server-authorized, tenant-safe, valid, idempotent where required and have explicit lifecycle/capacity/history semantics.

### AIAC003 — Configuration contract

Released trigger/condition/action schemas are independently typed/versioned and backend/frontend/runtime vocabulary is aligned.

### AIAC004 — Trigger consumption

Every released event trigger has a named producer contract, durable tenant-scoped delivery, duplicate/concurrency proof and compatibility/backlog policy.

### AIAC005 — Execution identity and revision

Duplicate occurrence cannot create duplicate logical work and every Execution identifies the immutable Rule semantics it runs.

### AIAC006 — Target actions

Every released product Action reaches the target through a public capability contract that revalidates current authorization, scope, concurrency and invariants.

### AIAC007 — Provider effect safety

Every released external provider Action has stable operation identity, one retry owner, outcome classification and no-blind-retry reconciliation for unknown outcomes.

### AIAC008 — N8n reference

ADR-008 prepare/effect/settle, fresh/stale claim behavior and an executable governed reconciliation path pass production-composition tests.

### AIAC009 — Connection and secret lifecycle

Released Connection flows enforce ManageIntegrations or approved equivalent, store only opaque secret references in Domain, survive restart and define disconnect/revoke/cleanup outcomes.

### AIAC010 — Inbound webhook

Released webhook flow verifies raw provider authenticity before tenant adoption, dedups correctly, processes under tenant/RLS scope and reaches a real semantic terminal state.

### AIAC011 — Calendar sync

Released Calendar sync defines direction, cursor, mapping, conflicts, overlap/restart behavior and contains no advertised `NotImplementedException` path.

### AIAC012 — Deferred capability honesty

Schedules, Templates, AI Agents and generic outbound webhook runtime are either fully admitted/released with proof or explicitly non-release-scoped.

### AIAC013 — Frontend authoring/management

Web/mobile types, routes, repositories, query keys and provider catalogs consume canonical contracts and do not rely on demo/fake state in production composition.

### AIAC014 — Realtime status

If released, backend producer and frontend adapter share a contract and gap recovery converges from durable execution state before delivery resumes.

### AIAC015 — Tenant/data safety

RLS, Application authorization, resource location, worker tenant restoration and Account/Workspace isolation pass cross-tenant negative tests.

### AIAC016 — Migration/compatibility

Clean DB, supported upgrade, no pending model drift, persisted-config migration and queued/backlog mixed-version behavior pass on the candidate.

### AIAC017 — Operations/observability

Backlog, retries, stale claims, reconciliation, sync lag, provider errors/rate limits and correlation are visible without leaking secrets.

### AIAC018 — Exact-candidate certification

All release-scoped AIREQ requirements have PLAN work units, executable TESTS, exact SHA/runtime/CI evidence, and consumer-specific readiness/handoff records.

# Required implementation invariants

## 32. Rule revision / execution snapshot target

The target must make this sequence deterministic:

```text
Rule revision R1 enabled
→ source event E arrives
→ Execution X created and bound to R1
→ user edits Rule to R2 / disables Rule
→ X still has an explicit, reviewable policy:
   execute R1
   OR cancel/fail under a documented transition
```

Forbidden:

```text
Execution X created under R1
→ dispatcher later reloads mutable Rule R2
→ silently performs R2 without historical evidence
```

## 33. Cross-context target-action target

Reference flow:

```text
source event
→ Automation Execution
→ durable action intent
→ Automation-owned port / ACL
→ target public Application capability
→ current target authorization + scope + invariant + idempotency
→ durable target outcome
→ Automation Execution settles
```

Forbidden:

```text
Automation
→ target DbContext / table / Domain aggregate mutation
```

## 34. External provider-effect target

Reference flow:

```text
durable execution + provider operation identity
→ durable prepare
→ provider call outside DB transaction
→ outcome classification
→ durable settle
```

Unknown:

```text
no blind retry
→ explicit reconciliation ownership
```

Retryable:

```text
prove safe-to-retry evidence
→ durable retry evidence
→ release/reacquire claim under the same logical identity
```

## 35. Inbound webhook target

Reference flow:

```text
raw request bounds
→ exact raw-byte verification
→ trusted binding
→ provider authenticity
→ derive tenant
→ atomic receipt/dedup + durable processing intent
→ tenant-restored semantic reconciliation
→ terminal receipt status
```

Payload-supplied tenant identifiers are never authority.

## 36. Frontend contract target

Target:

```text
backend/public contract
→ OpenAPI / event schema / approved metadata
→ deterministic generation or explicit adapter
→ web/mobile state + query + realtime consumer
```

Not:

```text
backend strings/enums
+
separate handwritten FE unions
+
manual translation forever
```

# Testing obligations for the next TESTS file

## 37. Minimum deep-test families

The subsequent TESTS file MUST define executable scenarios for at least:

```text
Rule:
- create/list/enable/disable
- invalid config
- edit/disable during queued execution
- revision snapshot
- capacity concurrency + release lifecycle
- tenant/resource authorization

Trigger:
- each released source event
- duplicate/concurrent source occurrence
- producer compatibility
- unsupported trigger fail-safe

Condition:
- true / false
- null/missing
- invalid configuration
- unavailable fact
- deterministic consistency point

Action:
- released executor parity
- target permission revoked
- target conflict/not-found
- technical retry
- duplicate target operation
- unsupported action fail-closed

Execution:
- one Rule+occurrence => one Execution
- retry identity
- crash boundaries
- history redaction

N8n:
- every ADR-008 outcome class
- fresh Processing duplicate
- stale Running+Processing
- stale Queued+Processing operator recovery
- lost claim transition fail-closed
- provider call count proves no blind replay

Connection/secrets:
- Owner/Admin allow; Member/Guest deny
- secret round trip across restart-compatible key ring
- rotation
- last-binding disconnect
- secret cleanup outcome
- no secret in API/log/event

Webhook:
- content type/body size/read timeout
- invalid UTF-8/JSON
- invalid signature/timestamp
- unknown path/provider mismatch
- concurrent replay
- tenant derivation from binding
- receipt/outbox atomicity
- semantic processing terminal outcome

Sync:
- push/pull authority
- cursor commit ordering
- restart/backfill/live overlap
- ETag/conflict
- `TriggerCalendarSync` not advertised until implemented

Frontend:
- generated/adapter contract parity
- A→B→A Account/Workspace isolation
- real repository composition
- no demo fallback
- realtime producer/consumer schema
- gap recovery convergence

Migration:
- clean DB
- supported upgrade
- persisted config/discriminator evolution
- no pending model drift
```

# Handoff and escalation

## 38. Canonical cross-team handoff schemas

The team authority in `docs/workstreams/teams/automation-integrations.md` defines two mandatory handoff shapes. This execution package preserves them verbatim and only appends certification metadata.

### Source-event → Automation

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

### Automation → Integrations

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

### Certification metadata appended to either handoff

```text
Candidate SHA:
Breaking/additive:
Migration/rollout strategy:
Evidence locator:
Known debt/blocker:
Invalidation trigger:
Rollback/forward-fix:
```

For Automation → target-context actions that are not provider operations, record the target public capability, current authorization revalidation, operation identity, failure taxonomy and target-owner tests in the same source/consumer handoff record. Generic Platform/P5 labels MUST NOT replace the authority fields above.

## 39. Escalation boundary

Team-local:

```text
private evaluator decomposition
private provider adapter internals
test fixtures/builders
performance optimization preserving contracts
local UI composition
```

Escalate before implementation when:

```text
new scripting/expression engine
arbitrary code execution
AI Agent production admission
new provider SDK as repo-wide dependency
new secret architecture
new background/system principal model
source-event breaking change
new cross-context command ownership
new global delivery/retry semantics
new service/project/package architecture
weakened authorization/tenant boundary
provider effect semantics conflict with ADR-008
```

# Exit rule

## 40. P5 completion contract

P5 is ready for broad delivery when:

```text
released Rule/trigger/action schemas are stable and consumer-compatible
execution identity + immutable Rule semantics are durable
duplicate delivery cannot duplicate logical execution/effect
target contexts revalidate current authorization/invariants
provider effects have safe retry/unknown-outcome semantics
Connection/secret/webhook/sync ownership is explicit
tenant/RLS scope is proven in request + worker paths
frontend consumes canonical contracts
realtime (if released) converges from durable state after gaps
persisted configuration/migrations are compatible
operational recovery is executable and observable
all release-scoped AIREQ requirements have TEST/CERT evidence on exact SHA
```

A capability that is `DOMAIN_ONLY`, `CONTRACT_DRIFT`, `PARTIAL_GAP`, or `EXPLICIT_ADMISSION_REQUIRED` cannot be represented as D5 merely because adjacent P5 flows are stable.
