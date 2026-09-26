---
document_id: WRK-SPEC-BILLING-ENTITLEMENTS
document_type: workstream-specification
status: active
owner: billing-entitlements-team
source_branch: develop
source_commit: 35702d0fa9fb01ed68b0667bab500030d60bd028
applies_to:
  - backend
  - frontend
  - billing
  - plans
  - subscriptions
  - entitlements
  - resource-capacity
  - metered-usage
  - invoices
  - payments
  - payment-methods
  - billing-administration
  - provider-integration
  - provider-webhooks
  - reconciliation
  - billing-migration
canonical_evidence:
  - docs/product/billing.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/capability-delivery-map.md
  - docs/workstreams/cross-team-dependencies.md
  - docs/workstreams/teams/billing-entitlements.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
source_evidence:
  - backend/src/Notrelix.Domain/Billing
  - backend/src/Notrelix.Application/Features/Billing
  - backend/src/Notrelix.Application/Common/Requests/Gates/IRequireSubscription.cs
  - backend/src/Notrelix.Application/EventMappers/Billing
  - backend/src/Notrelix.Application/Events/Billing
  - backend/src/Notrelix.Infrastructure/Data/Configurations/Billing
  - backend/src/Notrelix.Infrastructure/Data/ApplicationDbContext.BillingCapacity.cs
  - backend/src/Notrelix.Infrastructure/DependencyInjection/BillingRegistration.cs
  - backend/src/Notrelix.Infrastructure/Messaging/Consumers/Billing
  - backend/src/Notrelix.Infrastructure/Data/Rls/RlsSqlScripts/008_policies_workspace_scoped_domain.sql
  - backend/tests/Notrelix.Domain.Tests/Billing
  - backend/tests/Notrelix.Application.Tests/Features/Billing
  - backend/tests/Notrelix.Integration.Tests/Billing
  - frontend/packages/features/billing
  - frontend/apps/web/src/routes/workspaces/$workspaceId/billing.tsx
  - frontend/apps/web/src/router/guards/require-entitlement.ts
review_on:
  - billing-domain-change
  - commercial-catalog-change
  - plan-versioning-change
  - subscription-lifecycle-change
  - entitlement-contract-change
  - entitlement-precedence-change
  - usage-semantics-change
  - hard-quota-change
  - provider-contract-change
  - webhook-contract-change
  - payment-state-change
  - billing-authorization-change
  - account-billing-boundary-change
  - billing-data-ownership-change
  - billing-retention-change
---

# SPEC — Billing & Entitlements

## 1. Purpose

This specification defines the target commercial authority of Notrelix and the requirements that all Billing implementation, migration, tests, API contracts, provider adapters, frontend behavior and certification evidence MUST satisfy.

Billing is not a CRUD module and is not a thin wrapper around a payment provider.

The target execution spine is:

```text
Account
  ↓
Commercial Plan revision
  ↓
Subscription
  ↓
Entitlement derivation/reconciliation
  ↓
Product capability decision
  ├── Resource capacity
  └── Metered usage
  ↓
Provider / Invoice / Payment evidence
  ↓
Reconciliation + retained commercial history
```

The implementation MUST make the causal relationship between these concepts executable. The existence of Domain classes alone is not sufficient evidence of a delivered Billing capability.

## 2. Scope

This workstream owns requirements for:

```text
commercial catalog
plan revisions and prices
subscription lifecycle
entitlement derivation and overrides
capability evaluation
hard resource-capacity enforcement
metered commercial usage
billing administration
billing provider mapping and operations
verified provider webhooks
provider reconciliation
invoice/payment/payment-method evidence
billing events
billing frontend contracts
commercial retention and migration
```

This specification intentionally separates internal commercial correctness from payment-provider depth so product contexts can safely consume stable Entitlement contracts before every provider/payment feature is complete.

## 3. Commercial authority boundary

Billing owns the meaning of:

```text
what commercial offer exists
which commercial terms an Account is subscribed to
what capabilities those terms grant
what finite/unlimited limits apply
what commercial usage has occurred
what payment/invoice/provider state means inside Notrelix
```

Billing MUST remain the sole authoritative source for these decisions.

The following MUST NOT independently become commercial authorities:

```text
Account.PlanCode
Workspace DTO Plan strings
frontend hard-coded plan tiers
provider price/product identifiers
provider subscription statuses
Analytics usage projections
feature-context plan checks
```

They may exist temporarily as compatibility projections during migration, but MUST NOT decide access or commercial behavior.

## 4. Current source evidence boundary

This spec is based on `develop` at:

```text
35702d0fa9fb01ed68b0667bab500030d60bd028
```

Current source proves that Billing already contains substantial Domain vocabulary, including:

```text
Plan
PlanPrice
PlanLimit
Subscription
SubscriptionItem
SubscriptionTier
Entitlement
BillingCustomer
UsageMetric
UsageMetricHistory
WorkspaceFeatureUsage
FeatureUsageLedger
Invoice
InvoiceLineItem
PaymentMethod
BillingEvent
```

Current Application source proves a narrow executable Billing surface around:

```text
IBillingCapabilityFacts
IBillingCapacityActions
IBillingSubscriptionFacts
BillingCapabilityFactsProvider
BillingCapacityActions
BillingSubscriptionFactsProvider
```

Current source also proves that the Automation Rule create flow is the reference consumer for a concurrency-safe hard-capacity decision.

Current source does NOT prove complete Plan administration, Subscription orchestration, Plan-to-Entitlement derivation, Billing API delivery, provider checkout/portal behavior, verified payment webhooks, provider reconciliation, or a complete Payment lifecycle.

Those capabilities are therefore requirements of this workstream rather than assumed existing behavior.

## 5. Current source classification

The execution plan MUST start from the following classification and may change it only with source evidence recorded in the plan/certification artifacts.

| Current source | Classification | Target treatment |
|---|---|---|
| `Domain/Billing/Plans/Plan.cs` | REFACTOR | preserve aggregate identity/event style; introduce stable commercial identity/revision semantics; retire embedded price and `Plan.Period` as commercial authorities |
| `Domain/Billing/Plans/PlanPrice.cs` | REFACTOR | authoritative sellable offer for one immutable Plan revision, including currency + billing interval + amount |
| `Domain/Billing/Plans/PlanLimit` | REFACTOR | become a complete Plan capability definition with explicit finite/unlimited semantics |
| `Domain/Billing/Subscriptions/Subscription.cs` | REFACTOR | Account-rooted closed state machine; bind one exact `PlanPriceId`; remove Workspace/tier/PlanId duplicate commercial authority |
| `Domain/Billing/Subscriptions/SubscriptionItem.cs` | RETIRE for initial certified scope | multi-item/add-on pricing is not certified; a Subscription binds one exact `PlanPriceId` directly |
| `Domain/Billing/Subscriptions/SubscriptionTier.cs` | MIGRATE/RETIRE-AS-AUTHORITY | may remain temporarily as compatibility metadata; MUST NOT be product entitlement authority |
| `Domain/Billing/Entitlements/Entitlement.cs` | KEEP + HARDEN | add source lineage/effective semantics; use one Workspace target semantic mapped to one physical `workspace_id` scope column |
| `Entitlement.WorkspaceId` + `TargetWorkspaceId` | MERGE | one semantic `TargetWorkspaceId` property maps to the RLS-visible `workspace_id`; duplicate target columns are forbidden |
| `BillingCapabilityFactsProvider` | KEEP + HARDEN | retain Billing-owned decision boundary; normalize denial/error contract and remove arbitrary precedence |
| `WorkspaceFeatureUsage` | KEEP | authoritative hard resource-capacity aggregate |
| `FeatureUsageLedger` | KEEP + HARDEN | authoritative capacity evidence ledger; do not silently reuse as periodic metering without period semantics |
| `BillingCapacityActions` | KEEP | preserve transaction-bound consume/release and idempotency behavior |
| `UsageMetric` / `UsageMetricHistory` | REDEFINE | own metered-period semantics or be replaced by an explicit equivalent; MUST NOT duplicate capacity semantics |
| `BillingCustomer` | HARDEN | include provider-relative identity and reconciliation lineage |
| `PaymentMethod` | MIGRATE | move commercial ownership from Workspace-rooted to Account-rooted semantics; remove `workspace_id` from payer/security scope |
| `Invoice` | KEEP + EXTEND | Account-scoped immutable/evidence-oriented financial state; remove Workspace security ownership |
| `InvoiceLineItem` | KEEP + HARDEN | retained child financial evidence; enforce RLS through explicit parent-Invoice policy because it has no direct Account scope column |
| `ProviderEventId` / `BillingEvent` | REFACTOR/REHOME | provider receipt identity/raw payload belongs to Infrastructure inbox; normalized Billing facts remain Domain/Application-owned |
| `Account.PlanCode` | MIGRATE/RETIRE-AS-AUTHORITY | compatibility projection only during migration; Billing Subscription remains authority |
| Workspace `Plan` DTO/frontend `workspace.plan` | RETIRE-AS-AUTHORITY | Billing UI must query Billing contracts instead |
| hard-coded frontend Plan catalog | RETIRE | replace with Billing API data |
| Billing API endpoints | BUILD | expose stable Account-scoped read/admin contracts |
| provider gateway/adapter | BUILD | provider-neutral Application seam with Infrastructure adapter |
| provider operation/reconciliation model | BUILD | represent pending/success/failure/unknown outcomes safely |
| payment transaction evidence | BUILD | normalize financial settlement without storing raw payment credentials |

## 6. Physical architecture constraint

Billing remains a bounded context inside the existing modular monolith unless a separately accepted architecture decision authorizes service extraction.

The target dependency direction remains:

```text
Domain
  ↑
Application
  ↑
Infrastructure / API
```

Provider SDKs, provider DTOs, webhook signatures and raw provider payloads MUST remain outside Billing Domain.

Product contexts MUST consume Billing through producer-owned public Application contracts, not through Billing DbSets, EF configurations, provider adapters or Domain aggregates.

## 7. Capability ownership split

Billing owns commercial semantics.

Accounts owns the billable Account identity and Account lifecycle.

Governance owns who may perform Billing administration.

Product contexts own the business fact/resource that consumes a capability.

Platform/Foundation owns shared mechanisms such as:

```text
transaction pipeline
idempotency mechanism
outbox/inbox transport
secret storage mechanism
HTTP client/runtime mechanism
RLS/session context mechanism
observability primitives
```

Billing owns how those mechanisms are applied to commercial invariants.

## 8. Explicit non-ownership

Billing does NOT own:

- User authentication;
- Account creation semantics beyond commercial bootstrap reaction;
- Workspace lifecycle;
- resource authorization;
- Work Management resource rules;
- Documents resource rules;
- Automation execution semantics;
- generic integration/OAuth provider semantics;
- Analytics metric truth;
- generic secret storage;
- generic messaging transport;
- generic request idempotency infrastructure.

Billing MUST NOT become a second tenant root, identity system or authorization engine.

## 9. Foundational invariant — Account pays, scopes consume

The billable commercial subject is the Account.

The target hierarchy is:

```text
Account
  ├── BillingCustomer
  ├── Subscription
  ├── PaymentMethod
  ├── Invoice
  └── PaymentTransaction

Workspace
  ├── may be an Entitlement target
  └── may be a capacity/usage scope
```

A Workspace is not the payer merely because the Billing page is rendered inside a Workspace shell.

All Billing administration requests MUST resolve the Account from trusted server-side tenant/resource context. A client-provided Account ID alone MUST NOT establish Billing authority.

## 10. Foundational invariant — provider independence

Provider concepts are external references, not Notrelix commercial identities.

The following are forbidden as canonical Domain identities:

```text
Stripe product ID as Plan ID
provider price ID as Plan price authority
provider customer ID as Account identity
provider subscription status as Subscription state machine
provider invoice JSON as Invoice aggregate
```

Provider state MUST be translated through an anti-corruption adapter before it changes Billing state.

# Commercial catalog

## 11. BILREQ001 — Stable Plan family identity

A commercial offer MUST have a stable Billing-owned code independent of display name and provider identifiers.

Conceptually:

```text
PlanCode = PRO
PlanCode = ENTERPRISE
```

`PlanCode` is commercial vocabulary. It is not an authorization claim and MUST NOT be interpreted by product contexts.

Renaming a display label MUST NOT change Plan identity.

## 12. BILREQ002 — Plan revision is immutable commercial meaning

Once a Plan revision is referenced by a Subscription, its commercial terms MUST NOT be destructively rewritten.

Commercial-term changes requiring a new revision include at minimum:

- price;
- billing period;
- included capability;
- finite limit;
- unlimited/finite classification;
- terms that change effective entitlement meaning.

Existing subscribers remain bound to the referenced revision until an explicit migration/change operation moves them.

This is the grandfathering rule.

## 13. BILREQ003 — Plan lifecycle is separate from Subscription lifecycle

A Plan revision may be:

```text
sellable/active
deprecated
archived
```

Deprecating or archiving a Plan prevents new commercial selection according to policy but MUST NOT automatically cancel existing Subscriptions.

For the initial certified catalog, at any effective instant there MUST be at most one sellable/current revision for a given `PlanCode`. Publishing a new sellable revision MUST atomically retire/deprecate the previously sellable revision for that code or otherwise prove non-overlapping effective ranges.

A Subscription transition is always explicit.

## 14. BILREQ004 — One price authority

`PlanPrice` becomes the authoritative price representation for a Plan revision.

The embedded `Plan.Price` representation in current source is transitional and MUST be migrated/retired as an independent source of truth.

After migration, no command/query/UI may choose between `Plan.Price` and `PlanPrice`.

## 15. BILREQ005 — Price is historical commercial evidence

A price record MUST identify at least:

```text
Plan revision
currency
billing interval
amount
provider price mapping when applicable
```

Price changes MUST create new commercial terms rather than mutate historical meaning for an already-subscribed revision.

Provider price IDs are mapping metadata only.

For the initial certified single-price Subscription model, every selectable commercial offer — including Free — has a `PlanPrice` row. Free is represented by an explicit zero-amount price row rather than a nullable/no-price special case.

A Subscription MUST bind the exact `PlanPriceId` selected at purchase/bootstrap time. `PlanPrice.PlanId` then identifies the immutable Plan revision. This binding is what preserves currency + billing interval + amount historically.

Current `Plan.Period` is transitional and MUST be retired as an independent billing-interval authority. The certified billing interval comes from the bound `PlanPrice`.

## 16. BILREQ006 — Plan capability definition

A Plan revision MUST define the commercial capabilities it grants.

Each definition MUST identify:

```text
FeatureCode
Included
finite or unlimited semantics
finite limit when applicable
Unit
LimitAggregationScope
```

`LimitAggregationScope` and Entitlement target scope are different concepts.

Examples:

```text
Account-targeted grant + Workspace aggregation
→ every Workspace can receive the commercial capability,
  but each Workspace consumes its own finite resource capacity.

Account-targeted grant + Account aggregation
→ one finite capacity is shared across the Account.

Workspace-targeted Manual override
→ changes the grant only for that Workspace.
```

For the first certified hard-resource-capacity slice, `AutomationRule` is an integer-count capability with `LimitAggregationScope=Workspace`.

Normative invariants:

```text
0 means zero capacity
unlimited is explicit
negative sentinel values are forbidden
one Plan revision may have at most one capability definition
for the same (FeatureCode, LimitAggregationScope)
```

A database uniqueness constraint or equivalent authoritative protection MUST enforce that last invariant.

A capability whose aggregation scope has no implemented enforcement mechanism MUST NOT be published as enforced.

## 17. BILREQ007 — FeatureCode is Billing-owned stable vocabulary

`FeatureCode` is the stable commercial capability identifier consumed across bounded contexts.

Feature codes MUST NOT be:

- frontend component names;
- provider SKU IDs;
- arbitrary Plan display names;
- duplicated literals independently maintained by consuming contexts.

Billing Public contracts own exported capability codes.

## 18. BILREQ008 — Catalog read model

Billing MUST expose a read contract for currently selectable Plans that returns product-safe data required by frontend and administration flows.

The catalog read contract MAY expose:

```text
Plan code/revision ID
display name
description
prices
billing interval
feature descriptions/limits
sellability
```

It MUST NOT expose provider secrets or make provider IDs required frontend knowledge.

# Billing bootstrap

## 19. BILREQ009 — Every active Account has deterministic commercial state

Notrelix MUST NOT infer Free access from the absence of Billing state.

For normal Accounts, successful Account creation MUST eventually establish an explicit default/Free commercial state.

The target bootstrap is:

```text
AccountCreated
  ↓
idempotent Billing bootstrap
  ↓
resolve current FREE Plan revision
  ↓
resolve its one active default/bootstrap PlanPrice
  ↓
create Account Subscription(PlanPriceId)
  ↓
derived Entitlements
```

Every sellable current Free Plan revision MUST have exactly one active default/bootstrap `PlanPrice`.

The catalog MUST make that default explicit, for example through a provider-neutral `IsDefaultOffer`/equivalent flag protected by a uniqueness invariant. Bootstrap MUST NOT use `FirstOrDefault`, lowest amount, arbitrary currency ordering or “first active price”.

A product context MUST NOT implement:

```text
if no subscription then treat as free
```

## 20. BILREQ010 — Free/internal subscriptions do not require an external provider

A Free/default Subscription may be entirely internal.

Provider customer creation MUST NOT be required merely to use Free capabilities.

External provider state begins only when a provider-backed commercial operation requires it.

## 21. BILREQ011 — Billing bootstrap is idempotent

Replaying Account creation/bootstrap MUST NOT create duplicate Subscriptions or duplicate Entitlements.

The Billing bootstrap operation MUST use a stable Account-rooted logical identity and database uniqueness sufficient to reject duplicates under concurrency.

# Subscription lifecycle

## 22. BILREQ012 — Subscription is Account-rooted commercial state

A Subscription belongs to one payer Account.

For the first certified Billing closure, a Subscription MUST NOT be Workspace-rooted or Workspace-targeted. Workspace-specific commercial variation is represented through Workspace-targeted Entitlements.

A Subscription binds one exact `PlanPriceId`. That price row identifies the immutable Plan revision, currency, billing interval and amount selected for the Subscription. `PlanId`, `SubscriptionTier`, `WorkspaceId` and `Plan.Period` MUST NOT remain competing commercial authorities inside the target Subscription model.

## 23. BILREQ013 — Subscription state machine is closed

All persisted Subscription statuses MUST have explicit creation, entry, exit and terminal semantics.

No enum value may exist only because a provider exposes a similarly named value.

Target canonical states for the first certified closure are:

```text
PendingActivation
Active
PastDue
Canceled
Expired
```

`Trialing` is reserved/deferred vocabulary and is NOT part of the first certified closure. Account-level `Trialing` or a provider trial flag MUST NOT silently become Billing trial semantics.

Provider-shaped states such as `Incomplete`, `Unpaid` or provider trial statuses MUST NOT remain canonical Subscription states. They are normalized into BillingOperation, Invoice/PaymentTransaction evidence and an explicit Notrelix Subscription transition only where this SPEC defines one.

Terminal paid activation failure transitions:

```text
PendingActivation
→ Expired
```

while failed BillingOperation/provider evidence remains retained. A failed activation MUST NOT leave a permanently stuck `PendingActivation` row.

If a new state is introduced later, its entitlement effect, provider relation, allowed transitions, recovery behavior and migration must be explicitly specified and recertified.

## 24. BILREQ014 — Subscription creation does not silently force Active

Subscription creation MUST distinguish:

```text
internal Free activation
provider-backed PendingActivation
confirmed paid activation
terminal activation failure
```

For the first certified closure:

```text
internal Free
→ Active immediately

provider-backed purchase/change requiring activation
→ PendingActivation

confirmed/reconciled provider success
→ Active

definitive activation failure
→ Expired
```

A provider-backed Subscription MUST NOT become Active solely because a checkout/provider request was submitted.

Trial creation/conversion is outside the first certified scope and MUST NOT be exposed as completed behavior.

## 25. BILREQ015 — Subscription transition table is authoritative

Every transition MUST define:

```text
source state
target state
initiator/cause
effective time
provider requirement
entitlement effect
idempotency behavior
published event
failure behavior
reconciliation behavior
```

Arbitrary status assignment is forbidden.

## 26. BILREQ016 — Plan change is a commercial operation, not a field assignment

Changing a Subscription commercial offer requires an Application use case whose request selects an exact target `PlanPriceId`.

The use case coordinates:

```text
target PlanPrice validation
target immutable Plan revision validation
commercial transition classification
effective timing
provider operation when applicable
Subscription PlanPrice transition
Entitlement reconciliation
usage/capacity consequences
canonical events
```

The client MUST NOT tell the server that a change is an upgrade/downgrade.

Billing owns an explicit transition policy.

For the first certified closure:

```text
different PlanCode
→ transition kind/timing comes from Billing-owned PlanTransitionPolicy

same PlanCode + different PlanPrice
→ OfferChange
→ CurrentPeriodEnd unless a future explicit policy says otherwise

same PlanPrice
→ semantic no-op
```

A `PlanTransitionPolicy`/equivalent authority MUST explicitly identify at least:

```text
FromPlanCode
ToPlanCode
TransitionKind = Upgrade | Downgrade
EffectiveTiming
```

and MUST be deterministic for every published cross-Plan transition that the product exposes.

Price amount, feature count, enum ordinal and client input MUST NOT be used to guess transition kind.

`Subscription.ChangePlan()` or `Subscription.ChangePlanPrice()` alone is not a complete upgrade/downgrade capability.

## 27. BILREQ017 — SubscriptionTier is not an independent authority

Current `SubscriptionTier` is transitional compatibility metadata.

It MUST NOT disagree with the subscribed Plan revision and MUST NOT be a second source of truth for product access.

Target product decisions are based on Entitlements, not tier comparisons.

`IRequireSubscription.MinimumTier` MUST be migrated away from consumer-owned tier vocabulary for product capability gating. A remaining generic requirement for “active subscription” may be retained where it represents a true commercial prerequisite rather than a feature entitlement.

## 28. BILREQ018 — Renewal is explicit

Renewal MUST define:

- new commercial period;
- whether price/Plan revision changes;
- provider settlement dependency;
- entitlement continuity;
- metered usage period transition;
- idempotency.

Renewal MUST NOT erase previous period evidence.

## 29. BILREQ019 — Scheduled cancellation and effective cancellation are different facts

A cancellation request at period end MUST preserve paid access until the defined effective time.

The first certified product policy has an explicit Free fallback:

```text
normal user cancellation of an active paid Subscription
→ schedule transition to the current default Free PlanPrice at CurrentPeriodEnd
→ keep current paid PlanPrice/Entitlements until the boundary
→ at the boundary, switch to the default Free PlanPrice
→ remain Active
→ reconcile Free Entitlements
```

Therefore a normal paid user cancellation is commercially a scheduled downgrade-to-Free, not an immediate terminal `Canceled` status.

`Canceled` is reserved for cases where the commercial relationship itself ends without Free fallback, for example:

```text
Account closure
privileged/system immediate termination
another explicitly approved no-fallback policy
```

The system MUST support cancellation reversal/resume before the effective boundary.

If the default Free offer is unavailable or ambiguous at execution time, the effective cancellation MUST fail closed into repair/reconciliation rather than selecting an arbitrary Free price.

## 30. BILREQ020 — Immediate cancellation is explicit and audited

Immediate cancellation MUST identify:

```text
actor/system cause
effective time
provider effect
entitlement revocation timing
financial consequences
```

It MUST be idempotent.

## 31. BILREQ021 — Past-due/delinquency semantics are product-owned

Provider payment failure does not automatically define Notrelix capability behavior.

The product must explicitly define whether PastDue grants:

```text
full grace access
reduced access
read-only behavior
immediate denial
```

For the first certified closure, no PastDue grace window is configured: paid Entitlements fail closed while the Subscription is `PastDue`. A future grace policy requires an explicit product decision, effective-time rule and recertification.

The system MUST NOT assume paid capability from provider delinquency state alone.

## 32. BILREQ022 — Upgrade timing

The initial certified upgrade policy is:

```text
internal/free-to-free upgrade
→ immediate after local commercial validation succeeds

provider-backed upgrade
→ immediate only after the provider operation is confirmed/reconciled Succeeded
```

Until confirmation, the existing Subscription/Entitlements remain authoritative and the operation is Pending/Unknown as applicable.

Notrelix MUST NOT grant the upgraded capability merely because an external request was sent.

## 33. BILREQ023 — Downgrade is non-destructive

The initial certified downgrade policy is scheduled-at-period-end: the current `PlanPrice`/Plan revision remains authoritative until `CurrentPeriodEnd`, then the target `PlanPriceId` becomes effective through the normal Subscription transition and Entitlement reconciliation.

A downgrade to lower resource limits MUST NOT automatically delete user data.

If current resource usage exceeds the new finite limit after the downgrade becomes effective:

```text
existing resources remain
new capacity consumption is denied
release/delete operations may reduce usage
normal creation resumes when usage <= limit
```

Any stronger destructive policy requires a separate product decision and explicit migration behavior.

# Entitlements

## 34. BILREQ024 — Entitlement is the product-facing commercial contract

Product contexts consume Entitlements/capability decisions rather than Plans, Subscription tiers or provider state.

The normal consumer question is:

```text
Can Account A / Workspace W use capability C for requested amount N?
What limit applies?
What usage applies?
What remains?
```

not:

```text
Which Plan string is this Account on?
```

## 35. BILREQ025 — Entitlement is separate from authorization

Entitlement answers commercial availability.

Governance answers whether the actor may perform an action on a resource.

A granted Entitlement MUST NEVER imply resource permission.

A denied permission MUST NEVER be presented as a paid-plan upgrade requirement.

## 36. BILREQ026 — Entitlement lineage is mandatory

Every Entitlement MUST be traceable to the commercial fact that created it.

Target lineage includes:

```text
SourceType
SourceId
effective-from/effective-to semantics
Account
optional target Workspace
FeatureCode
```

For Subscription-derived grants, `SourceId` MUST identify the Subscription or immutable entitlement-revision source sufficient to reconstruct why the grant exists.

`EntitlementSource.Subscription` without a source identity is insufficient for certification.

## 37. BILREQ027 — Subscription-to-Entitlement derivation is an owned use case

Billing MUST implement an authoritative reconciliation operation:

```text
Subscription commercial state
  + referenced Plan revision
  ↓
Desired entitlement set
  ↓
reconcile current Subscription-derived Entitlements
```

The operation MUST be deterministic and idempotent.

It MUST run on all transitions that change effective commercial capability, including at minimum:

```text
bootstrap/activation
Plan change
renewal when terms change
effective cancellation
expiry
past-due/grace transitions when policy changes access
```

## 38. BILREQ028 — Entitlement reconciliation preserves history

Changing Plan or Subscription state MUST NOT destructively rewrite historical Entitlement evidence without trace.

Previous grants are revoked/expired/superseded according to explicit lifecycle semantics and new effective grants are produced as needed.

## 39. BILREQ029 — Entitlement precedence is business-defined

The resolver MUST NOT use `CreatedAt`/`Id` ordering as the sole business precedence rule between different entitlement sources.

For the first certified Billing closure, supported precedence is:

```text
explicit Manual override
  > Subscription-derived grant
```

with Workspace-targeted Manual override taking precedence only for its target Workspace.

`AddOn` and `Promo` source enum values remain non-certified until their combination/precedence semantics are explicitly implemented and tested. Their existence in the enum MUST NOT imply feature support.

Within the same effective source identity, revision/effective-time rules determine the current grant.

The normal write model MUST prevent overlapping active grants with conflicting meaning for the same source identity/target/feature. If migration or corrupted data yields more than one equally applicable active grant and no version/effective-time rule selects exactly one, capability evaluation MUST fail closed with a commercial-state conflict and emit repair evidence. Falling back to `Id DESC`, `CreatedAt DESC` or arbitrary newest-row selection is forbidden.

## 40. BILREQ030 — Manual override is auditable

Manual override requires:

```text
privileged authorization
actor identity
reason
source identity
effective time
optional expiry
before/after commercial decision evidence
```

Manual overrides MUST NOT be a hidden database edit.

## 41. BILREQ031 — Expiry is evaluated correctly

An Entitlement with `ExpiresAt <= now` is not active even if an asynchronous expiry worker has not yet persisted `Expired` status.

Persisted expiry transition may be eventual, but capability evaluation MUST remain correct at the decision time.

## 42. BILREQ032 — Fail-closed capability decision

For a well-formed capability request, Billing MUST return a deterministic decision object.

Absence of an applicable grant means denied/unavailable.

Billing dependency failure MUST NOT silently become unlimited access.

The target public contract SHOULD be non-null for valid requests; a missing grant is represented as an explicit denied decision rather than `null` ambiguity.

## 43. BILREQ033 — Capability decision has stable reason semantics

The public capability result MUST provide enough stable machine-readable meaning for callers/API/frontend to distinguish at least:

```text
available
missing entitlement
finite capacity exceeded
subscription commercially inactive
commercial state temporarily unavailable/pending when applicable
```

Human-readable text is not the contract.

## 44. BILREQ034 — Entitlement cache is optional, not foundational

Initial correctness MUST NOT depend on a cache.

If caching is introduced, the design MUST prove:

```text
Account/scope keying
commercial revision/version keying
invalidations on entitlement/subscription change
maximum stale window
failure behavior
```

A stale frontend or cache value MUST never become authoritative backend enforcement.

# Resource capacity

## 45. BILREQ035 — Resource capacity is distinct from metered usage

A hard resource count such as:

```text
max 10 automation rules
```

is a stock/capacity invariant.

A period-based quantity such as:

```text
10,000 executions per month
```

is metered usage.

These concepts MUST NOT share reset semantics merely because both contain counters.

## 46. BILREQ036 — WorkspaceFeatureUsage owns hard-capacity concurrency

Current `WorkspaceFeatureUsage` remains the authoritative aggregate for workspace-scoped hard resource capacity where that model applies.

It MUST preserve optimistic-concurrency protection for last-slot races.

Two concurrent creates competing for one remaining slot MUST NOT both commit.

## 47. BILREQ037 — Capacity consume is transaction-bound

Where the quota-bearing resource and Billing state share the existing modular-monolith transaction boundary, capacity consume MUST commit atomically with resource creation.

Target behavior:

```text
resource mutation + capacity consume + ledger entry
      one request transaction
```

If any part fails, all effects roll back.

A future distributed extraction would require a separately approved reservation/saga protocol; it is not part of this closure.

## 48. BILREQ038 — Capacity operation idempotency

Every consume/release operation MUST carry a stable logical operation identity.

Same identity + same semantic payload:

```text
replay without second effect
```

Same identity + conflicting payload:

```text
deterministic conflict
```

Database uniqueness MUST participate in the guarantee.

## 49. BILREQ039 — Capacity ledger is append/evidence-oriented

Every committed capacity consume/release produces an immutable ledger effect.

Corrections are represented by compensating entries, not silent mutation/deletion of historical entries.

## 50. BILREQ040 — Capacity limit shrink preserves committed usage

Reconfiguring a finite limit below current usage MUST preserve current usage and ledger history.

New consumption is denied until usage falls within the new limit.

## 51. BILREQ041 — Capacity release follows resource lifecycle

Deleting or otherwise permanently releasing a quota-bearing resource MUST release its capacity exactly once.

Disabling a resource does not automatically release capacity unless product semantics explicitly define disabled resources as non-counting.

The source resource context owns the lifecycle fact; Billing owns the capacity effect.

## 52. BILREQ042 — Resource-capacity counters do not periodically reset

`WorkspaceFeatureUsage` is the hard persistent-resource-capacity aggregate for the first certified reference slice.

The following legacy metering/reset concepts MUST be retired from this hard-capacity authority:

```text
ResetPeriod
LastResetAt
Reset()
```

unless a future separately certified capacity type proves a non-periodic semantic for them.

The first certified hard-resource-capacity quantity is an integer resource count.

Public hard-capacity contracts MUST use an integer amount end-to-end; no `decimal → int` truncation/cast is permitted.

`SoftLimit`/`OverageAllowed` are NOT part of the first certified hard-resource-capacity semantics. If retained temporarily for migration compatibility, they MUST NOT affect the authoritative hard-capacity decision and block D5 until removed or separately specified.

Periodic/consumption metering belongs to the Metered Usage subsystem, not `WorkspaceFeatureUsage`.

## 53. BILREQ043 — Metered usage is generated from successful source facts

The source bounded context owns the business fact that occurred.

Example:

```text
Automation owns AutomationExecutionSucceeded
Billing owns whether that success counts as billable AUTOMATION_EXECUTION usage
```

Failed or rolled-back source operations MUST NOT produce billable usage unless the commercial contract explicitly bills attempts.

## 54. BILREQ044 — Metered usage has stable metric vocabulary

Each billable metric MUST define:

```text
MetricCode
unit
producer fact
Account scope
optional Workspace scope
quantity semantics
period semantics
```

Analytics metrics are not automatically Billing metrics.

## 55. BILREQ045 — Metered usage ingestion is idempotent

A usage fact MUST have a logical identity that survives transport retry/replay.

Duplicate delivery MUST NOT double-count usage.

Idempotency identity MUST distinguish genuinely different source facts even when payload values are equal.

## 56. BILREQ046 — Metered usage is period-aware

Every metered usage entry MUST be attributable to an explicit commercial period.

Period identity is not inferred from “current month” unless the commercial product explicitly defines calendar-month billing.

For subscription-period billing, period boundaries derive from the effective Subscription period.

## 57. BILREQ047 — Usage history remains authoritative across period transition

Starting a new period MUST NOT erase old usage evidence.

A mutable current-period aggregate may be reset/advanced only if historical ledger/evidence remains queryable and rebuildable.

## 58. BILREQ048 — Late/out-of-order usage is explicit

Usage arriving after its occurrence period MUST be handled according to the occurrence timestamp and period policy.

It MUST NOT be silently added to whichever period happens to be current at ingestion time.

If a closed financial period cannot be mutated, the late fact becomes an explicit correction/adjustment according to provider/commercial policy.

## 59. BILREQ049 — Usage correction is compensating evidence

Incorrect usage is corrected through a linked adjustment/reversal.

The original commercial evidence remains retained.

A correction MUST identify the fact/entry it corrects where practical.

## 60. BILREQ050 — Capacity ledger and metered ledger are not interchangeable

Current `FeatureUsageLedger` is certified first for resource capacity.

Periodic metering MUST either:

```text
extend the ledger with explicit period/metric/source-event semantics
```

or use a separate Billing-owned metered-usage ledger.

It MUST NOT compute monthly usage by summing an all-time capacity ledger with no period predicate.

## 61. BILREQ051 — Metered limit enforcement defines timing

For a hard metered quota, the implementation MUST specify whether enforcement is:

```text
pre-reservation
transactional reservation
post-commit accounting with allowed overage
```

A hard quota that can be exceeded by concurrent operations is not certified until race behavior is proven.

# Billing API and administration

## 62. BILREQ052 — Billing exposes Account-scoped read contracts

The Backend MUST expose stable read use cases for at least:

```text
ListAvailablePlans
GetBillingSummary
GetCurrentSubscription
GetEffectiveEntitlements
GetUsageSummary
ListInvoices
GetInvoice when product UX requires detail
ListPaymentMethods when provider/payment feature is enabled
```

The exact HTTP route naming belongs to API delivery conventions, but the commercial scope is Account-rooted.

## 63. BILREQ053 — Billing administration is command-oriented

Billing writes MUST be use cases representing commercial intent rather than generic entity CRUD.

Required command categories include as applicable:

```text
ChangeSubscriptionPlan
ScheduleSubscriptionCancellation
ResumeScheduledSubscription
CancelSubscriptionImmediately (privileged/internal policy only where supported)
Start/complete provider checkout
Start provider billing-portal session
Set/replace default payment method
ReconcileBillingAccount / ReconcileSubscription (privileged operational path)
Grant/RevokeManualEntitlement (privileged path)
```

Generic `PUT Subscription.Status` and arbitrary `DELETE Invoice` are forbidden.

## 64. BILREQ054 — Billing administration requires Governance permission

At minimum, semantics MUST distinguish:

```text
view billing
manage subscription
manage payment methods
view invoices
privileged operational repair/manual entitlement override
```

A Workspace membership role by itself is not a Billing authorization decision unless Governance maps it to the required action.

## 65. BILREQ055 — Billing authorization and entitlement are different checks

A Billing administrator does not gain paid product features because they can manage Billing.

A paid Account member does not gain permission to change Billing because the Account has an Entitlement.

Both dimensions MUST be testable independently.

## 66. BILREQ056 — Stable Billing error taxonomy

Billing-facing APIs and product consumers MUST distinguish machine-readable failures at minimum equivalent to:

```text
BILLING_ENTITLEMENT_REQUIRED
BILLING_CAPACITY_EXCEEDED
BILLING_SUBSCRIPTION_INACTIVE
BILLING_OPERATION_PENDING
BILLING_PROVIDER_UNAVAILABLE
BILLING_OPERATION_CONFLICT
```

These remain distinct from:

```text
AUTHENTICATION_REQUIRED
PERMISSION_DENIED
VALIDATION_FAILED
CONCURRENCY_CONFLICT
```

Frontend behavior MUST use error semantics, not message text matching.

# Provider boundary

## 67. BILREQ057 — Provider gateway is Application-owned contract

Billing Application defines a provider-neutral gateway for required commercial operations.

Infrastructure implements the concrete provider adapter.

The first implementation MAY support only one real payment provider; the architecture MUST NOT pretend multiple providers are implemented merely because `PaymentProvider` contains enum values.

## 68. BILREQ058 — Provider object mapping is explicit and provider-relative

Provider customer mapping MUST identify:

```text
AccountId
Provider
ProviderCustomerId
mapping status/validity
```

Provider-backed Subscription synchronization MUST additionally have an explicit binding:

```text
SubscriptionId
AccountId
Provider
ProviderSubscriptionId
LastObservedProviderAt / provider version-watermark when available
```

Provider-backed price mappings are also provider-relative:

```text
PlanPriceId
Provider
ProviderPriceId
```

The same principle applies to enabled provider Invoice, PaymentTransaction and PaymentMethod mappings.

Required uniqueness is at least:

```text
UNIQUE(Provider, ProviderCustomerId)
UNIQUE(Provider, ProviderSubscriptionId)
UNIQUE(Provider, ProviderPriceId)
UNIQUE(Provider, ProviderInvoiceId)       // when invoices enabled
UNIQUE(Provider, ProviderPaymentId)       // when payments enabled
UNIQUE(Provider, ProviderMethodId)        // when payment methods enabled
```

Provider object IDs MUST NOT be stored as globally meaningful IDs without provider identity.

Creation/recovery MUST be idempotent.

## 69. BILREQ059 — Provider operation has durable logical identity and uses prepare/effect/settle

Every externally mutating provider operation that may create financial/commercial effect MUST carry a stable Billing operation identity.

A `BillingOperation` MUST retain enough immutable intent to reconcile the attempted effect:

```text
LogicalOperationId
AccountId
OperationType
SubjectType
SubjectId
Provider
RequestFingerprint
normalized target parameters (for example TargetPlanPriceId)
provider idempotency key
RequestedAt
LastAttemptAt?
AttemptCount
NextAttemptAt?
Outcome = Pending | Succeeded | Failed | Unknown
ProviderObjectReference?
CorrelationId
ErrorCode?
ReconciliationRequired
```

Notrelix request writes are already wrapped by the repository data-session transaction. Therefore the provider network call MUST NOT occur inside the same Application write request transaction.

The certified execution pattern is:

```text
API/Application command transaction
    validate + authorize
    create/load BillingOperation(Pending)
    persist immutable request fingerprint/target
    persist durable provider-effect intent/outbox message
    commit
    return Pending/Accepted operation state

provider-effect consumer / worker
    Tx Prepare:
        acquire durable provider-effect claim
        load BillingOperation
        persist attempt/processing evidence
        commit

    External Effect:
        invoke provider OUTSIDE any database transaction
        using the same provider idempotency key

    Tx Settle:
        normalize provider outcome
        update provider binding
        apply Subscription/financial transition if authoritative
        mark BillingOperation Succeeded/Failed/Unknown
        settle/release effect claim
        commit
```

This follows the repository's existing prepare/effect/settle provider-effect reliability pattern.

Application request idempotency and provider-effect claim/idempotency are separate but correlated guarantees.

If provider success is possible but the response or settle transaction is lost/fails, the durable BillingOperation remains `Pending` or becomes `Unknown` and MUST be reconciled.

A design that holds the request PostgreSQL transaction open across the provider call, or rolls back the only durable intent after a provider effect may have happened, does not satisfy this requirement.

## 70. BILREQ060 — Provider operation outcome taxonomy

A provider operation MUST support at least:

```text
Pending
Succeeded
Failed
Unknown
```

`Unknown` means the request may have reached the provider but Notrelix cannot safely determine the result.

Transport timeout after submission MUST NOT automatically become `Failed`.

## 71. BILREQ061 — Unknown provider outcome forbids blind retry

When duplicate financial/commercial effects are possible, an Unknown operation MUST be reconciled by stable operation/provider identity before a new external mutation is attempted.

Blind retry is forbidden.

## 72. BILREQ062 — Checkout/portal redirect is not authority

Browser redirect success does not prove provider state was committed.

After returning from hosted checkout/portal, frontend MUST reload authoritative Billing state and may show Pending until webhook/reconciliation confirms the commercial transition.

## 73. BILREQ063 — Provider secrets stay outside Domain/Application data contracts

Raw provider secret keys, webhook signing secrets and raw payment credentials MUST NOT be stored in Billing Domain entities, returned to frontend or logged.

Application may depend on abstract secret/provider configuration contracts; Infrastructure owns materialization.

# Provider webhooks and inbox

## 74. BILREQ064 — Webhook authenticity is verified before business mutation

Provider webhook processing MUST validate the provider-defined authenticity mechanism using the required raw request representation.

Where supported, verification includes timestamp/replay-window validation.

Unverified callbacks MUST NOT persist semantic Billing transitions.

## 75. BILREQ065 — Provider receipt/inbox is separate from Domain payload

Raw provider delivery data belongs to a provider/inbox boundary, not to the normalized Billing Domain schema.

The target receipt record identifies at least:

```text
Provider
ProviderEventId
received timestamp
verification outcome
processing status
correlation/reference metadata
safe retained payload or payload reference according to security/retention policy
```

Current `BillingEvent.RawData` MUST be refactored so provider-specific schema is not Domain authority.

## 76. BILREQ066 — Provider event identity is database-enforced

Provider delivery identity MUST be unique for the provider scope.

Conceptually:

```text
UNIQUE (Provider, ProviderEventId)
```

An ordinary non-unique index on `ProviderEventId` is insufficient for webhook idempotency.

## 77. BILREQ067 — Webhook acknowledgement follows durable receipt

After successful authenticity verification, the service SHOULD durably record/deduplicate the provider delivery before asynchronous semantic processing when provider retry behavior requires quick acknowledgement.

A process crash after acknowledging MUST NOT lose the event.

## 78. BILREQ068 — Webhook event mapping is explicit

Provider event types are mapped to normalized Billing commands/facts.

Unknown provider event types:

- do not crash the whole endpoint;
- are retained/ignored according to policy;
- remain observable;
- do not mutate commercial state.

## 79. BILREQ069 — Provider event ordering is not assumed

Billing MUST tolerate duplicate and out-of-order provider events unless the provider contract explicitly guarantees ordering.

Verified webhook receipts are durable observations/triggers; they do NOT directly overwrite Subscription/Invoice/Payment state from raw event order.

For provider objects whose current state can be fetched, the preferred path is:

```text
verified receipt
→ identify provider object
→ enqueue/trigger reconciliation
→ fetch current provider object snapshot
→ normalize current state
→ apply only if not older than the last-applied provider observation
```

Provider bindings MUST retain a provider freshness watermark when the provider exposes a reliable version/updated timestamp/sequence.

If the provider does not expose reliable event sequence, fetching the current object state is the anti-regression mechanism.

A late stale event MUST NOT revert newer authoritative state.

Provider API-response settlement and webhook reconciliation may race. Both paths MUST converge through the same idempotent transition logic and optimistic-concurrency/idempotency protections so that one commercial transition/event is produced semantically.

## 80. BILREQ070 — Reconciliation is first-class capability

Webhooks alone are insufficient as the only provider synchronization mechanism.

Billing MUST provide reconciliation for at least:

```text
provider request timed out after submission
webhook missed/delayed
local persistence failed after provider success
provider duplicate customer/subscription
provider/local state divergence
Unknown BillingOperation
stale Pending BillingOperation
stale provider-effect claim / interrupted effect attempt
```

A reconciliation candidate is:

```text
Outcome == Unknown
OR
Outcome == Pending AND the operation/claim is older than the configured freshness threshold
```

A fresh active provider-effect claim MUST NOT be stolen/re-fired.

For stale Pending/Unknown:

```text
inspect durable effect claim
query provider by provider object reference or idempotency correlation
classify current provider state
apply canonical transition if required
mark Succeeded/Failed or retain Unknown
or safely requeue the SAME logical operation only when provider absence is proven
schedule bounded retry/backoff
```

Reconciliation MUST use the original logical operation/provider idempotency identity. It MUST NOT manufacture a new financial operation merely because a worker crashed.

## 81. BILREQ071 — Reconciliation has scoped repair authority

Repair operations MUST be explicit privileged use cases.

They may:

```text
refetch provider state
resolve Unknown operation
repair provider mapping
replay/reprocess verified receipt
reconcile Subscription
regenerate Subscription-derived Entitlements
```

They MUST NOT bypass Account scope, Governance/system authorization, auditability or idempotency.

## 82. BILREQ072 — Source of truth is field-specific

Notrelix internal Billing state is authoritative for product capability behavior.

The provider may be authoritative for external settlement facts it owns.

Reconciliation rules MUST define which side is authoritative for each mapped fact instead of declaring either side universally authoritative.

# Financial evidence

## 83. BILREQ073 — Invoice is evidence-oriented

Invoice state represents durable commercial evidence.

Certified Invoice data MUST identify at least:

```text
Account
Subscription when applicable
provider invoice mapping when provider-backed
invoice number/reference
currency
amount
status
commercial period or covered interval where applicable
issued/due/payment-relevant timestamps
line items/snapshot information required to preserve billed meaning
```

Financial amount invariants:

```text
currency is normalized uppercase code
amounts are exact decimal values, never binary floating point
supported currency precision is validated before persistence
invoice and line-item currency must match
negative ordinary charge amounts are rejected unless an explicit credit/refund model exists
provider-observed settled amounts are stored as evidence rather than recomputed from mutable catalog data
```

For provider invoices, the provider-normalized invoice total is authoritative financial evidence. `Quantity * UnitAmount` MUST NOT silently replace a provider total because tax, discount and provider rounding may exist.

When Notrelix internally computes a financial amount, the currency precision/rounding rule MUST be explicit and tested before persistence.

Historical Invoice meaning MUST NOT change because current Plan data changed.

## 84. BILREQ074 — Invoice state transition is closed

The initial certified Invoice transition matrix is:

```text
Draft → Open
Open  → Paid
Open  → Uncollectible
Open  → Void
```

The following direct transitions are rejected:

```text
Draft → Paid
Draft → Uncollectible
Draft → Void
Paid → ordinary unpaid/void/uncollectible state
Void → Paid
Uncollectible → Paid through an ordinary setter
```

Arbitrary status assignment or ordinary deletion is forbidden.

A provider observation must map through normalized Invoice semantics before changing state.

Any future adjustment/refund/reopen behavior requires an explicit evidence model rather than mutation that erases the prior financial fact.

## 85. BILREQ075 — PaymentTransaction is first-class settlement evidence

The target Billing model MUST contain normalized payment/settlement evidence rather than treating Invoice status as the only Payment history.

A PaymentTransaction identifies at least:

```text
Account
Invoice when applicable
Provider
ProviderPaymentId/reference
amount/currency
normalized status
occurred/updated time
```

Provider-backed PaymentTransaction identity MUST be protected by:

```text
UNIQUE(Provider, ProviderPaymentId)
```

or an equivalent authoritative uniqueness mechanism.

The initial certified status model is:

```text
Pending
Succeeded
Failed
Canceled
Unknown
```

`Unknown` is distinct from `Pending`: `Pending` means known non-terminal/in progress; `Unknown` means Notrelix cannot safely determine provider terminal settlement state. `Unknown` MUST enter reconciliation and MUST NOT be blindly retried.

Payment currency/precision follows the same exact-money rules as Invoice evidence.

Refund semantics are NOT implicitly required unless product/provider scope enables refunds.

## 86. BILREQ076 — PaymentMethod is Account-rooted

PaymentMethod belongs to the payer Account.

Current mandatory `WorkspaceId` ownership is transitional and MUST be migrated.

Workspace may be present only as optional context if a future product requirement explicitly scopes methods below Account; it is not the default ownership model.

## 87. BILREQ077 — Raw card/payment credentials are prohibited

Notrelix stores provider-safe references and display metadata only, such as:

```text
provider method ID/token reference
brand
last4
status
default indicator
```

Full PAN/CVV/raw card credentials MUST NOT enter Domain/Application persistence or logs.

## 88. BILREQ078 — Single default PaymentMethod is concurrency-safe

If the product supports a default PaymentMethod, at most one active default per Account/provider billing profile may exist.

The invariant MUST be enforced under concurrent updates by transaction/database design, not only by frontend behavior.

# Account lifecycle and retention

## 89. BILREQ079 — Account closure coordinates Billing

Accounts owns Account close/delete semantics.

Billing reacts through an explicit contract to perform required commercial actions such as:

```text
stop new paid renewal where policy requires
cancel/schedule provider subscription according to product rule
revoke capability at the effective commercial time
retain required invoice/payment/usage evidence
```

Billing MUST NOT delete the Account aggregate or define Account lifecycle itself.

## 90. BILREQ080 — Commercial evidence retention is independent of product deletion

Ordinary deletion of Workspace/product resources MUST NOT cascade away required Billing evidence.

At minimum, retention policy MUST explicitly cover:

```text
Invoice
PaymentTransaction
Plan/price snapshot meaning
usage evidence used for billing
provider operation/receipt evidence required for reconciliation/audit
```

## 91. BILREQ081 — Retained financial evidence is minimized

Where Account/User personal data may be deleted/anonymized while financial evidence must remain, Billing retains only the legally/product-required references and safe metadata.

Retention does not justify retaining unnecessary secrets or raw personal payloads indefinitely.

# Data isolation and RLS

## 92. BILREQ082 — Billing data is Account-isolated

All Account-owned Billing reads/writes MUST be protected by the same tenant-boundary architecture as other Account-scoped data.

RLS semantics are part of the physical schema contract. The current helper chooses Workspace scope when a table contains both `account_id` and `workspace_id`; therefore target migrations MUST NOT leave accidental `workspace_id` columns on Account-owned financial tables, and MUST NOT rename the Entitlement target column away from the RLS-visible Workspace scope without an explicit policy replacement.

The current RLS policy layer already classifies Billing catalog tables separately from scoped business tables; target migrations MUST preserve or strengthen that separation.

## 93. BILREQ083 — Catalog, transactional, reporting and worker RLS are different classes

Global catalog tables such as Plans/PlanPrices/PlanCapability/PlanTransitionPolicy use catalog read policy and SYSTEM/OPERATOR-only mutation policy.

`Account Billing Admin` is NOT a global catalog administrator.

Transactional commercial tables:

```text
Subscription/BillingCustomer/Invoice/PaymentTransaction/PaymentMethod/BillingOperation
→ Account-scoped

Entitlement
→ Account-scoped when workspace_id IS NULL
→ Workspace-scoped when workspace_id is present

Workspace hard-capacity / Workspace metered state
→ Workspace-scoped
```

Child tables without direct scope columns receive explicit parent-correlated RLS.

Account-wide Billing administration MUST NOT bypass Workspace RLS with worker/system context merely to render Billing UI.

When an Account Billing Admin needs cross-Workspace commercial usage visibility, Billing exposes an Account-scoped reporting/read projection containing only Billing-owned commercial usage facts. Product-resource private data remains Workspace/resource protected.

Provider receipt/internal tables use worker/internal policy, but raw provider bodies require stricter treatment than generic support-readable worker tables:

```text
ordinary tenant → no access
support_readonly → no raw payload access by default
worker/system → required processing access only
```

Raw provider bodies SHOULD be encrypted/protected or stored through a restricted blob/reference mechanism; support-visible receipt rows contain only safe metadata.

RLS verification MUST classify every Billing table explicitly.

## 94. BILREQ084 — Cross-context access does not bypass RLS/ownership

Product contexts consume Billing Application contracts and MUST NOT query Billing tables directly.

Background consumers MUST establish explicit Account/system context before accessing scoped Billing state.

Account-wide Billing read APIs use Account-scoped Billing projections/contracts; they do not switch the incoming request into worker scope to read all Workspace rows.

Global catalog mutation is SYSTEM/OPERATOR-only and is distinct from Account Billing administration.

RLS verification MUST compare an explicit expected Billing table/policy inventory against actual PostgreSQL state and fail on any missing/wrong policy. Returning issue rows without failing the verification gate is insufficient.

## 95. BILREQ085 — Billing public events are canonical commercial facts

Public integration events represent completed normalized Billing facts.

After the `PlanPriceId` authority migration, Subscription commercial-change events MUST represent the exact offer change.

The current Plan-ID-only shape cannot represent:

```text
same Plan revision
monthly → annual
```

Therefore the target `subscription.changed` version/equivalent contains at least:

```text
AccountId
SubscriptionId
PreviousPlanPriceId
NewPlanPriceId
EffectiveAt
```

Derived Plan IDs may be included for compatibility but are not authority.

WorkspaceId MUST NOT remain a required Subscription ownership field in the new Account-scoped event contract.

Event names/versions MUST follow repository event-registry compatibility rules.

## 96. BILREQ086 — Provider payloads are not public Billing events

A provider event is an input to Billing, not automatically a Notrelix integration event.

No downstream context should need Stripe/PayPal/provider DTO knowledge to consume Billing facts.

## 97. BILREQ087 — Existing subscription events require consumer maturity

Current `subscription.changed` and `subscription.canceled` integration events exist, while current Billing consumers include logging/stub behavior.

Certification MUST inventory actual consumers and classify each as:

```text
required functional consumer
observability-only consumer
placeholder/stub to retire
```

A logging stub is not evidence that a business reaction has been delivered.

## 98. BILREQ088 — Entitlement propagation is explicit

When product contexts rely on local projections/caches rather than synchronous capability calls, Billing MUST publish sufficient versioned entitlement facts for deterministic invalidation/rebuild.

The initial closure may use synchronous Billing Public contracts where latency is acceptable; local projection is an optimization, not a second authority.

# Cross-context contracts

## 99. BILREQ089 — Accounts → Billing contract

Billing consumes stable Account identity and lifecycle facts only.

It MUST NOT read Account private persistence to infer Plan truth.

Required interactions include:

```text
Account creation → commercial bootstrap
Account closure/deletion → Billing lifecycle coordination
Account identity → payer reference
```

## 100. BILREQ090 — Billing → product contexts contract

WorkManagement, Documents, Automation, Integrations and other product capabilities consume only stable Billing Public contracts/events.

They MUST NOT:

```text
branch on PlanCode
branch on SubscriptionTier
read Entitlement tables
sum Billing usage tables
call payment provider directly for feature gating
```

## 101. BILREQ091 — Product source fact → Billing usage contract

For metered usage, producers emit/offer a stable successful business fact.

Billing classifies that fact into commercial usage.

The producer MUST NOT encode provider invoice/SKU semantics.

## 102. BILREQ092 — Governance → Billing administration contract

Governance owns actor permission semantics for Billing administration.

Billing commands declare the required action/resource and remain responsible for commercial business rules after authorization passes.

## 103. BILREQ093 — Platform → Billing mechanism contract

Billing relies on Platform/Foundation for approved:

```text
transactions
idempotency
messaging/outbox/inbox mechanism
secret mechanism
RLS/tenant context
observability
```

Billing MUST NOT fork local infrastructure mechanisms merely because financial operations require stronger semantics; it extends business guarantees on top of the shared mechanism.

## 104. BILREQ094 — Analytics is downstream

Analytics may consume normalized Billing events/read models.

Analytics aggregates are not commercial authorities and MUST NOT be used to decide real-time entitlement or invoice correctness.

# Frontend contract

## 105. BILREQ095 — Billing frontend is data-driven

The production Billing page MUST load Plan/subscription/usage/invoice information from Billing API contracts.

The current hard-coded Plan array and hard-coded prices/features are prototype fixtures and MUST be retired from production behavior.

## 106. BILREQ096 — Workspace shell does not own Plan truth

The current pattern:

```text
workspace.plan
→ cast to BillingPlanTier
→ render Billing state
```

MUST be migrated.

A Workspace route may resolve the active Account and host the Billing screen, but the commercial state comes from Billing.

## 107. BILREQ097 — Billing query cache is Account-scoped

Frontend Billing cache/query identity MUST contain the payer Account identity.

Workspace-only keys are insufficient when multiple Workspaces share one Account subscription.

Account switching MUST NOT expose stale Billing state from a different Account.

## 108. BILREQ098 — Frontend entitlement guards are UX only

Frontend may hide/disable routes/actions based on entitlement for user experience.

Backend Application enforcement remains authoritative.

Removing a frontend guard MUST never bypass paid capability enforcement.

## 109. BILREQ099 — Billing UX distinguishes denial classes

Frontend MUST distinguish at least:

```text
no permission to manage billing
feature not included
capacity exhausted
subscription pending
provider unavailable
payment issue/past due
```

It MUST NOT display every denied action as “upgrade required”.

# Security

## 110. BILREQ100 — Commercial mutation is security-sensitive

Subscription, PaymentMethod, manual Entitlement, provider repair/reconciliation and financial-history operations require explicit authorization and audit evidence.

## 111. BILREQ101 — Secret/log redaction

Logs, traces, errors and events MUST NOT contain:

```text
provider secret keys
webhook signing secrets
raw card data
full payment credentials
sensitive provider response payloads not required for diagnosis
```

Provider IDs may be logged only according to approved observability/privacy policy.

## 112. BILREQ102 — Webhook endpoint is not tenant-authorized by browser identity

Provider webhooks authenticate the provider delivery, then resolve Account/Billing scope from verified provider mappings.

They MUST NOT trust Account/Workspace IDs supplied in unverified payload metadata as authorization proof.

## 113. BILREQ103 — Billing repair paths are privileged

Operational reconciliation/manual entitlement repair cannot be exposed as ordinary workspace-user commands.

They require system/administrative authority, audit correlation and strict input validation.

# Concurrency and consistency

## 114. BILREQ104 — One effective Subscription policy

The database/Application design MUST prevent conflicting simultaneously-effective Subscription states for the same Account/commercial scope where product semantics allow only one.

Concurrency behavior MUST be deterministic for simultaneous upgrade/cancel/renew operations.

## 115. BILREQ105 — Commercial commands are idempotent

Commands capable of external or financial side effects require stable application idempotency in addition to provider idempotency where supported.

The two identities SHOULD correlate but are not interchangeable.

## 116. BILREQ106 — Entitlement reconciliation is concurrency-safe

Concurrent replay of the same Subscription transition MUST converge on the same effective entitlement set without duplicate active grants or lost revocation.

## 117. BILREQ107 — Last-slot capacity race remains proven

The current reference invariant remains mandatory:

```text
remaining capacity = 1
concurrent create A + B
→ exactly one commits
```

No refactor of Entitlement/Usage may regress this property.

## 118. BILREQ108 — Default payment-method race is deterministic

Concurrent attempts to set different default methods MUST result in one final default according to transaction/version policy without a committed state containing multiple defaults.

# Migration and compatibility

## 119. BILREQ109 — `Account.PlanCode` migration

`Account.PlanCode` MUST cease to be commercial authority.

Migration sequence MUST:

1. inventory every read/write of `PlanCode`;
2. classify required compatibility consumers;
3. bootstrap/reconcile Billing Subscription state;
4. switch consumers to Billing contracts;
5. stop direct business writes;
6. retain only a temporary projection if compatibility still requires it;
7. remove the projection when all consumers are migrated.

No new code may branch on `Account.PlanCode` for entitlement decisions.

## 120. BILREQ110 — Workspace Plan DTO migration

Workspace query contracts MUST stop presenting Workspace-owned `Plan` as canonical commercial truth.

Frontend Billing must migrate before removal breaks rendering.

If a compatibility field remains temporarily, it is explicitly documented as derived/non-authoritative.

## 121. BILREQ111 — Frontend hard-coded catalog migration

Hard-coded Plan names/prices/features are removed from production rendering once Billing catalog API exists.

Visual test fixtures may retain static scenario data only inside verification/test boundaries.

## 122. BILREQ112 — Plan price and Subscription offer migration

Existing embedded `Plan.Price` / `Plan.Period` data MUST migrate to explicit `PlanPrice` rows without changing amount/currency/billing interval.

Subscription backfill to exact `Subscription.PlanPriceId` uses deterministic classification:

```text
exactly one valid SubscriptionItem with Quantity=1
and item.PlanPrice belongs to legacy Subscription.PlanId
→ use item PlanPriceId after validation

no SubscriptionItem
→ match/create PlanPrice from legacy Plan.Price + Plan.Period

multiple SubscriptionItems
or Quantity != 1
→ migration BLOCKER for the first single-price model

item PlanPrice points to different Plan
→ data-integrity BLOCKER

embedded price/period conflicts with candidate PlanPrice
→ report/classify; never silently choose
```

The selected current Free revision MUST have exactly one active default/bootstrap PlanPrice.

Migration evidence reports every blocked/ambiguous row before old authority is removed.

## 123. BILREQ113 — SubscriptionTier and legacy status migration

`SubscriptionTier` MUST cease to be commercial authority.

Before removal:

```text
inventory consumers
compare legacy tier checks with target capability/effective-subscription results
switch consumers
remove tier branching
```

Legacy provider-shaped Subscription statuses require explicit classification:

```text
Incomplete
→ PendingActivation only when evidence proves unresolved activation
→ otherwise BLOCKED

Unpaid
→ PastDue only when authoritative payment/provider evidence proves delinquency
→ otherwise BLOCKED

Trialing
→ not silently certified
→ explicit migration decision or BLOCKED
```

No enum-name-only mapping is allowed.

## 124. BILREQ114 — PaymentMethod scope migration

Current Workspace-rooted PaymentMethod records MUST be migrated to Account ownership without cross-Account reassignment.

If multiple Workspace records map to the same provider method, migration MUST deduplicate deterministically and preserve the active/default choice through an explicit rule.

## 125. BILREQ115 — Entitlement lineage backfill

Existing Entitlements lacking source identity MUST be classified before certification.

Subscription-derived grants are backfilled to a known Subscription/source where provable.

Unprovable/manual legacy grants remain explicitly marked as migrated/manual legacy evidence rather than falsely attributed.

## 126. BILREQ116 — Provider receipt uniqueness migration

Before enabling real provider webhooks, the provider receipt table/index MUST enforce `(Provider, ProviderEventId)` uniqueness.

Existing duplicates, if any, must be reconciled before the unique constraint becomes authoritative.

## 127. BILREQ117 — Usage semantic migration

Existing `UsageMetric`, `UsageMetricHistory`, `WorkspaceFeatureUsage` and `FeatureUsageLedger` records/usages MUST be inventoried by caller and semantic purpose.

The migration MUST classify each metric as:

```text
persistent resource capacity
periodic metered usage
analytics-only/non-billing legacy
```

No counter may be moved into a new billing period model solely based on table name.

## 128. BILREQ118 — Billing migration preserves referential and financial integrity

Target Billing schema MUST add/verify authoritative relationships for at least:

```text
PlanPrice → Plan
PlanCapability → Plan
Subscription → PlanPrice
Invoice → Subscription when applicable
InvoiceLineItem → Invoice
PaymentTransaction → Invoice when applicable
provider binding → its internal Billing object
```

Historical commercial/financial relations default to:

```text
RESTRICT / NO ACTION
```

rather than destructive cascade unless explicitly proven safe.

Application invariants also prove same-Account ownership across relationships.

Migration order MUST preserve source evidence until all backfill/consistency checks complete. A Workspace/source scope column MUST NOT be dropped before it is used to validate target Account ownership.

Clean install and audited-baseline upgrade are both required evidence.

## 129. BILREQ119 — Billing operation correlation

Every externally mutating Billing flow MUST be traceable through:

```text
request/correlation ID
application idempotency key or logical operation ID
Account ID
Subscription/Billing operation ID
provider operation/reference when available
webhook receipt/event ID when applicable
```

without logging secrets.

## 130. BILREQ120 — Reconciliation observability

Operational metrics/logs MUST expose at least:

```text
Unknown provider operations
failed verified webhook processing
reconciliation attempts/outcomes
provider/local divergence
stale pending operations
```

A provider outage must be distinguishable from a product authorization denial.

## 131. BILREQ121 — Entitlement/capacity observability

The system MUST make it possible to diagnose:

```text
why capability was denied
which entitlement/source was effective
finite/unlimited limit
current usage
capacity conflict/replay
```

without exposing private Billing persistence to consumers.

# Performance and availability

## 132. BILREQ122 — Product hot path never calls payment provider

Authoritative product capability evaluation MUST use internal Billing state/projections.

A normal resource creation request MUST NOT synchronously call Stripe/PayPal/provider merely to determine entitlement.

## 133. BILREQ123 — Capability evaluation is bounded

Capability lookup MUST use indexed Account/Workspace/Feature identities and avoid unbounded historical scans on hot paths.

If ledger sum becomes too expensive, a versioned/materialized aggregate may be used, but it MUST remain reconcilable against retained ledger evidence.

## 134. BILREQ124 — Provider degradation does not corrupt internal state

Provider outage may block new paid commercial mutations, but it MUST NOT arbitrarily corrupt existing internal Subscription, Entitlement, Invoice or product resource state.

Existing capability during outage follows explicit current commercial state/grace policy.

# Reliability and failure modes

## 135. BILREQ125 — Database failure before external provider submission

If local validation/persistence fails before provider submission, no external operation is issued.

The command may fail normally under the repository idempotency contract.

## 136. BILREQ126 — Database failure after provider success

If the provider succeeds but local persistence fails, the operation is not safely classified as ordinary failure.

The logical Billing operation remains/reconstructs a reconciliable state and must converge from provider evidence/webhook/reconciliation.

## 137. BILREQ127 — Duplicate webhook processing is harmless

Reprocessing the same verified provider event must converge without duplicate Subscription transition, Invoice effect, PaymentTransaction or Entitlement derivation.

## 138. BILREQ128 — Entitlement dependency failure is explicit

A product write depending on paid capability must fail deterministically if Billing cannot provide authoritative capability state, unless an explicitly accepted grace/fallback policy applies.

It MUST NOT silently grant the operation.

# API contract

## 139. BILREQ129 — API contracts expose Billing semantics, not persistence models

HTTP DTOs MUST NOT expose EF entities, provider SDK objects or internal row structure.

Contracts use stable IDs, normalized statuses and product-safe fields.

Provider-backed mutations follow the durable provider-effect protocol and therefore return an accepted/pending operation rather than pretending that provider completion happened inside the request:

```text
POST provider-backed Billing command
→ 202 Accepted
→ BillingOperationId + normalized Pending state

GET /api/billing/operations/{operationId}
→ Pending | Succeeded | Failed | Unknown
→ safe operation result fields when completed
```

A hosted checkout/portal URL, when returned, is treated as short-lived sensitive operation output: it is not written to ordinary logs/events and is not exposed to another Account.

Internal commands with no external provider effect MAY complete synchronously when their full authoritative transition commits in the request transaction.

## 140. BILREQ130 — API Account binding is server-authoritative

Where the route is nested under Workspace, the backend resolves Workspace → Account through approved ownership contracts/context and applies Billing operation to that Account.

A request cannot switch the payer by posting a different Account ID in the body.

## 141. BILREQ131 — OpenAPI compatibility

Breaking Billing API changes require explicit contract migration and coordinated frontend regeneration/update.

Handwritten duplicate frontend Billing types MUST not become a competing contract authority where generated contracts are available.

# Functional use-case catalog

## 142. BILREQ132 — Required catalog use cases

The final Billing core supports and tests:

```text
CreatePlanRevision (system/admin)
DeprecatePlanRevision
ArchivePlanRevision
ListAvailablePlans
GetPlanDetails
```

Generic deletion of referenced Plan history is not a supported user operation.

## 143. BILREQ133 — Required Subscription use cases

The final Billing core supports and tests:

```text
BootstrapDefaultSubscription
GetCurrentSubscription
Activate/ConfirmSubscription
ChangeSubscriptionPlanPrice(TargetPlanPriceId)
ScheduleCancellation
ResumeScheduledCancellation
ApplyScheduledCancellationToFree
RenewSubscription
MarkPastDue / recover from PastDue
ExpireSubscription
TerminateSubscriptionImmediately when privileged/no-fallback
```

Provider-backed variants create/use durable `BillingOperation` and provider-effect intent rather than calling the provider inside the request transaction.

Time-driven scheduled operations are executed by the Billing lifecycle worker/reconciliation path.

## 144. BILREQ134 — Required Entitlement use cases

The final Billing core supports and tests:

```text
ReconcileSubscriptionEntitlements
GetCapabilityDecision
GetEffectiveEntitlements
GrantManualEntitlementOverride
Revoke/ExpireManualEntitlementOverride
```

Unsupported `AddOn`/`Promo` semantics are not exposed as completed use cases.

## 145. BILREQ135 — Required capacity use cases

The final Billing core supports and tests:

```text
ConsumeResourceCapacity
ReleaseResourceCapacity
ReconcileResourceCapacity from retained ledger/source state when repair is required
```

At least Automation Rule creation/deletion remains a real reference consumer.

## 146. BILREQ136 — Required metered-usage use cases

When metered billing is enabled, the final capability supports and tests:

```text
IngestUsageFact
ApplyUsageCorrection
GetCurrentPeriodUsage
Advance/closeUsagePeriod
Rebuild/ReconcileUsageAggregate
```

If provider billing does not yet charge metered usage, the workstream may certify internal metering separately, but MUST not label provider metered billing complete.

## 147. BILREQ137 — Required provider/admin use cases

When paid provider integration is enabled, released scope supports/tests:

```text
EnsureProviderCustomer
StartCheckoutOrSubscriptionChange
StartBillingPortalSession when used
GetBillingOperation
ProcessVerifiedProviderReceipt
ReconcileBillingOperation
ReconcileSubscription
```

Provider-effect commands return accepted/pending operation identity first. Provider completion is observed through BillingOperation/read state, webhook and reconciliation.

Safe hosted action URL/result, when applicable, is available only after successful operation settlement and follows secret/log-minimization rules.

## 148. BILREQ138 — Required financial read use cases

When provider financial features are enabled, Billing supports and tests:

```text
ListInvoices
GetInvoice
ListPaymentMethods
GetPayment/settlement evidence required by product/admin support
```

These are Account-scoped and authorization-protected.

# Functional acceptance criteria

## 149. BILAC001 — Catalog acceptance

A subscribed `PlanPriceId` retains its historical currency/billing-interval/amount and transitively its immutable Plan revision after a newer revision is published.

At one effective instant, each `PlanCode` has at most one sellable/current revision.

Frontend catalog displays data returned by Billing, not hard-coded production prices/features.

## 150. BILAC002 — Bootstrap acceptance

Creating an eligible Account and replaying the Account-created delivery multiple times results in exactly one effective default Subscription and one deterministic Subscription-derived entitlement set.

## 151. BILAC003 — Subscription acceptance

Every persisted Subscription status has proven transition behavior; invalid transitions are rejected; each Subscription is Account-rooted and pinned to one exact `PlanPriceId`; provider-backed upgrade is effective only after confirmed success; downgrade is scheduled to current-period end; cancel timing is explicit and idempotent.

## 152. BILAC004 — Entitlement acceptance

For a fixed Account/Workspace/Feature and commercial state, repeated evaluation returns the same effective entitlement decision.

A missing/expired/revoked grant does not grant capability.

A manual override resolves according to the specified precedence and can be traced to actor/reason/source.

## 153. BILAC005 — Capacity acceptance

With one remaining finite slot, two concurrent creates result in exactly one committed quota-bearing resource and exactly one positive ledger effect.

Retry of the winning logical operation does not double-consume.

Deleting/releasing the resource records exactly one compensating release when product lifecycle says it no longer counts.

## 154. BILAC006 — Metered usage acceptance

Duplicate source-fact delivery counts once.

Late usage is attributed/corrected according to its commercial period rather than silently moved to current period.

Period transition retains historical usage evidence.

## 155. BILAC007 — Provider operation acceptance

A durable `BillingOperation(Pending)` is committed before provider submission.

A timeout after provider submission, or a provider-success/local-persistence failure, leaves that durable operation reconciliable as `Unknown/Pending` rather than causing an unsafe ordinary retry.

Reconciliation converges the operation and Subscription state without duplicate commercial effect.

## 156. BILAC008 — Webhook acceptance

Invalid signatures are rejected before semantic mutation.

Duplicate verified provider event delivery has one semantic effect.

Out-of-order stale provider events cannot revert newer normalized Billing state.

## 157. BILAC009 — Financial evidence acceptance

Invoice/payment evidence remains stable after Plan catalog changes and survives ordinary product-resource deletion according to retention policy.

`PaymentTransaction` distinguishes `Pending` from `Unknown` and sends unknown settlement state to reconciliation.

Raw payment credentials are absent from Domain/Application persistence and logs.

## 158. BILAC010 — Authorization acceptance

A user with product Entitlement but without Billing-management permission cannot change Subscription/payment settings.

A Billing administrator without resource permission does not gain unrelated product-resource access.

## 159. BILAC011 — Frontend acceptance

Billing frontend renders authoritative catalog/subscription/usage/invoice state; Account switch cannot reuse another Account's Billing cache; pending/provider failures are visually distinguishable from permission and entitlement denial.

# Non-functional acceptance criteria

## 160. BILAC012 — Architecture

Billing Domain contains no provider SDK/DTO dependency.

Product contexts do not consume Billing persistence types.

Provider adapter remains Infrastructure-owned.

## 161. BILAC013 — Data isolation

RLS/tenant tests prove no Account can read or mutate another Account's Billing transactional state.

The expected Billing RLS table inventory has no missing policy, child financial tables are protected through explicit parent policies, Account-owned financial tables are not accidentally Workspace-scoped, and Workspace-targeted Entitlements use the single RLS-visible target scope column.

Catalog read policy remains deliberately separate.

## 162. BILAC014 — Idempotency

Application command replay, provider idempotency and webhook deduplication are independently proven at the layers where each applies.

## 163. BILAC015 — Concurrency

Subscription mutation, entitlement reconciliation, last-slot capacity and default payment-method invariants have deterministic concurrent outcomes.

## 164. BILAC016 — Migration

Both clean schema creation and upgrade from the accepted baseline preserve required commercial history and complete without dual commercial authority remaining in production paths.

## 165. BILAC017 — Security

Provider secrets, raw payment credentials and unverified callback data cannot directly mutate commercial state or leak through normal API/logging paths.

## 166. BILAC018 — Observability

A support/operator investigation can correlate a Billing request through internal operation, provider request, webhook receipt, reconciliation and final Subscription/Invoice/Payment state without inspecting arbitrary raw tenant tables.

## 167. BILAC019 — Performance

Entitlement/capacity hot paths do not synchronously call payment provider and remain bounded by indexed internal state/projection access.

## 168. BILAC020 — CI evidence

Domain, Application, architecture and integration tests execute for Billing-affecting source changes. A documentation-only CI success with Backend CI skipped is not implementation certification evidence.

# Requirement traceability contract

## 169. TESTS artifact obligation

`billing-entitlements.tests.md` MUST map every critical `BILREQ` and `BILAC` to concrete test/evidence classes, including negative and concurrency cases.

At minimum it must cover:

```text
catalog/versioning
bootstrap
subscription transition matrix
entitlement derivation/precedence
capacity race + replay
metered usage idempotency/period correction
Billing authorization
provider Unknown outcome
webhook verification/dedup/out-of-order
financial retention
migration
RLS
frontend contract/E2E where released
```

## 170. PLAN artifact obligation

`billing-entitlements.plan.md` MUST classify exact source paths as:

```text
KEEP
HARDEN
REFACTOR
MIGRATE
RETIRE
BUILD
BLOCKED
```

and sequence changes so no consumer is switched to a contract before its producer/migration path is available.

The plan MUST not rewrite this specification into implementation options.

## 171. CERTIFICATION artifact obligation

`billing-entitlements.certification.md` MUST evaluate the concrete candidate SHA.

It MUST distinguish at least:

```text
internal catalog/subscription readiness
Entitlement consumer readiness
hard-capacity readiness
metered-usage readiness
provider/payment readiness
webhook/reconciliation readiness
billing-admin/frontend readiness
migration/security readiness
```

One narrow D4/D5 slice MUST NOT be used to certify the entire workstream.

## 172. Source-audit evidence rule

A capability is not considered implemented because:

- an entity exists;
- an enum contains a status;
- a DbSet/configuration exists;
- a frontend fixture renders a screen;
- a logging/stub consumer receives an event;
- prose says the capability exists.

Certification requires executable Application/API/integration behavior and tests at the appropriate boundary.

# Stop conditions

## 173. BILSTOP001 — dual commercial authority

Stop implementation if a change would leave two production authorities for Plan/Subscription/Entitlement meaning, including `Account.PlanCode` or Workspace Plan alongside Billing Subscription.

## 174. BILSTOP002 — provider schema becomes Domain schema

Stop if provider DTO/status/payload is being persisted or exposed as the canonical Billing Domain contract without normalized mapping.

## 175. BILSTOP003 — unsafe financial retry

Stop if a provider timeout/unknown result is implemented as blind retry where duplicate commercial/financial effect is possible.

## 176. BILSTOP004 — entitlement and authorization merged

Stop if Billing Entitlement is used as proof of Governance permission or Governance role is used as proof of paid capability.

## 177. BILSTOP005 — non-idempotent usage

Stop if duplicate source/provider delivery can double-count billable usage or capacity.

## 178. BILSTOP006 — destructive downgrade/history mutation

Stop if downgrade or migration requires deleting user data or rewriting commercial history without an explicit accepted product/legal rule.

## 179. BILSTOP007 — raw payment secret boundary violation

Stop if raw card/provider secret material enters Domain/Application persistence, integration events, normal logs or frontend contracts.

## 180. BILSTOP008 — unsupported source enum treated as feature support

Stop if `AddOn`, `Promo`, PayPal or another enum value is documented/certified as implemented solely because the enum exists.

## 181. BILSTOP009 — periodic metering built on all-time capacity sum

Stop if period-based quota is implemented by summing `FeatureUsageLedger` without explicit period/source semantics.

## 182. BILSTOP010 — Billing API trusts client payer identity

Stop if a Billing mutation accepts Account/Workspace identity from the client without server-side resource/tenant binding and authorization.

# Downstream readiness contract

## 183. Entitlement D4 handoff

Product teams may rely on Billing for a capability only when that specific capability has:

```text
stable FeatureCode
Subscription/Manual source semantics
source lineage
 deterministic resolver
fail-closed public decision contract
Account/Workspace isolation
consumer integration test
stable error semantics
```

Provider/payment completion is not required for Free/internal capability use, but paid-plan capabilities cannot be declared production-ready without a trustworthy way to establish paid Subscription state.

## 184. Capacity D4 handoff

A hard resource quota may be consumed by another context only when:

```text
capacity owner is Billing
last-slot concurrency is proven
logical-operation replay is proven
resource create/delete lifecycle is integrated
transaction boundary is explicit
limit shrink behavior is proven
```

Automation Rule is the reference implementation and regression baseline.

## 185. Provider/payment release handoff

A paid provider flow is releasable only when:

```text
provider gateway exists
logical BillingOperation exists
provider idempotency/correlation exists
verified webhook receipt exists
provider event uniqueness is enforced
Unknown outcome is reconciliable
Subscription + Entitlement convergence is tested
financial evidence is retained
frontend does not treat redirect as final authority
```

# Definition of Done — Billing & Entitlements final scope

## 186. Functional DoD

The workstream is functionally complete when:

- catalog revisions/prices/capabilities are authoritative and historical;
- Account bootstrap establishes explicit commercial state;
- Subscription lifecycle is closed and executable;
- Subscription → Entitlement derivation is implemented and idempotent;
- product capability contract is stable and fail-closed;
- hard resource capacity remains race-safe and evidence-backed;
- metered usage, when claimed, is period-aware/idempotent/correctable;
- Billing read/admin APIs exist for the released feature set;
- provider-backed flows, when claimed, include webhook + reconciliation;
- Invoice/Payment/PaymentMethod are usable commercial evidence, not Domain-only models;
- Billing frontend consumes real contracts.

## 187. Architecture DoD

Architecture closure requires:

- Account is the payer root;
- Workspace is only a target/usage scope where required;
- Billing is the only commercial authority;
- `Account.PlanCode`/Workspace Plan/front-end tier literals no longer decide access;
- product contexts use Billing Public contracts;
- provider code remains outside Domain;
- RLS and tenant boundaries match ownership;
- capacity and metered usage are explicitly distinct.

## 188. Security DoD

Security closure requires:

- Billing administration is Governance-authorized;
- webhook authenticity is verified before semantic processing;
- provider secrets/raw payment credentials are outside Domain/Application persistence;
- Account scope cannot be forged by request payload;
- repair/manual override paths are privileged/audited;
- fail-closed capability behavior is proven.

## 189. Financial/reliability DoD

Financial/reliability closure requires:

- externally mutating operations have durable logical identity;
- Unknown provider outcomes are represented/reconciled;
- duplicate webhook/provider/source usage delivery is harmless;
- Invoice/Payment evidence is retained according to policy;
- Account/product deletion cannot accidentally cascade financial evidence;
- migrations preserve historical commercial meaning.

## 190. Verification DoD

Final certification requires candidate-SHA evidence for:

```text
Domain tests
Application tests
Architecture tests
PostgreSQL/RLS integration tests
hard-capacity concurrency tests
migration clean + upgrade tests
provider contract/integration tests when provider is released
webhook verification/idempotency/out-of-order tests
reconciliation/Unknown-outcome tests
API authorization tests
frontend contract/component/E2E tests for released Billing UX
```

No `NOT_EVALUATED` critical assertion may remain for a capability being declared complete.

---

# Final Billing rule

Billing & Entitlements is complete only when Notrelix can answer, from one commercial authority and without provider/frontend guesswork:

```text
What commercial terms does this Account currently hold?
Why does it hold them?
Which capabilities do those terms grant to this Account/Workspace?
What finite/unlimited capacity applies?
What commercial usage occurred in which period and from which source fact?
What external financial operation happened, is it known or uncertain, and how is it reconciled?
What financial evidence must remain after product state changes?
```

The target system is therefore not “CRUD for Plan/Subscription/Entitlement”. It is a provider-independent, Account-rooted commercial state machine with deterministic Entitlement derivation, concurrency-safe capacity, evidence-oriented usage/financial history and recoverable external-provider effects.
