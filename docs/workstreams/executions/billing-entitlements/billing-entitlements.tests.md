---
document_id: WRK-TESTS-BILLING-ENTITLEMENTS
document_type: workstream-tests
status: active
owner: billing-entitlements-team
source_branch: develop
source_commit: 35702d0fa9fb01ed68b0667bab500030d60bd028
spec:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
plan:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.plan.md
applies_to:
  - backend
  - frontend
  - billing
  - catalog
  - subscriptions
  - entitlements
  - resource-capacity
  - metered-usage
  - billing-api
  - provider-integration
  - webhooks
  - reconciliation
  - invoices
  - payments
  - payment-methods
  - migration
  - rls
  - security
  - reliability
  - observability
  - performance
review_on:
  - billing-requirement-change
  - billing-plan-change
  - test-topology-change
  - provider-selection
  - provider-contract-change
  - migration-change
  - rls-change
  - billing-api-change
  - frontend-billing-change
---

# TESTS — Billing & Entitlements

## 1. Purpose

This document is the executable verification authority for the Billing & Entitlements workstream.

It converts:

```text
billing-entitlements.spec.md
billing-entitlements.plan.md
```

into concrete evidence obligations.

The document does not assume that a capability exists merely because:

- a Domain type exists;
- an EF configuration exists;
- an enum contains a provider;
- a frontend story renders a Billing page;
- a unit test proves one method;
- CI is green for an unrelated/documentation-only change.

A capability is considered verified only when the test level matches the risk and boundary being claimed.

## 2. Verification principle

Billing verification follows five rules:

```text
commercial semantics → Domain/Application proof
persistence/uniqueness → real database proof
cross-context boundary → integration/architecture proof
external provider failure → provider-contract/reliability proof
financial/security property → negative-path evidence
```

Do not replace an integration requirement with an in-memory unit test.

Do not replace a database concurrency requirement with two sequential calls.

Do not replace provider uncertainty with a mocked “success/failure only” adapter.

Do not replace RLS evidence with application query filters.

## 3. Candidate and evidence rule

All evidence is candidate-specific.

Every certification run records:

```text
candidate SHA
migration set
provider mode
provider adapter enabled/disabled
test command
selected test count
passed/failed/skipped count
database/provider fixture
generated-contract state
frontend build/test state
```

A stale result from another SHA is not certification evidence.

A command that executes zero tests is failure evidence.

## 4. Current audited evidence boundary

The audited `develop` baseline already contains useful Billing tests.

These MUST be preserved unless superseded by stronger equivalent evidence.

### Domain evidence already present

Current Billing Domain test surfaces include:

```text
AccountRootedBillingTests
BillingCustomerTests
BillingEventTests
BillingEvents/BillingEventImmutabilityTests
Entitlements/EntitlementBoundaryTests
Entitlements/EntitlementTests
Payments/InvoiceTests
Payments/PaymentMethodTests
Plans/FeatureCodeTests
Plans/PlanTests
Plans/PlanVersionTests
Rules/EntitlementRulesTests
Rules/PlanRulesTests
Rules/SubscriptionRulesTests
Rules/UsageRulesTests
Subscriptions/SubscriptionBoundaryTests
Subscriptions/SubscriptionCancelImmediatelyTests
Subscriptions/SubscriptionIdempotencyTests
Subscriptions/SubscriptionLifecycleTests
Subscriptions/SubscriptionTests
Subscriptions/SubscriptionTransitionMatrixTests
Usage/FeatureUsageLedgerTests
Usage/UsageBoundaryTests
Usage/UsageMetricKeyTests
Usage/UsageMetricTests
Usage/UsagePeriodTests
Usage/WorkspaceFeatureUsageTests
```

These are source evidence, not proof of the target design after refactor.

### Application evidence already present

```text
BillingCapabilityFactsProviderTests
BillingCapacityActionsTests
BillingSubscriptionFactsProviderTests
CreateAutomationRuleBillingGateTests
```

### Integration evidence already present

```text
BillingCapacityFlowIntegrationTests
CreateAutomationRulePipelineTests
```

`BillingCapacityFlowIntegrationTests` is the pinned real-PostgreSQL evidence for:

- last-slot race;
- usage/capacity idempotency;
- transaction atomicity;
- lifecycle feed into capacity ledger;
- scope isolation.

This evidence MUST NOT be weakened during Billing refactor.

### Architecture evidence already present

Relevant architecture tests include:

```text
BillingCapacitySemanticsArchitectureTests
CommonEntitlementsAntiRegressionTests
PlanTraceabilityMatrixArchitectureTests
```

The target suite extends these protections to catalog authority, tier/plan leakage, provider boundaries and frontend contract authority.

## 5. Test-level model

### T0 — Static/source/generation checks

Use for:

- source inventory;
- forbidden type/reference scans;
- generated contract drift;
- no hard-coded commercial authority;
- migration snapshot presence;
- documentation traceability.

### T1 — Domain tests

Use for:

- aggregate/value-object invariants;
- closed transition matrices;
- event emission;
- immutable historical meaning;
- local business rules without persistence.

### T2 — Application tests

Use for:

- orchestration;
- Billing-owned decision rules;
- use-case idempotency with controlled dependencies;
- error/result mapping;
- source-to-target derivation logic.

### T3 — Infrastructure tests

Use for:

- provider adapters;
- webhook verification primitives;
- configuration validation;
- redaction;
- persistence mapping behavior not requiring full application graph.

### T4 — API tests

Use for:

- route/contract semantics;
- server-authoritative Account binding;
- authorization declarations;
- stable error contracts;
- payload minimization.

### T5 — Architecture tests

Use for:

- bounded-context ownership;
- forbidden Plan/tier leakage;
- provider SDK boundary;
- private persistence;
- command-oriented API shape;
- no duplicate commercial authority.

### T6 — Integration tests

Use production graph plus real PostgreSQL for:

- RLS;
- transaction atomicity;
- outbox/event flow;
- entitlement derivation;
- migration/upgrade;
- API/Application/persistence integration;
- concurrency.

### T7 — Security tests

Use for:

- webhook authenticity;
- secret/log scanning;
- client payer spoofing;
- privileged repair;
- cross-Account access;
- unsafe provider retry.

### T8 — Concurrency tests

Use real concurrent transactions/tasks for:

- one effective Subscription;
- entitlement reconcile races;
- last-slot capacity;
- payment-method default race;
- provider receipt dedup;
- usage ingestion replay.

### T9 — Migration tests

Use for:

- clean install;
- audited-baseline upgrade;
- expand/contract compatibility;
- authority switch;
- backfill verification;
- retained history.

### T10 — Reliability/performance tests

Use for:

- provider timeout classifications;
- DB failure points;
- reconciliation convergence;
- bounded hot path;
- no external provider call on capability checks.

### T11 — Cross-context contract tests

Use for:

- Accounts → Billing;
- Billing → product contexts;
- product fact → Billing usage;
- Governance → Billing;
- Billing → Analytics/events;
- generated frontend contracts.

### T12 — Frontend unit/component tests

Use for:

- query-key scope;
- denial-state rendering;
- pending/unknown UX;
- generated DTO adaptation;
- no production hard-coded plan authority.

### T13 — E2E/UI evidence

Use for released user flows:

- view Billing;
- upgrade/change;
- cancel/resume;
- pending provider flow;
- invoice view;
- cross-Account switching;
- permission denial.

## 6. Test naming convention

Use:

```text
BIL-TST-<AREA>-<LEVEL>-NNN
```

Examples:

```text
BIL-TST-CAT-DOM-001
BIL-TST-SUB-INT-004
BIL-TST-WHK-SEC-003
BIL-TST-MIG-DB-006
BIL-TST-FE-CMP-004
```

Do not reuse one test ID for a materially different assertion.

## 7. Scenario naming convention

Each critical test name should express:

```text
Given_<precondition>_When_<operation>_Then_<observable result>
```

or equivalent readable xUnit/Vitest naming.

Race tests explicitly state the race:

```text
TwoConcurrent...
RetrySame...
OlderEventAfterNewer...
DbFailureAfterProviderSuccess...
```

## 8. Required implementation-note metadata

Every test PR reports for new/changed tests:

```text
Test ID:
Requirement(s):
Test level:
Production code under test:
Persistence/provider fixture:
Concurrency/failure injection:
Expected result:
Observed result:
```

# Traceability master map

## 9. Requirement family mapping

| Requirements | Primary evidence |
|---|---|
| BILREQ001–008 | catalog Domain/Application/migration/API |
| BILREQ009–023 | Subscription Domain/Application/integration/concurrency |
| BILREQ024–034 | Entitlement Domain/Application/integration/architecture |
| BILREQ035–042 | capacity Domain/Application/PostgreSQL concurrency |
| BILREQ043–051 | metered usage Domain/Application/integration/migration |
| BILREQ052–056 | Billing API/authorization/contracts |
| BILREQ057–063 | provider gateway/operation/reliability/security |
| BILREQ064–069 | webhook security/inbox/idempotency |
| BILREQ070–072 | reconciliation/reliability |
| BILREQ073–081 | financial Domain/integration/migration/security |
| BILREQ082–084 | RLS/cross-Account integration |
| BILREQ085–094 | events/cross-context architecture/contracts |
| BILREQ095–099 | frontend component/E2E |
| BILREQ100–108 | security/idempotency/concurrency |
| BILREQ109–118 | migration |
| BILREQ119–128 | observability/reliability/performance |
| BILREQ129–138 | API/use-case completeness |

# Catalog tests

## 10. BIL-TST-CAT-DOM-001 — stable PlanCode

Requirements:

```text
BILREQ001
```

Prove:

- PlanCode normalization is deterministic;
- display-name changes do not change PlanCode;
- invalid/blank PlanCode is rejected;
- provider identifiers cannot substitute for PlanCode.

## 11. BIL-TST-CAT-DB-001 — PlanCode/revision uniqueness

Requirements:

```text
BILREQ001
BILREQ002
```

Real PostgreSQL MUST reject duplicate:

```text
(plan_code, revision)
```

under concurrent inserts.

Sequential pre-check alone is insufficient.

## 12. BIL-TST-CAT-DOM-002 — immutable commercial revision

Requirements:

```text
BILREQ002
```

After the revision is commercially published/referenced, attempts to mutate:

```text
price-defining terms
capability definition
billing interval meaning
```

must fail or require creation of a new revision.

Description-only/display-safe fields may follow the exact SPEC policy.

## 13. BIL-TST-CAT-DOM-003 — Plan lifecycle does not cancel Subscription

Requirements:

```text
BILREQ003
```

Deprecating/archiving a Plan revision does not mutate existing Subscription state.

## 14. BIL-TST-CAT-DOM-004 — PlanPrice is sole recurring-price authority

Requirements:

```text
BILREQ004
BILREQ005
```

After target migration:

- `Plan.Price` is absent or non-authoritative;
- Application catalog reads prices from PlanPrice;
- old Subscription history resolves its original revision/price.

## 15. BIL-TST-CAT-MIG-001 — Plan.Price backfill is lossless

Requirements:

```text
BILREQ112
```

Given audited-baseline Plan rows:

```text
old Plan.Price
```

after upgrade:

```text
matching PlanPrice exists
currency preserved
interval preserved
amount preserved
no duplicate target row
```

## 16. BIL-TST-CAT-DOM-005 — finite zero is not unlimited

Requirements:

```text
BILREQ006
```

Prove:

```text
Included=true
IsUnlimited=false
Limit=0
```

is zero capacity.

It must never grant unbounded use.

## 17. BIL-TST-CAT-DOM-006 — explicit unlimited

Requirements:

```text
BILREQ006
```

Prove explicit unlimited representation grants unbounded capability without negative sentinel values.

## 18. BIL-TST-CAT-DOM-007 — excluded capability

Requirements:

```text
BILREQ006
```

A Plan capability marked excluded does not derive an active Entitlement.

## 19. BIL-TST-CAT-DOM-008 — FeatureCode normalization

Requirements:

```text
BILREQ007
```

Prove FeatureCode equality/normalization remains stable across catalog, Entitlement and usage paths.

## 20. BIL-TST-CAT-ARCH-001 — FeatureCode owner remains Billing

Requirements:

```text
BILREQ007
BILREQ090
```

Architecture test rejects new commercial feature-code authorities in product contexts.

## 21. BIL-TST-CAT-APP-001 — catalog query returns provider-neutral model

Requirements:

```text
BILREQ008
BILREQ129
```

Catalog read returns:

```text
PlanCode
revision
display data
price
capabilities
availability
```

and does not expose provider product/price IDs to normal users.

## 22. BIL-TST-CAT-API-001 — catalog endpoint contract

Requirements:

```text
BILREQ008
BILREQ131
BILREQ132
```

Verify HTTP/OpenAPI schema and status semantics.

## 23. BIL-TST-CAT-APP-002 — deprecated plan excluded from new purchase options

Requirements:

```text
BILREQ003
BILREQ008
```

Existing subscribers remain resolvable; new purchase catalog excludes deprecated revision.

## 24. BIL-TST-CAT-INT-001 — existing subscription keeps historical Plan revision

Requirements:

```text
BILREQ002
BILREQ005
```

Create Subscription on revision 1, publish revision 2, then prove revision 1 Subscription still resolves its original terms.

## 24A. BIL-TST-CAT-DB-002 — one current sellable revision per PlanCode

Requirements:

```text
BILREQ003
```

Real PostgreSQL/Application publication flow proves two overlapping sellable/current revisions for the same `PlanCode` cannot both become authoritative.

Publishing revision N+1 atomically retires/ends the previous current revision or enforces non-overlapping effective ranges.

## 24B. BIL-TST-CAT-DOM-009 — Plan billing interval is not a second authority

Requirements:

```text
BILREQ004
BILREQ005
```

After migration, Subscription/catalog commercial interval comes from `PlanPrice.BillingInterval`; `Plan.Period` cannot alter runtime billing meaning.

## 24C. BIL-TST-CAT-DB-003 — exactly one default offer on current Free revision

Requirements:

```text
BILREQ009
```

Publication/bootstrap cannot observe zero or multiple active default PlanPrices for current Free revision.

Bootstrap never selects by row order, amount order or arbitrary currency.

## 24D. BIL-TST-CAT-DB-004 — PlanCapability semantic uniqueness

Requirements:

```text
BILREQ006
```

Real PostgreSQL rejects duplicate:

```text
(PlanId, FeatureCode, LimitAggregationScope)
```

COUNT capabilities reject non-integer limit semantics.

## 24E. BIL-TST-CAT-APP-003 — transition policy is authoritative

Requirements:

```text
BILREQ016
BILREQ022
BILREQ023
```

Prove:

```text
cross PlanCode → explicit PlanTransitionPolicy required
same PlanCode/different PlanPrice → OfferChange
client upgrade/downgrade flag → rejected/ignored
price comparison → not classification authority
missing transition policy → fail closed
```

# Subscription and Account Billing bootstrap tests

## 25. BIL-TST-SUB-INT-001 — AccountCreated bootstraps Free Subscription

Requirements:

```text
BILREQ009
BILREQ010
BILREQ011
BILREQ089
BILREQ133
```

Production graph:

```text
AccountCreated
→ Billing consumer/use case
→ current Free Plan revision
→ one Account Subscription
→ Entitlement reconciliation
```

No provider call occurs.

## 26. BIL-TST-SUB-INT-002 — bootstrap replay is idempotent

Requirements:

```text
BILREQ011
BILREQ105
```

Deliver the same AccountCreated fact repeatedly.

Expected:

```text
one effective Subscription
one derived Subscription entitlement set
no duplicate provider operation
```

## 27. BIL-TST-SUB-CONC-001 — concurrent Account bootstrap

Requirements:

```text
BILREQ011
BILREQ104
BILREQ105
```

Two concurrent bootstrap executions for one Account must converge to one effective commercial Subscription.

Use real PostgreSQL uniqueness/concurrency.

## 28. BIL-TST-SUB-DOM-001 — Subscription is Account-rooted

Requirements:

```text
BILREQ012
```

Target Subscription requires `AccountId` + exact `PlanPriceId`, does not require WorkspaceId, and derives its immutable Plan revision through `PlanPrice.PlanId`.

## 28A. BIL-TST-SUB-DOM-001A — Subscription pins exact PlanPrice

Requirements:

```text
BILREQ005
BILREQ012
BILREQ018
```

Given one Plan revision with monthly and annual prices, creating a Subscription with the monthly `PlanPriceId` must preserve that exact price/interval even if another price/revision later becomes current.

## 28B. BIL-TST-SUB-ARCH-001A — SubscriptionItem and Plan.Period are not target authorities

Requirements:

```text
BILREQ004
BILREQ012
```

After migration:

```text
SubscriptionItem
Plan.Period
direct Subscription.PlanId
```

cannot independently determine the subscribed commercial offer.

## 29. BIL-TST-SUB-ARCH-001 — Workspace cannot become Subscription owner

Requirements:

```text
BILREQ012
BILREQ096
```

Architecture/source gate rejects new Workspace-owned Subscription or payer mapping.

## 30. BIL-TST-SUB-DOM-002 — PendingActivation creation

Requirements:

```text
BILREQ013
BILREQ014
```

Provider-backed creation path does not silently create `Active`.

## 31. BIL-TST-SUB-DOM-003 — internal/free creation can be Active explicitly

Requirements:

```text
BILREQ010
BILREQ014
```

Internal Free bootstrap uses an explicit internal activation path, not generic defaulting.

## 32. BIL-TST-SUB-DOM-004 — transition matrix allows valid transitions

Requirements:

```text
BILREQ013
BILREQ015
```

Cover all target valid transitions from SPEC/PLAN.

## 33. BIL-TST-SUB-DOM-005 — transition matrix rejects invalid transitions

Requirements:

```text
BILREQ013
BILREQ015
```

Examples:

```text
Canceled → PastDue
Expired → Renew
PendingActivation → arbitrary Active setter
```

must fail.

## 34. BIL-TST-SUB-APP-001 — Plan change is orchestration

Requirements:

```text
BILREQ016
BILREQ022
BILREQ023
```

Application handler loads target Plan, classifies timing, coordinates provider when needed and invokes Entitlement reconciliation.

It must not be equivalent to direct PlanId assignment.

## 35. BIL-TST-SUB-APP-002 — upgrade effective timing

Requirements:

```text
BILREQ022
```

The certified policy is explicit:

```text
provider-backed upgrade
→ current PlanPrice remains authoritative while operation Pending/Unknown
→ confirmed/reconciled Succeeded
→ new PlanPriceId + Entitlements become effective immediately

internal upgrade without provider effect
→ effective immediately after local commercial validation
```

No test may use provider redirect/request submission as success authority.

## 36. BIL-TST-SUB-APP-003 — downgrade non-destructive

Requirements:

```text
BILREQ023
```

Downgrade is scheduled:

```text
before CurrentPeriodEnd
→ current PlanPriceId and Entitlements remain effective

at CurrentPeriodEnd
→ PendingPlanPriceId becomes current
→ Entitlements reconcile
```

When the new lower Plan limit is below current resources:

- existing product data is untouched;
- new creation is denied by capacity;
- release/delete remains allowed.

## 37. BIL-TST-SUB-DOM-006 — renewal

Requirements:

```text
BILREQ018
```

Prove period moves forward only through valid renewal semantics.

## 38. BIL-TST-SUB-DOM-007 — cancel scheduling is not cancellation

Requirements:

```text
BILREQ019
```

After schedule-cancel:

```text
Status remains effective
CancelAtPeriodEnd=true
Entitlement remains effective until policy boundary
```

## 39. BIL-TST-SUB-DOM-008 — resume scheduled cancellation

Requirements:

```text
BILREQ019
BILREQ133
```

Resume clears pending cancellation without creating a new Subscription.

## 40. BIL-TST-SUB-DOM-009 — effective cancellation

Requirements:

```text
BILREQ019
```

At effective time, Subscription becomes Canceled and triggers Entitlement reconciliation.

## 41. BIL-TST-SUB-DOM-010 — immediate cancellation is explicit

Requirements:

```text
BILREQ020
```

Immediate cancellation requires an explicit command/system path and emits auditable fact.

## 42. BIL-TST-SUB-DOM-011 — past-due transition

Requirements:

```text
BILREQ021
```

Only allowed source states enter PastDue.

## 43. BIL-TST-SUB-DOM-012 — payment recovery

Requirements:

```text
BILREQ021
```

PastDue → Active is explicit and does not create a second Subscription.

## 43A. BIL-TST-SUB-APP-004 — PastDue fails paid capability closed

Requirements:

```text
BILREQ021
BILREQ032
```

In the first certified closure:

```text
Active paid Subscription → capability available
MarkPastDue
→ paid capability decision = unavailable / SubscriptionInactive equivalent
Recover from authoritative payment evidence
→ capability can become available again after Entitlement reconciliation
```

No implicit grace window is allowed.

## 44. BIL-TST-SUB-ARCH-002 — SubscriptionTier cannot leak to consumers

Requirements:

```text
BILREQ017
BILREQ113
```

Architecture test rejects production `SubscriptionTier` references outside allowed Billing migration paths.

## 45. BIL-TST-SUB-MIG-001 — SubscriptionTier migration preserves behavior

Requirements:

```text
BILREQ113
```

Before removal, compare:

```text
legacy tier-requirement result
vs
target Billing capability/effective-subscription result
```

for all known consumers.

Differences are explicit migration blockers.

## 46. BIL-TST-SUB-MIG-002 — Account.PlanCode backfill

Requirements:

```text
BILREQ109
```

Known PlanCode maps to exact Plan revision and creates/aligns Subscription.

Unknown/conflicting PlanCode is reported, not silently assigned.

## 47. BIL-TST-SUB-ARCH-003 — Account.PlanCode cannot grant product access

Requirements:

```text
BILREQ109
BILREQ090
```

After authority switch, architecture/source tests reject product authorization/capability logic based on `Account.PlanCode`.

## 48. BIL-TST-SUB-INT-003 — one effective Subscription policy

Requirements:

```text
BILREQ104
```

Database/Application behavior prevents ambiguous simultaneous effective Subscriptions for the Account policy.

## 48A. BIL-TST-SUB-DOM-013 — Trialing is not a certified first-closure state

Requirements:

```text
BILREQ013
BILREQ014
```

Target persistence/creation path does not expose Trialing as completed behavior.

Legacy/provider trial data cannot silently activate paid Entitlements.

## 48B. BIL-TST-SUB-DOM-014 — terminal activation failure is not stuck Pending

Requirements:

```text
BILREQ013
BILREQ014
```

Definitive provider activation failure:

```text
PendingActivation → Expired
BillingOperation → Failed
no paid Entitlements
```

Replay is idempotent.

## 48C. BIL-TST-SUB-APP-005 — normal paid cancellation falls back to Free

Requirements:

```text
BILREQ019
BILREQ023
```

```text
schedule cancellation
→ paid offer remains until CurrentPeriodEnd
→ default Free PlanPrice recorded as pending target

effective boundary
→ Status remains Active
→ PlanPriceId becomes default Free
→ Free Entitlements derived
```

No ordinary user cancellation becomes terminal Canceled while valid Free fallback exists.

## 48D. BIL-TST-SUB-REL-001 — invalid Free fallback blocks effective cancellation

Requirements:

```text
BILREQ009
BILREQ019
```

Missing/ambiguous/deprecated scheduled Free target does not cause arbitrary price selection.

Operation becomes observable/retriable/repairable.

## 48E. BIL-TST-SUB-INT-005 — lifecycle worker applies due internal transitions once

Requirements:

```text
BILREQ018
BILREQ019
BILREQ023
```

Concurrent workers processing one due boundary produce one semantic transition.

Provider-backed due boundaries trigger reconciliation and do not blindly mutate local state from clock time alone.

# Entitlement tests

## 49. BIL-TST-ENT-DOM-001 — Account-scoped Entitlement

Requirements:

```text
BILREQ024
```

Valid Account grant has no target Workspace.

## 50. BIL-TST-ENT-DOM-002 — Workspace-scoped Entitlement

Requirements:

```text
BILREQ024
```

Workspace grant requires target Workspace and remains within the same Account boundary.

## 51. BIL-TST-ENT-ARCH-001 — entitlement is not authorization

Requirements:

```text
BILREQ025
BILREQ055
```

Architecture/Application tests prove entitlement result cannot grant Governance permission.

## 52. BIL-TST-ENT-DOM-003 — source lineage required

Requirements:

```text
BILREQ026
```

New non-legacy grants require:

```text
SourceType
SourceId
```

and valid effective interval.

## 53. BIL-TST-ENT-APP-001 — derive Subscription baseline

Requirements:

```text
BILREQ027
BILREQ134
```

Given Subscription + PlanCapability, reconcile creates the exact Subscription-sourced Entitlements.

## 54. BIL-TST-ENT-APP-002 — derivation replay is idempotent

Requirements:

```text
BILREQ027
BILREQ106
```

Repeated reconcile with unchanged source produces no duplicate active grants and no duplicate semantic event.

## 55. BIL-TST-ENT-APP-003 — plan change supersedes old Subscription grant

Requirements:

```text
BILREQ028
```

History remains queryable; old row is ended/superseded rather than destructively overwritten.

## 56. BIL-TST-ENT-APP-004 — removed Plan capability ends derived grant

Requirements:

```text
BILREQ027
BILREQ028
```

Non-Subscription sources remain untouched.

## 57. BIL-TST-ENT-ARCH-002 — Promo/AddOn enum values are not certified features

Requirements:

```text
BILREQ029
BILREQ134
```

Prove production API/Application use-case inventory does not expose Promo/AddOn grant/combination behavior in the first certified closure merely because enum values exist.

## 58. BIL-TST-ENT-APP-006 — Manual override precedence

Requirements:

```text
BILREQ029
```

For the same Account/Workspace/Feature, one active authorized Manual override wins over the Subscription-derived baseline only within its target scope.

## 59. BIL-TST-ENT-APP-007 — Manual override audit contract

Requirements:

```text
BILREQ030
BILREQ134
```

Grant requires:

```text
privileged authorization
actor
reason
stable source/operation identity
effective time
optional expiry
before/after capability evidence
```

Grant replay is idempotent and underlying Subscription evidence remains intact.

## 59A. BIL-TST-ENT-APP-007A — manual override revoke/expire

Requirements:

```text
BILREQ030
BILREQ134
```

Revoke/expire is authorized, idempotent, auditable and restores the effective Subscription-derived result when one exists.

## 59B. BIL-TST-ENT-APP-007B — conflicting equal-precedence grants fail closed

Requirements:

```text
BILREQ029
BILREQ032
```

If corrupted/migration data yields two equally applicable active grants with no valid source-revision/effective-time winner:

```text
no `Id DESC` / CreatedAt fallback
capability denied with commercial-state conflict/dependency-unavailable reason
repair telemetry emitted
```

## 60. BIL-TST-ENT-DOM-004 — expired grant inactive at boundary

Requirements:

```text
BILREQ031
```

Test before, exactly at, and after `ExpiresAt`.

## 61. BIL-TST-ENT-APP-008 — expired Workspace grant does not shadow active Account grant

Requirements:

```text
BILREQ029
BILREQ031
```

Preserve the good current resolver behavior.

## 62. BIL-TST-ENT-APP-009 — fail closed on missing entitlement

Requirements:

```text
BILREQ032
```

Missing grant returns unavailable, never unlimited.

## 63. BIL-TST-ENT-REL-001 — Billing dependency failure is explicit

Requirements:

```text
BILREQ032
BILREQ128
```

Dependency/storage failure does not silently return capability available.

## 64. BIL-TST-ENT-APP-010 — stable reason taxonomy

Requirements:

```text
BILREQ033
BILREQ056
```

Verify normalized reasons:

```text
Available
NotGranted
Disabled
Expired
SubscriptionInactive
LimitExceeded
DependencyUnavailable
```

as applicable.

## 65. BIL-TST-ENT-APP-011 — explicit unlimited capability fact

Requirements:

```text
BILREQ033
```

Returns:

```text
IsAvailable=true
IsUnlimited=true
Limit=null
```

without conflating zero.

## 66. BIL-TST-ENT-APP-012 — zero-capacity fact

Requirements:

```text
BILREQ033
```

Returns unavailable/remaining zero with explicit finite zero.

## 67. BIL-TST-ENT-CONC-001 — concurrent reconciliation converges

Requirements:

```text
BILREQ106
```

Two concurrent reconciliation executions produce one effective source result without lost/split history.

Use real PostgreSQL.

## 68. BIL-TST-ENT-MIG-001 — legacy lineage backfill

Requirements:

```text
BILREQ115
```

Existing Entitlements are classified with explicit legacy lineage when source cannot be proven.

No fabricated Subscription/Manual/Promo/AddOn provenance; unsupported Promo/AddOn remains explicitly legacy/reserved rather than certified.

## 69. BIL-TST-ENT-MIG-002 — legacy vs derived capability comparison

Requirements:

```text
BILREQ115
```

Before authority switch, produce a report comparing effective legacy and target derived capabilities.

Any difference requires classification.

## 70. BIL-TST-ENT-CACHE-001 — cache is not correctness authority

Requirements:

```text
BILREQ034
```

If cache exists, stale/empty cache cannot create a paid grant absent authoritative Billing state.

## 71. BIL-TST-ENT-CACHE-002 — revocation invalidates/bounds cache

Requirements:

```text
BILREQ034
```

A revoked/expired entitlement ceases granting within the explicit accepted cache window.

# Hard resource-capacity tests

## 72. BIL-TST-RCAP-ARCH-001 — capacity and metered usage are separate

Requirements:

```text
BILREQ035
BILREQ050
```

Architecture/source test rejects metered-period logic added to `WorkspaceFeatureUsage`/capacity ledger.

## 73. BIL-TST-RCAP-DOM-001 — WorkspaceFeatureUsage finite consume

Requirements:

```text
BILREQ036
```

Valid consume increments current usage/version and emits owned fact.

## 74. BIL-TST-RCAP-DOM-002 — hard-limit rejection

Requirements:

```text
BILREQ036
```

Consume exceeding hard limit fails and records no committed increment.

## 75. BIL-TST-RCAP-DOM-003 — unlimited capacity

Requirements:

```text
BILREQ036
```

No numeric hard ceiling means explicit unlimited only after capability reconciliation says so.

## 76. BIL-TST-RCAP-DOM-004 — release cannot go below zero

Requirements:

```text
BILREQ041
```

## 77. BIL-TST-RCAP-APP-001 — consume writes capacity + ledger

Requirements:

```text
BILREQ037
BILREQ039
BILREQ135
```

Application action mutates aggregate and appends ledger evidence.

## 78. BIL-TST-RCAP-INT-001 — resource create and capacity consume are atomic

Requirements:

```text
BILREQ037
```

Inject failure before SaveChanges/commit and prove neither resource nor capacity effect survives.

## 79. BIL-TST-RCAP-INT-002 — capacity consume failure rolls back resource

Requirements:

```text
BILREQ037
```

If hard capacity rejects, product resource is not persisted.

## 80. BIL-TST-RCAP-APP-002 — same LogicalOperation replay

Requirements:

```text
BILREQ038
BILREQ105
```

Same operation/payload returns replay result without duplicate ledger delta.

## 81. BIL-TST-RCAP-APP-003 — conflicting LogicalOperation payload

Requirements:

```text
BILREQ038
```

Same operation ID with different scope/amount/resource fails deterministically.

## 82. BIL-TST-RCAP-DB-001 — logical operation unique index

Requirements:

```text
BILREQ038
```

Real database rejects duplicate logical operation identity under race.

## 83. BIL-TST-RCAP-DOM-005 — limit shrink preserves committed usage

Requirements:

```text
BILREQ040
BILREQ023
```

If current usage 10 and new limit 5:

```text
CurrentUsage remains 10
new consume denied
release allowed
```

## 84. BIL-TST-RCAP-INT-003 — deleted Automation Rule releases capacity

Requirements:

```text
BILREQ041
```

Use production lifecycle path, not direct ledger insertion.

## 85. BIL-TST-RCAP-INT-004 — disabled Automation Rule keeps capacity

Requirements:

```text
BILREQ041
```

Preserve current reference behavior.

## 86. BIL-TST-RCAP-DOM-006 — capacity does not reset by month

Requirements:

```text
BILREQ042
```

No billing-period transition resets live resource stock.

## 87. BIL-TST-RCAP-CONC-001 — last-slot race

Requirements:

```text
BILREQ107
```

Mandatory retained scenario:

```text
limit=1
two concurrent CreateAutomationRule commands
exactly one succeeds
one rule committed
usage=1
one +1 ledger row
```

## 88. BIL-TST-RCAP-CONC-002 — first-use row race

Requirements:

```text
BILREQ036
BILREQ107
```

No existing `WorkspaceFeatureUsage` row.

Two concurrent first consumes must converge safely.

## 89. BIL-TST-RCAP-INT-005 — workspace isolation

Requirements:

```text
BILREQ082
```

Usage in Workspace A does not reduce Workspace B capacity unless entitlement scope explicitly defines shared Account capacity.

## 89A. BIL-TST-RCAP-APP-004 — ReconcileResourceCapacity repairs explicit drift

Requirements:

```text
BILREQ135
```

A privileged/system repair compares approved product-resource facts with retained capacity evidence and applies an idempotent explicit reconciliation/compensating effect.

It does not silently rewrite ledger history or accept arbitrary client-provided counts.

## 89B. BIL-TST-RCAP-SEC-001 — capacity repair is privileged and scoped

Requirements:

```text
BILREQ103
BILREQ135
```

Wrong Account/Workspace actor cannot repair another scope; every repair is correlated/audited.

## 89C. BIL-TST-RCAP-ARCH-002 — hard capacity has no reset authority

Requirements:

```text
BILREQ042
```

Target production hard-capacity model has no authoritative:

```text
ResetPeriod
LastResetAt
Reset()
```

for persistent resource counts.

## 89D. BIL-TST-RCAP-DOM-007 — hard-capacity quantity is integer end-to-end

Requirements:

```text
BILREQ006
BILREQ036
```

No decimal→int truncation path exists.

Fractional resource-count consume is rejected at contract boundary.

## 89E. BIL-TST-RCAP-APP-005 — aggregation scope is explicit

Requirements:

```text
BILREQ006
BILREQ035
```

`AUTOMATION_RULE` is Workspace-aggregated capacity.

Unsupported hard-capacity aggregation scope fails closed rather than using wrong ledger.

# Metered usage tests

## 90. BIL-TST-USG-DOM-001 — stable MetricCode

Requirements:

```text
BILREQ044
```

Metric identity is normalized and Billing-owned.

## 91. BIL-TST-USG-ARCH-001 — source context does not own billing meter IDs

Requirements:

```text
BILREQ043
BILREQ044
BILREQ091
```

Architecture test rejects provider/meter commercial IDs in Automation/WorkManagement/Documents business code.

## 92. BIL-TST-USG-APP-001 — successful source fact produces usage

Requirements:

```text
BILREQ043
BILREQ091
```

Representative example:

```text
AutomationExecutionSucceeded
→ one Billing MeteredUsageEvent
```

## 93. BIL-TST-USG-APP-002 — failed source operation is not billed

Requirements:

```text
BILREQ043
```

Unless a future explicit product rule says otherwise.

## 94. BIL-TST-USG-INT-001 — duplicate source fact is idempotent

Requirements:

```text
BILREQ045
BILREQ105
```

Repeated delivery produces one semantic usage entry.

## 95. BIL-TST-USG-DB-001 — source-event uniqueness under race

Requirements:

```text
BILREQ045
```

Concurrent duplicate ingestion is protected by database uniqueness.

## 96. BIL-TST-USG-DOM-002 — period boundaries

Requirements:

```text
BILREQ046
```

Test event at:

```text
period start
just before period end
exact period end
next period
```

according to canonical interval semantics.

## 97. BIL-TST-USG-INT-002 — period transition preserves history

Requirements:

```text
BILREQ047
```

Opening new period does not delete prior events/aggregate evidence.

## 98. BIL-TST-USG-INT-003 — current aggregate rebuild

Requirements:

```text
BILREQ047
```

Delete/rebuild projection from immutable MeteredUsageEvent and compare exact total.

## 99. BIL-TST-USG-INT-004 — late event assigned to correct period

Requirements:

```text
BILREQ048
```

Arrival time differs from occurrence time.

## 100. BIL-TST-USG-INT-005 — out-of-order events converge

Requirements:

```text
BILREQ048
```

Final aggregate is independent of processing order for commutative usage semantics.

## 101. BIL-TST-USG-DOM-003 — correction is compensating event

Requirements:

```text
BILREQ049
```

Original usage remains immutable; correction references it.

## 102. BIL-TST-USG-INT-006 — duplicate correction is idempotent

Requirements:

```text
BILREQ045
BILREQ049
```

## 103. BIL-TST-USG-ARCH-002 — no all-time capacity sum for periodic usage

Requirements:

```text
BILREQ050
BILREQ117
BILSTOP009
```

Source/architecture gate rejects monthly-meter decisions based on all-time `FeatureUsageLedger` sum.

## 104. BIL-TST-USG-APP-003 — enforcement mode classification

Requirements:

```text
BILREQ051
```

Each released meter declares one of:

```text
observe-only
soft-limit
hard-limit
overage-allowed
```

## 105. BIL-TST-USG-CONC-001 — hard metered quota requires reservation

Requirements:

```text
BILREQ051
```

If any hard metered quota is released, concurrent final-unit requests prove reservation/allocation correctness.

If none is released, certification marks this `NOT_APPLICABLE`, not PASS.

## 106. BIL-TST-USG-MIG-001 — UsageMetric semantic migration

Requirements:

```text
BILREQ117
```

Compare old current values/history to rebuilt target period aggregates before old authority is retired.

## 106A. BIL-TST-USG-DB-002 — period aggregate identity is unique with nullable Workspace

Requirements:

```text
BILREQ046
BILREQ047
```

Real PostgreSQL proves one aggregate for:

```text
(AccountId, WorkspaceId?, MetricCode, PeriodStart, PeriodEnd)
```

including Account-scoped `WorkspaceId IS NULL`.

Use `UNIQUE NULLS NOT DISTINCT` or equivalent indexes.

# Billing API and administration tests

## 107. BIL-TST-API-APP-001 — Account-scoped Billing summary

Requirements:

```text
BILREQ052
BILREQ129
BILREQ138
```

Returns current Subscription/catalog/effective usage summary for trusted current Account.

## 108. BIL-TST-API-SEC-001 — client Account ID cannot select payer

Requirements:

```text
BILREQ130
BILSTOP010
```

Route/body/query Account ID injection cannot access another payer.

## 109. BIL-TST-API-INT-001 — Account A cannot read Account B Billing

Requirements:

```text
BILREQ082
BILREQ084
```

Use real API + PostgreSQL/RLS where feasible.

## 110. BIL-TST-API-INT-002 — Account A cannot mutate Account B Billing

Requirements:

```text
BILREQ082
BILREQ084
BILREQ130
```

## 111. BIL-TST-API-CONTRACT-001 — catalog endpoint

Requirements:

```text
BILREQ129
BILREQ132
```

## 112. BIL-TST-API-CONTRACT-002 — subscription read endpoint

Requirements:

```text
BILREQ129
BILREQ133
```

## 113. BIL-TST-API-CONTRACT-003 — entitlement/usage read endpoints

Requirements:

```text
BILREQ129
BILREQ134
BILREQ135
BILREQ136
```

## 114. BIL-TST-API-CONTRACT-004 — invoice/payment-method read endpoints

Requirements:

```text
BILREQ129
BILREQ138
```

## 115. BIL-TST-API-APP-002 — change-plan command maps to use case

Requirements:

```text
BILREQ053
BILREQ133
```

No generic entity update endpoint.

## 116. BIL-TST-API-APP-003 — cancel command

Requirements:

```text
BILREQ053
BILREQ133
```

## 117. BIL-TST-API-APP-004 — resume command

Requirements:

```text
BILREQ053
BILREQ133
```

## 118. BIL-TST-AUTHZ-APP-001 — Billing read permission

Requirements:

```text
BILREQ054
BILREQ092
```

Request declares canonical authorization requirement.

## 119. BIL-TST-AUTHZ-APP-002 — manage Subscription permission

Requirements:

```text
BILREQ054
BILREQ092
```

## 120. BIL-TST-AUTHZ-APP-003 — manage PaymentMethod permission

Requirements:

```text
BILREQ054
BILREQ092
```

## 121. BIL-TST-AUTHZ-SEC-001 — entitlement does not imply Billing admin permission

Requirements:

```text
BILREQ055
```

Paid capability + no Governance permission still denies admin operation.

## 122. BIL-TST-AUTHZ-SEC-002 — admin permission does not imply product entitlement

Requirements:

```text
BILREQ055
```

Billing admin may manage plan but cannot use product capability absent effective entitlement.

## 123. BIL-TST-API-CONTRACT-005 — error taxonomy

Requirements:

```text
BILREQ056
```

Prove machine-readable distinction for:

```text
BILLING_ENTITLEMENT_REQUIRED
BILLING_CAPACITY_EXCEEDED
BILLING_SUBSCRIPTION_INACTIVE
BILLING_OPERATION_PENDING
BILLING_PROVIDER_UNAVAILABLE
BILLING_OPERATION_CONFLICT
```

## 124. BIL-TST-API-CONTRACT-006 — authorization/error distinction

Requirements:

```text
BILREQ055
BILREQ056
```

Permission denial is not mapped to “upgrade required”.

## 125. BIL-TST-API-OAS-001 — OpenAPI drift

Requirements:

```text
BILREQ131
```

Generated frontend contracts match candidate API.

## 126. BIL-TST-API-ARCH-001 — no persistence model in public API

Requirements:

```text
BILREQ129
```

API response/request signatures cannot expose EF entities or provider SDK DTOs.

## 127. BIL-TST-API-ARCH-002 — no generic Subscription/Invoice state patch

Requirements:

```text
BILREQ053
```

Architecture/endpoint inventory rejects generic arbitrary-state CRUD.

## 127A. BIL-TST-API-CONTRACT-007 — plan-change command selects exact PlanPrice

Requirements:

```text
BILREQ016
BILREQ053
BILREQ133
```

Request contains `TargetPlanPriceId` + idempotency identity.

It does not accept authoritative:

```text
TargetPlanId only
isUpgrade
isDowngrade
price amount
SubscriptionTier
```

## 127B. BIL-TST-API-CONTRACT-008 — provider-backed command returns accepted BillingOperation

Requirements:

```text
BILREQ059
BILREQ129
BILREQ137
```

For provider-backed change/checkout/portal:

```text
request response = 202 Accepted
BillingOperationId present
state = Pending/accepted
no provider-complete success claim
```

Provider adapter executes after request transaction commit.

## 127C. BIL-TST-API-SEC-002 — operation result is Account-bound and sensitive result minimized

Requirements:

```text
BILREQ101
BILREQ129
```

Account A cannot read Account B BillingOperation.

Hosted checkout/portal URL, when returned after success:

```text
not in ordinary logs
not in integration events
not returned before operation success
not exposed cross-Account
```

# Provider gateway and BillingOperation tests

## 128. BIL-TST-PRV-ARCH-001 — provider SDK boundary

Requirements:

```text
BILREQ057
BILREQ063
```

Provider SDK types may exist only inside approved Infrastructure adapter paths.

## 129. BIL-TST-PRV-APP-001 — provider-neutral gateway contract

Requirements:

```text
BILREQ057
BILREQ137
```

Application consumes neutral requests/results only.

## 130. BIL-TST-PRV-DB-001 — explicit BillingCustomer provider mapping

Requirements:

```text
BILREQ058
```

Database proves:

```text
UNIQUE(AccountId, Provider)
UNIQUE(Provider, ProviderCustomerId)
```

or the final equivalent constraints.

## 131. BIL-TST-PRV-CONC-001 — concurrent EnsureCustomer

Requirements:

```text
BILREQ058
BILREQ105
```

Two simultaneous customer-create requests converge to one provider mapping/logical operation.

## 132. BIL-TST-OP-DOM-001 — durable BillingOperation identity

Requirements:

```text
BILREQ059
```

Operation ID/idempotency key remains stable across request retry.

## 133. BIL-TST-OP-APP-001 — replay same commercial command

Requirements:

```text
BILREQ059
BILREQ105
```

No second logical provider operation.

## 134. BIL-TST-OP-APP-002 — conflicting idempotency payload

Requirements:

```text
BILREQ059
BILREQ105
```

Same logical operation identity with different commercial payload fails deterministically.

## 134A. BIL-TST-OP-INT-001 — durable intent commits before provider mutation

Requirements:

```text
BILREQ059
BILREQ125
BILREQ126
```

Failure-injection proof:

```text
TX #1 commits BillingOperation(Pending)
provider call begins only after commit
no DB transaction remains open across provider network call
```

If provider succeeds and subsequent local persistence fails, the pre-existing BillingOperation remains queryable/reconcilable.

## 134B. BIL-TST-OP-REL-001A — provider success + TX #2 failure preserves reconciliation identity

Requirements:

```text
BILREQ059
BILREQ061
BILREQ126
```

Simulate provider success followed by failure persisting the authoritative Subscription/financial result.

Expected:

```text
durable logical operation still exists
outcome becomes/remains Unknown/Pending-reconciliation
same idempotency key retained
blind duplicate provider call forbidden
```

## 134C. BIL-TST-OP-ARCH-001 — provider effect never runs inside request DataSession transaction

Requirements:

```text
BILREQ059
BILREQ125
```

Billing API/Application write command persists only:

```text
BillingOperation(Pending)
provider-effect outbox intent
```

inside request transaction.

Provider adapter is invoked by consumer after commit.

A test provider that fails when `DbContext.Database.CurrentTransaction != null` during external call MUST pass.

## 134D. BIL-TST-OP-INT-002 — prepare/effect/settle survives crash points

Requirements:

```text
BILREQ059
BILREQ126
```

Cover:

```text
crash after request TX before claim
crash after prepare TX before provider call
provider success then crash before settle
settle TX failure after provider success
```

Durable operation/claim evidence remains recoverable and provider is not blindly re-fired under a new identity.

## 134E. BIL-TST-OP-APP-003A — request fingerprint protects logical operation

Requirements:

```text
BILREQ059
BILREQ105
```

Same logical/idempotency identity + different `TargetPlanPriceId`/commercial payload fails deterministically.

## 134F. BIL-TST-PRV-DB-002 — provider object mappings are provider-relative unique

Requirements:

```text
BILREQ058
```

Real DB proves applicable uniqueness for customer/subscription/price/invoice/payment/method mappings.

## 135. BIL-TST-OP-DOM-002 — outcome taxonomy

Requirements:

```text
BILREQ060
```

Only supported normalized outcomes are persisted.

## 136. BIL-TST-OP-REL-001 — timeout before submission

Requirements:

```text
BILREQ060
BILREQ124
```

If provider contract proves request was not submitted, operation can be classified safely according to adapter semantics.

## 137. BIL-TST-OP-REL-002 — timeout after possible submission becomes Unknown

Requirements:

```text
BILREQ060
BILREQ061
BILREQ124
```

Must not be automatically marked Failed.

## 138. BIL-TST-OP-SEC-001 — Unknown operation cannot blind retry

Requirements:

```text
BILREQ061
BILSTOP003
```

Retry path triggers reconciliation or provider idempotent lookup, not a new uncorrelated financial action.

## 139. BIL-TST-OP-APP-003 — checkout return is not authority

Requirements:

```text
BILREQ062
```

Browser success-return does not directly activate Subscription.

## 140. BIL-TST-OP-APP-004 — portal return is not authority

Requirements:

```text
BILREQ062
```

## 141. BIL-TST-PRV-SEC-001 — provider secret absent from Domain/Application contracts

Requirements:

```text
BILREQ063
BILREQ101
```

Static/reflection/source scan.

## 142. BIL-TST-PRV-CFG-001 — provider adapter disabled without selected/configured provider

Requirements:

```text
BILREQ057
BILSTOP008
```

Enum presence alone cannot make checkout/payment feature available.

# Webhook tests

## 143. BIL-TST-WHK-SEC-001 — missing signature rejected

Requirements:

```text
BILREQ064
```

No receipt reaches semantic processing as verified.

## 144. BIL-TST-WHK-SEC-002 — invalid signature rejected

Requirements:

```text
BILREQ064
```

## 145. BIL-TST-WHK-SEC-003 — tampered raw body rejected

Requirements:

```text
BILREQ064
```

Verification must use the exact raw-body contract required by provider.

## 146. BIL-TST-WHK-SEC-004 — replay-window/timestamp rule

Requirements:

```text
BILREQ064
```

When provider supports timestamp validation, stale/replayed signed requests are rejected according to adapter policy.

## 147. BIL-TST-WHK-SEC-005 — browser identity cannot authorize webhook

Requirements:

```text
BILREQ102
```

Valid user cookie/token with invalid provider signature does not grant webhook trust.

## 148. BIL-TST-WHK-INF-001 — raw provider receipt stays Infrastructure-owned

Requirements:

```text
BILREQ065
BILREQ086
```

Domain/public event payload does not contain raw webhook schema.

## 149. BIL-TST-WHK-DB-001 — `(Provider, ProviderEventId)` uniqueness

Requirements:

```text
BILREQ066
BILREQ116
```

Database-enforced.

## 150. BIL-TST-WHK-CONC-001 — concurrent duplicate provider receipt

Requirements:

```text
BILREQ066
BILREQ127
```

Two simultaneous HTTP deliveries converge to one receipt/semantic effect.

## 151. BIL-TST-WHK-INT-001 — durable receipt before acknowledgement

Requirements:

```text
BILREQ067
```

Failure to persist receipt cannot be reported as safely accepted if provider contract requires retry.

## 152. BIL-TST-WHK-APP-001 — known event normalized explicitly

Requirements:

```text
BILREQ068
```

Provider status mapping is adapter/Application mapping, not arbitrary Domain setter.

## 153. BIL-TST-WHK-APP-002 — unknown event ignored observably

Requirements:

```text
BILREQ068
```

Unknown type does not crash pipeline or mutate state.

## 154. BIL-TST-WHK-INT-002 — duplicate processed event harmless

Requirements:

```text
BILREQ066
BILREQ127
```

Semantic handler replay does not duplicate Subscription/Invoice/Payment transition.

## 155. BIL-TST-WHK-INT-003 — out-of-order subscription observations

Requirements:

```text
BILREQ069
```

Older provider observation arriving after a newer one does not regress canonical Billing state.

## 156. BIL-TST-WHK-INT-004 — invoice before subscription event

Requirements:

```text
BILREQ069
```

Pipeline either reconciles/defers safely without assuming event order.

## 157. BIL-TST-WHK-REL-001 — crash after receipt before semantic processing

Requirements:

```text
BILREQ067
BILREQ127
```

Receipt remains retryable and semantic effect is eventually applied at most once.

## 158. BIL-TST-WHK-MIG-001 — legacy BillingEvent migration

Requirements:

```text
BILREQ116
```

Legacy provider events with unknown provider remain historical evidence and are not accepted as new active dedup authority.

## 158A. BIL-TST-WHK-INT-005 — stale event cannot regress newer provider snapshot

Requirements:

```text
BILREQ069
```

Deliver newer observation first and older event second.

Expected:

```text
receipt retained
provider freshness watermark/current snapshot remains newer
canonical state does not regress
```

## 158B. BIL-TST-WHK-CONC-002 — provider response and webhook settle race converges

Requirements:

```text
BILREQ066
BILREQ069
BILREQ127
```

Race provider-effect settlement against verified webhook reconciliation.

Expected:

```text
one final Billing state
one semantic completed transition/event
BillingOperation settles consistently
no duplicate financial effect
```

## 158C. BIL-TST-WHK-SEC-006 — support role cannot read raw provider body

Requirements:

```text
BILREQ065
BILREQ083
BILREQ101
```

`notrelix_support_readonly` can read only explicitly safe receipt metadata, not raw provider body/blob.

# Reconciliation tests

## 159. BIL-TST-REC-APP-001 — Unknown and stale Pending operations are selectable

Requirements:

```text
BILREQ070
BILREQ120
BILREQ126
```

Eligible:

```text
Unknown
stale Pending without fresh active provider-effect claim
```

Ineligible:

```text
fresh Pending with active claim
```

Selection is bounded/scoped and records age/attempt metrics.

## 159A. BIL-TST-REC-INT-000A — crash-before-provider stale Pending is repaired safely

Requirements:

```text
BILREQ070
```

Persist Pending + intent, prepare claim, then crash before provider call.

After threshold:

```text
provider proves effect absent
→ same logical operation may requeue
→ same provider idempotency key retained
```

## 159B. BIL-TST-REC-INT-000B — provider success plus failed settle leaves stale Pending repairable

Requirements:

```text
BILREQ070
BILREQ126
```

Provider contains completed effect while local operation remains Pending because settle failed.

Reconciler finds provider effect and settles local state without second provider mutation.

## 160. BIL-TST-REC-INT-001 — Unknown provider success reconciles to Succeeded

Requirements:

```text
BILREQ070
BILREQ072
BILREQ126
```

Scenario:

```text
provider action succeeded
local response lost / DB state incomplete
reconciliation observes provider success
local Subscription/financial state converges
```

## 161. BIL-TST-REC-INT-002 — Unknown provider absence reconciles safely

Requirements:

```text
BILREQ070
BILREQ072
```

No duplicate operation is created before provider state is classified.

## 162. BIL-TST-REC-INT-003 — missed webhook repaired

Requirements:

```text
BILREQ070
```

Provider state changes without webhook delivery; periodic/scoped reconcile converges local state.

## 163. BIL-TST-REC-INT-004 — delayed webhook after reconciliation is harmless

Requirements:

```text
BILREQ069
BILREQ070
BILREQ127
```

## 164. BIL-TST-REC-SEC-001 — repair action requires privileged authority

Requirements:

```text
BILREQ071
BILREQ103
```

## 165. BIL-TST-REC-SEC-002 — repair is Account-scoped

Requirements:

```text
BILREQ071
BILREQ082
```

Repair cannot target another Account through client-controlled ID.

## 166. BIL-TST-REC-APP-002 — field-specific authority

Requirements:

```text
BILREQ072
```

Tests distinguish:

- provider settlement fact;
- Notrelix product-entitlement meaning.

Provider status cannot directly overwrite unrelated internal fields.

## 167. BIL-TST-REC-OBS-001 — reconciliation metrics/correlation

Requirements:

```text
BILREQ119
BILREQ120
```

Observability includes safe operation/provider/correlation IDs and attempt result.

# Invoice tests

## 168. BIL-TST-INV-DOM-001 — create Draft invoice

Requirements:

```text
BILREQ073
```

Retain provider-neutral invoice evidence.

## 169. BIL-TST-INV-DOM-002 — Draft → Open

Requirements:

```text
BILREQ074
```

## 170. BIL-TST-INV-DOM-003 — Open → Paid

Requirements:

```text
BILREQ074
```

## 171. BIL-TST-INV-DOM-004 — Open → Uncollectible

Requirements:

```text
BILREQ074
```

## 172. BIL-TST-INV-DOM-005 — Open → Void

Requirements:

```text
BILREQ074
```

## 173. BIL-TST-INV-DOM-006 — invalid invoice transitions rejected

Requirements:

```text
BILREQ074
```

The initial closure explicitly rejects:

```text
Draft → Paid
Draft → Uncollectible
Draft → Void
Paid → Void
Paid → Uncollectible
Void → Paid
Uncollectible → Paid through an ordinary setter
```

unless a future explicit adjustment/reopen model supersedes this.

## 174. BIL-TST-INV-INT-001 — provider invoice observation is normalized

Requirements:

```text
BILREQ073
BILREQ086
```

No provider DTO becomes stored Domain shape.

## 175. BIL-TST-INV-INT-002 — historical invoice survives Plan revision changes

Requirements:

```text
BILREQ073
BILREQ080
```

## 175A. BIL-TST-INV-DOM-007 — invoice money invariants

Requirements:

```text
BILREQ073
```

Prove:

```text
unsupported currency precision rejected
line currency == invoice currency
ordinary negative charge rejected
provider total not silently recomputed from mutable Plan data
```

## 175B. BIL-TST-INV-DB-001 — provider invoice identity is unique

Requirements:

```text
BILREQ073
```

`(Provider, ProviderInvoiceId)` is unique when enabled.

## 175C. BIL-TST-INV-DOM-008 — line-item snapshot immutable after Issue

Requirements:

```text
BILREQ073
BILREQ074
```

Issued line evidence cannot be rewritten by current catalog changes.

# PaymentTransaction tests

## 176. BIL-TST-PAY-DOM-001 — pending transaction

Requirements:

```text
BILREQ075
```

## 177. BIL-TST-PAY-DOM-002 — success settlement

Requirements:

```text
BILREQ075
```

## 178. BIL-TST-PAY-DOM-003 — failed settlement

Requirements:

```text
BILREQ075
```

Failure category is normalized and safe.

## 178A. BIL-TST-PAY-DOM-004 — unknown settlement is distinct from pending

Requirements:

```text
BILREQ075
```

`Pending` means known non-terminal/in-progress; `Unknown` means provider terminal state cannot be determined safely.

Unknown requires reconciliation.

## 178B. BIL-TST-PAY-REL-001 — unknown settlement forbids blind retry

Requirements:

```text
BILREQ061
BILREQ075
```

A lost provider settlement response cannot create a second uncorrelated settlement attempt.

## 179. BIL-TST-PAY-INT-001 — provider success produces one settlement record

Requirements:

```text
BILREQ075
BILREQ105
```

## 180. BIL-TST-PAY-INT-002 — duplicate provider payment observation

Requirements:

```text
BILREQ075
BILREQ127
```

No duplicate settled amount/effect.

## 181. BIL-TST-PAY-SEC-001 — transaction contains no raw payment secret

Requirements:

```text
BILREQ077
```

# PaymentMethod tests

## 182. BIL-TST-PM-DOM-001 — PaymentMethod is Account-rooted

Requirements:

```text
BILREQ076
```

Workspace ID is absent from target authority.

## 183. BIL-TST-PM-DOM-002 — provider reference required

Requirements:

```text
BILREQ076
BILREQ077
```

Only safe provider reference and display metadata are stored.

## 184. BIL-TST-PM-DB-001 — one default per Account/provider

Requirements:

```text
BILREQ078
```

Database constraint/transaction protocol proves at most one active default.

## 185. BIL-TST-PM-CONC-001 — concurrent set-default race

Requirements:

```text
BILREQ078
BILREQ108
```

Two concurrent default selections converge deterministically.

## 186. BIL-TST-PM-MIG-001 — Workspace → Account migration

Requirements:

```text
BILREQ114
```

Verify:

```text
workspace resolves to same AccountId
provider reference preserved
status preserved
default conflict resolved deterministically
```

Cross-account mismatch fails migration verification.

## 187. BIL-TST-PM-SEC-001 — no PAN/CVV/full credential

Requirements:

```text
BILREQ077
BILREQ101
```

Scan DB model/API/events/log fixtures.

# Retention and lifecycle tests

## 188. BIL-TST-LIFE-INT-001 — Account closure ceases entitlement grant

Requirements:

```text
BILREQ079
```

## 189. BIL-TST-LIFE-INT-002 — Account closure coordinates provider cancellation

Requirements:

```text
BILREQ079
```

Where provider-backed subscription is enabled.

## 190. BIL-TST-LIFE-INT-003 — Account deletion does not erase financial evidence

Requirements:

```text
BILREQ080
```

Real DB test verifies retained Invoice/PaymentTransaction/evidence rows.

## 191. BIL-TST-LIFE-INT-004 — Workspace deletion does not erase Account Billing

Requirements:

```text
BILREQ080
```

## 192. BIL-TST-LIFE-PRIV-001 — retained evidence minimizes personal data

Requirements:

```text
BILREQ081
```

Retained rows/events do not unnecessarily preserve mutable profile/contact PII.

# RLS and tenancy tests

## 193. BIL-TST-RLS-ARCH-001 — every Billing table has RLS classification

Requirements:

```text
BILREQ082
BILREQ083
```

Expected classes:

```text
catalog
Account/Workspace scoped
worker/internal
```

New tables without classification fail architecture/infrastructure verification.

## 193A. BIL-TST-RLS-DB-001 — expected Billing table policy inventory is complete

Requirements:

```text
BILREQ082
BILREQ083
BILAC013
```

Build an explicit expected table classification and compare it with PostgreSQL `pg_class` / `pg_policies`.

The difference MUST be empty.

This test fails if a Billing table:

```text
has RLS disabled
has no expected policy
is silently skipped because it has no account_id/workspace_id
has the wrong catalog/account/workspace/worker classification
```

A non-empty generic RLS result set is insufficient.

## 193B. BIL-TST-RLS-DB-002 — InvoiceLineItem inherits parent Invoice Account policy

Requirements:

```text
BILREQ083
BILREQ084
```

Direct tenant-role SELECT of `invoice_line_items` is allowed only when the parent Invoice Account is accessible.

Account A cannot read line items of Account B.

## 193C. BIL-TST-RLS-DB-003 — Entitlement target uses one RLS-visible Workspace column

Requirements:

```text
BILREQ082
BILREQ083
```

Target mapping proves:

```text
TargetWorkspaceId property → workspace_id column
Account grant → workspace_id NULL → Account access
Workspace grant → workspace_id set → Workspace access
```

No duplicate `target_workspace_id` security authority remains.

## 193D. BIL-TST-RLS-DB-004 — Account financial tables do not become Workspace-scoped

Requirements:

```text
BILREQ076
BILREQ082
BILREQ083
```

Target `subscriptions`, `invoices`, `payment_methods`, `payment_transactions`, `billing_operations` do not retain `workspace_id` merely as metadata that would make the generic RLS helper require Workspace membership.

## 193E. BIL-TST-RLS-INT-000A — Account Billing summary does not bypass Workspace RLS

Requirements:

```text
BILREQ083
BILREQ084
BILREQ129
```

Account Billing admin reads Account-scoped Billing usage projection across Workspaces without worker/system scope.

Same principal still cannot directly read unauthorized Workspace product-resource data.

## 193F. BIL-TST-RLS-SEC-001 — Account Billing Admin cannot mutate global catalog

Requirements:

```text
BILREQ083
BILREQ092
```

Account Billing permission cannot create/publish/deprecate global catalog entities.

Only SYSTEM/OPERATOR catalog authority can mutate them.

## 194. BIL-TST-RLS-INT-001 — Account A cannot SELECT Account B Subscription

Requirements:

```text
BILREQ082
BILREQ084
```

Use restricted runtime role/session context.

## 195. BIL-TST-RLS-INT-002 — Account A cannot SELECT Account B Entitlement

Requirements:

```text
BILREQ082
BILREQ084
```

## 196. BIL-TST-RLS-INT-003 — Account A cannot SELECT Account B Invoice/Payment

Requirements:

```text
BILREQ082
BILREQ084
```

## 197. BIL-TST-RLS-INT-004 — Account A cannot mutate Account B Billing

Requirements:

```text
BILREQ082
BILREQ084
```

## 198. BIL-TST-RLS-INT-005 — catalog is readable under catalog policy only

Requirements:

```text
BILREQ083
```

Catalog visibility does not permit mutation by ordinary tenant role.

## 199. BIL-TST-RLS-INT-006 — worker receipt table inaccessible to ordinary tenant

Requirements:

```text
BILREQ083
BILREQ102
```

Provider receipt/inbox uses worker/internal policy.

## 200. BIL-TST-RLS-INT-007 — system repair context remains explicit

Requirements:

```text
BILREQ084
BILREQ103
```

No null/default tenant context becomes unrestricted business access accidentally.

# Billing event tests

## 201. BIL-TST-EVT-DOM-001 — subscription event after successful transition

Requirements:

```text
BILREQ085
```

Failed transition emits no canonical completed-fact event.

## 202. BIL-TST-EVT-DOM-002 — entitlement event after effective change

Requirements:

```text
BILREQ085
BILREQ088
```

No event for semantic no-op reconciliation.

## 203. BIL-TST-EVT-ARCH-001 — raw provider payload cannot be public event

Requirements:

```text
BILREQ086
```

## 204. BIL-TST-EVT-ARCH-002 — Billing event manifest/inventory completeness

Requirements:

```text
BILREQ085
BILREQ087
```

Every public Billing integration event is inventory-visible with version/tenant scope.

## 205. BIL-TST-EVT-ARCH-003 — consumer maturity is explicit

Requirements:

```text
BILREQ087
```

A registered logging stub cannot be reported as an implemented business consumer.

## 205A. BIL-TST-EVT-CONTRACT-001 — subscription.changed v2 carries PlanPrice identity

Requirements:

```text
BILREQ085
BILREQ087
```

Same Plan/revision monthly→annual change produces:

```text
PreviousPlanPriceId != NewPlanPriceId
```

even if derived Plan IDs are equal.

v2 is Account-scoped and does not require Subscription Workspace ownership.

v1 compatibility is explicit.

## 206. BIL-TST-EVT-INT-001 — SubscriptionChanged consumer contract

Requirements:

```text
BILREQ087
```

If retained, at least one real consumer test proves meaningful business behavior.

Otherwise the stub is retired.

## 207. BIL-TST-EVT-INT-002 — SubscriptionCanceled consumer contract

Requirements:

```text
BILREQ087
```

Same maturity rule.

## 208. BIL-TST-EVT-INT-003 — Entitlement propagation

Requirements:

```text
BILREQ088
```

Downstream projection/cache invalidation receives sufficient stable Account/Workspace/capability identity.

## 209. BIL-TST-X-INT-001 — Accounts → Billing identity/lifecycle contract

Requirements:

```text
BILREQ089
```

Billing consumes public Account facts/contracts, not Accounts private DbSet.

## 210. BIL-TST-X-ARCH-001 — product contexts only use Billing public surface

Requirements:

```text
BILREQ090
```

Reject direct product-context dependency on Billing persistence/Domain aggregate.

## 211. BIL-TST-X-INT-002 — source fact → metered usage

Requirements:

```text
BILREQ091
```

Representative source producer contract.

## 212. BIL-TST-X-APP-001 — Governance owns Billing admin permission

Requirements:

```text
BILREQ092
```

Billing does not embed hard-coded AccountRole authorization.

## 213. BIL-TST-X-ARCH-002 — Platform provides mechanism, not commercial semantics

Requirements:

```text
BILREQ093
```

Shared idempotency/messaging/tenancy infrastructure cannot branch on Plan/tier.

## 214. BIL-TST-X-ARCH-003 — Analytics is downstream

Requirements:

```text
BILREQ094
```

Billing never reads Analytics tables as billing-usage authority.

# Frontend tests

## 215. BIL-TST-FE-ARCH-001 — production has no hard-coded Plan catalog authority

Requirements:

```text
BILREQ095
BILREQ111
```

Production Billing UI cannot define canonical Plan names/prices/features locally.

Story/test fixtures are exempt only when not imported into production behavior.

## 216. BIL-TST-FE-ARCH-002 — workspace.plan is not Billing authority

Requirements:

```text
BILREQ096
BILREQ110
```

Production Billing route/page does not derive current Plan from Workspace summary.

## 217. BIL-TST-FE-UNIT-001 — Account-scoped query key

Requirements:

```text
BILREQ097
```

Billing keys include Account ID.

## 218. BIL-TST-FE-UNIT-002 — Workspace usage key includes Account + Workspace

Requirements:

```text
BILREQ097
```

## 219. BIL-TST-FE-INT-001 — Account switch invalidates Billing cache

Requirements:

```text
BILREQ097
```

No prior Account plan/invoice/usage flashes as valid state after switch.

## 220. BIL-TST-FE-CMP-001 — entitlement guard is UX only

Requirements:

```text
BILREQ098
```

Hidden/disabled UI cannot substitute for backend rejection; component test plus API negative path.

## 221. BIL-TST-FE-CMP-002 — permission denied UX

Requirements:

```text
BILREQ099
```

Does not render upgrade CTA as the primary explanation.

## 222. BIL-TST-FE-CMP-003 — capability not included UX

Requirements:

```text
BILREQ099
```

## 223. BIL-TST-FE-CMP-004 — capacity exhausted UX

Requirements:

```text
BILREQ099
```

## 224. BIL-TST-FE-CMP-005 — inactive subscription UX

Requirements:

```text
BILREQ099
```

## 225. BIL-TST-FE-CMP-006 — pending/unknown provider operation UX

Requirements:

```text
BILREQ062
BILREQ099
```

Do not show success until backend authority confirms.

## 226. BIL-TST-FE-CMP-007 — provider unavailable UX

Requirements:

```text
BILREQ099
```

Existing product capability remains based on current Entitlement state; Billing admin action communicates provider issue.

## 227. BIL-TST-FE-E2E-001 — view current Billing state

Requirements:

```text
BILREQ095
BILREQ138
```

Released production E2E.

## 228. BIL-TST-FE-E2E-002 — upgrade/change plan

Requirements:

```text
BILREQ095
BILREQ137
```

When provider flow is enabled.

## 229. BIL-TST-FE-E2E-003 — schedule cancel and resume

Requirements:

```text
BILREQ095
BILREQ133
```

## 230. BIL-TST-FE-E2E-004 — invoice list/detail

Requirements:

```text
BILREQ095
BILREQ138
```

## 231. BIL-TST-FE-OAS-001 — generated contract consumer

Requirements:

```text
BILREQ131
```

Frontend build fails on stale generated Billing contract.

# Security master tests

## 232. BIL-TST-SEC-MASTER-001 — commercial mutation is protected

Requirements:

```text
BILREQ100
```

Every released mutation declares authentication/Account/authorization/idempotency semantics.

## 233. BIL-TST-SEC-MASTER-002 — secret source scan

Requirements:

```text
BILREQ101
BILREQ077
```

Search production source/config/log templates for prohibited secrets.

## 234. BIL-TST-SEC-MASTER-003 — provider error redaction

Requirements:

```text
BILREQ101
```

Provider exception cannot dump secret/request credentials into logs/API.

## 235. BIL-TST-SEC-MASTER-004 — webhook payload logging minimization

Requirements:

```text
BILREQ101
```

Failure logs use receipt/provider IDs and safe classifications rather than raw body.

## 236. BIL-TST-SEC-MASTER-005 — privileged repair authorization

Requirements:

```text
BILREQ103
```

Ordinary billing admin cannot use system repair path unless policy explicitly grants it.

# Idempotency/concurrency master tests

## 237. BIL-TST-IDEM-001 — command replay matrix

Requirements:

```text
BILREQ105
BILAC014
```

At minimum cover:

```text
Account bootstrap
change Plan
schedule cancel
resume
capacity consume
metered usage ingestion
provider customer create
provider subscription operation
webhook receipt
webhook semantic apply
```

## 238. BIL-TST-CONC-001 — one effective Subscription race

Requirements:

```text
BILREQ104
BILAC015
```

## 239. BIL-TST-CONC-002 — entitlement reconcile race

Requirements:

```text
BILREQ106
BILAC015
```

## 240. BIL-TST-CONC-003 — last-slot capacity race

Requirements:

```text
BILREQ107
BILAC015
```

Pinned existing evidence.

## 241. BIL-TST-CONC-004 — payment default race

Requirements:

```text
BILREQ108
BILAC015
```

## 242. BIL-TST-CONC-005 — webhook receipt dedup race

Requirements:

```text
BILREQ066
BILREQ127
BILAC015
```

## 243. BIL-TST-CONC-006 — metered usage dedup race

Requirements:

```text
BILREQ045
BILAC015
```

# Migration tests

## 244. BIL-TST-MIG-DB-001 — clean install

Requirements:

```text
BILREQ118
BILAC016
```

Fresh database applies all migrations and RLS scripts successfully.

## 245. BIL-TST-MIG-DB-002 — audited-baseline upgrade

Requirements:

```text
BILREQ112–BILREQ118
```

Start from schema/data compatible with audited baseline, apply target migration and verify all authority/backfill assertions.

## 246. BIL-TST-MIG-DB-003 — expand window compatibility

Requirements:

```text
BILREQ118
```

Where rollout requires mixed versions, old/new binaries can coexist only for the explicitly supported expand window.

## 247. BIL-TST-MIG-DB-004 — no indefinite dual-write

Requirements:

```text
BILREQ109
BILREQ110
BILREQ113
BILREQ118
```

Source/architecture gate proves legacy writes are removed after authority switch.

## 248. BIL-TST-MIG-DB-005 — forward-fix financial migration

Requirements:

```text
BILREQ118
```

Failure after a non-reversible commercial backfill leaves sufficient marker/state for safe forward-fix; test does not require destructive rollback of financial history.

## 249. BIL-TST-MIG-DB-006 — Plan price migration

Requirements:

```text
BILREQ112
```

## 249A. BIL-TST-MIG-DB-006A — Subscription PlanPrice deterministic backfill

Requirements:

```text
BILREQ112
```

Fixture covers:

```text
one valid SubscriptionItem quantity=1 → item PlanPrice
no item → exact Plan.Price + Plan.Period mapping
multiple items → BLOCKED
quantity != 1 → BLOCKED
item Plan mismatch → BLOCKED
embedded price/period conflict → BLOCKED
```

No blocked row is coerced.

## 249B. BIL-TST-MIG-DB-006B — scope evidence is validated before workspace columns are dropped

Requirements:

```text
BILREQ114
BILREQ118
```

Workspace→Account consistency is proven for legacy Subscription/Invoice/PaymentMethod before source Workspace columns are removed.

## 249C. BIL-TST-MIG-DB-006C — legacy Subscription statuses are classified

Requirements:

```text
BILREQ113
```

`Incomplete`, `Unpaid`, `Trialing` map only with sufficient authoritative evidence; ambiguous rows block migration.

## 250. BIL-TST-MIG-DB-007 — SubscriptionTier migration

Requirements:

```text
BILREQ113
```

## 251. BIL-TST-MIG-DB-008 — PaymentMethod scope migration

Requirements:

```text
BILREQ114
```

## 252. BIL-TST-MIG-DB-009 — Entitlement lineage migration

Requirements:

```text
BILREQ115
```

## 253. BIL-TST-MIG-DB-010 — provider receipt uniqueness migration

Requirements:

```text
BILREQ116
```

## 254. BIL-TST-MIG-DB-011 — usage semantic migration

Requirements:

```text
BILREQ117
```

## 255. BIL-TST-MIG-DB-012 — Workspace Plan DTO compatibility removal

Requirements:

```text
BILREQ110
```

OpenAPI/frontend generated contract moves without stale production consumer.

## 255A. BIL-TST-MIG-DB-013 — Billing FK and delete-behavior matrix

Requirements:

```text
BILREQ118
```

Real PostgreSQL proves target FK relationships and retained financial/commercial evidence is protected from destructive cascades.

Cross-Account relation attempts fail integrity checks.

# Reliability tests

## 256. BIL-TST-REL-001 — DB failure before provider submission

Requirements:

```text
BILREQ125
```

Provider receives no operation and API cannot report success.

## 257. BIL-TST-REL-002 — DB failure after provider success

Requirements:

```text
BILREQ126
```

Operation remains discoverable/reconcilable and does not trigger blind duplicate.

## 258. BIL-TST-REL-003 — provider unavailable during product capability check

Requirements:

```text
BILREQ122
BILREQ124
```

No provider call should occur at all.

## 259. BIL-TST-REL-004 — provider unavailable during Billing admin operation

Requirements:

```text
BILREQ124
```

Current internal Subscription/Entitlement is not corrupted.

## 260. BIL-TST-REL-005 — webhook processor transient failure

Requirements:

```text
BILREQ127
```

Durable receipt remains retryable.

## 261. BIL-TST-REL-006 — Entitlement storage/dependency failure

Requirements:

```text
BILREQ128
```

Explicit failure/fail-closed path; no grant.

## 262. BIL-TST-REL-007 — reconciliation retry is bounded

Requirements:

```text
BILREQ070
BILREQ120
```

Backoff/attempt policy does not create hot infinite retry.

# Observability tests

## 263. BIL-TST-OBS-001 — operation correlation

Requirements:

```text
BILREQ119
```

One request can be traced through:

```text
request/idempotency
BillingOperation
provider reference
webhook receipt
Subscription transition
Entitlement reconciliation
financial evidence
```

## 264. BIL-TST-OBS-002 — reconciliation observability

Requirements:

```text
BILREQ120
```

Unknown age/attempt/outcome visible without secrets.

## 265. BIL-TST-OBS-003 — entitlement decision metric

Requirements:

```text
BILREQ121
```

Allowed/denied/limit-exceeded/dependency-unavailable categories are distinguishable.

## 266. BIL-TST-OBS-004 — capacity conflict metric

Requirements:

```text
BILREQ121
```

Concurrency conflicts and capacity-exceeded are not collapsed.

## 267. BIL-TST-OBS-005 — webhook verification failure metric

Requirements:

```text
BILREQ120
```

Invalid signatures are observable without raw payload logging.

# Performance tests/reviews

## 268. BIL-TST-PERF-001 — capability hot path never calls provider

Requirements:

```text
BILREQ122
BILAC019
```

Inject provider gateway mock that throws if called; capability query must not touch it.

## 269. BIL-TST-PERF-002 — capability evaluation bounded query count

Requirements:

```text
BILREQ123
```

Representative entitlement/capacity request has bounded DB query count and no N+1 catalog traversal.

Exact threshold is recorded in implementation evidence.

## 270. BIL-TST-PERF-003 — correctness before cache

Requirements:

```text
BILREQ034
BILREQ123
```

Cache optimization cannot change decision results.

## 271. BIL-TST-PERF-004 — Billing page query fanout review

Requirements:

```text
BILREQ095
BILREQ123
```

Billing summary UI does not require an unbounded per-capability/per-invoice request pattern.

# API use-case completeness tests

## 272. BIL-TST-USECASE-001 — catalog use-case inventory

Requirements:

```text
BILREQ132
```

Every required catalog use case has:

```text
Application handler/service
API/admin surface where intended
tests
```

## 273. BIL-TST-USECASE-002 — Subscription use-case inventory

Requirements:

```text
BILREQ133
```

Verify required read/change/cancel/resume/effective lifecycle operations.

## 274. BIL-TST-USECASE-003 — Entitlement use-case inventory

Requirements:

```text
BILREQ134
```

Verify evaluate/reconcile/override/revoke/expire paths as specified.

## 275. BIL-TST-USECASE-004 — capacity use-case inventory

Requirements:

```text
BILREQ135
```

Verify query/consume/release/lifecycle reconciliation.

## 276. BIL-TST-USECASE-005 — metered usage use-case inventory

Requirements:

```text
BILREQ136
```

Verify ingest/correct/query/aggregate/period transition.

## 277. BIL-TST-USECASE-006 — provider/admin use-case inventory

Requirements:

```text
BILREQ137
```

Provider-dependent items may be `BLOCKED` if provider not selected; they cannot be marked PASS.

## 278. BIL-TST-USECASE-007 — financial read use-case inventory

Requirements:

```text
BILREQ138
```

Invoice/payment/payment-method read contracts match released financial scope.

# Architecture test requirements

## 279. BIL-TST-ARCH-001 — no Plan/tier comparison outside Billing

Reject production patterns equivalent to:

```text
PlanCode == "Pro"
SubscriptionTier.Pro
workspace.plan == ...
```

outside approved compatibility/migration surfaces.

Covers:

```text
BILREQ007
BILREQ017
BILREQ090
BILREQ109
BILREQ110
BILREQ113
```

## 280. BIL-TST-ARCH-002 — no Billing DbSet access from product contexts

Covers:

```text
BILREQ090
```

## 281. BIL-TST-ARCH-003 — no provider types in Domain/Application public contracts

Covers:

```text
BILREQ057
BILREQ063
```

## 282. BIL-TST-ARCH-004 — no hard-coded frontend catalog authority

Covers:

```text
BILREQ095
BILREQ111
```

## 283. BIL-TST-ARCH-005 — Account remains payer

Covers:

```text
BILREQ012
BILREQ076
BILREQ130
```

## 284. BIL-TST-ARCH-006 — Billing and Governance remain separate

Covers:

```text
BILREQ025
BILREQ054
BILREQ055
BILREQ092
```

## 285. BIL-TST-ARCH-007 — Analytics cannot become billing-usage source

Covers:

```text
BILREQ094
```

## 286. BIL-TST-ARCH-008 — capacity semantics regression gate

Retain existing:

```text
zero != unlimited
WorkspaceFeatureUsage owns hard capacity
AUTOMATION_RULE vocabulary Billing-owned
```

Covers:

```text
BILREQ035–042
```

## 287. BIL-TST-ARCH-009 — public event/provider payload boundary

Covers:

```text
BILREQ085
BILREQ086
```

## 288. BIL-TST-ARCH-010 — command-oriented Billing mutations

Reject public generic CRUD setters for Subscription/Invoice/Payment/provider receipt.

Covers:

```text
BILREQ053
BILREQ129
```

# Security test matrix

## 289. BIL-TST-SEC-MATRIX-001 — payer spoofing

Paths:

```text
Billing summary
change plan
cancel
payment method
invoice detail
repair
```

All use server-authoritative Account.

## 290. BIL-TST-SEC-MATRIX-002 — webhook authentication

Paths:

```text
valid signature
missing
invalid
tampered
replayed/stale
unknown provider route
```

## 291. BIL-TST-SEC-MATRIX-003 — secret exposure

Surfaces:

```text
Domain
Application DTO
API DTO
events/outbox
structured logs
exception logs
database columns
frontend props/state
```

## 292. BIL-TST-SEC-MATRIX-004 — Billing admin authorization

Actors:

```text
unauthenticated
ordinary member
authorized Billing admin
system/reconciliation actor
wrong Account admin
```

## 293. BIL-TST-SEC-MATRIX-005 — RLS defense in depth

Run direct DB queries under restricted role/session contexts, not only HTTP tests.

# Provider failure matrix

## 294. BIL-TST-PRV-MATRIX-001 — submission/result taxonomy

At minimum:

| Failure point | Expected semantic |
|---|---|
| validation before provider | no provider call |
| DB failure before provider | no provider call |
| provider definitive rejection | Failed |
| timeout before proven submission | adapter-defined safe classification |
| timeout after possible submission | Unknown |
| provider success + local DB failure | Unknown/reconciliation required |
| webhook missing | periodic reconciliation |
| duplicate webhook | one semantic effect |
| out-of-order webhook | no state regression |
| provider 5xx/rate-limit | bounded retry or Unknown according to operation contract |

# Migration compatibility matrix

## 295. BIL-TST-MIG-MATRIX-001 — legacy authority switch

Track each old authority:

| Legacy source | Target authority | Required proof |
|---|---|---|
| `Plan.Price` | `PlanPrice` | value-equivalent backfill |
| `Plan.Period` | `PlanPrice.BillingInterval` | no second billing-interval authority |
| `SubscriptionItem` / direct `Subscription.PlanId` | `Subscription.PlanPriceId` | exact currency/interval/amount offer remains pinned |
| `PlanLimit` | `PlanCapability` | finite/unlimited semantic equivalence |
| `Account.PlanCode` | Billing Subscription | no product access from legacy field |
| `SubscriptionTier` | capability/effective-subscription facts | all consumers migrated |
| `WorkspaceDto.Plan` | Billing API | frontend no longer consumes it |
| hard-coded frontend PLANS | Billing catalog API | production array removed |
| Workspace PaymentMethod scope | Account PaymentMethod scope | mapping/default integrity |
| lineage-less Entitlement | source-lineaged Entitlement | legacy rows classified |
| `BillingEvent` raw Domain payload | provider receipt/inbox | new dedup authority |
| `UsageMetricHistory` authority | immutable MeteredUsageEvent | totals/history preserved |

# CI mapping

## 296. Backend Domain suite

Must include Billing target tests and execute non-zero:

```text
Notrelix.Domain.Tests
```

## 297. Backend Application suite

Must include:

```text
Billing application tests
Automation Billing reference-consumer tests
```

## 298. Backend Infrastructure suite

Must include:

```text
RLS/configuration/provider adapter tests
webhook verification tests
redaction/config validation
```

## 299. Backend Architecture suite

Must include target Billing authority/boundary gates.

## 300. Backend Integration suite

Must use real PostgreSQL for:

```text
RLS
migrations
capacity races
Subscription uniqueness
Entitlement concurrency
usage dedup
webhook dedup
payment-method default race
```

Provider behavior may use a deterministic fake/sandbox adapter unless certification explicitly targets the real provider sandbox.

## 301. Frontend suites

At minimum:

```text
pnpm codegen:check
pnpm check:architecture
pnpm typecheck
pnpm lint
pnpm test:node:guarded
pnpm test:web:guarded
pnpm test:integration:guarded
```

Released Billing UX also runs relevant Playwright/Storybook evidence.

## 302. CI non-zero requirement

For every certification command record:

```text
selected
passed
failed
skipped
```

Zero selected tests invalidates the evidence row.

## 303. Exact-SHA requirement

All final backend/frontend/generated-contract evidence must point to the same candidate SHA or an explicitly reproducible artifact set produced from it.

# Test implementation order

## 304. Required sequence

Test work follows implementation dependency order:

```text
1. catalog invariants + migration
2. Subscription/bootstrap
3. Entitlement derivation
4. capability/reference consumer
5. hard capacity regression
6. metered usage
7. API/authz
8. provider operation
9. webhook
10. reconciliation
11. financial evidence
12. frontend
13. lifecycle/RLS/migration
14. security/reliability/performance
15. full regression/certification
```

## 305. Test-first rule for critical boundaries

The following implementation slices MUST introduce failing verification before/with the behavioral change:

```text
Plan price authority migration
Subscription uniqueness
Entitlement reconcile idempotency
last-slot capacity regression
metered source-event uniqueness
provider Unknown outcome
webhook unique receipt
payment default uniqueness
Account payer spoofing
RLS for new Billing tables
```

# Test fixture design

## 306. Catalog fixture

Provides:

```text
Free revision
Pro revision 1
Pro revision 2
finite capability
zero capability
unlimited capability
deprecated revision
multi-currency price where supported
```

## 307. Account Billing fixture

Creates:

```text
User
Account
Account membership
Workspace
Workspace membership
current Plan/Subscription
Entitlements
```

using production constructors/DbContext.

## 308. Capacity fixture

Provides:

```text
capability grant
usage row present/absent
ledger history
resource lifecycle
controllable last slot
```

## 309. Metered usage fixture

Provides:

```text
source event identity
period clock
late arrival
duplicate
correction
period transition
```

## 310. Provider fake

A deterministic provider fake MUST be able to simulate:

```text
success
definitive failure
timeout before submission
timeout after possible submission
rate limit
state query
missing webhook
duplicate webhook
out-of-order observation
```

A fake that returns only `true/false` is insufficient.

## 311. Webhook fixture

Must generate:

```text
raw body
valid signature
invalid signature
old timestamp
provider event id
known/unknown event type
duplicate delivery
```

using the same verification primitive as production adapter where possible.

## 312. Failure-injection fixture

Provides deterministic injection at:

```text
before DB save
after DB save/before commit where possible
before provider submission
after provider submission/before local persistence
after webhook receipt/before semantic processing
during reconciliation
```

## 313. RLS fixture

Uses actual runtime roles/session context to test direct SQL/EF access under:

```text
Account A
Account B
system/worker
missing context
```

# Test anti-patterns

## 314. Do not certify Billing from in-memory EF only

In-memory providers do not prove:

- unique indexes;
- PostgreSQL concurrency;
- transaction semantics;
- RLS;
- raw SQL upsert behavior.

## 315. Do not mock away last-slot races

Sequential mocks cannot prove BILREQ107.

## 316. Do not infer provider support from enum values

`Stripe`, `PayPal`, `Manual` enum members are not feature evidence.

## 317. Do not use frontend fixtures as commercial truth

Storybook/mock fixtures are visual/test data only.

## 318. Do not auto-update snapshots to hide contract drift

OpenAPI/event/UI snapshots are reviewed semantic evidence.

## 319. Do not treat logging consumers as implemented consumers

A consumer that only logs a Billing event is classified as stub/observability, not business capability.

## 320. Do not accept destructive migration cleanup

Tests must fail if historical commercial evidence is removed simply to make constraints pass.

## 321. Do not make provider sandbox the only deterministic test layer

Core outcome/reconciliation semantics must be reproducible without external sandbox availability.

## 322. Do not classify skipped provider tests as PASS

If provider is unselected/unconfigured:

```text
BLOCKED or NOT_APPLICABLE
```

according to scope.

# Requirement-by-requirement completeness

## 323. BILREQ001–008

Covered by:

```text
BIL-TST-CAT-*
BIL-TST-ARCH-001
```

Required evidence levels:

```text
T1 + T2 + T6/T9 + T4/T0
```

## 324. BILREQ009–023

Covered by:

```text
BIL-TST-SUB-*
BIL-TST-CONC-001
BIL-TST-MIG-*
```

Required:

```text
T1 + T2 + T6 + T8 + T9
```

## 325. BILREQ024–034

Covered by:

```text
BIL-TST-ENT-*
BIL-TST-X-ARCH-001
```

Required:

```text
T1 + T2 + T5 + T6 + T8
```

## 326. BILREQ035–042

Covered by:

```text
BIL-TST-RCAP-*
BIL-TST-ARCH-008
```

Required:

```text
T1 + T2 + T6 + T8
```

## 327. BILREQ043–051

Covered by:

```text
BIL-TST-USG-*
```

Required:

```text
T1 + T2 + T6 + T8 + T9
```

## 328. BILREQ052–056

Covered by:

```text
BIL-TST-API-*
BIL-TST-AUTHZ-*
```

Required:

```text
T2 + T4 + T5 + T6 + T7
```

## 329. BILREQ057–063

Covered by:

```text
BIL-TST-PRV-*
BIL-TST-OP-*
```

Required:

```text
T2 + T3 + T5 + T7 + T10
```

## 330. BILREQ064–069

Covered by:

```text
BIL-TST-WHK-*
```

Required:

```text
T3 + T6 + T7 + T8 + T10
```

## 331. BILREQ070–072

Covered by:

```text
BIL-TST-REC-*
```

Required:

```text
T2 + T6 + T7 + T10
```

## 332. BILREQ073–081

Covered by:

```text
BIL-TST-INV-*
BIL-TST-PAY-*
BIL-TST-PM-*
BIL-TST-LIFE-*
```

Required:

```text
T1 + T6 + T7 + T8 + T9
```

## 333. BILREQ082–084

Covered by:

```text
BIL-TST-RLS-*
BIL-TST-API-INT-*
```

Required:

```text
T5 + T6 + T7
```

## 334. BILREQ085–094

Covered by:

```text
BIL-TST-EVT-*
BIL-TST-X-*
```

Required:

```text
T1 + T5 + T6 + T11
```

## 335. BILREQ095–099

Covered by:

```text
BIL-TST-FE-*
```

Required:

```text
T0 + T12 + T13
```

## 336. BILREQ100–108

Covered by:

```text
BIL-TST-SEC-*
BIL-TST-IDEM-*
BIL-TST-CONC-*
```

Required:

```text
T5 + T6 + T7 + T8
```

## 337. BILREQ109–118

Covered by:

```text
BIL-TST-MIG-*
BIL-TST-MIG-MATRIX-001
```

Required:

```text
T0 + T6 + T9
```

## 338. BILREQ119–128

Covered by:

```text
BIL-TST-OBS-*
BIL-TST-REL-*
BIL-TST-PERF-*
```

Required:

```text
T6 + T7 + T10
```

## 339. BILREQ129–138

Covered by:

```text
BIL-TST-API-*
BIL-TST-USECASE-*
BIL-TST-FE-OAS-001
```

Required:

```text
T2 + T4 + T5 + T6 + T11 + T12/T13 where released
```

# Acceptance criteria mapping

## 340. BILAC001 — Catalog acceptance

PASS requires:

- catalog invariant tests;
- real uniqueness evidence;
- one current Free default PlanPrice;
- PlanCapability semantic uniqueness;
- transition-policy classification proof;
- historical Plan/PlanPrice preservation;
- Plan.Price migration proof;
- provider-neutral read contract.

## 341. BILAC002 — Bootstrap acceptance

PASS requires:

- AccountCreated → Free Subscription integration;
- replay;
- concurrent bootstrap;
- no provider dependency.

## 342. BILAC003 — Subscription acceptance

PASS requires:

- closed first-closure transition matrix with Trial deferred;
- exact TargetPlanPrice command;
- Billing-owned transition classification;
- activation-failure terminal behavior;
- paid cancel/resume/effective Free fallback;
- lifecycle-worker once-only behavior;
- one effective Subscription policy;
- tier/PlanCode authority migration.

## 343. BILAC004 — Entitlement acceptance

PASS requires:

- Plan → Entitlement derivation;
- lineage;
- precedence;
- expiry;
- fail closed;
- concurrent reconcile;
- stable capability reason.

## 344. BILAC005 — Capacity acceptance

PASS requires retained real PostgreSQL:

- last-slot race;
- first-use race;
- transaction atomicity;
- replay/conflict;
- lifecycle release;
- limit shrink.

## 345. BILAC006 — Metered usage acceptance

PASS requires:

- immutable usage event;
- source-event idempotency;
- period;
- late/out-of-order;
- correction;
- rebuild;
- hard-quota reservation if such quota is released.

## 346. BILAC007 — Provider operation acceptance

PASS only if provider feature is in release scope and includes:

- provider-neutral gateway;
- durable BillingOperation with request fingerprint/subject;
- request TX persists provider-effect intent only;
- prepare/effect/settle provider consumer;
- provider-relative object bindings;
- idempotency/claim handling;
- Unknown + stale-Pending recovery;
- deterministic provider fake/adapter contract;
- selected provider adapter evidence.

Otherwise state is `BLOCKED` or `NOT_APPLICABLE`, not PASS.

## 347. BILAC008 — Webhook acceptance

PASS only for an enabled provider with:

- authenticity;
- durable receipt;
- DB uniqueness;
- duplicate/out-of-order safety;
- crash/replay recovery.

## 348. BILAC009 — Financial evidence acceptance

PASS requires:

- Invoice transition tests;
- PaymentTransaction evidence;
- Account-rooted PaymentMethod;
- default race;
- retention;
- secret prohibition.

## 349. BILAC010 — Authorization acceptance

PASS requires:

- central Governance declarations;
- allowed/denied actor matrix;
- entitlement/permission separation;
- server-authoritative Account binding.

## 350. BILAC011 — Frontend acceptance

PASS requires:

- no production hard-coded Plan authority;
- no workspace.plan authority;
- Account-scoped cache;
- denial-state UI;
- generated contracts;
- released-flow interaction/E2E evidence.

## 351. BILAC012 — Architecture acceptance

PASS requires all Billing architecture gates.

## 352. BILAC013 — Data isolation acceptance

PASS requires API + direct RLS cross-Account negative tests for every released Billing table family.

## 353. BILAC014 — Idempotency acceptance

PASS requires command/event replay matrix.

## 354. BILAC015 — Concurrency acceptance

PASS requires real DB races for the concurrency-owned invariants.

## 355. BILAC016 — Migration acceptance

PASS requires clean install + audited-baseline upgrade + backfill/authority-switch checks.

## 356. BILAC017 — Security acceptance

PASS requires secret, webhook, payer spoofing, repair authorization and RLS evidence.

## 357. BILAC018 — Observability acceptance

PASS requires correlation and safe failure metrics for provider/reconciliation/entitlement/capacity paths.

## 358. BILAC019 — Performance acceptance

PASS requires bounded capability hot path and no provider call from product capability checks.

## 359. BILAC020 — CI evidence acceptance

PASS requires exact-SHA, non-zero full required suites and clean generated artifacts.

# PR-by-PR minimum test gate

## 360. PR-BIL-00

Documentation/source inventory only.

Required:

```text
source inventory checks
consumer inventory
schema/RLS inventory
no behavioral PASS claims
```

## 361. PR-BIL-01

Must pass:

```text
BIL-TST-CAT-DOM-001..008
BIL-TST-CAT-DB-001
BIL-TST-CAT-MIG-001
BIL-TST-CAT-APP-001..002
BIL-TST-CAT-INT-001
BIL-TST-CAT-API-001 where endpoint added
```

## 362. PR-BIL-02

Must pass:

```text
BIL-TST-SUB-*
BIL-TST-CONC-001
relevant Account contract tests
```

## 363. PR-BIL-03

Must pass:

```text
BIL-TST-ENT-*
```

plus real DB Plan/Subscription → Entitlement flow.

## 364. PR-BIL-04

Must pass:

```text
CreateAutomationRuleBillingGateTests
CreateAutomationRulePipelineTests
Billing capacity architecture gates
tier/Plan leakage gates
stable error tests
```

## 365. PR-BIL-05

Must retain/extend:

```text
BillingCapacityFlowIntegrationTests
BillingCapacityActionsTests
WorkspaceFeatureUsageTests
FeatureUsageLedgerTests
BillingCapacitySemanticsArchitectureTests
```

Any regression is a hard blocker.

## 366. PR-BIL-06

Must pass all `BIL-TST-USG-*` applicable to released meter.

## 367. PR-BIL-07

Must pass API/authz/OpenAPI/account-binding tests.

## 368. PR-BIL-08

Must pass provider-operation outcome/idempotency/customer-mapping tests.

Provider-specific adapter tests only when selected.

## 369. PR-BIL-09

Must pass full webhook authenticity/dedup/out-of-order/crash matrix.

## 370. PR-BIL-10

Must pass reconciliation unknown/missed/delayed/privileged-repair matrix.

## 371. PR-BIL-11

Must pass invoice/payment/payment-method/retention/security tests.

## 372. PR-BIL-12

Must pass frontend contract/component/E2E/UI evidence for released Billing flows.

## 373. PR-BIL-13

Must pass migration/RLS/lifecycle/retention hard-close.

## 374. PR-BIL-14

Must run full candidate regression and produce certification evidence; it cannot introduce hidden functional behavior.

# Certification handoff

## 375. Test evidence row

Each certification capability row contains:

```text
Capability:
Requirement(s):
Candidate SHA:
Test IDs:
Command:
Selected:
Passed:
Failed:
Skipped:
Environment:
Evidence/artifact:
Decision:
Notes:
```

## 376. Missing test handling

If a required test is not implemented or not executed:

```text
NOT_EVALUATED
```

or:

```text
BLOCKED
```

when an external dependency/decision prevents execution.

Do not convert absence to PASS using prose review.

## 377. Provider-disabled handling

If no production provider is selected/enabled:

Internal catalog/subscription/entitlement/capacity/API core can still certify to declared scope.

These remain non-PASS for external billing release:

```text
provider adapter
checkout/portal
provider webhook
provider reconciliation
external payment settlement
```

## 378. Operational-only exception

Operational evidence may complement but not replace source/test evidence for:

```text
webhook backlog
reconciliation lag
provider latency
unknown operation age
```

No operational dashboard can prove missing authorization/idempotency/transition code.

# Test-review checklist

## 379. Catalog review

Confirm:

- one Plan identity family + revision model;
- immutable historical terms;
- one price authority;
- explicit unlimited;
- catalog API provider-neutral.

## 380. Subscription review

Confirm:

- Account root;
- Free bootstrap;
- closed transitions;
- no duplicated tier authority;
- non-destructive downgrade;
- one effective Subscription policy.

## 381. Entitlement review

Confirm:

- lineage;
- Subscription + Manual-only certified composition;
- unsupported Promo/AddOn not exposed;
- manual override authorization/reason/revoke/expire;
- equal-precedence ambiguity fails closed;
- history preservation;
- fail closed;
- reason taxonomy;
- reconcile race.

## 382. Capacity review

Confirm:

- current proven transaction/concurrency semantics preserved;
- create/delete/disable lifecycle coverage;
- no period reset.

## 383. Metered usage review

Confirm:

- immutable source fact;
- idempotency;
- period;
- late events;
- correction;
- no capacity-ledger reuse.

## 384. Provider review

Confirm:

- neutral Application boundary;
- selected provider only;
- durable operation;
- Unknown outcome;
- no blind retry;
- secrets localized.

## 385. Webhook review

Confirm:

- raw-body verification;
- durable receipt;
- unique provider event;
- normalized mapping;
- duplicate/out-of-order/crash replay.

## 386. Financial review

Confirm:

- invoice closed transitions;
- settlement evidence;
- Account-rooted PaymentMethod;
- default race;
- retained history;
- no raw payment secret.

## 387. API/authz review

Confirm:

- server Account binding;
- Governance declarations;
- no generic state patch;
- stable error taxonomy;
- generated contract clean.

## 388. Frontend review

Confirm:

- no workspace Plan authority;
- no production hard-coded Plan array;
- Account-scoped cache;
- denial categories;
- pending/unknown UX;
- real backend authority.

## 389. Migration/RLS review

Confirm:

- clean/upgrade;
- source-to-target equivalence;
- no dual-write leftovers;
- every new table has correct RLS class;
- financial evidence survives deletion.

## 390. Reliability/security review

Confirm:

- DB-before-provider;
- provider-success/local-failure;
- dependency failure;
- webhook security;
- repair authorization;
- redaction;
- no provider call on capability hot path.

## 391. CI review

Confirm exact SHA, non-zero selected tests, generated artifacts and all required suites.

# Definition of Done — TESTS artifact

## 392. TESTS document is complete when

- all `BILREQ001–138` map to verification;
- all `BILAC001–020` have objective PASS evidence rules;
- critical races/failures use correct test level;
- current valid capacity evidence is explicitly preserved;
- provider-disabled scope cannot be misreported as provider-ready;
- migration/RLS/frontend/provider/financial tests are included;
- CI non-zero/exact-SHA rules are explicit.

## 393. TESTS document does not mean implementation exists

This artifact is a verification contract.

A named target test is not evidence until:

```text
test source exists
test runs
test selects non-zero
test passes on candidate
```

## 394. Final verification rule

Billing & Entitlements may be certified only when the evidence proves the released causal chain:

```text
Account
→ Subscription
→ immutable Plan revision
→ derived Entitlements
→ capability/capacity or metered usage
→ authorized Billing operation
→ provider flow where enabled
→ verified callback/reconciliation
→ retained financial evidence
→ data-driven frontend
```

If any released link is unverified, certification for that capability is not PASS.
