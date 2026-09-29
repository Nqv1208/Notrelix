---
document_id: WRK-PLAN-BILLING-ENTITLEMENTS
document_type: workstream-plan
status: active
owner: billing-entitlements-team
source_branch: develop
source_commit: 35702d0fa9fb01ed68b0667bab500030d60bd028
spec:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
applies_to:
  - backend
  - frontend
  - billing
  - plans
  - subscriptions
  - entitlements
  - resource-capacity
  - metered-usage
  - billing-api
  - billing-administration
  - provider-integration
  - provider-webhooks
  - reconciliation
  - invoices
  - payments
  - payment-methods
  - migration
  - security
  - observability
canonical_evidence:
  - docs/product/billing.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/billing-entitlements.md
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
review_on:
  - billing-source-change
  - commercial-catalog-change
  - plan-versioning-change
  - subscription-lifecycle-change
  - entitlement-contract-change
  - hard-capacity-change
  - metered-usage-change
  - billing-api-change
  - provider-selection
  - provider-contract-change
  - webhook-change
  - reconciliation-change
  - payment-state-change
  - account-billing-boundary-change
  - billing-authorization-change
  - billing-retention-change
---

# PLAN — Billing & Entitlements

## 1. Purpose

This plan converts `billing-entitlements.spec.md` into an executable delivery sequence.

It is intentionally source-first. The current Billing bounded context already contains substantial Domain vocabulary and one mature reference slice for entitlement-driven hard capacity. The task is therefore not to rebuild Billing from zero and not to expose generic CRUD over the existing entities.

The execution goal is to close the commercial causal chain:

```text
Account
  ↓
immutable commercial Plan revision
  ↓
Account-rooted Subscription
  ↓
Entitlement derivation/reconciliation
  ↓
stable capability decision
  ├── hard resource capacity
  └── metered usage
  ↓
billing administration
  ↓
provider operation / webhook / reconciliation
  ↓
invoice + payment + payment-method evidence
  ↓
retained commercial history
```

Every work unit in this plan MUST preserve the ownership and failure semantics defined by the SPEC.

## 2. Candidate baseline

All classifications and source paths in this plan are based on:

```text
branch: develop
commit: 35702d0fa9fb01ed68b0667bab500030d60bd028
```

Before implementation begins, Phase 0 MUST re-record the actual candidate SHA. If the source has moved, the executor MUST diff the Billing-relevant changes and reconcile this plan before applying any destructive refactor or migration.

The audited baseline contains, among other evidence:

```text
backend/src/Notrelix.Domain/Billing
backend/src/Notrelix.Application/Features/Billing
backend/src/Notrelix.Application/Common/Requests/Gates/IRequireSubscription.cs
backend/src/Notrelix.Application/EventMappers/Billing
backend/src/Notrelix.Application/Events/Billing
backend/src/Notrelix.Infrastructure/Data/Configurations/Billing
backend/src/Notrelix.Infrastructure/Data/ApplicationDbContext.BillingCapacity.cs
backend/src/Notrelix.Infrastructure/DependencyInjection/BillingRegistration.cs
backend/src/Notrelix.Infrastructure/Messaging/Consumers/Billing
backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts
backend/tests/Notrelix.Domain.Tests/Billing
backend/tests/Notrelix.Application.Tests/Features/Billing
backend/tests/Notrelix.Integration.Tests/Billing
frontend/packages/features/billing
frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx
frontend/apps/web/src/router/guards/require-entitlement.ts
```

## 3. Master execution goal

Billing is complete only when the codebase proves all of the following:

```text
one commercial authority
one immutable catalog/version model
one Account-rooted Subscription authority
one deterministic Entitlement derivation path
one stable capability contract for consumers
separate capacity and metered-usage semantics
command-oriented billing administration
provider-independent Billing Application contracts
verified/idempotent provider webhook receipt
durable unknown-outcome reconciliation
evidence-oriented invoice/payment state
Account-rooted payment-method ownership
data-driven Billing frontend
safe migration from legacy plan/tier/workspace-plan state
tenant/RLS/security/observability evidence
```

A Domain class, table, enum or frontend mock does not count as a delivered capability without a vertical execution path and tests.

## 4. Non-goals

This plan does NOT authorize:

- splitting Billing into another deployable service;
- creating one project per Billing subdomain;
- introducing a generic policy/rules language;
- supporting every value in `PaymentProvider` merely because the enum exists;
- implementing raw card handling or storing PAN/CVV;
- making Analytics the Billing usage authority;
- moving Governance permission semantics into Billing;
- moving Account lifecycle ownership into Billing;
- exposing arbitrary CRUD for Subscription, Entitlement, Invoice, Payment or provider receipts;
- using provider status strings as Domain state;
- using `Account.PlanCode`, `Workspace.Plan` or frontend plan strings as commercial truth;
- weakening current capacity concurrency/idempotency evidence.

## 5. Physical architecture constraint

Billing remains inside the current modular monolith:

```text
Notrelix.Domain
Notrelix.Application
Notrelix.Infrastructure
Notrelix.API
```

New Billing code MUST follow current feature/package organization. A new provider adapter namespace/folder is allowed inside Infrastructure. A new backend project or independently deployed Billing service requires a separate architecture decision and is outside this plan.

## 6. Source classification vocabulary

Every source item touched by the execution MUST be classified as one of:

| Class | Meaning |
|---|---|
| `KEEP` | semantics are already correct; preserve and add evidence only where needed |
| `HARDEN` | retain ownership/model but strengthen invariants, errors, constraints, tests or observability |
| `REFACTOR` | retain business purpose but change representation or orchestration materially |
| `MIGRATE` | temporary compatibility source must move to the target authority |
| `RETIRE` | source is no longer an authority and must be removed after migration |
| `BUILD` | required capability does not exist |
| `BLOCKED` | implementation cannot begin until an explicit external/product decision or dependency is resolved |

No executor may silently reinterpret `MIGRATE` as permanent dual-write or `BLOCKED` as permission to invent a product decision.

## 7. Frozen source classification

The execution starts from this classification:

| Current source | Class | Required treatment |
|---|---|---|
| `Domain/Billing/Plans/Plan.cs` | REFACTOR | one immutable commercial revision per row; stable `PlanCode`; retire embedded price and `Plan.Period` as authorities |
| `Domain/Billing/Plans/PlanPrice.cs` | REFACTOR | authoritative sellable offer: Plan revision + currency + billing interval + amount |
| `PlanLimit` in `Plan.cs` | REFACTOR | replace with complete Plan capability definition, including explicit unlimited semantics |
| `Domain/Billing/Subscriptions/Subscription.cs` | REFACTOR | Account-rooted closed state machine; bind exact `PlanPriceId`; remove Workspace/PlanId/tier duplicate commercial authority |
| `SubscriptionItem.cs` | RETIRE | initial certified Subscription is single-price; multi-item/add-on pricing is not in released scope |
| `SubscriptionTier.cs` | MIGRATE → RETIRE-AS-AUTHORITY | remove from consumer contracts and later from Subscription state unless proven display-only |
| `Domain/Billing/Entitlements/Entitlement.cs` | KEEP + HARDEN | add source lineage/effective semantics; merge duplicate Workspace target representation into one RLS-visible `workspace_id` |
| `BillingCapabilityFactsProvider` | KEEP + HARDEN | preserve Billing-owned decision seam; stable reason/error semantics |
| `WorkspaceFeatureUsage` | KEEP | hard resource-capacity concurrency owner |
| `FeatureUsageLedger` | KEEP + HARDEN | capacity evidence only; preserve global logical operation id |
| `BillingCapacityActions` | KEEP | preserve same-transaction consume/release semantics |
| `UsageMetric` | REFACTOR | become explicit period aggregate/projection for metered usage |
| `UsageMetricHistory` | MIGRATE/RETIRE-AS-AUTHORITY | immutable metered usage event ledger becomes source evidence |
| `BillingCustomer` | HARDEN | add explicit provider identity/mapping semantics |
| `Invoice` | KEEP + HARDEN | Account-rooted provider/period/evidence state; remove Workspace security scope; closed transition proof |
| `InvoiceLineItem` | KEEP + HARDEN | retained child evidence with explicit parent-Invoice RLS policy |
| `PaymentMethod` | MIGRATE + REFACTOR | Account-rooted, provider-reference only, concurrency-safe default; remove Workspace security scope |
| `ProviderEventId` / `BillingEvent` | REFACTOR/REHOME | Infrastructure webhook receipt owns raw provider identity/payload; Domain/Application own normalized Billing facts |
| `IBillingDbContext` | REFACTOR | expose target Billing persistence without making it a cross-context public contract |
| `IRequireSubscription.MinimumTier` | MIGRATE | remove consumer dependence on commercial tier vocabulary |
| `SubscriptionChangedIntegrationEvent` / `SubscriptionCanceledIntegrationEvent` | HARDEN | retain only if consumer inventory and schema semantics are proven |
| Billing stub consumers | RETIRE/REPLACE | logging-only consumers are not business delivery |
| `Account.PlanCode` | MIGRATE → RETIRE-AS-AUTHORITY | compatibility projection only during rollout |
| `WorkspaceDto.Plan` | MIGRATE → RETIRE | Workspace contract must not expose commercial truth as Workspace-owned state |
| frontend hard-coded plan catalog | RETIRE | replace with Billing API query |
| Billing API endpoints | BUILD | no production Billing endpoint surface exists at audited baseline |
| provider gateway | BUILD | provider-neutral Application contract + Infrastructure adapter |
| Billing operation/reconciliation | BUILD | durable external-operation identity/outcome/repair |
| PaymentTransaction | BUILD | normalized settlement evidence |
| metered usage event ledger | BUILD | separate from resource-capacity ledger |

## 8. Execution invariants

All phases obey these invariants:

```text
Account is the payer/billable subject.
Workspace may be an Entitlement/usage target, not an independent payer.
Plan provider IDs are references, never Plan identity.
Plan revisions are immutable once commercially referenced.
Subscription selects exactly one `PlanPriceId`; that price row transitively selects the immutable Plan revision, currency, billing interval and amount. Subscription does not duplicate those terms.
Entitlements are derived commercial contracts, not ad-hoc feature flags.
Authorization and Entitlement remain separate decisions.
Hard capacity and metered usage remain separate mechanisms.
Product hot paths never call a payment provider.
Financial/provider commands are idempotent.
Provider UNKNOWN outcomes are reconciled, never blindly retried.
Provider webhook payload is verified before semantic mutation.
Billing history is evidence-oriented and non-destructive.
Frontend Billing state is server-driven.
```

## 9. Required serial order

The normative order is:

```text
Phase 0   baseline + semantic decision freeze
Phase 1   commercial catalog
Phase 2   Account Billing bootstrap + Subscription
Phase 3   Entitlement derivation/reconciliation
Phase 4   capability contract + reference consumer handoff
Phase 5   hard resource capacity
Phase 6   metered usage
Phase 7   Billing read/admin API + Governance
Phase 8   provider gateway + durable Billing operations
Phase 9   webhook inbox + normalization
Phase 10  reconciliation
Phase 11  financial evidence
Phase 12  frontend migration
Phase 13  Account lifecycle + retention
Phase 14  events + cross-context handoff
Phase 15  migration/backfill hard-close
Phase 16  security/reliability/observability/performance
Phase 17  test/evidence hard-close
Phase 18  documentation/generated-contract handoff
Phase 19  certification
```

Phases may contain parallel work only when their entry dependencies are already at the required readiness level.

## 10. Safe parallelization

After Phase 3 is accepted:

- additional product capability consumers may be implemented in parallel with Phase 6;
- provider-neutral gateway contracts may be prepared while metered usage is built;
- frontend read-only Billing views may begin after Phase 7 read contracts stabilize;
- observability instrumentation may be added alongside each provider/financial phase;
- Analytics consumers may work from stable Billing public events without accessing private tables.

Unsafe parallelization includes:

- provider adapter implementation before provider-neutral operation semantics are fixed;
- frontend plan/pricing work before the catalog API is canonical;
- account/workspace Plan migration before Entitlement handoff exists;
- deleting `SubscriptionTier` before all consumers are inventoried;
- replacing capacity storage while the Automation hard-quota reference flow is changing;
- webhook mutation handlers before receipt verification/idempotency exists.

# Phase 0 — Baseline and semantic freeze

## 11. BIL-INV-001 — Capture exact source baseline

### Goal

Prevent execution against stale assumptions.

### Required actions

Record:

```text
repository
branch
candidate SHA
Billing-related changed files since 35702d0f
current migrations
current RLS scripts
current OpenAPI/generated-contract state
current Billing tests and CI selection
```

The executor MUST compare the candidate against the audited baseline and call out every Billing-relevant delta.

### Output

Create/update the Billing execution decision note with the candidate SHA and source inventory.

### Exit

No Billing-relevant source change remains unclassified.

## 12. BIL-INV-002 — Build complete Billing source inventory

Inventory Domain/Application/Infrastructure/API/frontend/tests by capability:

```text
catalog
subscription
entitlement
capacity
metered usage
billing customer
provider
webhook
reconciliation
invoice
payment
payment method
admin
events
migration
```

For each item record:

```text
path
current owner
current behavior
classification
requirements covered
consumer(s)
migration impact
```

### Exit

There are no hidden Billing surfaces outside the inventory.

## 13. BIL-INV-003 — Consumer inventory

Search the entire production tree for:

```text
PlanCode
WorkspaceDto.Plan
workspace.plan
SubscriptionTier
IRequireSubscription
IBillingCapabilityFacts
IBillingCapacityActions
AUTOMATION_RULE
Entitlement
billingQueryKeys
requireEntitlement
SubscriptionChangedIntegrationEvent
SubscriptionCanceledIntegrationEvent
```

Classify every consumer as:

```text
canonical
compatibility debt
migration target
test-only
dead/retirable
```

### Stop

A consumer that depends on an undocumented Billing semantic MUST be resolved before the producer contract is changed.

## 14. BIL-INV-004 — Persistence and RLS inventory

Record every Billing table, key, unique constraint, FK, delete behavior and RLS policy.

Special checks:

```text
provider event uniqueness
subscription active uniqueness
plan revision uniqueness
payment-method default uniqueness
entitlement lineage uniqueness
capacity operation idempotency
metered usage source-event idempotency
financial-history delete behavior
```

RLS helpers MUST be reviewed with actual generated policies, not inferred from table configuration.

## 15. BIL-INV-005 — Freeze commercial decisions

The following decisions are fixed by the SPEC and MUST be written into `decisions/PR-BIL-00-decision-notes.md`:

1. Account is the payer.
2. Workspace is an optional Entitlement/usage target only.
3. A `Plan` row is an immutable commercial revision.
4. `PlanCode` is the stable family identity; no separate PlanFamily aggregate/table is introduced.
5. `PlanPrice` is the only recurring catalog price authority; `Plan.Price` is retired.
6. `PlanLimit` is replaced by a complete Plan capability definition with explicit finite/unlimited semantics.
7. Subscription is Account-rooted; provider-specific `Incomplete`/`Unpaid` do not define internal Subscription lifecycle.
8. `SubscriptionTier` is not a product capability authority and is removed after consumer migration.
9. Subscription → Entitlement derivation is a Billing-owned use case.
10. Entitlement composition for the first certified closure is explicit: Subscription-derived baseline, then scoped Manual override; Promo/AddOn remain reserved and unsupported.
11. `WorkspaceFeatureUsage` + `FeatureUsageLedger` remain hard resource-capacity mechanisms.
12. Metered usage receives its own immutable event ledger and period aggregate/projection.
13. Provider operations use durable logical identities and `Pending/Succeeded/Failed/Unknown` outcomes.
14. Provider webhook raw payload belongs to an Infrastructure receipt/inbox boundary, not Billing Domain.
15. `PaymentTransaction` is introduced as normalized settlement evidence.
16. PaymentMethod becomes Account-rooted.
17. `Account.PlanCode`, `WorkspaceDto.Plan` and frontend hard-coded plan data are migration sources, never final authorities.

### Exit

No implementation PR is allowed to reopen these decisions implicitly.

## 16. Phase 0 exit

Phase 0 is accepted when:

- exact candidate SHA is recorded;
- source and consumer inventories are complete;
- current schema/RLS evidence is recorded;
- all source is classified;
- the decision note exists;
- unresolved provider selection is explicitly marked `BLOCKED` without blocking internal Billing core work.

# Phase 1 — Commercial catalog

## 17. Phase purpose

Create one immutable commercial catalog that can safely drive Subscriptions and Entitlements.

Requirement coverage:

```text
BILREQ001–BILREQ008
BILREQ112
BILAC001
```

## 18. BIL-CAT-001 — Stable PlanCode and revision identity

### Target model

`Plan` remains the commercial aggregate/root representing one immutable sellable revision.

It gains the equivalent of:

```text
PlanCode       stable family/business identifier
Revision       monotonic integer within PlanCode
DisplayName
Description
Status
EffectiveFrom
EffectiveTo?
```

Database authority:

```text
UNIQUE(plan_code, revision)
```

In addition, the publish path MUST guarantee at most one sellable/current revision per `PlanCode` at any effective instant. Publishing a new current revision atomically deprecates/ends the previous current revision or proves non-overlapping effective ranges.

Once a Plan revision is referenced by any Subscription, fields that change commercial meaning MUST NOT be mutated in place.

### Preserve

- aggregate identity;
- audit/version mechanism;
- archive/deprecate concepts;
- Domain event style.

### Retire

Using mutable `Name` or provider price IDs as the stable Plan identity.

## 19. BIL-CAT-002 — Make PlanPrice the sole price authority

Remove `Plan.Price` as a competing price source.

`PlanPrice` becomes the canonical selectable commercial offer for one immutable Plan revision:

```text
PlanId
Currency
BillingInterval
Amount
IsActive / availability semantics
```

Every selectable Plan, including Free, has an explicit PlanPrice row; Free uses amount `0`.

`Plan.Period` is retired as an independent billing-interval authority. `PlanPrice.BillingInterval` is authoritative.

Provider price identity is stored only in `ProviderPriceBinding(PlanPriceId, Provider, ProviderPriceId)`, not on the provider-neutral PlanPrice itself.

Historical price is preserved by retaining the old Plan revision and its prices.

Changing recurring commercial amount creates a new Plan revision rather than rewriting the subscribed revision.

### Migration

Backfill existing `Plan.Price` into `PlanPrice` for the matching currency/period before removing the column/property.

Migration MUST prove:

```text
old value == backfilled value
no subscribed Plan loses price data
duplicate price rows are detected
```

## 20. BIL-CAT-003 — Replace PlanLimit with PlanCapability

Introduce a Billing-owned Plan capability definition:

```text
PlanId
FeatureCode
Included
Limit?
IsUnlimited
Unit
LimitAggregationScope = Account | Workspace
```

`LimitAggregationScope` is not Entitlement target scope.

Initial reference semantics:

```text
AUTOMATION_RULE
grant target = Account-derived Entitlement
LimitAggregationScope = Workspace
unit = COUNT
quantity type = integer
```

Normative rules:

```text
Included = false → capability not granted
Included = true && IsUnlimited = true → unbounded
Included = true && IsUnlimited = false && Limit = 0 → zero capacity
finite COUNT Limit → non-negative integer
```

Database authority:

```text
UNIQUE(PlanId, FeatureCode, LimitAggregationScope)
```

A capability MUST NOT be published as enforced when its aggregation scope has no implementation.

No negative sentinel values are permitted. Target vocabulary is `PlanCapability`.

## 21. BIL-CAT-004 — Catalog lifecycle and transition policy

Implement SYSTEM/OPERATOR-only catalog administration:

```text
CreatePlanRevision
Publish/ActivatePlanRevision
DeprecatePlanRevision
ArchivePlanRevision
ConfigurePlanTransitionPolicy
```

Account Billing Admin is NOT a global catalog administrator.

Each Plan revision has one explicit active default offer (`PlanPrice.IsDefaultOffer` or equivalent), protected so at most one default exists per revision.

The current Free revision MUST have exactly one active default offer.

Cross-Plan transitions are explicit Billing catalog policy:

```text
PlanTransitionPolicy
- FromPlanCode
- ToPlanCode
- TransitionKind: Upgrade | Downgrade
- EffectiveTiming
- IsActive
```

First closure:

```text
same PlanCode + different PlanPrice → OfferChange → CurrentPeriodEnd
different PlanCode → explicit PlanTransitionPolicy
```

Never infer transition kind from amount, feature count, enum order or client input.

After a referenced revision is published, commercial fields are immutable. Deprecation/archive preserve historical resolvability.

## 22. BIL-CAT-005 — Catalog read model

Build an Application read contract returning:

```text
PlanId
PlanCode
Revision
DisplayName
Description
available prices
capabilities/features
status
availability
```

It MUST NOT expose provider product/price IDs to normal frontend consumers.

### Exit

The frontend can render the commercial catalog without a hard-coded plan array.

## 23. BIL-CAT-006 — Catalog tests

Required evidence:

- stable `PlanCode` validation;
- `(PlanCode, Revision)` uniqueness;
- commercial mutation blocked after reference/publication;
- new revision creation preserves old revision;
- price history remains queryable;
- `0` and unlimited are different;
- deprecated revision is not offered to new purchase flow;
- existing Subscription reference remains valid;
- migration from `Plan.Price` is lossless.

## 24. Phase 1 exit

Phase 1 is accepted when:

- one price authority exists;
- Plan revision immutability is executable;
- Plan capabilities can derive Entitlements;
- no provider ID is canonical Plan identity;
- catalog read contract is stable;
- migration tests prove existing catalog data is preserved.

# Phase 2 — Account Billing bootstrap and Subscription lifecycle

## 25. Phase purpose

Make Subscription the Account-rooted selector of commercial terms and eliminate ambiguous free/tier state.

Requirement coverage:

```text
BILREQ009–BILREQ023
BILREQ089
BILREQ104–BILREQ105
BILREQ109
BILREQ113
BILAC002–BILAC003
```

## 26. BIL-SUB-001 — Account-root Subscription

Refactor `Subscription` so the authority is:

```text
AccountId
PlanPriceId        // exact sellable offer; PlanPrice.PlanId identifies immutable Plan revision
Status
CurrentPeriodStart
CurrentPeriodEnd
CancelAtPeriodEnd
PendingPlanPriceId?  // only while a scheduled downgrade is waiting for CurrentPeriodEnd
provider mapping/reference metadata through explicit adapter-owned mapping
```

`WorkspaceId`, persisted `PlanId`, `SubscriptionTier` and `Plan.Period` are removed as competing Subscription commercial authorities.

`SubscriptionItem` is retired for the initial certified single-price model. Multi-item/add-on pricing requires a future explicit design.

Workspace-specific commercial differences are represented as Workspace-targeted Entitlements, not Workspace-owned Subscriptions.

## 27. BIL-SUB-002 — Close internal SubscriptionStatus

Target first-closure states:

```text
PendingActivation
Active
PastDue
Canceled
Expired
```

`Trialing`, `Incomplete` and `Unpaid` are not canonical first-closure states.

```text
Trialing → reserved/deferred vocabulary
Incomplete → provider observation / BillingOperation evidence
Unpaid → payment/Invoice evidence + explicit PastDue transition only when proven
```

Legacy rows require migration classification before target constraint.

## 28. BIL-SUB-003 — Authoritative transition table

| From | Operation | To |
|---|---|---|
| none | create Free/internal Subscription | Active |
| none | start external paid purchase | PendingActivation |
| PendingActivation | confirmed/reconciled activation | Active |
| PendingActivation | definitive activation failure | Expired |
| Active | mark past due | PastDue |
| PastDue | payment recovered | Active |
| Active/PastDue | schedule paid cancellation | same status + scheduled default-Free `PendingPlanPriceId` |
| Active/PastDue | resume scheduled cancellation | same status + clear scheduled target |
| Active/PastDue | confirmed upgrade | state preserved; current `PlanPriceId` changes after commercial/provider success |
| Active/PastDue | schedule downgrade/offer change | state preserved; `PendingPlanPriceId` until `CurrentPeriodEnd` |
| Active/PastDue | apply scheduled downgrade/cancel-to-Free | Active; target becomes current `PlanPriceId`; Entitlements reconcile |
| Active/PastDue/PendingActivation | privileged no-fallback immediate termination | Canceled |
| Active/PastDue | Account/commercial expiry | Expired |

`Canceled` is not ordinary paid-to-Free cancellation.

Any extra transition requires SPEC/decision update.

## 29. BIL-SUB-004 — Remove duplicated SubscriptionTier authority

Execution order:

1. inventory every production reference to `SubscriptionTier`;
2. migrate consumer checks to capability/subscription facts;
3. stop persisting `Tier` as a separate authority;
4. migrate any display need to catalog metadata;
5. remove `SubscriptionTier` from public/consumer signatures;
6. remove the Domain field/type when no compatibility reader remains.

`IBillingSubscriptionFacts` may continue to answer “has effective subscription” but MUST stop accepting `minimumTier`.

## 30. BIL-SUB-005 — Default/free Billing bootstrap

```text
AccountCreated
→ Billing bootstrap
→ current FREE Plan revision
→ exactly one active IsDefaultOffer PlanPrice
→ Account Subscription(PlanPriceId)
→ derived Entitlements
```

Invariants:

```text
one current/sellable FREE revision
one active default PlanPrice on it
initial default Free amount = 0
```

No provider is required. Duplicate delivery is idempotent. Missing/ambiguous default offer fails bootstrap; no arbitrary row/price/currency selection.

Database protection prevents multiple effective Subscriptions for the Account commercial slot.

## 31. BIL-SUB-006 — PlanPrice change is orchestration, not field update

Command:

```text
ChangeSubscriptionPlanPrice
- TargetPlanPriceId
- IdempotencyKey / request identity
```

Execution:

```text
authorize
load current Subscription
load exact target PlanPrice + immutable Plan revision
validate sellability
classify through Billing transition policy
determine effective timing
provider-backed immediate effect → durable BillingOperation + provider-effect intent
scheduled effect → PendingPlanPriceId + boundary
change current PlanPrice only after approved condition
reconcile Entitlements
publish canonical event
```

Classification:

```text
same PlanPriceId → no-op
same PlanCode + different PlanPriceId → OfferChange → CurrentPeriodEnd
different PlanCode → PlanTransitionPolicy
```

Request does NOT accept authoritative `isUpgrade`, `isDowngrade`, amount or tier.

## 31A. BIL-SUB-006A — Explicit lifecycle Application use cases

```text
ActivateOrConfirmSubscription
RenewSubscription
MarkSubscriptionPastDue
RecoverSubscriptionFromPastDue
ApplyScheduledPlanPriceChange
ExpireSubscription
TerminateSubscriptionImmediately
```

Rules:

- provider-backed activation requires `Succeeded`/reconciled BillingOperation;
- terminal activation failure moves `PendingActivation → Expired`;
- renewal advances only from authoritative evidence;
- PastDue fails paid Entitlements closed;
- recovery requires authoritative payment/provider evidence;
- scheduled offer changes apply only at approved boundary;
- every path is idempotent and reconciles Entitlements.

Trial operations are absent from first closure.

## 32. BIL-SUB-007 — Cancellation semantics

Implement:

```text
ScheduleSubscriptionCancellation
ResumeSubscription
ApplyScheduledCancellationToFree
TerminateSubscriptionImmediately   // privileged/system no-fallback only
```

Normal paid cancellation:

```text
resolve current default Free PlanPrice
record as PendingPlanPriceId
CancelAtPeriodEnd = true
keep paid offer active
```

At boundary:

```text
validate scheduled Free target
switch PlanPriceId to Free
clear pending/cancel flag
keep Status=Active
reconcile Free Entitlements
publish effective cancellation + subscription change
```

Invalid/ambiguous Free target routes to repair; never select another price arbitrarily.

`Canceled` is only no-Free-fallback termination.

## 32A. BIL-SUB-007A — Subscription lifecycle worker

Billing-owned scheduled processor handles:

```text
due scheduled PlanPrice changes
paid cancellation → Free fallback
internal renewal boundaries
Subscription expiry
stale PendingActivation repair trigger
```

Use bounded claims/idempotency so one boundary applies once.

Provider-backed boundaries:

```text
boundary reached
→ reconcile/fetch provider
→ apply only after authoritative confirmation
```

Do not apply provider-owned transitions solely from local clock time.

## 33. BIL-SUB-008 — Account.PlanCode compatibility migration

During transition:

```text
Billing Subscription = authority
Account.PlanCode     = compatibility projection only
```

Required sequence:

1. read current `Account.PlanCode` values;
2. map each known code to a PlanCode/revision;
3. create/backfill Subscription where missing;
4. compare projection with Billing authority;
5. switch all capability consumers to Billing;
6. stop writing `Account.PlanCode` from business flows;
7. remove or explicitly retain it only as non-authoritative compatibility/read metadata;
8. architecture gate forbids new plan logic under Accounts.

No indefinite dual-write is allowed.

## 34. BIL-SUB-009 — Subscription tests

Required:

- one deterministic commercial Subscription per Account policy;
- bootstrap replay;
- transition matrix;
- cancel scheduling vs effective cancellation;
- resume;
- past-due recovery;
- plan change idempotency;
- invalid transition rejection;
- no Workspace payer drift;
- tier migration regression;
- Account.PlanCode disagreement cannot grant capability.

## 35. Phase 2 exit

Phase 2 is accepted when:

- an Account can deterministically obtain commercial state;
- Subscription no longer depends on Workspace;
- state transitions are closed and executable;
- `SubscriptionTier` is no longer a cross-context authority;
- `Account.PlanCode` cannot decide product access.

# Phase 3 — Entitlement derivation, lineage and reconciliation

## 36. Phase purpose

Create the missing commercial spine:

```text
Subscription + PlanCapability
          ↓
effective Entitlement set
```

Requirement coverage:

```text
BILREQ024–BILREQ034
BILREQ088
BILREQ115
BILAC004
```

## 37. BIL-ENT-001 — Add entitlement lineage

Harden `Entitlement` with the equivalent of:

```text
SourceType
SourceId
SourceRevision?
TargetScope
TargetWorkspaceId?
FeatureCode
Limit?
IsUnlimited
EffectiveFrom
EffectiveTo?
Status
SupersededBy?
```

`SourceId` is mandatory for non-legacy grants.

Certified examples:

```text
Subscription → SubscriptionId
Manual       → Manual override operation Id
```

`Promo` and `AddOn` enum/source values remain reserved vocabulary only. They are not exposed as supported use cases in the first certified closure.

The migration MAY temporarily label legacy rows with a dedicated legacy source marker, but new rows cannot be lineage-less.

## 38. BIL-ENT-002 — Define deterministic composition

Effective entitlement calculation for the first certified closure is fixed as:

```text
1. resolve applicable Account/Workspace scope
2. build Subscription-derived baseline from the current `PlanPriceId` → Plan revision
3. apply one active Manual override when present
4. apply expiry/status
5. return one normalized capability result
```

Scope rule:

```text
Workspace-targeted Manual override wins only for that Workspace.
Otherwise the Account-targeted effective grant applies.
```

The target write path MUST prevent conflicting overlapping active grants for the same source identity/target/feature.

If legacy/corrupted data contains more than one equally applicable active grant and no explicit source revision/effective-time rule selects exactly one:

```text
fail closed
return CommercialStateConflict / DependencyUnavailable equivalent
emit repair telemetry
```

Do NOT fall back to `CreatedAt DESC`, `EffectiveFrom DESC` or `Id DESC` merely to obtain a deterministic row.

`Promo` and `AddOn` are not combined in this closure.

## 39. BIL-ENT-003 — Build Plan → Entitlement derivation

Introduce a Billing Application use case/service responsible for:

```text
ReconcileSubscriptionEntitlements(AccountId, SubscriptionId, effectiveAt)
```

It MUST:

1. load the effective Subscription and exact Plan revision;
2. enumerate Plan capabilities;
3. compare desired Subscription-sourced grants with current Subscription-sourced Entitlements;
4. create new grants for new capability meaning;
5. end/supersede prior grants whose meaning changed;
6. leave Manual override sources untouched;
7. preserve historical rows;
8. emit canonical entitlement change facts;
9. be idempotent when replayed.

It MUST NOT delete and recreate all Entitlements blindly.

## 40. BIL-ENT-004 — Trigger entitlement reconciliation

Run derivation after:

```text
free bootstrap
subscription activation
plan change becoming effective
renewal when Plan revision changes by explicit migration
effective cancellation/expiry
provider reconciliation that changes canonical subscription state
```

Do not couple product contexts to these triggers.

## 40A. BIL-ENT-004A — Manual Entitlement override use cases

Implement:

```text
GrantManualEntitlementOverride
RevokeManualEntitlementOverride
ExpireManualEntitlementOverride
```

Every mutation requires:

```text
privileged Governance authorization
Account/Workspace target binding
actor identity
non-empty reason
stable manual-operation/source identity
effective time
optional expiry
before/after effective capability snapshot
idempotency
audit/event correlation
```

No generic Entitlement row editor/admin endpoint is allowed.

## 40B. BIL-ENT-004B — Effective Entitlement query

Implement `GetEffectiveEntitlements` as a Billing-owned read use case.

It returns normalized effective grants/decisions for the trusted Account and optional Workspace without exposing raw overlapping historical rows as if they were simultaneously effective.

## 41. BIL-ENT-005 — Capability decision result

Expand `BillingCapabilityFact` into a stable decision contract containing at minimum:

```text
CapabilityCode
IsAvailable
Reason
Limit?
Used?
Remaining?
IsUnlimited
Scope
EffectiveUntil?
```

Required reason vocabulary includes:

```text
Available
NotGranted
Disabled
Expired
SubscriptionInactive
LimitExceeded
DependencyUnavailable
```

Do not overload HTTP authorization codes or user-facing English strings as the business reason.

## 42. BIL-ENT-006 — Fail-closed dependency behavior

For protected paid capabilities:

```text
unknown/unavailable Billing authority
→ no implicit grant
```

The first certified closure has no PastDue grace: paid capabilities are unavailable while Subscription status is `PastDue`.

Any future grace behavior must be an explicit Billing policy with duration and eligibility. There is no “provider down therefore allow everything” fallback.

## 43. BIL-ENT-007 — Entitlement reconciliation concurrency

Two concurrent/replayed reconciliation operations MUST converge to the same effective set.

Required protection:

- version/concurrency control on mutable status transitions;
- unique/filtered constraints where applicable;
- source identity uniqueness;
- idempotent event handling.

## 44. BIL-ENT-008 — Entitlement tests

Required tests include:

- Subscription baseline derivation;
- Plan finite limit;
- Plan unlimited grant;
- absent capability;
- Workspace scope;
- Subscription-derived baseline;
- Manual override precedence/audit/revoke/expiry;
- unsupported Promo/AddOn anti-regression;
- expiry;
- cancellation;
- duplicate reconciliation;
- concurrent reconciliation;
- historical row preservation;
- reason taxonomy.

## 45. Phase 3 exit — Entitlement D4 handoff

Phase 3 opens downstream product work only when:

```text
Plan → Subscription → Entitlement
```

is executable and:

- deterministic;
- Account-isolated;
- source-lineaged;
- idempotent;
- tested against one real database integration flow;
- consumable without Plan/tier knowledge.

# Phase 4 — Capability contract and reference-consumer handoff

## 46. Phase purpose

Stabilize the product-facing commercial seam before expanding Billing depth.

Requirement coverage:

```text
BILREQ024–BILREQ034
BILREQ052
BILREQ056
BILREQ090
BILREQ122–BILREQ123
```

## 47. BIL-CAP-001 — Preserve Billing-owned public seam

Keep:

```text
Notrelix.Application.Features.Billing.Public
```

as the producer-owned public Application surface.

Product contexts MUST NOT reference:

```text
Plan
Subscription
SubscriptionTier
Entitlement aggregate
WorkspaceFeatureUsage
FeatureUsageLedger
provider models
```

## 48. BIL-CAP-002 — Remove minimum-tier consumer semantics

Replace `IRequireSubscription.MinimumTier` with a neutral requirement.

Target forms are fixed by meaning:

```text
IRequireEffectiveSubscription
```

for operations that merely require any effective commercial subscription, and:

```text
IBillingCapabilityFacts
```

for feature capability checks.

There is no consumer-facing `minimumTier`.

Update policy/access-fact composition so Governance/Application consumes a boolean/fact, not Billing tier ordering.

## 49. BIL-CAP-003 — Stable error mapping

Map Billing business failures to machine-readable API errors, including:

```text
BILLING_ENTITLEMENT_REQUIRED
BILLING_CAPACITY_EXCEEDED
BILLING_SUBSCRIPTION_INACTIVE
BILLING_OPERATION_PENDING
BILLING_PROVIDER_UNAVAILABLE
BILLING_OPERATION_CONFLICT
```

They MUST remain distinguishable from:

```text
AUTHENTICATION_REQUIRED
PERMISSION_DENIED
VALIDATION_FAILED
CONCURRENCY_CONFLICT
```

## 50. BIL-CAP-004 — Automation Rule remains the pinned reference consumer

Do not rewrite the valid reference architecture.

The flow remains:

```text
CreateAutomationRule
  → Governance authorization
  → Billing capability decision
  → Automation aggregate create
  → Billing capacity consume
  → one request transaction
```

The Phase 4 work only adapts it to the finalized capability/result contract and Entitlement derivation.

## 51. BIL-CAP-005 — Architecture gates

Add/extend architecture tests to forbid:

- plan code comparisons outside Billing;
- `SubscriptionTier` outside Billing compatibility paths;
- Billing capacity aggregate use from product contexts;
- new `Workspace.Plan` use;
- direct product-context reads from Billing DbSets;
- provider SDK types outside Infrastructure adapter paths.

## 52. Phase 4 exit

Entitlement contract reaches D4 when:

- Automation reference consumer passes;
- no tier/plan knowledge leaks to the consumer;
- errors are stable;
- provider availability is irrelevant to the hot path;
- architecture gates reject regressions.

# Phase 5 — Hard resource capacity hardening

## 53. Phase purpose

Preserve the strongest existing Billing slice while making its boundaries explicit.

Requirement coverage:

```text
BILREQ035–BILREQ042
BILREQ107
BILAC005
```

## 54. BIL-RCAP-001 — Preserve hard-capacity concurrency owner and remove metering semantics

`WorkspaceFeatureUsage` remains hard-capacity aggregate.

Target:

```text
AccountId
WorkspaceId
FeatureCode
CurrentUsage : integer
HardLimit    : integer?   // null = unlimited
Version
```

Retire from authoritative hard capacity:

```text
ResetPeriod
LastResetAt
Reset()
```

`SoftLimit`/`OverageAllowed` are not first-closure hard-resource semantics and, if temporarily retained, are non-authoritative.

Public capacity amount is integer end-to-end; remove decimal→int casts.

Keep optimistic concurrency/version and quota-exceeded behavior.

## 55. BIL-RCAP-002 — Preserve capacity ledger

`FeatureUsageLedger` remains append/evidence-oriented for resource capacity.

Its `LogicalOperationId` remains globally unique and identifies one logical capacity effect.

Harden:

- normalized feature-code comparison;
- source resource type/id representation;
- correction/release linkage where needed;
- audit timestamps.

It MUST NOT gain billing-period reset semantics.

## 56. BIL-RCAP-003 — Lifecycle release coverage

For every capacity-backed product resource, define:

```text
create → consume
delete/terminal removal → release
restore/recreate → consume or restore exactly once
disable → product-specific; does not automatically release unless capability semantics say so
```

For the current Automation Rule reference:

```text
disabled rule still occupies capacity
deleted rule releases capacity
```

must remain tested.

## 57. BIL-RCAP-004 — Last-slot integration proof

Retain and strengthen real PostgreSQL evidence for:

```text
limit = 1
two concurrent creates
exactly one commit
exactly one ledger +1
exactly one resource
```

Also prove first-use row creation race and idempotent request replay.

## 58. BIL-RCAP-005 — Limit-shrink behavior

When effective Entitlement drops below current resource usage:

```text
existing resources remain
capacity state remains > new limit
new consume is denied
release is allowed
```

No resource deletion is triggered by Billing.

## 58A. BIL-RCAP-006 — Capacity repair/reconciliation

Implement a privileged/system repair use case:

```text
ReconcileResourceCapacity(AccountId, WorkspaceId, FeatureCode)
```

It compares the retained capacity ledger and the authoritative product-resource lifecycle facts available through approved producer contracts.

Rules:

- ordinary request paths do not silently rewrite capacity counters;
- repair is Account/Workspace-scoped and audited;
- correction uses explicit compensating/reconciliation evidence;
- repair is idempotent;
- direct manual row edits are not the supported mechanism.

## 59. Phase 5 exit

Capacity D4+ requires:

- transaction atomicity;
- idempotency;
- last-slot race proof;
- resource lifecycle release proof;
- non-destructive downgrade;
- no periodic reset behavior.

# Phase 6 — Metered usage

## 60. Phase purpose

Build period-based commercial usage without corrupting the capacity mechanism.

Requirement coverage:

```text
BILREQ043–BILREQ051
BILREQ091
BILREQ117
BILAC006
```

## 61. BIL-USG-001 — Introduce immutable MeteredUsageEvent

Build an append-only Billing record with at least:

```text
Id
AccountId
WorkspaceId?
MetricCode
Quantity
Unit
SourceContext
SourceEventId
SourceResource?
OccurredAt
IngestedAt
CorrectionOf?
BillingPeriodKey
```

Database identity MUST make source-fact replay harmless.

Recommended unique authority:

```text
(SourceContext, SourceEventId, MetricCode)
```

If one source fact may legally emit multiple entries of the same metric, include an explicit source-line identity rather than weakening idempotency.

## 62. BIL-USG-002 — Refactor UsageMetric into period aggregate/projection

`UsageMetric` becomes the current-period aggregate/projection keyed by:

```text
AccountId
WorkspaceId?
MetricCode
PeriodStart
PeriodEnd
```

It is rebuildable from MeteredUsageEvent.

`UsageMetricHistory` no longer serves as the sole authoritative history and is migrated/retired once the immutable event ledger is active.

## 63. BIL-USG-003 — Period calculation

Define Billing period from commercial Subscription terms, not local wall-clock defaults.

Required semantics:

```text
period start/end
UTC storage
provider alignment mapping if provider-backed
late event attribution
period close
next period opening
```

A period transition never deletes historical usage.

## 64. BIL-USG-004 — Usage ingestion contract

Source contexts publish completed business facts.

Billing maps them into commercial usage.

Example:

```text
AutomationExecutionSucceeded
      ↓
Billing usage mapper
      ↓
AUTOMATION_EXECUTION +1
```

The source context MUST NOT know provider meter IDs or pricing rules.

## 65. BIL-USG-005 — Late and out-of-order events

Rules:

- event belongs to period determined by occurrence/effective policy;
- already-closed periods remain historically correct;
- late usage either adjusts the retained period and downstream provider report or creates explicit adjustment evidence according to provider contract;
- processing order does not change total commercial meaning.

## 66. BIL-USG-006 — Corrections

Corrections are append-only compensating entries.

Forbidden:

```text
UPDATE old usage quantity
DELETE old usage event
```

Required:

```text
new correction event
CorrectionOf = original usage event
negative/positive compensating quantity
```

## 67. BIL-USG-007 — Metered enforcement timing

For each metered capability classify:

```text
observe-only
soft-limit
hard-limit
overage-allowed
```

Hard pre-consumption quotas require a reservation/allocation mechanism and MUST NOT use an eventually consistent aggregate as the only race guard.

No such hard metered quota may be released until its concurrency protocol is implemented and tested.

## 68. BIL-USG-008 — Capacity/metering anti-regression gate

Add a structural test that prevents `FeatureUsageLedger` all-time sums from being used as a periodic metered total.

The current `BillingCapabilityFactsProvider` ledger sum remains valid only for non-resetting resource capacity.

## 68A. BIL-USG-010 — Period aggregate uniqueness

Materialized identity:

```text
(AccountId, WorkspaceId?, MetricCode, PeriodStart, PeriodEnd)
```

Nullable Workspace requires `UNIQUE NULLS NOT DISTINCT` or equivalent partial/expression indexes.

Immutable MeteredUsageEvent remains rebuild authority.

## 69. Phase 6 exit

Metered usage is accepted when:

- immutable source-event identity exists;
- period semantics are explicit;
- replay is idempotent;
- late events and corrections are tested;
- current-period aggregate is rebuildable;
- no capacity ledger is misused as a monthly meter.

# Phase 7 — Billing read API and administration

## 70. Phase purpose

Expose Billing as an actual vertical product capability while preserving Account authority and Governance authorization.

Requirement coverage:

```text
BILREQ052–BILREQ056
BILREQ092
BILREQ129–BILREQ138
BILAC010
```

## 71. BIL-API-001 — Server-authoritative Account binding

Normal authenticated Billing API requests derive Account from the trusted request context.

The API MUST NOT accept a client payer Account ID and treat it as authority.

Workspace route context may be used to resolve/check Account membership, but the payer is still server-resolved Account state.

## 72. BIL-API-002 — Read endpoints

Implement semantic reads equivalent to:

```text
GET /api/billing/plans
GET /api/billing/subscription
GET /api/billing/entitlements
GET /api/billing/usage
GET /api/billing/invoices
GET /api/billing/invoices/{invoiceId}
GET /api/billing/payment-methods
GET /api/billing/operations/{operationId}
GET /api/billing/summary
```

`GET /operations/{id}` is Account-bound and returns normalized:

```text
Pending
Succeeded
Failed
Unknown
```

plus safe operation result metadata.

Checkout/portal action URLs, if surfaced through operation result, are short-lived/sensitive: never normal-log them, never publish them in integration events and never expose them cross-Account.

Responses expose Billing DTOs, never EF entities/provider SDK payloads.

## 73. BIL-API-003 — Administration commands

Implement endpoints equivalent to:

```text
POST /api/billing/subscription/change-plan
POST /api/billing/subscription/cancel
POST /api/billing/subscription/resume
POST /api/billing/checkout
POST /api/billing/portal
POST /api/billing/payment-methods/default
```

Plan change request selects exact:

```text
TargetPlanPriceId
```

and never accepts authoritative upgrade/downgrade classification from the client.

Provider-backed commands:

```text
persist BillingOperation + provider-effect intent
commit request
return 202 Accepted + BillingOperationId
```

They do NOT return commercial success merely because the request was accepted.

Internal no-provider commands MAY return ordinary committed success when authoritative local transition is complete.

There is no generic `PUT /subscription` or `PATCH /invoice/status`.

## 74. BIL-AUTHZ-001 — Governance actions

Introduce/confirm distinct protected actions semantically equivalent to:

```text
billing.view
billing.manage_subscription
billing.manage_payment_methods
billing.view_invoices
billing.repair            // privileged operational path
```

Governance owns who receives these permissions.

Billing handlers declare requirements through the existing authorization pipeline.

Direct role checks inside Billing handlers are prohibited.

## 75. BIL-API-004 — Contract error mapping

All API endpoints use the stable Billing error taxonomy.

Frontend must be able to distinguish:

```text
permission denial
entitlement/capacity denial
inactive subscription
pending provider operation
provider unavailable
validation
concurrency
```

## 76. BIL-API-005 — OpenAPI/codegen

Every public Billing request/response change must regenerate and validate frontend contracts through the repository's existing codegen workflow.

Handwritten frontend types that duplicate generated API shapes are migrated or limited to UI-specific view models.

## 77. Phase 7 exit

The backend has a real Billing API with:

- account binding;
- Governance authorization;
- stable Billing DTOs/errors;
- read surfaces for current commercial state;
- command surfaces aligned to business operations.

# Phase 8 — Provider gateway and durable Billing operations

## 78. Phase purpose

Create provider-independent external-operation semantics before implementing provider-specific behavior.

Requirement coverage:

```text
BILREQ057–BILREQ063
BILREQ105
BILREQ119
BILREQ124–BILREQ126
BILAC007
```

## 79. BIL-PRV-001 — Provider contract and provider-relative bindings

Provider-neutral contract:

```text
EnsureCustomer
CreateCheckoutSession
CreateBillingPortalSession
CreateOrChangeSubscription
ScheduleCancellation
ResumeSubscription
FetchSubscriptionState
FetchInvoiceState
FetchPaymentState
```

SDK types do not cross Application boundary.

Bindings:

```text
BillingCustomer
    AccountId + Provider + ProviderCustomerId

ProviderSubscriptionBinding
    AccountId + SubscriptionId + Provider + ProviderSubscriptionId
    LastObservedProviderAt / VersionToken?

ProviderPriceBinding
    PlanPriceId + Provider + ProviderPriceId

ProviderInvoiceBinding / ProviderPaymentBinding / ProviderPaymentMethodBinding
    when enabled
```

Provider IDs never replace Notrelix IDs.

## 80. BIL-PRV-002 — Durable BillingOperation + repository-compatible provider-effect protocol

```text
BillingOperation
- Id / LogicalOperationId
- AccountId
- OperationType
- SubjectType
- SubjectId
- Provider
- RequestFingerprint
- TargetPlanPriceId? / normalized target
- ProviderObjectReference?
- ProviderIdempotencyKey
- RequestedAt
- LastAttemptAt?
- AttemptCount
- NextAttemptAt?
- Outcome: Pending | Succeeded | Failed | Unknown
- CorrelationId
- ErrorCode?
- ReconciliationRequired
```

Do NOT call provider inside an ordinary Application Write handler.

Repository reality:

```text
DataSessionBehavior → transaction around Write handler
EfIdempotencyStore → same request transaction
```

Certified protocol:

```text
API/Application request TX
    idempotency acquire
    persist BillingOperation(Pending)
    persist RequestFingerprint + provider idempotency key
    persist ProviderEffectRequested outbox message
    return Pending/Accepted replayable result
COMMIT

provider-effect consumer

    Prepare TX
        acquire IProviderEffectClaimStore/equivalent claim
        load operation
        increment AttemptCount / LastAttemptAt
    COMMIT

    provider call
        NO DB transaction
        SAME provider idempotency key

    Settle TX
        normalize result
        update provider binding
        apply Billing transition
        mark Succeeded/Failed/Unknown
        settle/release claim
    COMMIT
```

Reuse/generalize existing prepare/effect/settle reliability primitive.

Provider success + failed settle leaves durable Pending/Unknown identity for reconciliation.

## 81. BIL-PRV-003 — Outcome mapping

Provider call results are normalized to:

```text
Succeeded
Failed
Unknown
```

`Unknown` is mandatory for transport/timeouts where provider execution may already have occurred.

Do not classify a network timeout after submission as safe failure.

## 82. BIL-PRV-004 — BillingCustomer provider mapping

Harden `BillingCustomer` to include:

```text
AccountId
Provider
ProviderCustomerId
Status
```

Target uniqueness:

```text
UNIQUE(AccountId, Provider)
UNIQUE(Provider, ProviderCustomerId)
```

Do not overwrite an old provider mapping in place if historical evidence depends on it; deactivate/retain according to migration policy.

## 83. BIL-PRV-005 — Provider selection blocker

The audited source does not establish a released payment provider merely because `PaymentProvider` contains `Stripe`, `PayPal`, and `Manual`.

Therefore:

```text
provider-neutral Billing work: NOT BLOCKED
specific production adapter: BLOCKED until product/deployment selects the provider
```

When one provider is selected, implement exactly that adapter first.

Do not implement three speculative adapters.

## 83A. BIL-PRV-005A — Provider object identity constraints

For enabled object types:

```text
UNIQUE(Provider, ProviderCustomerId)
UNIQUE(Provider, ProviderSubscriptionId)
UNIQUE(Provider, ProviderPriceId)
UNIQUE(Provider, ProviderInvoiceId)
UNIQUE(Provider, ProviderPaymentId)
UNIQUE(Provider, ProviderMethodId)
```

Bindings retain last-applied provider freshness watermark where available.

## 84. BIL-PRV-006 — Provider idempotency

Where the selected provider supports idempotency keys, map the stable BillingOperation identity to provider idempotency.

Application idempotency and provider idempotency are correlated but separate guarantees.

Tests must prove:

```text
same application command replay
→ no second logical BillingOperation
→ no second provider-side commercial operation where provider contract supports it
```

## 85. Phase 8 exit

Provider-neutral Billing is ready when:

- gateway contract is stable;
- BillingOperation exists;
- Unknown outcome is durable;
- customer mapping is explicit;
- no provider SDK leaks into Domain/Application contracts;
- selected-provider implementation is either completed or explicitly remains BLOCKED.

# Phase 9 — Verified provider webhook inbox

## 86. Phase purpose

Make provider callback handling authenticated, durable and idempotent before it can mutate Billing state.

Requirement coverage:

```text
BILREQ064–BILREQ069
BILREQ102
BILREQ116
BILREQ127
BILAC008
```

## 87. BIL-WHK-001 — Infrastructure receipt model

Replace the current `BillingEvent.RawData` Domain role with an Infrastructure-owned provider receipt/inbox model.

Required fields:

```text
Provider
ProviderEventId
ReceivedAt
RawPayload or protected payload reference
Signature metadata needed for audit
VerificationStatus
ProcessingStatus
AttemptCount
LastError
CorrelationId
```

Target uniqueness:

```text
UNIQUE(Provider, ProviderEventId)
```

Raw provider payload does not become a public Integration Event or Domain value object.

## 88. BIL-WHK-002 — Raw-body verification

Webhook endpoint flow:

```text
receive raw body
  ↓
identify configured provider endpoint
  ↓
verify signature/authenticity
  ↓
verify timestamp/replay rule if provider supports it
  ↓
persist verified receipt idempotently
  ↓
acknowledge according to provider contract
  ↓
process normalized event asynchronously/reliably
```

Unverified input never reaches Billing mutation logic.

Browser Account authorization is not used for webhook trust.

## 89. BIL-WHK-003 — Normalize provider events

Map provider event types into internal commands/facts such as:

```text
ProviderSubscriptionStateObserved
ProviderInvoiceStateObserved
ProviderPaymentStateObserved
```

Mapping owns provider-specific status translation.

Unknown events:

- remain observable;
- can be marked ignored;
- do not crash the receipt endpoint;
- do not mutate commercial state.

## 90. BIL-WHK-004 — Duplicate and out-of-order safety

Tests MUST include:

```text
same provider event delivered twice
older subscription update arriving after newer observation
invoice/payment event arriving before subscription event
unknown event type
processing failure after receipt persisted
replay after process crash
```

Semantic application must be idempotent independently of transport retry.

## 90A. BIL-WHK-004A — Webhook observation convergence

Verified receipt is not a direct Domain setter.

```text
verify + persist receipt
→ identify Provider + ProviderObjectId
→ enqueue reconciliation
→ fetch current provider object when supported
→ compare freshness watermark
→ apply canonical transition
```

Persist reliable provider version/updated timestamp/sequence when available. Otherwise current-object fetch is anti-stale mechanism.

Provider response settlement, webhook reconciliation, periodic reconciliation and privileged repair use the same semantic transition service, so races converge to one result/event.

## 91. BIL-WHK-005 — Retire logging-only Billing consumers

Current stub/logging consumers do not count as delivered business behavior.

For each:

```text
SubscriptionChangedConsumer
SubscriptionCanceledConsumer
BillingStubConsumers
```

classify as:

```text
real downstream business consumer → implement + test
no business consumer → remove
observability-only → route through canonical telemetry, not fake business consumer
```

## 92. Phase 9 exit

No provider callback can mutate Billing without:

- authenticity verification;
- durable unique receipt;
- normalized mapping;
- duplicate protection;
- out-of-order policy;
- observable processing state.

# Phase 10 — Provider reconciliation

## 93. Phase purpose

Close uncertain and divergent provider state.

Requirement coverage:

```text
BILREQ060–BILREQ072
BILREQ103
BILREQ120
BILREQ124–BILREQ127
```

## 94. BIL-REC-001 — Unknown and stale-Pending reconciliation

Eligible:

```text
Unknown
OR
Pending + no fresh active provider-effect claim + age beyond threshold
```

Process:

```text
inspect claim
fresh → do nothing
stale/missing → query provider by object reference/original idempotency correlation

effect exists → apply local state + Succeeded
definitive failure → Failed
provider proves absence → requeue SAME logical operation / SAME provider idempotency key
still ambiguous → Unknown + bounded NextAttemptAt

emit age/attempt/correlation metrics
```

Covers crash-before-provider, provider-success+failed-settle, lost response and missed webhook.

Never create a new financial operation because a worker crashed.

## 95. BIL-REC-002 — Periodic state reconciliation

Implement scoped reconciliation for:

```text
active provider-backed subscriptions
recent invoices/payments
stuck BillingOperations
failed webhook receipts eligible for retry
```

It must be bounded and observable, not a full-table unscoped repair loop.

## 96. BIL-REC-003 — Field-specific authority

Document in code/tests:

```text
Notrelix Billing
→ authority for product access, entitlement and internal lifecycle meaning

Provider
→ authority for externally settled/payment-processor facts

Reconciliation
→ maps provider observations into internal meaning without copying arbitrary provider state
```

## 97. BIL-REC-004 — Privileged repair commands

Operational repair actions may include:

```text
reconcile one Subscription
reprocess one verified webhook receipt
refresh one Invoice/PaymentTransaction
rebuild one Account's derived Entitlements
```

They require the privileged Billing repair permission/system actor and full audit correlation.

No “edit DB row” admin endpoint is introduced.

## 98. BIL-REC-005 — Observability

Every reconciliation attempt records:

```text
operation id
account
provider
object reference
reason
previous outcome/state
observed state
resulting state
correlation id
attempt count
duration
safe error class
```

No provider secret or raw card material is logged.

## 99. Phase 10 exit

Provider uncertainty is an executable state with a repair path; no supported provider flow relies only on “webhook probably arrives”.

# Phase 11 — Financial evidence: Invoice, PaymentTransaction, PaymentMethod

## 100. Phase purpose

Turn existing payment vocabulary into retained, normalized commercial evidence.

Requirement coverage:

```text
BILREQ073–BILREQ081
BILREQ108
BILREQ114
BILAC009
```

## 101. BIL-FIN-001 — Harden Invoice as exact financial evidence

Target Invoice:

```text
AccountId
SubscriptionId
InvoiceNumber
Currency
Amount
PeriodStart?
PeriodEnd?
IssuedAt?
DueAt?
PaidAt?
VoidedAt?
Status
safe hosted document reference?
```

Provider identity lives in provider-relative Invoice binding.

Money rules:

```text
exact base-10 decimal
validated supported currency precision
line currency == invoice currency
ordinary amounts non-negative
provider-observed total retained as evidence
```

InvoiceLineItem is immutable snapshot evidence after Issue.

Do not recompute provider-settled total from mutable Plan data or line multiplication when tax/discount/provider rounding may exist.

Internal calculation requires explicit currency rounding.

## 102. BIL-FIN-002 — Closed Invoice transition table

At minimum:

```text
Draft → Open
Open → Paid
Open → Uncollectible
Open → Void
```

The initial closure explicitly rejects direct:

```text
Draft → Paid
Draft → Uncollectible
Draft → Void
Paid/Void/Uncollectible → ordinary backward transition
```

Any provider-specific intermediate status is normalized before Domain transition.

Paid invoices are not silently returned to unpaid state without an explicit adjustment/refund/chargeback model.

## 103. BIL-FIN-003 — Introduce PaymentTransaction

```text
Id
AccountId
InvoiceId?
Provider
ProviderPaymentId
Amount
Currency
Status
AttemptedAt
SettledAt?
FailureCode?
FailureCategory?
CorrelationId
```

Database authority:

```text
UNIQUE(Provider, ProviderPaymentId)
```

Statuses:

```text
Pending
Succeeded
Failed
Canceled
Unknown
```

Unknown requires reconciliation and forbids blind retry.

Money rules match Invoice. Refund/chargeback remains deferred until explicitly modeled.

## 104. BIL-FIN-004 — Migrate PaymentMethod to Account root

Target PaymentMethod:

```text
AccountId
Provider
ProviderMethodId
Last4
Brand
Status
IsDefault
```

Remove mandatory Workspace ownership.

Target unique/default constraint must guarantee at most one active default method per `(AccountId, Provider)`.

Changing default is concurrency-safe.

## 105. BIL-FIN-005 — Payment security

Only provider-safe metadata is stored.

Hard prohibited:

```text
PAN/full card number
CVV/CVC
raw bank credential
provider secret key
client secret that is not intended for persistent storage
```

Logs, events, exception messages and API responses receive the same redaction rule.

## 106. BIL-FIN-006 — Financial retention

Invoice/PaymentTransaction and required provider references survive ordinary Workspace/product deletion.

Account closure follows Phase 13 retention/anonymization policy.

No FK cascade is allowed to erase retained financial evidence accidentally.

## 107. Phase 11 exit

Financial capability is accepted when:

- invoice lifecycle is closed;
- settlement evidence exists;
- payment methods are Account-rooted;
- default-method race is protected;
- secrets are absent;
- provider reconciliation can repair financial state;
- retention behavior is tested.

# Phase 12 — Billing frontend migration

## 108. Phase purpose

Replace the current visual prototype with a server-driven Billing client.

Requirement coverage:

```text
BILREQ095–BILREQ099
BILREQ110–BILREQ111
BILREQ131
BILAC011
```

## 109. BIL-FE-001 — Remove workspace Plan authority

Current behavior:

```text
workspace?.plan
→ cast to BillingPlanTier
→ render current plan
```

is retired.

`WorkspaceDto.Plan` is removed after compatibility migration.

Billing page resolves Billing state through Billing queries.

## 110. BIL-FE-002 — Remove hard-coded plan catalog

Delete the production authority represented by the local `PLANS` array.

The UI renders:

```text
catalog plans
current subscription
effective plan
price
capabilities
current usage
billing operation state
```

from Billing APIs/generated contracts.

Story/visual fixtures may remain deterministic test data but MUST NOT be imported into production decision logic.

## 111. BIL-FE-003 — Query-key isolation

Billing query keys MUST include Account authority.

Workspace-specific entitlement/usage queries additionally include Workspace ID.

Account switch must invalidate/reset Billing state so no prior Account commercial state is shown.

## 112. BIL-FE-004 — Admin UX

Implement:

```text
view current plan
view available plans
start upgrade/change
schedule cancel
resume
show pending/unknown provider operation
view usage
view invoices
manage payment method/provider portal where enabled
```

Buttons are enabled only when the backend capability and permission exist.

## 113. BIL-FE-005 — Denial UX

Different messages/actions for:

```text
permission denied
capability not included
capacity exhausted
subscription inactive
billing operation pending
provider temporarily unavailable
```

Frontend entitlement guards remain UX only. Backend rejection is authoritative.

## 114. BIL-FE-006 — Return flow reconciliation

After hosted checkout/portal return:

```text
do not assume URL success == payment success
refetch BillingOperation/subscription
show Pending when reconciliation is incomplete
transition UI only from authoritative backend state
```

## 115. BIL-FE-007 — Frontend evidence

Required:

```text
typecheck
lint
billing package unit/component tests
interaction tests
UI fixtures
visual evidence
real/mock E2E according to repository conventions
codegen:check
architecture checks
```

## 116. Phase 12 exit

No production Billing decision depends on hard-coded plan/tier/price data.

# Phase 13 — Account lifecycle, closure and retention

## 117. Phase purpose

Coordinate Account lifecycle without transferring Account ownership to Billing.

Requirement coverage:

```text
BILREQ079–BILREQ084
BILREQ089
BILREQ118
BILAC013
```

## 118. BIL-LIFE-001 — Account closure contract

On Account closure/deletion:

```text
product access stops according to Account semantics
Billing receives canonical Account lifecycle fact
provider-backed active subscription is scheduled/canceled according to commercial policy
Entitlements cease to grant active capability
financial evidence is retained
provider reconciliation continues if required
```

Billing does not delete the Account.

## 119. BIL-LIFE-002 — Workspace deletion

Workspace deletion may:

```text
end Workspace-targeted Entitlements
release capacity tied to deleted resources according to owning-context facts
retain commercial usage evidence
```

It MUST NOT delete Account Subscription, invoices or payment evidence.

## 120. BIL-LIFE-003 — Retention and anonymization

Classify Billing data into:

```text
commercial configuration
active operational state
retained financial evidence
provider receipt/audit evidence
personal/billing contact data
```

For retained evidence:

- preserve legally/product-required financial facts;
- minimize/remove unrelated personal data;
- preserve stable non-sensitive references needed for audit/reconciliation.

Retention duration that depends on legal jurisdiction remains a deployment/product policy input; the code must support non-destructive retention rather than hard-coded cascade deletion.

## 121. BIL-LIFE-004 — RLS review and Billing-admin visibility

Target classes:

```text
plans / plan_prices / plan_capabilities / plan_transition_policies
→ catalog read as approved; mutation SYSTEM/OPERATOR only

subscriptions / billing_customers / invoices / payment_transactions /
payment_methods / billing_operations / account_billing_usage_summaries
→ Account-scoped

entitlements
→ workspace_id NULL = Account scope
→ workspace_id present = Workspace scope

workspace_feature_usage / feature_usage_ledger / Workspace metering
→ Workspace-scoped

provider effect claims / webhook receipts / reconciliation
→ worker/internal
```

Account Billing Admin cannot mutate global catalog.

Account-wide Billing API MUST NOT enter worker scope to bypass Workspace RLS.

Create Account-scoped commercial projection:

```text
AccountBillingUsageSummary
- AccountId
- WorkspaceId?      // dimension, not RLS scope
- Feature/MetricCode
- usage/limit summary
- AsOf / Version
```

It contains Billing facts only, not product-resource private payload.

Child policies use parent scope for invoice_line_items, transitional usage_metric_history and transitional subscription_items.

Raw provider body MUST NOT become readable through generic `support_readonly`. Use restricted encrypted/blob storage plus safe metadata or custom policy denying raw-payload support access.

Verification fails on disabled/missing/wrong policies, skipped child tables or unintended support raw-body access.

## 122. Phase 13 exit

Account/Workspace lifecycle cannot erase retained financial evidence or create a second Billing tenant identity.

# Phase 14 — Billing events and cross-context contracts

## 123. Phase purpose

Expose canonical commercial facts without leaking provider payloads or private persistence.

Requirement coverage:

```text
BILREQ085–BILREQ094
BILREQ119
```

## 124. BIL-EVT-001 — Event inventory and Subscription event version migration

Inventory name/version/owner/producer/consumer/tenant scope/payload/replay/compatibility for all Billing events.

Plan-ID-only `subscription.changed` cannot represent same-Plan offer changes.

Introduce v2/equivalent:

```text
AccountId
SubscriptionId
PreviousPlanPriceId
NewPlanPriceId
PreviousPlanId?   // derived compatibility only
NewPlanId?        // derived compatibility only
EffectiveAt
```

New event is Account-scoped; Subscription Workspace ownership is removed.

Migrate producer/consumers explicitly; do not reinterpret v1.

Logging-only consumers do not count as mature business consumers.

## 125. BIL-EVT-002 — Canonical event set

Public events should represent completed internal facts such as:

```text
billing.subscription-activated
billing.subscription-changed
billing.subscription-cancel-scheduled
billing.subscription-canceled
billing.entitlement-changed
billing.invoice-issued
billing.invoice-paid
billing.payment-succeeded
billing.payment-failed
```

Exact existing names may be retained for compatibility, but ownership and semantic meaning must match Billing, not provider event names.

## 126. BIL-EVT-003 — Event payload minimization

Do not publish:

```text
raw provider webhook JSON
provider secret
full payment metadata
unnecessary billing contact PII
```

Publish stable IDs, scoped commercial facts and correlation only.

## 127. BIL-EVT-004 — Product entitlement propagation

Consumers may react to `EntitlementChanged` by invalidating/rebuilding local UX/read projections.

They MUST NOT create their own Plan mapping.

Security-sensitive product behavior still asks Billing or an approved fresh projection under explicit consistency semantics.

## 128. BIL-EVT-005 — Account producer contract

Billing consumes canonical Account identity/lifecycle facts.

It MUST NOT query or mutate private Accounts persistence to infer lifecycle.

## 129. BIL-EVT-006 — Usage producer contracts

Each source context that emits billable usage documents:

```text
source event
metric code mapping owner
quantity derivation
idempotency identity
resource/scope
successful-fact requirement
```

No “attempted but failed” product operation is billed unless product semantics explicitly say so.

## 130. Phase 14 exit

Every public Billing event has an owner, schema/version identity, consumer inventory and replay semantics.

# Phase 15 — Migration and backfill hard-close

## 131. Phase purpose

Move from current representations to target Billing authority without an unsafe flag day.

Requirement coverage:

```text
BILREQ109–BILREQ118
BILAC016
```

## 132. BIL-MIG-001 — Migration order

```text
1. preflight legacy Plan/Subscription/Workspace/payment/status inconsistencies
2. add PlanCode/revision/current-revision protection
3. add PlanPrice target fields including IsDefaultOffer
4. backfill PlanPrice from Plan.Price + Plan.Period
5. map each Subscription to exact PlanPriceId while legacy evidence exists
6. block/classify multi-item, quantity != 1, Plan mismatch, price conflicts
7. add PlanCapability + PlanTransitionPolicy
8. add target Account/provider mapping columns/tables
9. validate Workspace → Account ownership for Subscription/PaymentMethod/Invoice BEFORE dropping workspace_id
10. add/verify target FK + provider uniqueness constraints
11. add/merge Entitlement lineage + one RLS-visible workspace target
12. add target RLS/read projections while old columns permit comparison
13. bootstrap/reconcile Account Subscription + Entitlements
14. switch product consumers to capability contracts
15. switch Billing reads/writes to PlanPriceId + Account-rooted finance
16. migrate/remove SubscriptionTier + legacy provider-shaped statuses
17. introduce metered event ledger/projections
18. migrate PaymentMethod default/provider identities
19. introduce provider receipt/effect/reconciliation persistence
20. switch frontend to Billing API
21. stop Account.PlanCode + Workspace.Plan authority
22. verify no dual authority
23. only then drop Workspace payer columns, SubscriptionItem, Plan.Period/direct PlanId/tier/obsolete fields
24. clean-install + baseline-upgrade + RLS + semantic reconciliation evidence
```

Never drop source scope/FK evidence before validation.

## 133. BIL-MIG-002 — Expand/contract discipline

Schema-changing PRs use expand/contract:

```text
add new representation
backfill
dual-read only where necessary
switch authority
verify
stop old writes
remove old representation later
```

Indefinite dual-write is forbidden.

## 134. BIL-MIG-003 — Plan price backfill

For each existing Plan:

- derive PlanCode;
- assign revision;
- copy `Plan.Price` into canonical `PlanPrice`;
- verify currency/period;
- detect collisions;
- preserve IDs referenced by Subscriptions.

## 135. BIL-MIG-004 — Subscription backfill

For each Account commercial state:

```text
existing Subscription wins if valid
otherwise map Account.PlanCode to Plan revision
otherwise assign deterministic Free subscription only when product bootstrap semantics permit
```

Conflicting evidence is reported, not silently overwritten.

## 136. BIL-MIG-005 — Entitlement lineage backfill

Existing Entitlements without lineage receive an explicit migration source marker and source reference where derivable.

After Subscription derivation is active, compare:

```text
legacy effective capability
vs
derived target capability
```

Differences must be classified before authority switch.

## 136A. BIL-MIG-005A — Subscription → PlanPrice deterministic backfill

```text
one SubscriptionItem + Quantity=1 + item.PlanPrice.PlanId == Subscription.PlanId
→ use item PlanPriceId

no SubscriptionItem
→ exact Plan.Price + Plan.Period mapping

multiple items OR Quantity != 1
→ BLOCKED

item Plan mismatch
→ BLOCKED

candidate price/period conflict
→ BLOCKED/report
```

Only after complete classification may SubscriptionItem/direct PlanId/Plan.Period be contracted.

## 136B. BIL-MIG-005B — Subscription status migration

```text
Incomplete → PendingActivation only with unresolved-activation evidence, else BLOCKED
Unpaid → PastDue only with authoritative delinquency evidence, else BLOCKED
Trialing → deferred scope; explicit migration decision or BLOCKED
```

Report every blocked row.

## 137. BIL-MIG-006 — PaymentMethod scope migration

For every Workspace-scoped PaymentMethod:

1. resolve Workspace → Account;
2. assert the row AccountId matches resolved Account;
3. migrate to Account scope;
4. deduplicate provider method references;
5. deterministically resolve multiple defaults;
6. add target uniqueness constraints;
7. remove Workspace dependency.

Cross-account mismatch is a blocker, not auto-fixed.

## 138. BIL-MIG-007 — Webhook receipt migration

Existing `billing_events` rows are classified as historical provider receipts.

Backfill Provider where provable.

Rows whose provider cannot be determined remain legacy evidence but MUST NOT be used as active dedup authority for the new provider path.

New receipts use `(Provider, ProviderEventId)` uniqueness.

## 138A. BIL-MIG-007A — Referential-integrity matrix

```text
PlanPrice.plan_id → Plan.id
PlanCapability.plan_id → Plan.id
Subscription.plan_price_id → PlanPrice.id
Invoice.subscription_id? → Subscription.id
InvoiceLineItem.invoice_id → Invoice.id
PaymentTransaction.invoice_id? → Invoice.id
ProviderSubscriptionBinding.subscription_id → Subscription.id
ProviderPriceBinding.plan_price_id → PlanPrice.id
```

Commercial/financial history defaults to `RESTRICT / NO ACTION`.

Application verifies same-Account ownership. No cascade may erase retained financial evidence.

## 139. BIL-MIG-008 — Upgrade/rollback proof

Migration testing covers:

```text
clean install
upgrade from audited baseline
mixed-version expand window where required
forward-fix after partial deployment
rollback of application code without destructive schema rollback
```

Financial evidence migration favors forward-fix over destructive rollback.

## 140. Phase 15 exit

No legacy Plan/tier/workspace Plan representation can independently grant access after migration.

# Phase 16 — Security, reliability, observability and performance

## 141. Phase purpose

Hard-close cross-cutting production requirements.

Requirement coverage:

```text
BILREQ100–BILREQ108
BILREQ119–BILREQ128
BILAC014–BILAC019
```

## 142. BIL-SEC-001 — Secret surface scan

Search Domain/Application/API/events/logs for:

```text
provider secret keys
webhook signing secrets
PAN/CVV
provider client secrets
raw authorization headers
unredacted sensitive provider payloads
```

Any persistence/logging of prohibited material blocks release.

## 143. BIL-SEC-002 — Webhook attack matrix

Test:

```text
missing signature
invalid signature
expired/replayed timestamp
modified raw body
unknown provider endpoint
duplicate valid event
oversized/invalid payload
```

Only valid authenticated receipts enter semantic processing.

## 144. BIL-SEC-003 — Authorization bypass matrix

Test:

```text
non-admin reads allowed/denied according to policy
non-admin plan change denied
non-admin payment-method mutation denied
cross-Account identifier injection denied
system/reconciliation actor path explicitly authorized
```

## 145. BIL-REL-001 — Provider failure matrix

Test at least:

```text
timeout before request submission
timeout after possible submission
5xx definite failure where contract permits classification
rate limiting
provider unavailable
webhook delayed
webhook missed
provider state differs from local
```

Each path maps to a safe BillingOperation/reconciliation state.

## 146. BIL-REL-002 — Persistence failure matrix

Test:

```text
DB fails before provider submission
DB fails after provider success
DB fails while persisting webhook receipt
DB fails during semantic webhook processing
DB fails during Entitlement reconciliation
```

The result must not silently grant capability or lose the need to reconcile.

## 147. BIL-OBS-001 — Correlation

Correlate:

```text
incoming request
idempotency key
BillingOperation
provider request/reference
webhook receipt
Subscription transition
Entitlement reconciliation
Invoice/PaymentTransaction
outbox/integration event
```

Use safe IDs only.

## 148. BIL-OBS-002 — Metrics

Minimum operational metrics:

```text
billing operation success/failure/unknown
unknown age
reconciliation attempts/outcomes
webhook verification failures
duplicate webhook receipts
webhook processing backlog/failures
entitlement decision failures
capacity-exceeded count
capacity concurrency conflicts
metered usage duplicate/correction count
provider latency/errors
```

## 149. BIL-PERF-001 — Product hot-path bound

Capability evaluation MUST remain database/local-cache bound and must never call external payment providers.

Measure representative entitlement/capacity hot paths and prevent N+1 catalog/subscription queries.

## 150. BIL-PERF-002 — Cache policy

Introduce caching only after correctness.

Any Entitlement cache key includes:

```text
Account
Workspace when relevant
FeatureCode
authority version/revision
```

Invalidation follows Entitlement change.

A stale cache may not outlive a revocation/cancellation beyond the explicitly accepted window.

## 151. Phase 16 exit

Security/reliability gates pass without weakening existing RLS, idempotency, transaction or architecture protections.

# Phase 17 — TESTS handoff and executable evidence

## 152. Phase purpose

Convert `billing-entitlements.tests.md` from skeleton into requirement-complete verification.

## 153. BIL-TEST-HO-001 — Requirement traceability

Every `BILREQ001–BILREQ138` receives at least one of:

```text
Domain test
Application test
Integration test
Architecture test
API contract test
frontend unit/component test
E2E test
migration verification
operational/manual evidence where automation is not meaningful
```

No requirement may be marked covered only because a similarly named class exists.

## 154. BIL-TEST-HO-002 — Test project mapping

Use the existing suites:

```text
backend/tests/Notrelix.Domain.Tests
backend/tests/Notrelix.Application.Tests
backend/tests/Notrelix.Infrastructure.Tests
backend/tests/Notrelix.Integration.Tests
backend/tests/Notrelix.Architecture.Tests
frontend Vitest suites
frontend Playwright/Storybook evidence
```

Create new test projects only with architecture approval.

## 155. BIL-TEST-HO-003 — Critical integration scenarios

Mandatory real-database/provider-fake scenarios include:

```text
Account bootstrap → Free Subscription → Entitlements
plan change → entitlement reconciliation
cancel-at-period-end → effective cancellation
last hard-capacity slot race
capacity replay/compensation
metered usage duplicate/late/correction
cross-Account RLS isolation
Billing admin authorization
provider operation Unknown → reconciliation
verified webhook duplicate/out-of-order
invoice/payment state reconciliation
payment-method default race
Account closure with retained financial evidence
```

## 156. BIL-TEST-HO-004 — Non-zero execution evidence

Certification MUST record test counts/results.

A command that selects zero tests is failure evidence, not PASS.

## 157. Phase 17 exit

The TESTS artifact maps every critical requirement to executable evidence and contains no aspirational PASS.

# Phase 18 — Documentation and generated-contract handoff

## 158. BIL-DOC-001 — Product/architecture alignment

Update only canonical docs whose semantics changed:

```text
docs/product/billing.md
bounded-context/contract docs
data ownership/consistency docs
event docs
security/RLS docs
```

Execution docs do not replace canonical product/architecture authority.

## 159. BIL-DOC-002 — OpenAPI/generated contracts

Run contract generation and prove the frontend consumes generated Billing API contracts where repository conventions support them.

No handwritten duplicate DTO becomes a second backend contract.

## 160. BIL-DOC-003 — Source classification closure

For every item in the Phase 0 classification table, certification records final status:

```text
KEPT
HARDENED
REFACTORED
MIGRATED
RETIRED
BUILT
BLOCKED-BY-EXTERNAL-DECISION
```

A `BLOCKED` provider adapter prevents provider/payment release but does not invalidate an otherwise certified internal Entitlement core if the certification scope says so explicitly.

## 161. Phase 18 exit

Docs describe the source that actually exists at the candidate SHA.

# Phase 19 — Final certification handoff

## 162. BIL-CERT-HO-001 — Candidate freeze

Record:

```text
candidate SHA
migration set
provider mode
feature flags/config
test commands
test counts
generated contract diff
known blocked/deferred capabilities
```

Certification is always candidate-specific.

## 163. BIL-CERT-HO-002 — Readiness levels

Minimum target:

| Capability | Required readiness |
|---|---|
| Catalog | D5 |
| Account Billing bootstrap | D5 |
| Subscription lifecycle | D5 |
| Entitlement derivation/evaluation | D5 |
| Product capability contract | D5 |
| Hard resource capacity reference slice | D5 |
| Metered usage | D4+ if released |
| Billing read/admin API | D4+ |
| provider gateway | D4+ if external billing released |
| webhook/reconciliation | D4+ if provider enabled |
| Invoice/PaymentTransaction | D4+ if provider/payment released |
| frontend Billing | D4+ for released flows |
| migration/security/RLS | D5 for affected released scope |

## 164. BIL-CERT-HO-003 — No prefilled PASS

`billing-entitlements.certification.md` starts from evidence, not intent.

Allowed states are unified with the certification artifact:

```text
NOT_EVALUATED
BLOCKED
PARTIALLY_VERIFIED
VERIFIED
STABLE
FAILED
NOT_APPLICABLE
```

`VERIFIED` means D4. `STABLE` means D5. No generic `PASS` label is used because it hides readiness level.

Every `VERIFIED`/`STABLE` row includes source and executable test evidence.

## 165. BIL-CERT-HO-004 — Split certification when provider is not selected

If internal Billing core is complete but no production payment provider has been selected:

```text
Internal commercial core
→ may certify to its declared scope

Provider/payment release
→ remains BLOCKED / not released
```

The certification MUST NOT imply checkout/payment support.

## 166. Phase 19 exit

The workstream is complete only to the exact scope proven by candidate evidence.

# Recommended PR / execution slicing

## 167. PR-BIL-00 — Source reality + decision freeze

### Scope

Documentation/evidence only.

### Deliverables

```text
candidate inventory
consumer inventory
schema/RLS inventory
source classification
PR-BIL-00 decision notes
provider-selection blocker
```

### Must not

Modify Billing behavior.

## 168. PR-BIL-01 — Commercial catalog normalization

### Source

```text
Domain/Billing/Plans
Infrastructure/Data/Configurations/Billing/Plan*
migration
Application Billing catalog queries/commands
```

### Changes

```text
PlanCode + Revision + one-current-sellable-revision invariant
immutable commercial revision
PlanPrice authority, including Free zero-price offer
retire Plan.Period authority
PlanCapability
catalog read model
catalog lifecycle commands
```

### Exit tests

Catalog Domain + migration + Application query tests.

## 169. PR-BIL-02 — Account bootstrap + Subscription lifecycle

### Changes

```text
Account-rooted Subscription bound to exact PlanPriceId
explicit default Free PlanPrice
PlanTransitionPolicy / OfferChange classification
retire SubscriptionItem for initial single-price model
closed status table with Trial deferred and activation-failure terminal state
explicit activate/renew/past-due/recovery use cases
immediate-confirmed upgrade + period-end downgrade/offer change
paid cancellation → Free fallback
SubscriptionLifecycleWorker
Free bootstrap
remove Workspace subscription authority
begin SubscriptionTier migration
Account.PlanCode backfill/projection boundary
```

### Critical evidence

AccountCreated replay and Subscription transition matrix.

## 170. PR-BIL-03 — Entitlement derivation and lineage

### Changes

```text
source lineage
effective times
Subscription + Manual-only composition policy
Plan → Entitlement reconcile use case
manual override grant/revoke/expire use cases
GetEffectiveEntitlements query
conflicting-overlap fail-closed rule
reconciliation idempotency
entitlement events
```

### Exit

One Account bootstrap/plan-change integration flow proves derived capabilities.

## 171. PR-BIL-04 — Stable capability handoff

### Changes

```text
BillingCapabilityFact reason taxonomy
remove minimum-tier consumer contract
Automation reference flow migration
error mapping
architecture gates
```

### Exit

Downstream teams can consume Billing without Plan/tier knowledge.

## 172. PR-BIL-05 — Hard capacity closure

### Changes

Only harden current mechanism:

```text
capacity lifecycle coverage
ledger semantics
last-slot race
first-use race
release/restore behavior
limit shrink
privileged ReconcileResourceCapacity repair path
```

Do not mix metered usage into this PR.

## 173. PR-BIL-06 — Metered usage ledger + periods

### Changes

```text
MeteredUsageEvent
period aggregate/projection
source event ingestion
late events
corrections
period transition
anti-capacity-reuse gate
```

### Exit

A representative metered metric is proven end to end.

## 174. PR-BIL-07 — Billing API + Governance

### Changes

```text
read endpoints
admin command contracts
Account binding
Governance actions
error contract
OpenAPI/codegen
```

Provider-dependent commands may return unavailable/not-enabled until provider release rather than simulating success.

## 175. PR-BIL-08 — Provider gateway + BillingOperation

### Changes

```text
provider-neutral gateway
BillingOperation with subject/request fingerprint/attempt scheduling
durable provider-effect outbox + prepare/effect/settle consumer
BillingCustomer + ProviderSubscription/Price bindings
provider-relative uniqueness
request/provider idempotency correlation
Pending/Unknown reconciliation identity
selected provider adapter only if provider decision exists
```

### Stop

Do not invent provider credentials/config/API semantics.

## 176. PR-BIL-09 — Verified webhook inbox

### Changes

```text
raw-body verification
provider receipt
unique provider event identity
normalized event mapping
duplicate/out-of-order handling
worker processing
```

## 177. PR-BIL-10 — Reconciliation

### Changes

```text
Unknown + stale-Pending operation repair
fresh/stale provider-effect claim handling
subscription reconciliation
invoice/payment reconciliation
same-idempotency safe requeue only when provider absence is proven
privileged repair actions
metrics
```

## 178. PR-BIL-11 — Financial evidence

### Changes

```text
Invoice hardening
PaymentTransaction
Account-rooted PaymentMethod
single-default race protection
retention constraints
```

## 179. PR-BIL-12 — Billing frontend

### Changes

```text
remove workspace.plan authority
remove production hard-coded PLANS
Billing queries
upgrade/cancel/resume state
usage/invoices
pending/unknown UX
generated contracts
```

## 180. PR-BIL-13 — Lifecycle + migration hard-close

### Changes

```text
Account closure
Workspace cleanup contract
retention/anonymization
RLS for new tables
legacy column/type removal
upgrade migration proof
```

## 181. PR-BIL-14 — Observability + tests + certification

### Changes

```text
security scans/gates
provider failure matrix
metrics/tracing
full tests
generated-contract evidence
final docs
certification
```

This PR does not hide functional implementation that should have been delivered earlier.

# PR dependency graph

## 182. Required dependency DAG

```text
PR-BIL-00
    ↓
PR-BIL-01
    ↓
PR-BIL-02
    ↓
PR-BIL-03
    ↓
PR-BIL-04
    ├───────────────┐
    ↓               ↓
PR-BIL-05       PR-BIL-06
    └───────┬───────┘
            ↓
        PR-BIL-07
            ↓
        PR-BIL-08
            ↓
        PR-BIL-09
            ↓
        PR-BIL-10
            ↓
        PR-BIL-11
            ↓
        PR-BIL-12
            ↓
        PR-BIL-13
            ↓
        PR-BIL-14
```

Provider-specific implementation inside PR-BIL-08+ may remain blocked while PR-BIL-01..07 continue.

# Coding-agent execution contract

## 183. Required report after every work unit

Every agent/work unit reports:

```text
candidate SHA
requirements addressed
source files inspected
classification changes
files modified
migration changes
public contracts changed
events changed
tests added/changed
commands executed
test counts/results
remaining blockers
next allowed work unit
```

## 184. Source-first rule

Before changing a named type, the executor MUST read:

- the type;
- its configuration/mapping;
- direct production consumers;
- relevant tests;
- event mappings;
- migration/RLS implications.

Do not implement from this plan using file names alone.

## 185. No hidden decisions

If execution discovers a conflict with a frozen semantic decision:

```text
STOP
record evidence
update decision/spec/plan deliberately
then resume
```

Do not bury a changed commercial rule in code.

## 186. Preserve valid completed work

Current capacity semantics and tests are protected baseline behavior.

Refactors MUST preserve:

```text
zero != unlimited
last-slot correctness
logical operation idempotency
same-transaction resource + capacity mutation
fail-closed capability checks
Billing-owned capability vocabulary
```

# Stop-condition registry

## 187. BIL-PLAN-STOP-001 — Dual Plan/price authority

Stop if implementation leaves both `Plan.Price` and `PlanPrice` as independent runtime authorities.

## 188. BIL-PLAN-STOP-002 — Account/Workspace payer ambiguity

Stop if any new Billing flow treats Workspace as an independently billable customer without a new product decision.

## 189. BIL-PLAN-STOP-003 — Tier leak

Stop if a product context must branch on `Free/Pro/Enterprise/...` to enforce a capability.

## 190. BIL-PLAN-STOP-004 — Entitlement lineage cannot be derived

Stop migration for affected rows if the authoritative source cannot be determined. Mark legacy evidence explicitly; do not fabricate provenance.

## 191. BIL-PLAN-STOP-005 — Capacity/metering conflation

Stop if a periodic quota implementation sums the all-time capacity ledger without period identity.

## 192. BIL-PLAN-STOP-006 — Unsafe provider retry

Stop if a timeout/transport failure can trigger an unconditional second financial operation.

## 193. BIL-PLAN-STOP-007 — Unverified webhook mutation

Stop if semantic Billing state can be changed before signature verification and durable receipt.

## 194. BIL-PLAN-STOP-008 — Provider SDK leak

Stop if provider SDK types appear in Billing Domain or public Application contracts.

## 195. BIL-PLAN-STOP-009 — Destructive commercial history

Stop if migration/cascade behavior can delete Invoice/Payment/Usage evidence as a side effect of ordinary product deletion.

## 196. BIL-PLAN-STOP-010 — Raw payment secret

Stop immediately if raw card or provider secret material is introduced into Domain/Application persistence or logs.

## 197. BIL-PLAN-STOP-011 — Client payer spoofing

Stop if an authenticated Billing endpoint trusts arbitrary Account ID from route/body instead of server-authoritative Account context.

## 198. BIL-PLAN-STOP-012 — Provider selection invented

Stop provider-specific implementation if no canonical product/deployment decision selects the provider.

## 198A. BIL-PLAN-STOP-013 — Subscription offer not pinned

Stop if a Subscription can be Active without one exact `PlanPriceId`, or if currency/billing interval must be guessed from current catalog state.

## 198B. BIL-PLAN-STOP-014 — RLS helper silently skips a Billing table

Stop if an expected Billing table has no direct scope column and is passed to `apply_scoped_business_policies` without an explicit parent-correlated policy, or if RLS verification only reports issue rows without failing the gate.

## 198C. BIL-PLAN-STOP-015 — provider call inside request transaction

Stop if a Billing Application write handler calls external provider while `DataSessionBehavior` owns the request transaction.

## 198D. BIL-PLAN-STOP-016 — stale Pending has no recovery

Stop if reconciliation selects only Unknown and cannot recover stale durable Pending operations.

## 198E. BIL-PLAN-STOP-017 — transition classification is guessed

Stop if transition kind/timing is inferred from price, feature count, enum order or client input.

## 198F. BIL-PLAN-STOP-018 — ambiguous legacy Subscription backfill

Stop if multi-item, quantity mismatch, Plan mismatch or price conflict is silently collapsed into one PlanPrice.

## 198G. BIL-PLAN-STOP-019 — raw provider payload support exposure

Stop if generic worker/support RLS makes raw provider webhook bodies readable to `support_readonly`.

# Required source searches

## 199. Commercial authority search

Before authority switch:

```text
PlanCode
workspace.plan
WorkspaceDto(
SubscriptionTier
MinimumTier
PlanTier
free
pro
business
enterprise
```

Classify production hits; do not blindly replace test fixtures.

## 200. Billing persistence search

Search:

```text
DbSet<Plan
DbSet<Subscription
DbSet<Entitlement
DbSet<Usage
DbSet<Invoice
DbSet<Payment
billing.
HasIndex
OnDelete
RLS
apply_scoped_business_policies
apply_catalog_policies
```

## 201. Provider boundary search

Search:

```text
Stripe
PayPal
PaymentProvider
ProviderCustomerId
ProviderMethodId
ProviderEventId
RawData
webhook
checkout
portal
```

Every production hit receives provider-boundary classification.

## 202. Consumer search

Search all non-Billing product contexts for:

```text
IBillingCapabilityFacts
IBillingCapacityActions
IRequireSubscription
Entitlement
SubscriptionTier
PlanCode
AUTOMATION_RULE
```

New direct Billing Domain dependencies outside Billing are prohibited.

# Required verification commands

## 203. Backend build

From repository/backend context, use the repository-supported .NET SDK and run at minimum:

```text
dotnet build backend.slnx
```

or the canonical CI-equivalent command if repository automation wraps it.

## 204. Backend test families

Run affected suites and final full regression:

```text
dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj
dotnet test tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj
dotnet test tests/Notrelix.Infrastructure.Tests/Notrelix.Infrastructure.Tests.csproj
dotnet test tests/Notrelix.Integration.Tests/Notrelix.Integration.Tests.csproj
dotnet test tests/Notrelix.Architecture.Tests/Notrelix.Architecture.Tests.csproj
```

At final certification run the repository's canonical full backend test path.

## 205. Frontend verification

From `frontend`:

```text
pnpm codegen:check
pnpm check:architecture
pnpm typecheck
pnpm lint
pnpm test:node:guarded
pnpm test:web:guarded
pnpm test:integration:guarded
```

For released Billing UI also run relevant UI/Playwright evidence according to repository CI.

## 206. Migration/RLS verification

Run:

- clean database migration;
- upgrade from audited baseline;
- RLS verification suite;
- cross-Account Billing isolation integration tests;
- schema/model snapshot drift checks.

No candidate is certified from an in-memory-only Billing test suite.

# Normative traceability matrix

## 207. Catalog

```text
BILREQ001–008
→ Phase 1
→ PR-BIL-01
→ Domain/Application/Migration tests
→ BILAC001
```

## 208. Bootstrap and Subscription

```text
BILREQ009–023
BILREQ089
BILREQ104–105
BILREQ109
BILREQ113
→ Phase 2
→ PR-BIL-02
→ Domain + Application + Integration + migration evidence
→ BILAC002–003
```

## 209. Entitlement

```text
BILREQ024–034
BILREQ088
BILREQ115
→ Phase 3–4
→ PR-BIL-03/04
→ Domain/Application/Integration/Architecture evidence
→ BILAC004
```

## 210. Hard capacity

```text
BILREQ035–042
BILREQ107
→ Phase 5
→ PR-BIL-05
→ PostgreSQL concurrency/idempotency evidence
→ BILAC005
```

## 211. Metered usage

```text
BILREQ043–051
BILREQ091
BILREQ117
→ Phase 6
→ PR-BIL-06
→ event/idempotency/period/correction evidence
→ BILAC006
```

## 212. Billing API/admin

```text
BILREQ052–056
BILREQ092
BILREQ129–138
→ Phase 7
→ PR-BIL-07
→ API/authorization/OpenAPI tests
→ BILAC010
```

## 213. Provider/webhook/reconciliation

```text
BILREQ057–072
BILREQ102–105
BILREQ116
BILREQ119–120
BILREQ124–127
→ Phase 8–10
→ PR-BIL-08/09/10
→ provider fake/contract + integration + security evidence
→ BILAC007–008
```

## 214. Financial evidence

```text
BILREQ073–081
BILREQ108
BILREQ114
→ Phase 11
→ PR-BIL-11
→ Domain + migration + provider reconciliation evidence
→ BILAC009
```

## 215. Frontend

```text
BILREQ095–099
BILREQ110–111
BILREQ131
→ Phase 12
→ PR-BIL-12
→ generated contract + component + E2E/UI evidence
→ BILAC011
```

## 216. Lifecycle/migration/security

```text
BILREQ079–084
BILREQ100–128
→ Phase 13–16
→ PR-BIL-13/14
→ RLS/migration/security/reliability/observability evidence
→ BILAC012–019
```

# Phase-level acceptance

## 217. Phase 0 accepted when

The candidate and all current Billing authorities/debts are known and frozen.

## 218. Phase 1 accepted when

The catalog has immutable revisions, one price authority and explicit capability definitions.

## 219. Phase 2 accepted when

Subscription is Account-rooted, closed-state and bootstrap-safe.

## 220. Phase 3 accepted when

Subscription/Plan causally and idempotently derive Entitlements with lineage.

## 221. Phase 4 accepted when

Product contexts consume Billing without plan/tier knowledge.

## 222. Phase 5 accepted when

Hard resource capacity preserves current concurrency/idempotency guarantees.

## 223. Phase 6 accepted when

Metered usage has immutable period-aware evidence and correction semantics.

## 224. Phase 7 accepted when

Billing has real Account-bound, Governance-protected API/read/admin contracts.

## 225. Phase 8 accepted when

External commercial operations have provider-independent identities and unknown-outcome semantics.

## 226. Phase 9 accepted when

All supported provider callbacks are verified, durably deduplicated and normalized.

## 227. Phase 10 accepted when

Unknown/divergent provider state has an executable repair path.

## 228. Phase 11 accepted when

Invoice/payment/payment-method state is retained commercial evidence.

## 229. Phase 12 accepted when

Frontend is fully server-driven for production Billing data.

## 230. Phase 13 accepted when

Account/Workspace lifecycle does not destroy or re-own Billing evidence.

## 231. Phase 14 accepted when

Billing public events are canonical, versioned and consumer-audited.

## 232. Phase 15 accepted when

Legacy Plan/tier/workspace representations can no longer grant access independently.

## 233. Phase 16 accepted when

Security, reliability, observability and hot-path performance are proven.

## 234. Phase 17 accepted when

The TESTS artifact maps requirements to non-zero executable evidence.

## 235. Phase 18 accepted when

Canonical docs and generated contracts match the candidate source.

## 236. Phase 19 accepted when

Certification states only what the candidate actually proves.

# Definition of Done

## 237. Functional DoD

Billing supports, for the released scope:

- catalog discovery;
- deterministic Account commercial bootstrap;
- closed Subscription lifecycle;
- Plan-to-Entitlement derivation;
- entitlement query and denial reasons;
- hard capacity enforcement;
- metered usage where released;
- Billing reads/admin commands;
- provider/payment flow where enabled;
- invoice/payment/payment-method reads where released;
- frontend UX backed by server state.

## 238. Architecture DoD

- Account remains payer authority.
- Billing owns commercial semantics.
- Governance owns permission semantics.
- product contexts know capabilities, not Plan tiers.
- provider SDK/schema remains Infrastructure-local.
- Analytics remains downstream.
- no new service/project boundary is introduced.
- RLS policy classification covers every Billing table.

## 239. Consistency DoD

- commercial commands are idempotent;
- entitlement reconciliation converges;
- hard-capacity races are safe;
- metered usage replay cannot double-count;
- default payment method is concurrency-safe;
- webhook replay cannot double-apply;
- unknown provider outcomes remain recoverable.

## 240. Security DoD

- no raw payment secrets;
- webhook verification precedes mutation;
- Billing admin uses central authorization;
- Account binding is server-authoritative;
- repair operations are privileged/audited;
- provider/log payloads are redacted;
- cross-Account access fails in API and RLS evidence.

## 241. Financial/reliability DoD

- price history is immutable;
- invoices/payments are evidence-oriented;
- ordinary product deletion does not erase retained financial state;
- provider outage does not corrupt Entitlements;
- successful provider action followed by local failure can reconcile;
- blind financial retry is impossible by design.

## 242. Migration DoD

- clean install succeeds;
- audited-baseline upgrade succeeds;
- catalog prices are preserved;
- Account.PlanCode migration is verified;
- SubscriptionTier consumers are eliminated;
- Workspace Plan contract is retired;
- PaymentMethod scope is migrated safely;
- Entitlement lineage is backfilled/classified;
- provider receipt uniqueness is enforced.

## 243. Verification DoD

The final candidate has evidence for:

```text
build
Domain tests
Application tests
Infrastructure tests
Integration tests
Architecture tests
RLS verification
migration verification
OpenAPI/codegen
frontend typecheck/lint/tests
released-flow E2E/UI evidence
security scans/gates
certification traceability
```

No required command may be represented by a zero-test run or a stale result from another candidate.

# Final execution rule

## 244. Billing completion rule

The implementation is not complete because the repository contains:

```text
Plan.cs
Subscription.cs
Entitlement.cs
Invoice.cs
PaymentMethod.cs
```

It is complete only when the repository proves the full released causal flow:

```text
Account
→ Subscription
→ exact Plan revision
→ derived Entitlements
→ product capability/capacity or metered usage
→ authorized Billing operation
→ provider interaction when enabled
→ verified provider feedback
→ reconciliation
→ retained financial evidence
→ data-driven frontend
```

Any break in that chain must remain explicit in certification as incomplete or blocked.
