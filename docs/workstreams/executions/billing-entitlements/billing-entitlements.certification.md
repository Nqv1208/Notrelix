---
document_id: WRK-CERT-BILLING-ENTITLEMENTS
document_type: workstream-certification
status: active
owner: billing-entitlements-team
source_branch: develop
baseline_source_commit: 35702d0fa9fb01ed68b0667bab500030d60bd028
spec:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
plan:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.plan.md
tests:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.tests.md
applies_to:
  - billing
  - catalog
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
  - frontend
  - migration
  - rls
  - security
  - reliability
  - observability
  - performance
review_on:
  - billing-candidate-change
  - billing-requirement-change
  - provider-selection
  - provider-release
  - billing-schema-change
  - entitlement-contract-change
  - billing-api-change
  - migration-change
  - security-gate-change
  - downstream-consumer-change
---

# CERTIFICATION — Billing & Entitlements

## 1. Purpose

This document defines how Billing & Entitlements is certified against an exact repository candidate.

It does not certify intent, class presence, table presence, documentation completeness or a green CI badge by itself.

Certification answers:

```text
What Billing capability is being claimed?
At which readiness level?
For exactly which SHA?
Against which provider mode?
With which migrations/configuration?
Which executable evidence proves the claim?
Which blockers remain?
Which downstream consumers may now depend on the contract?
```

The certification artifact is the final evidence ledger for:

```text
billing-entitlements.spec.md
billing-entitlements.plan.md
billing-entitlements.tests.md
```

## 2. Certification authority chain

The authority order is:

```text
canonical product/architecture docs
        ↓
billing-entitlements.spec.md
        ↓
billing-entitlements.plan.md
        ↓
billing-entitlements.tests.md
        ↓
candidate source + schema + generated contracts
        ↓
executed tests / CI / migration / operational evidence
        ↓
this certification record
```

A lower layer cannot silently redefine a higher-layer semantic.

If source and SPEC disagree, certification stops until the conflict is resolved deliberately.

## 3. Readiness model

The workstream uses the repository readiness model:

```text
D0 — UNDEFINED
D1 — DEFINED
D2 — DESIGNED
D3 — IMPLEMENTING
D4 — VERIFIED
D5 — STABLE
```

### D0 — UNDEFINED

Ownership or semantics are unclear.

Downstream implementation MUST NOT depend on the capability.

### D1 — DEFINED

Owner and purpose are known.

The capability is still insufficient as an implementation dependency.

### D2 — DESIGNED

Contract/design is explicit.

Consumers may prepare adapters/mocks but must not harden production behavior against the contract.

### D3 — IMPLEMENTING

Producer implementation exists/in progress.

Consumers may build isolated work, but the contract is not yet verified.

### D4 — VERIFIED

The contract is implemented and tested on the exact candidate.

Dependent teams may integrate.

### D5 — STABLE

The contract is verified, migration/security/cross-team obligations are closed and it is accepted as a foundation for broad parallel work.

Breaking changes require explicit coordination and compatibility handling.

## 4. Certification status values

Each certification row uses one of:

### NOT_EVALUATED

Required evidence has not been executed/reviewed on the candidate.

This is the default.

### BLOCKED

A required decision, dependency, migration, provider selection/configuration or security prerequisite prevents verification.

`BLOCKED` is not a failure of already-completed internal work, but the capability cannot be claimed ready.

### PARTIALLY_VERIFIED

Some required evidence passes but the full capability gate is incomplete.

This state never satisfies a D4 dependency gate.

### VERIFIED

All D4 evidence for the declared capability scope passes on the exact candidate.

Equivalent readiness: D4.

### STABLE

All D5 evidence passes, downstream contract is frozen/stable and no blocking debt remains.

Equivalent readiness: D5.

### FAILED

Required evidence executed and did not satisfy the gate.

### NOT_APPLICABLE

The capability is deliberately outside the declared release scope.

This is valid only when its absence does not contradict the release claim.

Example:

```text
no production payment provider is selected
→ provider checkout/payment certification can be NOT_APPLICABLE
  for an internal-entitlement-only release
```

It cannot be `NOT_APPLICABLE` if marketing/release claims external paid checkout.

## 5. Evidence classes

Certification distinguishes:

| Code | Evidence |
|---|---|
| `SRC` | source inspection |
| `DOM` | Domain tests |
| `APP` | Application tests |
| `INF` | Infrastructure tests |
| `API` | API/contract tests |
| `ARCH` | Architecture/static gates |
| `INT` | integration tests with production graph |
| `DB` | real PostgreSQL/schema/constraint evidence |
| `RLS` | runtime RLS evidence |
| `CONC` | real concurrency/race evidence |
| `MIG` | migration/upgrade/backfill evidence |
| `SEC` | security/negative-path evidence |
| `REL` | reliability/failure-injection evidence |
| `OBS` | observability/operational evidence |
| `PERF` | performance/bounded-hot-path evidence |
| `FE` | frontend unit/component evidence |
| `E2E` | released flow end-to-end evidence |
| `GEN` | generated contract/OpenAPI evidence |
| `DOC` | canonical documentation consistency |

Source inspection is necessary but normally insufficient for D4.

## 6. Exact-candidate rule

Every certification execution records:

```text
repository
branch
candidate SHA
candidate timestamp
migration list/hash
provider mode
provider adapter/version when enabled
feature flags
configuration class
frontend commit/artifact if independently built
```

All test evidence must correspond to that candidate or a reproducible build artifact produced from it.

A result from another SHA is historical evidence only.

## 7. Non-zero execution rule

Every automated evidence row records:

```text
selected tests
passed
failed
skipped
```

If selected = 0:

```text
status = FAILED
```

for that evidence obligation.

A command exit code of zero with no selected tests does not count.

## 8. Skipped-test rule

A required critical test that is skipped makes the capability:

```text
PARTIALLY_VERIFIED
or
BLOCKED
```

depending on cause.

It cannot remain VERIFIED/STABLE.

Provider-specific tests may be `NOT_APPLICABLE` only when provider capability itself is outside the declared scope.

## 9. Flaky-test rule

A flaky critical Billing test blocks D5 until the cause is fixed.

Re-running until green is not certification evidence.

The certification record must note:

```text
number of attempts
failure signature
fix/decision
final deterministic run
```

## 10. Evidence-over-intent principle

The following do NOT prove a Billing capability:

```text
Domain class exists
EF table exists
frontend mock/stories render
provider enum contains Stripe/PayPal
README says payment is supported
test source exists but was not executed
CI green where Billing jobs were skipped
```

Only candidate-matched executable evidence can produce D4/D5.

# Baseline source-audit snapshot — NOT certification

## 11. Baseline purpose

This section captures what was found while auditing `develop` at:

```text
35702d0fa9fb01ed68b0667bab500030d60bd028
```

It is explicitly NOT a D4/D5 certification record.

The baseline is used to prevent two opposite errors:

1. rebuilding valid work that already exists;
2. claiming broad Billing completeness because foundational models exist.

## 12. Baseline source summary

Audited shape:

```text
Billing Domain files                         substantial
Billing Application feature files           thin
Billing Infrastructure/data                 substantial persistence/RLS foundation
Billing API endpoints                       none found
Billing frontend                            prototype/hard-coded commercial data
provider adapter                            none proven
provider reconciliation                     none proven
financial payment transaction               absent
hard-capacity reference flow                implemented with strong source/test evidence
```

## 13. Baseline CI limitation

At the audited HEAD, the repository workflow for that commit was documentation-selected and backend CI execution was skipped.

Therefore the following statement is prohibited:

```text
"Billing is verified because HEAD CI is green."
```

Existing Billing test source is valuable evidence of implementation intent/history but MUST be executed on the certification candidate before D4/D5.

## 14. Baseline capability snapshot

| Capability | Source-audit assessment | Certification status |
|---|---|---|
| Catalog/Plan model | model exists, authority/version semantics incomplete | NOT_EVALUATED |
| Account Billing bootstrap | no complete production flow proven | NOT_EVALUATED |
| Subscription lifecycle | Domain partial, duplicated tier/Workspace semantics | NOT_EVALUATED |
| Entitlement model/evaluation | useful implementation exists | NOT_EVALUATED |
| Plan → Entitlement derivation | critical missing spine | NOT_EVALUATED |
| Hard resource capacity | strongest existing slice; concurrency/idempotency tests exist in source | NOT_EVALUATED |
| Metered usage | ambiguous/partial mechanisms | NOT_EVALUATED |
| Billing API | production endpoint surface absent | NOT_EVALUATED |
| Billing administration | incomplete | NOT_EVALUATED |
| Provider gateway | not proven | NOT_EVALUATED |
| Verified webhook inbox | not proven | NOT_EVALUATED |
| Reconciliation | not proven | NOT_EVALUATED |
| Invoice model | Domain partial | NOT_EVALUATED |
| PaymentTransaction | absent | NOT_EVALUATED |
| PaymentMethod | model exists but Workspace-rooted | NOT_EVALUATED |
| Billing frontend | prototype/hard-coded plan data | NOT_EVALUATED |
| Billing RLS foundation | policy coverage exists in source | NOT_EVALUATED |
| Migration/hardening | target migration not executed | NOT_EVALUATED |

No baseline row is pre-certified.

# Certification scope model

## 15. Scope A — Internal commercial core

Scope A contains:

```text
commercial catalog
Account Billing bootstrap
Subscription lifecycle
Entitlement derivation/evaluation
product capability contract
hard resource capacity
metered usage only if released
Billing read API
Billing authorization boundary
migration/RLS/security for those capabilities
```

Scope A does not require an external payment provider.

This allows Notrelix to have a stable internal entitlement system before checkout/payment provider integration is selected.

## 16. Scope B — External provider Billing

Scope B contains:

```text
provider customer mapping
checkout/portal
provider subscription mutation
BillingOperation
Unknown outcome
webhook verification/inbox
provider reconciliation
```

Scope B requires a selected production provider and provider-specific evidence.

## 17. Scope C — Financial evidence

Scope C contains:

```text
Invoice
PaymentTransaction
PaymentMethod
provider financial reconciliation
retention
financial API/frontend
```

A production paid-Billing release normally requires Scope B + C.

## 18. Scope D — Billing frontend

Scope D contains:

```text
server-driven plan catalog
current Subscription
upgrade/change/cancel/resume
usage
invoice/payment views
payment-method/portal flows where enabled
pending/unknown UX
Account-switch isolation
```

Frontend certification cannot exceed backend scope.

## 19. Scope relationship

```text
Scope A
  ↓
Scope B when external billing enabled
  ↓
Scope C for financial release

Scope D depends on whichever backend scope it exposes
```

A release may certify Scope A to D5 while Scope B/C remain `BLOCKED` due to provider selection.

It MUST describe itself as internal commercial/entitlement readiness, not “payments complete”.

# Milestone A — Entitlement Core Certification

## 20. Purpose

This is the first downstream-enabling gate.

It exists so WorkManagement/Documents/Automation/other product teams can consume stable Billing capability decisions without waiting for provider/payment completion.

## 21. Core gate scope

Required:

```text
catalog authority
Account Subscription/bootstrap
Plan → Entitlement derivation
Entitlement precedence/lineage
capability public contract
hard-capacity reference slice
Account isolation
architecture boundary
migration from legacy plan/tier authorities
```

Metered usage is required only for consumers that depend on it.

Provider/payment is not required.

## 22. CORE-CERT-001 — Commercial catalog

Requirements:

```text
BILREQ001–009
BILREQ112
```

Evidence:

```text
SRC DOM APP DB MIG API/GEN ARCH
```

D4 conditions:

- stable PlanCode/revision identity;
- at most one current/sellable revision per PlanCode;
- exact PlanPrice authority;
- current Free revision has exactly one active default/bootstrap PlanPrice;
- PlanCapability has explicit Unit + LimitAggregationScope and semantic uniqueness;
- cross-Plan transition classification is Billing-owned;
- same-Plan offer change is explicitly classified;
- `Plan.Period` is not billing authority;
- historical PlanPrice/Plan revision preserved;
- catalog query works.

D5 adds removal of legacy price/period authority, complete transition policy and verified migration.

Initial status: `NOT_EVALUATED`.

## 23. CORE-CERT-002 — Account Billing bootstrap

Requirements:

```text
BILREQ009–011
BILREQ089
BILREQ104–105
```

Required evidence:

```text
APP
INT
DB
CONC
```

D4 conditions:

```text
AccountCreated
→ exactly one effective default/free Subscription
→ Entitlement reconciliation
```

Replay/concurrency safe; no provider dependency.

Initial status: `NOT_EVALUATED`.

## 24. CORE-CERT-003 — Subscription lifecycle

Requirements:

```text
BILREQ005
BILREQ012–023
BILREQ113
```

Evidence:

```text
DOM APP INT DB ARCH MIG REL
```

D4 conditions:

- Account-rooted Subscription pinned to exact `PlanPriceId`;
- no Workspace/direct PlanId/tier/Plan.Period competing authority;
- Trial is outside first certified closure;
- terminal activation failure does not leave stuck PendingActivation;
- change command selects exact TargetPlanPriceId;
- transition kind/timing comes from Billing policy;
- provider-backed upgrade only after confirmed success;
- downgrade/offer change occurs at approved boundary;
- normal paid cancellation falls back to default Free offer;
- time-driven transitions have idempotent lifecycle-worker execution;
- no consumer tier authority.

D5 adds complete legacy state/tier migration and stable lifecycle/reconciliation evidence.

Initial status: `NOT_EVALUATED`.

## 25. CORE-CERT-004 — Entitlement derivation

Requirements:

```text
BILREQ024–034
BILREQ106
BILREQ115
```

Required evidence:

```text
DOM
APP
INT
DB
CONC
MIG
```

D4 conditions:

```text
Subscription + exact Plan revision
→ deterministic source-lineaged Entitlements
```

with:

- certified Subscription + Manual-only precedence;
- manual override authorization/reason/revoke/expire evidence;
- unsupported Promo/AddOn not exposed as completed behavior;
- equal-precedence ambiguity fails closed rather than newest-row fallback;
- expiry;
- replay idempotency;
- concurrent convergence;
- history preservation.

This is a hard blocker for Entitlement Core D4.

Initial status: `NOT_EVALUATED`.

## 26. CORE-CERT-005 — Product capability contract

Requirements:

```text
BILREQ025
BILREQ032–034
BILREQ052
BILREQ056
BILREQ090
```

Required evidence:

```text
APP
ARCH
INT
```

D4 conditions:

- consumers ask capability/effective-subscription facts;
- no Plan/tier knowledge needed;
- stable reason taxonomy;
- fail closed;
- permission and entitlement remain distinct.

Initial status: `NOT_EVALUATED`.

## 27. CORE-CERT-006 — Hard resource capacity

Requirements:

```text
BILREQ035–042
BILREQ107
BILREQ135
```

Evidence:

```text
DOM APP INT DB CONC ARCH SEC
```

D4 conditions:

- `WorkspaceFeatureUsage` remains concurrency owner;
- quantity is integer end-to-end;
- no decimal→int truncation;
- no authoritative `ResetPeriod/LastResetAt/Reset()` for persistent resource counts;
- PlanCapability aggregation scope is explicit;
- same-transaction resource+capacity mutation;
- logical-operation idempotency;
- last-slot and first-use races;
- non-destructive limit shrink;
- lifecycle release;
- privileged scoped capacity repair.

D5 removes/non-authoritatively isolates legacy soft/reset semantics.

Initial status: `NOT_EVALUATED`.

## 28. CORE-CERT-007 — Account isolation / RLS

Requirements:

```text
BILREQ082–084
```

Required evidence:

```text
ARCH
RLS
INT
SEC
```

D4 conditions:

- all core Billing tables classified;
- Account A cannot read/mutate Account B;
- catalog policies do not grant tenant mutation;
- missing tenant context does not become global access.

D5 adds new-table drift protection.

Initial status: `NOT_EVALUATED`.

## 29. CORE-CERT-008 — Core migration

Requirements:

```text
BILREQ109–118
```

Core-required subset:

```text
Plan.Price
PlanLimit/PlanCapability
Account.PlanCode
SubscriptionTier
WorkspaceDto.Plan
Entitlement lineage
```

Required evidence:

```text
MIG
DB
GEN
ARCH
```

D5 is blocked until authority-switch migration is complete for the released core.

Initial status: `NOT_EVALUATED`.

## 30. CORE-CERT-009 — Core architecture

Required evidence:

```text
ARCH
SRC
```

Must prove:

- Account payer boundary;
- Billing commercial ownership;
- no provider SDK in Domain/Application public contract;
- no Plan/tier comparison outside allowed Billing/migration paths;
- product contexts do not use private Billing persistence;
- Governance owns permission semantics;
- Analytics remains downstream;
- no new production service/project.

Initial status: `NOT_EVALUATED`.

## 31. CORE-CERT-010 — Core API/errors

Requirements:

```text
BILREQ052–056
BILREQ129–136
```

For the endpoints released in Scope A, required:

```text
API
GEN
INT
SEC
```

No generic arbitrary-state mutation endpoint is allowed.

Initial status: `NOT_EVALUATED`.

## 32. Entitlement Core D4 decision

The core may be declared:

```text
D4 / VERIFIED
```

only if:

```text
CORE-CERT-001 .. CORE-CERT-007 = VERIFIED or STABLE
CORE-CERT-009 = VERIFIED or STABLE
required API surface for consumers = VERIFIED
critical migration compatibility is sufficient for the candidate
```

Full legacy cleanup may still be in progress only when it cannot create competing runtime authority.

## 33. Entitlement Core D5 decision

The core may be declared:

```text
D5 / STABLE
```

only if:

- all core certification rows are STABLE;
- legacy authority migration is complete;
- downstream consumer integration passes;
- exact-SHA CI/full regression passes;
- no blocking source debt remains;
- breaking changes now require explicit coordination.

# Milestone B — Metered Usage Certification

## 34. METER-CERT-001 — Metric ownership

Requirements:

```text
BILREQ043–044
BILREQ091
```

Evidence:

```text
APP
ARCH
INT
```

Metric mapping is Billing-owned; source contexts publish business facts.

Initial status: `NOT_EVALUATED`.

## 35. METER-CERT-002 — Immutable usage ingestion

Requirements:

```text
BILREQ045
BILREQ049
BILREQ105
```

Evidence:

```text
APP
DB
INT
CONC
```

Must prove source-event replay cannot double count and corrections are append-only.

Initial status: `NOT_EVALUATED`.

## 36. METER-CERT-003 — Period semantics

Requirements:

```text
BILREQ046–048
```

Evidence:

```text
DOM
APP
INT
MIG
```

Must prove boundary, rollover, late/out-of-order and rebuild behavior.

Initial status: `NOT_EVALUATED`.

## 37. METER-CERT-004 — Capacity/meter separation

Requirements:

```text
BILREQ035
BILREQ050
BILREQ117
```

Evidence:

```text
ARCH
INT
MIG
```

No periodic meter may use the all-time hard-capacity ledger as its billing-period authority.

Initial status: `NOT_EVALUATED`.

## 38. METER-CERT-005 — Metered enforcement

Requirements:

```text
BILREQ051
```

Evidence depends on released policy.

For observe-only/soft-limit:

```text
APP + INT
```

For hard metered quota:

```text
APP + DB + CONC + INT
```

and reservation/allocation race proof is mandatory.

If no hard metered quota is released, that sub-gate is `NOT_APPLICABLE`.

## 39. Metered Usage decision

Metered usage reaches D4 only when all released metric semantics are executable and replay-safe.

It does not block Entitlement Core D5 unless current product capabilities depend on metered usage.

# Milestone C — Billing API & Administration Certification

## 40. API-CERT-001 — Account binding

Requirements:

```text
BILREQ130
```

Evidence:

```text
API
INT
SEC
```

The server determines the payer Account from trusted context.

Initial status: `NOT_EVALUATED`.

## 41. API-CERT-002 — Read surface

Requirements:

```text
BILREQ052
BILREQ129
BILREQ132
BILREQ134–136
BILREQ138
```

Evidence:

```text
API
GEN
APP
INT
```

Every released read use case has a real endpoint/handler and typed response.

Initial status: `NOT_EVALUATED`.

## 42. API-CERT-003 — Command surface and asynchronous provider result

Requirements:

```text
BILREQ053
BILREQ129
BILREQ133
BILREQ137
```

Evidence:

```text
API APP ARCH INT SEC
```

Must prove:

- no generic CRUD state mutation;
- plan change selects exact TargetPlanPriceId;
- provider-backed mutations return `202 Accepted + BillingOperationId`;
- provider completion is not claimed inside request transaction;
- operation status read is Account-bound;
- short-lived hosted action URL/result is exposed only after successful operation and is not normally logged/evented.

Initial status: `NOT_EVALUATED`.

## 43. API-CERT-004 — Governance

Requirements:

```text
BILREQ054–055
BILREQ092
BILREQ100
```

Evidence:

```text
APP
ARCH
SEC
INT
```

Must distinguish Billing admin permission from product entitlement.

Initial status: `NOT_EVALUATED`.

## 44. API-CERT-005 — Error taxonomy

Requirements:

```text
BILREQ056
```

Evidence:

```text
APP
API
FE where consumed
```

Initial status: `NOT_EVALUATED`.

## 45. API-CERT-006 — Generated contract

Requirements:

```text
BILREQ131
```

Evidence:

```text
GEN
FE
```

OpenAPI/codegen candidate is clean.

Initial status: `NOT_EVALUATED`.

# Milestone D — External Provider Certification

## 46. Provider certification prerequisite

Before provider-specific certification, record:

```text
selected provider
provider account/environment
API version
SDK version
webhook protocol/version
required secret/config mechanism
sandbox/live certification mode
```

If provider is not selected:

```text
provider-specific rows = BLOCKED or NOT_APPLICABLE
```

depending on declared release scope.

No provider is inferred from `PaymentProvider` enum values.

## 47. PRV-CERT-001 — Provider-neutral gateway

Requirements:

```text
BILREQ057
BILREQ063
```

Evidence:

```text
APP
ARCH
```

D4 requires neutral Application contracts and no SDK leakage.

Initial status: `NOT_EVALUATED`.

## 48. PRV-CERT-002 — Provider object mappings

Requirements:

```text
BILREQ058
```

Evidence:

```text
APP INF DB CONC MIG
```

Must prove provider-relative identity/uniqueness for all enabled mappings:

```text
customer
subscription
price
invoice
payment
payment method
```

Provider Subscription binding retains Account/Subscription identity and freshness watermark where supported.

Initial status: `NOT_EVALUATED`.

## 49. PRV-CERT-003 — BillingOperation and prepare/effect/settle

Requirements:

```text
BILREQ059–061
BILREQ105
BILREQ125–126
```

Evidence:

```text
DOM APP INF DB REL SEC ARCH
```

Must prove:

```text
stable logical identity
subject + immutable RequestFingerprint/target
API request TX persists BillingOperation + provider-effect intent only
provider adapter is NOT called inside request DataSession transaction
provider-effect Prepare claim commits
external Effect runs with no DB transaction
Settle TX applies normalized result
same-command replay
payload conflict
Pending/Succeeded/Failed/Unknown
provider-success + failed settle remains recoverable
no blind retry
```

Initial status: `NOT_EVALUATED`.

## 50. PRV-CERT-004 — Selected provider adapter

Requirements:

```text
BILREQ057
BILREQ059–063
BILREQ137
```

Evidence:

```text
INF
APP
REL
provider sandbox/contract evidence where applicable
```

Initial status:

```text
BLOCKED until provider selection/configuration exists
```

unless candidate has an explicit provider decision.

## 51. PRV-CERT-005 — Checkout/portal

Requirements:

```text
BILREQ062
BILREQ137
```

Evidence:

```text
API
APP
INF
E2E
REL
```

Return URL cannot be the commercial authority.

Initial status: `NOT_EVALUATED` or `BLOCKED` when no provider.

## 52. External Provider D4 decision

Provider Billing is VERIFIED only when:

- selected provider adapter passes;
- BillingOperation/Unknown outcome passes;
- idempotency passes;
- checkout/portal or other released provider commands pass;
- webhook/reconciliation Milestones E/F are also VERIFIED for provider-mutated state.

# Milestone E — Provider Webhook Certification

## 53. WHK-CERT-001 — Authenticity

Requirements:

```text
BILREQ064
BILREQ102
```

Evidence:

```text
INF
SEC
INT
```

Must cover missing/invalid/tampered/stale/replay cases according to provider protocol.

Initial status: `NOT_EVALUATED`.

## 54. WHK-CERT-002 — Durable receipt

Requirements:

```text
BILREQ065
BILREQ067
```

Evidence:

```text
INF
DB
INT
REL
```

Verified receipt must be durable before semantic completion/ack behavior required by provider contract.

Initial status: `NOT_EVALUATED`.

## 55. WHK-CERT-003 — Dedup identity

Requirements:

```text
BILREQ066
BILREQ116
```

Evidence:

```text
DB
CONC
MIG
```

Target unique identity:

```text
(Provider, ProviderEventId)
```

Initial status: `NOT_EVALUATED`.

## 56. WHK-CERT-004 — Normalization

Requirements:

```text
BILREQ068
BILREQ086
```

Evidence:

```text
APP
ARCH
INT
```

Raw provider payload cannot become Domain/public event schema.

Initial status: `NOT_EVALUATED`.

## 57. WHK-CERT-005 — Out-of-order and response/webhook race safety

Requirements:

```text
BILREQ069
```

Evidence:

```text
INT REL CONC
```

Must prove:

- verified receipt does not regress state by delivery order;
- current provider-object fetch/freshness watermark prevents stale application;
- provider response settlement racing webhook reconciliation converges to one semantic transition/event;
- stale observations remain observable without reverting current state.

Initial status: `NOT_EVALUATED`.

## 58. WHK-CERT-006 — Crash/replay recovery

Requirements:

```text
BILREQ067
BILREQ127
```

Evidence:

```text
INT
REL
```

Crash after receipt but before semantic processing must remain recoverable and at-most-once semantically.

Initial status: `NOT_EVALUATED`.

# Milestone F — Reconciliation Certification

## 59. REC-CERT-001 — Unknown/stale-Pending reconciliation

Requirements:

```text
BILREQ070
BILREQ120
BILREQ124
BILREQ126
```

Evidence:

```text
APP INF INT REL
```

Must prove:

```text
Unknown selected
stale Pending without fresh claim selected
fresh Pending with active claim not stolen
crash-before-provider safely requeues same logical operation only when provider absence is proven
provider-success + failed settle converges without duplicate effect
same provider idempotency key retained
```

Initial status: `NOT_EVALUATED`.

## 60. REC-CERT-002 — Missed-webhook repair

Requirements:

```text
BILREQ070
```

Evidence:

```text
INT
REL
```

Initial status: `NOT_EVALUATED`.

## 61. REC-CERT-003 — Privileged repair

Requirements:

```text
BILREQ071
BILREQ103
```

Evidence:

```text
APP
SEC
OBS
```

Repair is explicit, Account-scoped and audited.

Initial status: `NOT_EVALUATED`.

## 62. REC-CERT-004 — Field-specific authority

Requirements:

```text
BILREQ072
```

Evidence:

```text
APP
INT
ARCH
```

Provider facts cannot overwrite unrelated internal product/commercial meaning.

Initial status: `NOT_EVALUATED`.

## 63. REC-CERT-005 — Bounded retry/backoff

Requirements:

```text
BILREQ120
```

Evidence:

```text
REL
OBS
```

No unbounded hot-loop reconciliation.

Initial status: `NOT_EVALUATED`.

# Milestone G — Financial Evidence Certification

## 64. FIN-CERT-001 — Invoice lifecycle and money evidence

Requirements:

```text
BILREQ073–074
```

Evidence:

```text
DOM APP INT DB SEC
```

Must prove:

- exact closed Invoice matrix;
- provider Invoice identity uniqueness when enabled;
- supported currency precision validation;
- line currency equals Invoice currency;
- ordinary negative charges rejected absent explicit credit model;
- provider total retained instead of silently recomputed;
- issued line snapshots remain immutable.

Initial status: `NOT_EVALUATED`.

## 65. FIN-CERT-002 — PaymentTransaction

Requirements:

```text
BILREQ075
```

Evidence:

```text
DOM
INT
DB
REL
```

One provider settlement fact maps to one normalized transaction effect.

The certified state model distinguishes:

```text
Pending
Succeeded
Failed
Canceled
Unknown
```

and Unknown settlement enters reconciliation rather than blind retry.

Initial status: `NOT_EVALUATED`.

## 66. FIN-CERT-003 — Account-rooted PaymentMethod

Requirements:

```text
BILREQ076
BILREQ078
BILREQ108
BILREQ114
```

Evidence:

```text
DOM
DB
CONC
MIG
INT
```

Initial status: `NOT_EVALUATED`.

## 67. FIN-CERT-004 — Payment-data security

Requirements:

```text
BILREQ077
BILREQ101
```

Evidence:

```text
SEC
ARCH
SRC
```

Prohibited material absent from:

```text
Domain
Application/API DTOs
DB
events/outbox
logs
frontend
```

Initial status: `NOT_EVALUATED`.

## 68. FIN-CERT-005 — Financial retention

Requirements:

```text
BILREQ079–081
```

Evidence:

```text
DB
MIG
INT
SEC/PRIV
```

Ordinary Account/Workspace/product deletion cannot accidentally erase retained evidence.

Initial status: `NOT_EVALUATED`.

## 69. Financial release decision

A release claiming paid Billing/financial history requires all applicable FIN rows VERIFIED or STABLE.

# Milestone H — Frontend Certification

## 70. FE-CERT-001 — server-driven catalog

Requirements:

```text
BILREQ095
BILREQ111
```

Evidence:

```text
FE
GEN
E2E
ARCH
```

No production hard-coded Plan/price/features authority.

Initial status: `NOT_EVALUATED`.

## 71. FE-CERT-002 — Billing source independent of Workspace.Plan

Requirements:

```text
BILREQ096
BILREQ110
```

Evidence:

```text
FE
ARCH
GEN
```

Initial status: `NOT_EVALUATED`.

## 72. FE-CERT-003 — Account cache isolation

Requirements:

```text
BILREQ097
```

Evidence:

```text
FE
E2E
```

Account switch cannot show prior payer state as valid.

Initial status: `NOT_EVALUATED`.

## 73. FE-CERT-004 — entitlement guard is UX only

Requirements:

```text
BILREQ098
```

Evidence:

```text
FE
API
INT
```

Backend denial remains authoritative.

Initial status: `NOT_EVALUATED`.

## 74. FE-CERT-005 — denial-state semantics

Requirements:

```text
BILREQ099
```

Evidence:

```text
FE
E2E
```

Distinct:

```text
permission denied
not included
capacity exhausted
inactive subscription
operation pending/unknown
provider unavailable
```

Initial status: `NOT_EVALUATED`.

## 75. FE-CERT-006 — released Billing flows

Evidence depends on release scope:

```text
view plan
upgrade/change
cancel/resume
usage
invoice
payment method/portal
```

Each claimed flow requires E2E/interaction evidence.

Unreleased flow is NOT_APPLICABLE, not implied complete.

# Cross-cutting Certification

## 76. CERT-ARCH-001 — Domain/Application purity

PASS requires:

- provider SDK absent from Domain/Application public contracts;
- raw webhook schema absent from Domain/public events;
- product contexts do not reference Billing persistence;
- no new Billing project/service boundary.

Initial status: `NOT_EVALUATED`.

## 77. CERT-ARCH-002 — one commercial authority

PASS requires no independent runtime authority from:

```text
Plan.Price
Account.PlanCode
WorkspaceDto.Plan
workspace.plan
frontend PLANS
SubscriptionTier consumer branching
```

Initial status: `NOT_EVALUATED`.

## 78. CERT-ARCH-003 — Account payer boundary

PASS requires:

- Subscription Account-rooted;
- PaymentMethod Account-rooted;
- BillingCustomer Account-rooted;
- Workspace only target/scope where explicitly defined.

Initial status: `NOT_EVALUATED`.

## 79. CERT-ARCH-004 — Governance separation

PASS requires:

- no handler-local role policy engine;
- Billing admin actions use canonical authorization;
- entitlement never substitutes for permission.

Initial status: `NOT_EVALUATED`.

## 80. CERT-ARCH-005 — capacity/meter separation

PASS requires hard-capacity stock and period metering remain separate authorities.

Initial status: `NOT_EVALUATED`.

# Data/RLS Certification

## 81. CERT-DATA-001 — schema authority

PASS requires schema/model snapshot matches target Billing model with no unreviewed pending model drift.

Initial status: `NOT_EVALUATED`.

## 82. CERT-DATA-002 — unique constraints and referential integrity

Applicable uniqueness:

```text
PlanCode + Revision
one current/sellable revision
one default PlanPrice per Plan revision
PlanCapability (PlanId, FeatureCode, LimitAggregationScope)
BillingCustomer Account + Provider
Provider + Customer/Subscription/Price/Invoice/Payment/Method IDs
logical capacity operation
meter source-event identity
period aggregate identity including NULL Workspace
Provider + ProviderEventId
single default PaymentMethod
effective Subscription policy
```

Target FK matrix:

```text
PlanPrice → Plan
PlanCapability → Plan
Subscription → PlanPrice
Invoice → Subscription when applicable
InvoiceLineItem → Invoice
PaymentTransaction → Invoice when applicable
provider binding → internal object
```

Retained commercial/financial relations default to RESTRICT/NO ACTION.

Evidence: `DB + CONC + MIG`.

Initial status: `NOT_EVALUATED`.

## 83. CERT-DATA-003 — Billing RLS coverage

PASS requires every Billing table to be explicitly classified and verified.

Mandatory evidence:

- Account-owned finance is not accidentally Workspace-scoped;
- Entitlement uses one RLS-visible Workspace target;
- child tables use parent-correlated policies;
- Account-wide Billing UI uses Account-scoped Billing projections, not worker-context bypass;
- Account Billing Admin cannot mutate global catalog;
- raw provider body is not readable by ordinary tenant or generic `support_readonly`;
- expected-table verification fails on missing/disabled/wrong policies.

Initial status: `NOT_EVALUATED`.

## 84. CERT-DATA-004 — cross-Account runtime isolation

Direct runtime-role evidence proves Account A cannot read/mutate Account B Billing.

Initial status: `NOT_EVALUATED`.

## 85. CERT-DATA-005 — worker/internal tables

Provider receipt/reconciliation internal state is inaccessible to ordinary tenant context.

Initial status: `NOT_EVALUATED`.

## 86. CERT-DATA-006 — historical retention

No destructive cascade removes retained Invoice/Payment/usage/provider evidence accidentally.

Initial status: `NOT_EVALUATED`.

# Migration Certification

## 87. CERT-MIG-001 — migration inventory

Record every migration introduced by Billing execution, its authority change and rollback/forward-fix semantics.

Initial status: `NOT_EVALUATED`.

## 88. CERT-MIG-002 — clean database

Fresh install applies:

```text
EF migrations
RLS scripts/policies
seed/bootstrap requirements
```

Initial status: `NOT_EVALUATED`.

## 89. CERT-MIG-003 — audited-baseline upgrade and deterministic classification

Upgrade is valid only when every legacy row is classified.

Required evidence:

```text
SubscriptionItem/PlanPrice deterministic backfill
multi-item and quantity!=1 blockers
legacy price/period conflict blockers
Workspace→Account validation before source columns are dropped
Incomplete/Unpaid/Trialing classification
target FK/uniqueness installation
no destructive financial cascade
```

Ambiguous rows are blockers, not silent coercions.

Initial status: `NOT_EVALUATED`.

## 90. CERT-MIG-004 — Plan price authority migration

Requirements:

```text
BILREQ112
```

No value/history loss.

Initial status: `NOT_EVALUATED`.

## 91. CERT-MIG-005 — Account.PlanCode migration

Requirements:

```text
BILREQ109
```

No capability remains dependent on legacy field.

Initial status: `NOT_EVALUATED`.

## 92. CERT-MIG-006 — Workspace.Plan contract removal

Requirements:

```text
BILREQ110
```

Backend/frontend generated contracts have no stale production dependency.

Initial status: `NOT_EVALUATED`.

## 93. CERT-MIG-007 — SubscriptionTier migration

Requirements:

```text
BILREQ113
```

No cross-context tier authority remains.

Initial status: `NOT_EVALUATED`.

## 94. CERT-MIG-008 — PaymentMethod scope migration

Requirements:

```text
BILREQ114
```

Account mapping/default integrity verified.

Initial status: `NOT_EVALUATED`.

## 95. CERT-MIG-009 — Entitlement lineage migration

Requirements:

```text
BILREQ115
```

Unknown legacy provenance remains explicit; no fabricated source.

Initial status: `NOT_EVALUATED`.

## 96. CERT-MIG-010 — provider receipt migration

Requirements:

```text
BILREQ116
```

New active dedup authority is `(Provider, ProviderEventId)`.

Initial status: `NOT_EVALUATED`.

## 97. CERT-MIG-011 — usage migration

Requirements:

```text
BILREQ117
```

Target metered totals/history reconcile before old authority retires.

Initial status: `NOT_EVALUATED`.

## 98. CERT-MIG-012 — expand/contract closure

Requirements:

```text
BILREQ118
```

No indefinite dual-write or compatibility path remains.

Initial status: `NOT_EVALUATED`.

# Security Certification

## 99. CERT-SEC-001 — payment/provider secret non-exposure

Requirements:

```text
BILREQ077
BILREQ101
```

Evidence includes source scan + negative tests + log review.

Initial status: `NOT_EVALUATED`.

## 100. CERT-SEC-002 — webhook trust

Requirements:

```text
BILREQ064
BILREQ102
```

Only provider authenticity establishes webhook trust.

Initial status: `NOT_EVALUATED`.

## 101. CERT-SEC-003 — tenant/payer spoofing

Requirements:

```text
BILREQ082
BILREQ084
BILREQ130
```

Initial status: `NOT_EVALUATED`.

## 102. CERT-SEC-004 — repair authority

Requirements:

```text
BILREQ071
BILREQ103
```

Initial status: `NOT_EVALUATED`.

## 103. CERT-SEC-005 — replay resistance

Must include:

```text
application command replay
capacity operation replay
meter event replay
provider command replay
webhook replay
semantic consumer replay
```

Initial status: `NOT_EVALUATED`.

## 104. CERT-SEC-006 — no security weakening

Certification fails if Billing implementation bypasses existing:

```text
Account context
Governance pipeline
RLS
idempotency behavior
outbox/messaging safety
secret management
```

Initial status: `NOT_EVALUATED`.

# Concurrency Certification

## 105. CERT-CONC-001 — Subscription uniqueness

Real PostgreSQL race evidence.

Initial status: `NOT_EVALUATED`.

## 106. CERT-CONC-002 — Entitlement reconciliation

Concurrent reconcile converges.

Initial status: `NOT_EVALUATED`.

## 107. CERT-CONC-003 — hard-capacity last slot

Retain current pinned real-DB evidence.

Initial status: `NOT_EVALUATED`.

## 108. CERT-CONC-004 — hard-capacity first use

Concurrent first-use row creation safe.

Initial status: `NOT_EVALUATED`.

## 109. CERT-CONC-005 — metered usage dedup

Required if metered usage released.

Initial status: `NOT_EVALUATED`.

## 110. CERT-CONC-006 — webhook receipt dedup

Required if provider enabled.

Initial status: `NOT_EVALUATED`.

## 111. CERT-CONC-007 — PaymentMethod default

Required if PaymentMethod management released.

Initial status: `NOT_EVALUATED`.

# Reliability Certification

## 112. CERT-REL-001 — provider timeout taxonomy

Requirements:

```text
BILREQ060
BILREQ124
```

Initial status: `NOT_EVALUATED`.

## 113. CERT-REL-002 — DB failure before provider

Requirements:

```text
BILREQ125
```

No false success/provider side effect.

Initial status: `NOT_EVALUATED`.

## 114. CERT-REL-003 — provider success then DB failure

Requirements:

```text
BILREQ126
```

Recoverable through Unknown/reconciliation.

Initial status: `NOT_EVALUATED`.

## 115. CERT-REL-004 — webhook crash/retry

Requirements:

```text
BILREQ127
```

Initial status: `NOT_EVALUATED`.

## 116. CERT-REL-005 — entitlement dependency failure

Requirements:

```text
BILREQ128
```

No implicit grant.

Initial status: `NOT_EVALUATED`.

## 117. CERT-REL-006 — reconciliation boundedness

No infinite tight retry.

Initial status: `NOT_EVALUATED`.

# Observability Certification

## 118. CERT-OBS-001 — critical-flow correlation

Requirements:

```text
BILREQ119
```

Trace:

```text
request
→ logical operation
→ provider reference
→ webhook/reconcile
→ Subscription
→ Entitlement
→ Invoice/Payment where relevant
```

Initial status: `NOT_EVALUATED`.

## 119. CERT-OBS-002 — reconciliation visibility

Requirements:

```text
BILREQ120
```

Initial status: `NOT_EVALUATED`.

## 120. CERT-OBS-003 — Billing metrics

Requirements:

```text
BILREQ121
```

Minimum categories documented in PLAN/TESTS.

Initial status: `NOT_EVALUATED`.

## 121. CERT-OBS-004 — safe logging

No raw secrets/provider sensitive payload.

Initial status: `NOT_EVALUATED`.

# Performance Certification

## 122. CERT-PERF-001 — capability hot path

Requirements:

```text
BILREQ122
```

No external provider call.

Initial status: `NOT_EVALUATED`.

## 123. CERT-PERF-002 — bounded entitlement/capacity queries

Requirements:

```text
BILREQ123
```

No unbounded/N+1 plan lookup.

Initial status: `NOT_EVALUATED`.

## 124. CERT-PERF-003 — cache correctness

Caching is optional; if enabled it preserves revocation/isolation semantics.

Initial status: `NOT_EVALUATED`.

## 125. CERT-PERF-004 — Billing frontend request fanout

Released page has bounded query strategy.

Initial status: `NOT_EVALUATED`.

# Cross-context Certification

## 126. CERT-X-001 — Accounts → Billing

Requirements:

```text
BILREQ089
```

Billing consumes stable Account identity/lifecycle contract, not private Accounts persistence.

Initial status: `NOT_EVALUATED`.

## 127. CERT-X-002 — Billing → Automation reference consumer

Requirements:

```text
BILREQ088
BILREQ090
```

`CreateAutomationRule` consumes Billing capability/capacity contract without Plan/tier logic.

Initial status: `NOT_EVALUATED`.

## 128. CERT-X-003 — Billing → WorkManagement/Documents/other product contexts

For every released consumer:

```text
capability contract only
no private Billing persistence
no plan-tier mapping
```

Initial status: `NOT_EVALUATED`.

## 129. CERT-X-004 — Product facts → metered usage

Required for each released meter.

Initial status: `NOT_EVALUATED`.

## 130. CERT-X-005 — Governance → Billing

Governance authorizes Billing administration; Billing owns commercial mutation.

Initial status: `NOT_EVALUATED`.

## 131. CERT-X-006 — Billing → Analytics

Requirements:

```text
BILREQ094
```

Analytics consumes derived Billing events/read models and is not billing authority.

Initial status: `NOT_EVALUATED`.

## 131A. CERT-X-006A — Platform → Billing mechanism boundary

Requirements:

```text
BILREQ093
```

Platform may provide tenancy, messaging, idempotency, scheduling, secret and transport mechanisms, but cannot own Plan/price/Entitlement/provider-commercial semantics.

Initial status: `NOT_EVALUATED`.

## 132. CERT-X-007 — frontend generated contract

Frontend consumes current Billing API contract and does not recreate backend DTO/commercial truth.

Initial status: `NOT_EVALUATED`.

# Events Certification

## 133. CERT-EVT-001 — Billing producer ownership

Requirements:

```text
BILREQ085
```

All public Billing events represent Billing-owned completed facts.

Initial status: `NOT_EVALUATED`.

## 134. CERT-EVT-002 — event identity/version and PlanPrice authority

Every public event has stable name/version/tenant scope.

Target `subscription.changed` version/equivalent carries:

```text
PreviousPlanPriceId
NewPlanPriceId
```

so same-Plan offer changes remain representable.

Legacy Plan-ID-only v1 compatibility is explicit and not silently reinterpreted.

Initial status: `NOT_EVALUATED`.

## 135. CERT-EVT-003 — payload minimization

No raw provider payload/secret/unnecessary PII.

Initial status: `NOT_EVALUATED`.

## 136. CERT-EVT-004 — consumer inventory

Requirements:

```text
BILREQ087
```

Every registered consumer is classified:

```text
implemented business consumer
observability-only
stub
obsolete
```

Initial status: `NOT_EVALUATED`.

## 137. CERT-EVT-005 — replay/idempotency

Consumer replay cannot duplicate commercial effect.

Initial status: `NOT_EVALUATED`.

## 138. CERT-EVT-006 — compatibility migration

Breaking Billing events use explicit producer/consumer migration.

Initial status: `NOT_EVALUATED`.

# API Certification

## 139. CERT-API-001 — endpoint inventory

Every released `BILREQ129–138` use case maps to a handler/endpoint or an explicit internal-only operation.

Initial status: `NOT_EVALUATED`.

## 140. CERT-API-002 — command-oriented mutation

No generic Subscription/Invoice/Payment/provider-receipt arbitrary update.

Initial status: `NOT_EVALUATED`.

## 141. CERT-API-003 — stable error taxonomy

Machine errors match SPEC/TESTS.

Initial status: `NOT_EVALUATED`.

## 142. CERT-API-004 — response minimization

No provider secret/raw payload/private persistence model.

Initial status: `NOT_EVALUATED`.

## 143. CERT-API-005 — OpenAPI

Generated schema is candidate-clean.

Initial status: `NOT_EVALUATED`.

# Frontend Certification Details

## 144. CERT-FE-001 — no hard-coded production commercial authority

Search and architecture checks are clean.

Initial status: `NOT_EVALUATED`.

## 145. CERT-FE-002 — no Workspace.Plan authority

Initial status: `NOT_EVALUATED`.

## 146. CERT-FE-003 — Account-scoped cache

Initial status: `NOT_EVALUATED`.

## 147. CERT-FE-004 — released interactions

Every claimed Billing button/action is backed by an enabled API operation and tested.

Initial status: `NOT_EVALUATED`.

## 148. CERT-FE-005 — provider return reconciliation

Where provider enabled, hosted-return flow waits for backend authoritative state.

Initial status: `NOT_EVALUATED`.

# Normative Acceptance/Test Traceability

## 149A. BILAC001–BILAC020 hard-close map

Certification MUST use the following acceptance-to-test chain. A generic evidence class without the concrete `BIL-TST-*` IDs below is insufficient for final `VERIFIED`/`STABLE`.

| Acceptance | Minimum pinned test evidence |
|---|---|
| `BILAC001` Catalog | `BIL-TST-CAT-DOM-001`, `BIL-TST-CAT-DB-001`, `BIL-TST-CAT-DB-002`, `BIL-TST-CAT-DB-003`, `BIL-TST-CAT-DB-004`, `BIL-TST-CAT-APP-003`, `BIL-TST-CAT-MIG-001`, `BIL-TST-CAT-INT-001`, `BIL-TST-SUB-DOM-001A` |
| `BILAC002` Bootstrap | `BIL-TST-SUB-INT-001`, `BIL-TST-SUB-INT-002`, `BIL-TST-SUB-CONC-001` |
| `BILAC003` Subscription | `BIL-TST-SUB-DOM-004`, `BIL-TST-SUB-DOM-005`, `BIL-TST-SUB-APP-001`, `BIL-TST-SUB-APP-002`, `BIL-TST-SUB-APP-003`, `BIL-TST-SUB-APP-004`, `BIL-TST-SUB-DOM-013`, `BIL-TST-SUB-DOM-014`, `BIL-TST-SUB-APP-005`, `BIL-TST-SUB-INT-005` |
| `BILAC004` Entitlement | `BIL-TST-ENT-APP-001`, `BIL-TST-ENT-APP-002`, `BIL-TST-ENT-APP-006`, `BIL-TST-ENT-APP-007`, `BIL-TST-ENT-APP-007A`, `BIL-TST-ENT-APP-007B`, `BIL-TST-ENT-CONC-001` |
| `BILAC005` Capacity | `BIL-TST-RCAP-INT-001`, `BIL-TST-RCAP-APP-002`, `BIL-TST-RCAP-CONC-001`, `BIL-TST-RCAP-CONC-002`, `BIL-TST-RCAP-APP-004`, `BIL-TST-RCAP-ARCH-002`, `BIL-TST-RCAP-DOM-007`, `BIL-TST-RCAP-APP-005` |
| `BILAC006` Metered usage | `BIL-TST-USG-INT-001`, `BIL-TST-USG-INT-004`, `BIL-TST-USG-INT-006`, `BIL-TST-USG-INT-003` |
| `BILAC007` Provider operation | `BIL-TST-API-CONTRACT-008`, `BIL-TST-API-SEC-002`, `BIL-TST-OP-INT-001`, `BIL-TST-OP-ARCH-001`, `BIL-TST-OP-INT-002`, `BIL-TST-OP-APP-003A`, `BIL-TST-OP-REL-001A`, `BIL-TST-OP-REL-002`, `BIL-TST-REC-APP-001`, `BIL-TST-REC-INT-000A`, `BIL-TST-REC-INT-000B`, `BIL-TST-REC-INT-001` |
| `BILAC008` Webhook | `BIL-TST-WHK-SEC-001`, `BIL-TST-WHK-SEC-002`, `BIL-TST-WHK-DB-001`, `BIL-TST-WHK-CONC-001`, `BIL-TST-WHK-INT-003`, `BIL-TST-WHK-INT-005`, `BIL-TST-WHK-CONC-002`, `BIL-TST-WHK-SEC-006`, `BIL-TST-WHK-REL-001` |
| `BILAC009` Financial evidence | `BIL-TST-INV-DOM-006`, `BIL-TST-INV-DOM-007`, `BIL-TST-INV-DB-001`, `BIL-TST-INV-DOM-008`, `BIL-TST-PAY-DOM-004`, `BIL-TST-PAY-REL-001`, `BIL-TST-PM-CONC-001`, `BIL-TST-LIFE-INT-003`, `BIL-TST-PM-SEC-001` |
| `BILAC010` Authorization | `BIL-TST-AUTHZ-APP-001`, `BIL-TST-AUTHZ-APP-002`, `BIL-TST-AUTHZ-APP-003`, `BIL-TST-AUTHZ-SEC-001`, `BIL-TST-AUTHZ-SEC-002` |
| `BILAC011` Frontend | `BIL-TST-FE-ARCH-001`, `BIL-TST-FE-ARCH-002`, `BIL-TST-FE-INT-001`, `BIL-TST-FE-CMP-002`, `BIL-TST-FE-CMP-006`, released `BIL-TST-FE-E2E-*` |
| `BILAC012` Architecture | `BIL-TST-ARCH-001` through `BIL-TST-ARCH-010` as applicable |
| `BILAC013` Data isolation | `BIL-TST-RLS-DB-001`, `BIL-TST-RLS-DB-002`, `BIL-TST-RLS-DB-003`, `BIL-TST-RLS-DB-004`, `BIL-TST-RLS-INT-000A`, `BIL-TST-RLS-SEC-001`, `BIL-TST-RLS-INT-001` through `BIL-TST-RLS-INT-007` |
| `BILAC014` Idempotency | `BIL-TST-IDEM-001` plus capability-specific replay tests |
| `BILAC015` Concurrency | `BIL-TST-CONC-001` through `BIL-TST-CONC-006` as released |
| `BILAC016` Migration | `BIL-TST-MIG-DB-001` through `BIL-TST-MIG-DB-013` as applicable, including `BIL-TST-MIG-DB-006A`, `BIL-TST-MIG-DB-006B`, `BIL-TST-MIG-DB-006C` |
| `BILAC017` Security | `BIL-TST-SEC-MASTER-001` through `BIL-TST-SEC-MASTER-005`, `BIL-TST-SEC-MATRIX-001` through `BIL-TST-SEC-MATRIX-005` |
| `BILAC018` Observability | `BIL-TST-OBS-001` through `BIL-TST-OBS-005` |
| `BILAC019` Performance | `BIL-TST-PERF-001` through `BIL-TST-PERF-004` |
| `BILAC020` CI evidence | exact-SHA/non-zero rules plus all required suite records in `CERT-CI-*` |

## 149B. Traceability rule

For final certification each capability row MUST point to:

```text
BILAC
→ BILREQ
→ BIL-TST ID(s)
→ exact command
→ selected/passed/failed/skipped counts
→ candidate SHA
```

A prose statement such as “covered by integration tests” is not final certification evidence.

# Test Evidence Requirements

## 149. Evidence source

The canonical verification IDs and scenarios are in:

```text
billing-entitlements.tests.md
```

Certification MUST reference test IDs rather than paraphrasing “tests passed”.

## 150. Test result record

Each automated record uses:

```text
Test IDs:
Command:
Candidate SHA:
Environment:
Selected:
Passed:
Failed:
Skipped:
Duration:
Artifact/log:
Result:
Notes:
```

## 151. Integration environment record

For database tests record:

```text
PostgreSQL version/container image
migration state
RLS role/context
parallelism
seed fixture
```

For provider tests record:

```text
fake/sandbox/live
provider API version
webhook signing mode
adapter version
```

## 152. Race evidence

Concurrency rows must prove actual overlap.

Record synchronization technique, for example:

```text
barrier/latch
two independent DbContexts/connections
concurrent tasks
transaction isolation
```

Sequential duplicate requests are idempotency tests, not concurrency tests.

## 153. Migration evidence

Migration evidence includes both:

```text
command succeeded
```

and semantic verification of target data/authority.

A green migration with wrong Plan price or duplicate Subscription fails certification.

# CI Certification

## 154. Candidate SHA

Record exact candidate:

```text
<fill during execution>
```

Baseline source commit is not automatically the certification candidate.

## 155. Required conceptual CI families

Final candidate must cover, directly or via equivalent repository jobs:

```text
backend build
Domain tests
Application tests
Infrastructure tests
Architecture tests
Integration tests
migration/RLS verification
frontend codegen
frontend architecture
frontend typecheck/lint
frontend tests
released Billing E2E/UI evidence
security/dependency checks applicable to candidate
documentation governance
```

## 156. CERT-CI-001 — backend build

Status: `NOT_EVALUATED`.

## 157. CERT-CI-002 — Domain tests

Status: `NOT_EVALUATED`.

## 158. CERT-CI-003 — Application tests

Status: `NOT_EVALUATED`.

## 159. CERT-CI-004 — Infrastructure tests

Status: `NOT_EVALUATED`.

## 160. CERT-CI-005 — Architecture tests

Status: `NOT_EVALUATED`.

## 161. CERT-CI-006 — Integration tests

Status: `NOT_EVALUATED`.

## 162. CERT-CI-007 — migration/RLS

Status: `NOT_EVALUATED`.

## 163. CERT-CI-008 — frontend contracts/build/tests

Status: `NOT_EVALUATED`.

## 164. CERT-CI-009 — final aggregate gate

Status: `NOT_EVALUATED`.

## 165. Exact-SHA CI rule

A final green gate is valid only if required jobs actually ran for the certification candidate.

Skipped backend/frontend jobs are recorded as skipped evidence, not inferred PASS.

# Documentation Certification

## 166. CERT-DOC-001 — SPEC alignment

Candidate behavior matches `billing-entitlements.spec.md`.

Status: `NOT_EVALUATED`.

## 167. CERT-DOC-002 — PLAN closure

Every required released PLAN work unit is:

```text
completed
blocked
or explicitly not applicable
```

No hidden deferred critical work.

Status: `NOT_EVALUATED`.

## 168. CERT-DOC-003 — TESTS alignment

Every released requirement has executable evidence mapping.

Status: `NOT_EVALUATED`.

## 169. CERT-DOC-004 — canonical product docs

`docs/product/billing.md` and architecture docs reflect final authority.

Status: `NOT_EVALUATED`.

## 170. CERT-DOC-005 — no duplicate documentation authority

Old docs do not continue instructing consumers to use retired plan/tier/workspace Plan mechanisms.

Status: `NOT_EVALUATED`.

# Source-debt Policy

## 171. Blocking debt

Examples that block D5:

```text
two commercial price authorities
Account.PlanCode still grants access
Workspace.Plan still drives Billing UI
SubscriptionTier still used by product capability policy
provider SDK leaks into Application public contract
unverified webhook mutation path
provider Unknown outcome has no reconciliation
missing RLS policy on Billing table
financial history can cascade-delete
required race test flaky/missing
critical migration not verified
```

## 172. D4-tolerable debt

A D4 capability may tolerate limited non-authoritative debt when:

- it cannot alter runtime commercial decision;
- it is explicitly tracked;
- downstream consumers do not depend on it;
- removal path is defined.

Example:

```text
legacy compatibility field still serialized for an old client
but no write path or capability decision uses it
```

Such debt blocks D5 if it remains an ambiguity risk.

## 173. Non-blocking debt

Examples:

- additional Billing admin UX polish;
- extra observability dashboards beyond required telemetry;
- unsupported second provider adapter;
- unreleased payment features outside declared scope;
- optional additional catalog display metadata.

## 174. Debt record format

```text
Debt ID:
Capability:
Source path:
Classification:
Runtime authority impact:
Security/financial impact:
Blocks D4?:
Blocks D5?:
Owner:
Removal milestone:
Evidence:
```

# Certification Stop Conditions

## 175. BIL-CERT-STOP-001 — commercial authority conflict

Stop if two sources can independently decide Plan/price/entitlement meaning.

## 176. BIL-CERT-STOP-002 — Account payer ambiguity

Stop if Workspace and Account both appear as independent payer authorities without explicit product decision.

## 177. BIL-CERT-STOP-003 — exact SHA mismatch

Stop if evidence is from a different candidate.

## 178. BIL-CERT-STOP-004 — zero-test evidence

Stop if a required command selects zero tests.

## 179. BIL-CERT-STOP-005 — architecture gate weakened

Stop if certification required deleting/broadly exempting a protective architecture test instead of fixing source.

## 180. BIL-CERT-STOP-006 — pending migration/model drift

Stop D5 if target code/schema is not represented by verified migrations.

## 181. BIL-CERT-STOP-007 — secret exposure

Stop immediately on raw payment credential/provider secret exposure.

## 182. BIL-CERT-STOP-008 — unverified webhook mutation

Stop provider release if callback can mutate Billing before authenticity verification.

## 183. BIL-CERT-STOP-009 — unsafe financial retry

Stop provider release if Unknown can cause blind duplicate commercial operation.

## 184. BIL-CERT-STOP-010 — missing reconciliation

Stop external-provider D4 if provider-mutated state has no repair path for missed/uncertain result.

## 185. BIL-CERT-STOP-011 — RLS gap

Stop affected capability if a tenant/worker Billing table lacks correct RLS classification/evidence.

## 186. BIL-CERT-STOP-012 — destructive financial migration

Stop if migration/Account deletion can erase required historical evidence.

## 187. BIL-CERT-STOP-013 — frontend commercial fork

Stop frontend D4 if production UI still owns prices/features/plan truth.

## 188. BIL-CERT-STOP-014 — provider selection invented

Stop provider-specific certification if no canonical provider decision exists.

## 189. BIL-CERT-STOP-015 — capacity regression

Stop core certification if existing last-slot/idempotency/zero-vs-unlimited guarantees regress.

## 189A. BIL-CERT-STOP-016 — Subscription commercial offer is not pinned

Stop if an active Subscription lacks one exact `PlanPriceId` or if its currency/billing interval/amount must be inferred from whichever catalog price is currently active.

## 189B. BIL-CERT-STOP-017 — Billing RLS inventory is incomplete

Stop affected certification if a Billing table is silently skipped by generic RLS helpers, child rows lack parent-correlated protection, or RLS verification only emits diagnostic rows without failing on the missing policy.

## 189C. BIL-CERT-STOP-018 — provider effect executes inside request transaction

Stop provider certification if external provider mutation runs while Application request DataSession transaction is open.

## 189D. BIL-CERT-STOP-019 — stale Pending has no reconciliation path

Stop provider certification if durable Pending operations can strand after crash/failed settle because reconciliation scans only Unknown.

## 189E. BIL-CERT-STOP-020 — commercial transition guessed

Stop Subscription certification if transition kind/timing is inferred from amount, feature count, enum order or client input.

## 189F. BIL-CERT-STOP-021 — ambiguous migration coerced

Stop migration certification if multi-item, quantity mismatch, price conflict, status ambiguity or Workspace→Account mismatch is silently converted.

## 189G. BIL-CERT-STOP-022 — raw provider body exposed to support role

Stop security certification if generic worker/internal RLS grants `support_readonly` raw provider body access.

# Core Certification Checklist

## 190. Source and ownership

- [ ] exact candidate SHA recorded
- [ ] Account is payer
- [ ] one Plan price authority
- [ ] one Subscription authority
- [ ] one Entitlement authority
- [ ] provider-specific state isolated
- [ ] no duplicate product commercial policy

## 191. Catalog

- [ ] PlanCode/revision
- [ ] immutable commercial terms
- [ ] PlanPrice authority
- [ ] PlanCapability explicit unlimited
- [ ] catalog read contract
- [ ] historical revision proof

## 192. Subscription

- [ ] Account bootstrap
- [ ] exact PlanPriceId pinned
- [ ] SubscriptionItem/Plan.Period/direct PlanId authority retired
- [ ] replay/concurrency
- [ ] closed transition matrix
- [ ] plan change orchestration
- [ ] cancel/resume/effective cancel
- [ ] downgrade non-destructive
- [ ] tier authority removed

## 193. Entitlement

- [ ] source lineage
- [ ] Subscription + Manual-only deterministic precedence
- [ ] unsupported Promo/AddOn not exposed
- [ ] manual override grant/revoke/expire audit
- [ ] ambiguous equal-precedence state fails closed
- [ ] Plan derivation
- [ ] idempotent reconciliation
- [ ] concurrent convergence
- [ ] history preserved
- [ ] fail-closed reasons

## 194. Hard capacity

- [ ] aggregate owner
- [ ] ledger idempotency
- [ ] same transaction
- [ ] last-slot race
- [ ] first-use race
- [ ] delete release
- [ ] disabled resource semantics
- [ ] limit shrink

## 195. Core API/authz

- [ ] Account binding
- [ ] read contract
- [ ] command contract
- [ ] Governance
- [ ] entitlement/permission separation
- [ ] stable errors
- [ ] OpenAPI/codegen

## 196. Core isolation/migration

- [ ] RLS
- [ ] cross-Account negative tests
- [ ] Plan.Price migrated
- [ ] Account.PlanCode no longer authority
- [ ] SubscriptionTier no longer consumer authority
- [ ] Workspace.Plan retired
- [ ] Entitlement lineage migration

## 197. Core D4/D5 decision

```text
Candidate:
Scope:
CORE-CERT-001:
CORE-CERT-002:
CORE-CERT-003:
CORE-CERT-004:
CORE-CERT-005:
CORE-CERT-006:
CORE-CERT-007:
CORE-CERT-008:
CORE-CERT-009:
CORE-CERT-010:

Readiness:
Decision:
Blocking debt:
Non-blocking debt:
Downstream teams allowed to integrate:
Evidence packet:
```

# Provider/Financial Certification Checklist

## 198. Provider operation

- [ ] selected provider recorded
- [ ] neutral gateway
- [ ] BillingCustomer mapping
- [ ] BillingOperation
- [ ] provider idempotency
- [ ] Unknown outcome
- [ ] no blind retry

## 199. Webhook

- [ ] raw-body authenticity
- [ ] timestamp/replay where applicable
- [ ] durable receipt
- [ ] `(Provider, EventId)` uniqueness
- [ ] normalized mapping
- [ ] duplicate safety
- [ ] out-of-order safety
- [ ] crash recovery

## 200. Reconciliation

- [ ] Unknown repair
- [ ] missed webhook repair
- [ ] delayed webhook harmless
- [ ] bounded retry
- [ ] privileged repair
- [ ] correlation/metrics

## 201. Financial

- [ ] Invoice closed transitions
- [ ] PaymentTransaction
- [ ] Account PaymentMethod
- [ ] single-default race
- [ ] secret non-exposure
- [ ] retention

## 202. Provider/financial decision

```text
Candidate:
Selected provider:
Provider environment:
Scope:
PRV-CERT-*:
WHK-CERT-*:
REC-CERT-*:
FIN-CERT-*:

Readiness:
Decision:
Blocking debt:
Evidence packet:
```

# Frontend Certification Checklist

## 203. Commercial authority

- [ ] no production hard-coded Plan catalog
- [ ] no `workspace.plan` current-plan authority
- [ ] generated Billing contracts
- [ ] Account-scoped query keys/cache

## 204. UX

- [ ] current plan/subscription
- [ ] available plans
- [ ] usage
- [ ] invoices
- [ ] change/upgrade where released
- [ ] cancel/resume
- [ ] pending/unknown provider state
- [ ] permission vs entitlement denial distinction

## 205. Frontend decision

```text
Candidate:
Backend API candidate:
Released flows:
FE-CERT-001:
FE-CERT-002:
FE-CERT-003:
FE-CERT-004:
FE-CERT-005:
FE-CERT-006:

Readiness:
Decision:
Evidence:
```

# Full Workstream Certification Record

## 206. Candidate identity

```text
Repository:
Branch:
Candidate SHA:
Date:
Evaluator:
Migration set:
Provider mode:
Selected provider:
Frontend artifact/SHA:
```

## 207. Capability matrix template

| Capability | Target | Status | Readiness | Evidence | Blocker/debt |
|---|---:|---|---:|---|---|
| Commercial catalog | D5 | NOT_EVALUATED | D0–D3 until verified | | |
| Account Billing bootstrap | D5 | NOT_EVALUATED | | | |
| Subscription lifecycle | D5 | NOT_EVALUATED | | | |
| Entitlement derivation | D5 | NOT_EVALUATED | | | |
| Capability public contract | D5 | NOT_EVALUATED | | | |
| Hard resource capacity | D5 | NOT_EVALUATED | | | |
| Metered usage | D4+/D5 if released | NOT_EVALUATED | | | |
| Billing API | D4+ | NOT_EVALUATED | | | |
| Billing authorization | D5 for released admin surface | NOT_EVALUATED | | | |
| Provider gateway | D4+ if released | NOT_EVALUATED/BLOCKED | | | |
| Webhook | D4+ if provider enabled | NOT_EVALUATED/BLOCKED | | | |
| Reconciliation | D4+ if provider enabled | NOT_EVALUATED/BLOCKED | | | |
| Invoice | D4+ if financial release | NOT_EVALUATED | | | |
| PaymentTransaction | D4+ if financial release | NOT_EVALUATED | | | |
| PaymentMethod | D4+ if released | NOT_EVALUATED | | | |
| Frontend | D4+ for released flows | NOT_EVALUATED | | | |
| RLS/tenant isolation | D5 | NOT_EVALUATED | | | |
| Migration | D5 | NOT_EVALUATED | | | |
| Security | D5 | NOT_EVALUATED | | | |
| Reliability | D4+/D5 | NOT_EVALUATED | | | |
| Observability | D4+ | NOT_EVALUATED | | | |
| Performance | D4+ | NOT_EVALUATED | | | |
| Events/contracts | D4+/D5 | NOT_EVALUATED | | | |
| CI/generated contracts | D5 | NOT_EVALUATED | | | |

## 208. Required evidence packet

Attach/reference:

```text
candidate source diff
source classification closure
migration files
schema/model snapshot
RLS policy verification
test result logs
race-test logs/results
OpenAPI/codegen diff
frontend test/E2E artifacts
provider contract/sandbox results where applicable
security scan results
observability evidence
debt register
canonical docs diff
```

## 209. Final decision values

Allowed final decisions:

### NOT_CERTIFIED

Critical scope is incomplete/failed/not evaluated.

### CORE_VERIFIED

Internal Entitlement Core is D4.

Downstream integration may begin against declared stable-enough contract, but broad D5 freeze has not been reached.

### CORE_STABLE

Internal Entitlement Core is D5.

Provider/payment may still be blocked/not applicable.

### PROVIDER_VERIFIED

External provider flows are D4 for selected provider.

### FINANCIAL_VERIFIED

Released Invoice/Payment/PaymentMethod flows are D4.

### FULL_SCOPE_STABLE

All declared release scope is D5 or explicitly NOT_APPLICABLE without contradicting the release claim.

Do not use a single “PASS” label without the declared scope.

## 210. Final decision template

```text
Decision:
Candidate SHA:
Certified scope:
Readiness by scope:
  Internal commercial core:
  Metered usage:
  Billing API/admin:
  Provider:
  Financial:
  Frontend:

Downstream handoff:
Blocking debt:
Non-blocking debt:
Provider limitations:
Migration limitations:
Security limitations:
Evidence packet:
Next recertification trigger:
```

# Handoff Rules

## 211. Handoff to product capability consumers

WorkManagement/Documents/Automation may integrate against Billing capability semantics when:

```text
Entitlement Core >= D4
```

They do not need provider/payment D4.

## 212. Handoff to broad product development

Broad dependency on Billing reaches safe stable foundation when:

```text
Entitlement Core = D5
```

Breaking changes then require cross-team compatibility coordination.

## 213. Handoff to frontend Billing

Frontend read-only plan/current subscription work may harden when API/catalog are D4.

Mutation/provider flows require their corresponding backend/provider rows D4.

## 214. Handoff to Analytics

Analytics may consume a Billing fact when:

```text
source semantic = D5
public event/reporting contract >= D4
```

Analytics cannot certify Billing correctness.

## 215. Handoff to production paid Billing

Paid provider-backed release requires at least:

```text
Core commercial semantics D5
Billing admin/API D4+
selected provider operation D4+
webhook D4+
reconciliation D4+
financial evidence D4+
security/RLS/migration D5 for affected paths
frontend released flows D4+
```

# D5 Stability Rules

## 216. Breaking catalog change after D5

A change to:

```text
PlanCode meaning
revision semantics
price authority
PlanCapability meaning
```

invalidates affected catalog/Subscription/Entitlement certification until compatibility is proven.

## 217. Breaking Entitlement contract after D5

A change to:

```text
capability code
precedence
scope
limit/unlimited interpretation
reason taxonomy
```

requires downstream coordination and recertification.

## 218. Capacity change after D5

Changes to:

```text
WorkspaceFeatureUsage
logical operation identity
ledger semantics
last-slot protocol
```

require rerunning capacity concurrency/integration certification.

## 219. Metered usage change after D5

Changes to metric identity/period/correction/idempotency require meter recertification.

## 220. Provider change

New provider or provider API/webhook version requires provider/webhook/reconciliation recertification.

Core Entitlement certification is not automatically invalidated if provider mapping remains isolated.

## 221. RLS/schema change

Any Billing table/policy/key migration affecting certified capability invalidates affected data/security certification until rerun.

## 222. Frontend contract change

Billing API/OpenAPI changes require generated-contract and released-flow frontend recertification.

# Recertification Triggers

## 223. Mandatory recertification

Trigger when any of the following changes:

```text
Plan/PlanPrice/PlanCapability semantics
Subscription state machine
Entitlement precedence/lineage
capacity concurrency
meter period/idempotency
Billing public API/errors
billing-admin authorization
provider adapter/API version
webhook verification
reconciliation
Invoice/PaymentTransaction
PaymentMethod ownership/default semantics
Billing RLS
Billing migration
public Billing events
frontend commercial authority/cache
```

## 224. Partial recertification

A change may use targeted recertification only if:

- ownership boundary is unchanged;
- no upstream/downstream contract changes;
- exact affected capability can be isolated;
- final aggregate CI still passes.

## 225. Full recertification

Required for:

```text
payer boundary change
commercial authority change
Entitlement protocol change
provider replacement
major schema rewrite
RLS policy model change
cross-context contract break
```

# What does not count as Done

## 226. Domain-model-only completion

Not sufficient:

```text
Plan/Subscription/Entitlement classes exist
```

without executable commercial flow.

## 227. UI-only completion

A Billing page with hard-coded plans/prices is not Billing completion.

## 228. Table-only completion

Invoice/payment tables without lifecycle/provider/reconciliation are not financial completion.

## 229. Enum-only provider support

`PaymentProvider.Stripe/PayPal` does not mean either is integrated.

## 230. Webhook endpoint without verification

Does not count.

## 231. Green documentation CI

Does not prove backend Billing behavior.

## 232. Unit-only concurrency proof

Does not prove database last-slot/default/dedup races.

## 233. Application query filter without RLS proof

Does not prove tenant isolation.

## 234. Provider success happy path only

Does not prove financial reliability.

Unknown outcome and reconciliation are mandatory for provider certification.

# Final Definition of Done

## 235. Internal commercial core Done

Internal Billing core is Done when:

```text
catalog D5
Account bootstrap D5
Subscription D5
Entitlement derivation D5
capability contract D5
hard capacity D5
RLS/security/migration D5
required core API D4+
cross-context reference consumer verified
CI exact-SHA green
```

Provider/payment may remain out of scope.

## 236. Metered usage Done

Metered usage is Done when all released meters have:

```text
Billing-owned metric mapping
immutable event identity
idempotency
period semantics
late/out-of-order behavior
correction
rebuild
enforcement protocol where applicable
```

## 237. Provider Billing Done

Provider Billing is Done when selected provider has:

```text
neutral gateway
durable BillingOperation
provider idempotency
Unknown outcome
verified webhook receipt
normalized events
reconciliation
failure-path evidence
```

## 238. Financial Billing Done

Financial scope is Done when:

```text
Invoice
PaymentTransaction
Account-rooted PaymentMethod
settlement/provider reconciliation
retention
payment-data security
```

are verified for the declared release.

## 239. Billing frontend Done

Frontend is Done when:

```text
server-driven catalog/current subscription
Account cache isolation
no workspace.plan authority
no hard-coded production commercial data
correct denial UX
pending/unknown reconciliation UX
released actions backed by verified backend flows
```

## 240. Full Billing & Entitlements Done

Full workstream is `FULL_SCOPE_STABLE` only when every capability claimed by the product release is D5, or explicitly `NOT_APPLICABLE` without contradicting that release.

The final causal chain must be proven:

```text
Account
→ Subscription
→ immutable Plan revision
→ derived Entitlements
→ capability / hard capacity / metered usage
→ authorized Billing operation
→ provider interaction where enabled
→ verified webhook + reconciliation
→ retained Invoice/Payment evidence
→ data-driven frontend
```

## 241. Final certification rule

No evaluator may infer completion from architecture sophistication or source volume.

The only valid certification is:

```text
exact candidate
+
declared scope
+
non-zero executable evidence
+
migration/RLS/security proof
+
no blocking authority conflict
+
explicit provider limitations
```

Everything else remains `NOT_EVALUATED`, `PARTIALLY_VERIFIED`, `BLOCKED`, `FAILED` or `NOT_APPLICABLE`.
