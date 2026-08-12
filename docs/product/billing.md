---
document_id: PROD-BILLING
document_type: product-context
status: active
owner: billing
applies_to:
  - billing
  - plans
  - subscriptions
  - entitlements
  - usage
  - invoices
  - payment-methods
  - billing-customers
evidence:
  - PRODUCT.md
  - docs/product/product-model.md
  - docs/product/product-experience.md
  - docs/product/contexts/accounts.md
  - docs/product/contexts/governance.md
  - docs/product/contexts/work-management.md
  - docs/product/contexts/automation.md
  - docs/product/contexts/integrations.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - backend/src/Notrelix.Domain/Billing/
  - backend/tests/
  - frontend/packages/features/billing/
review_on:
  - plan-catalog-change
  - subscription-lifecycle-change
  - entitlement-model-change
  - usage-metering-change
  - invoice-or-payment-model-change
  - provider-commercial-integration-change
  - billing-customer-scope-change
  - downgrade-or-grace-policy-change
  - billing-retention-change
---

# Billing Context

> **Billing owns the commercial contract of Notrelix: global Plan definitions, Account/customer subscription state, entitlements and limits, billable usage, invoices/payment references, and canonical commercial lifecycle.**
>
> Product contexts consume Billing facts. They do not become Billing authorities.

This document is the canonical product owner for Billing semantics.

Billing does not own Account identity, Workspace membership, resource authorization, Work Management state, Documents content, Automation rules, or provider SDK models.

---

# 1. Mission

Billing answers product-commercial questions such as:

```text
Which commercial customer/account is billed?
Which Plan/catalog product is available?
Which Subscription is currently effective?
Which features/limits are entitled?
How much billable Usage has been accepted?
Which Invoice/payment records exist?
What happens on trial, renewal, past-due, downgrade, cancellation, or expiry?
```

It keeps these questions out of scattered feature flags and provider-specific conditionals.

---

# 2. Owns

Billing owns product semantics for:

```text
BillingCustomer
Plan
PlanPrice
PlanLimit
FeatureCode
Subscription
SubscriptionItem
SubscriptionTier
Entitlement
Entitlement source/status/target scope
UsageMetric
UsageMetricHistory
FeatureUsageLedger
WorkspaceFeatureUsage
UsagePeriod
Invoice
InvoiceLineItem
PaymentMethod safe metadata/reference
canonical commercial lifecycle
provider-commercial mapping identifiers
```

Current source has dedicated `Customers`, `Plans`, `Subscriptions`, `Entitlements`, `Usage`, `Payments`, and `BillingEvents`.

---

# 3. Does not own

```text
Account lifecycle/enterprise administration
→ Accounts

Workspace lifecycle/membership
→ Workspaces

resource permission/sharing
→ Governance

product-resource state
→ owning product contexts

provider secret/storage/webhook transport
→ Infrastructure/Integrations

user authentication
→ Identity
```

---

# 4. Ubiquitous language

**Billing Customer** — Billing-side commercial identity mapped to a stable Notrelix Account/customer scope.

**Plan** — global commercial catalog definition.

**Plan Price** — commercial pricing identity/amount/period associated with a Plan.

**Plan Limit** — canonical feature/quantity limit defined by Plan catalog.

**Subscription** — Account/customer commercial agreement/lifecycle referencing Plan/product-price identities.

**Entitlement** — effective capability/limit contract granted to a target scope.

**Usage Metric** — defined billable/commercial measurement.

**Usage Record/Ledger** — accepted usage fact/history.

**Invoice** — commercial charge statement/evidence.

**Payment Method** — safe provider-backed payment instrument reference/metadata, never raw card secrets.

---

# 5. BIL-001 — Plan is a global catalog

Plan is not duplicated per Workspace merely to represent a Subscription.

A Subscription references stable Plan/product-price identities and an effective entitlement set/version.

---

# 6. Plan identity

Plan identity survives:

- display-name change;
- description change;
- price changes through explicit pricing semantics;
- deprecation/archive.

Do not use plan display name such as `Pro` as the durable business key everywhere.

---

# 7. Current Plan evidence

Current source has `Plan`, `PlanPrice`, `PlanLimit`, `FeatureCode`, `BillingPeriod`, and `PlanStatus`.

`Plan` currently owns price/period and a collection of feature limits, and distinguishes active/archive/deprecated lifecycle.

These are implementation evidence supporting a global commercial catalog.

---

# 8. BIL-002 — Plan name is not an entitlement API

Product code must not scatter:

```text
if plan == "Pro"
if plan == "Enterprise"
```

to decide feature access.

Feature availability is resolved through canonical Entitlement/limit semantics.

---

# 9. Plan lifecycle

A Plan may be:

```text
active
deprecated
archived
```

or equivalent approved states.

Deprecating a Plan does not automatically cancel existing Subscriptions unless migration policy says so.

---

# 10. BIL-003 — Plan lifecycle and Subscription lifecycle are separate

A Plan can stop being sold while existing Subscriptions continue under it.

A Subscription can cancel while the Plan remains available globally.

---

# 11. Plan Price

Pricing changes require explicit semantics.

Possible models include:

```text
immutable price versions
new provider price identity
effective-dated pricing
grandfathered existing Subscription
```

Do not mutate historical commercial meaning silently.

---

# 12. BIL-004 — Historical price meaning is preserved

An Invoice or historical Subscription period must remain interpretable after future Plan pricing changes.

---

# 13. FeatureCode

FeatureCode is a stable commercial capability identifier.

It should represent business capability such as:

```text
automation
advanced-analytics
premium-integration
workspace-count
member-count
storage
```

without depending on frontend route/component names.

---

# 14. BIL-005 — FeatureCode is stable commercial vocabulary

Renaming a UI package or source class must not silently rename a persisted/public entitlement identity.

---

# 15. Plan Limit

A Plan Limit defines a commercial limit for one FeatureCode.

Examples:

```text
max Workspaces
max members
Automation executions
Integration connections
storage
```

The precise unit must be explicit.

---

# 16. BIL-006 — Limit has defined unit and scope

A number such as `100` is incomplete unless Billing defines:

```text
what is counted
for which Account/Workspace
over which period
whether zero means disabled or zero capacity
whether unlimited has a distinct representation
```

---

# 17. Unlimited

Do not overload an arbitrary magic integer such as `-1` for unlimited unless that is an explicitly governed representation.

Current source disallows negative Plan limits, so unlimited should use explicit product semantics rather than an undocumented negative convention.

---

# 18. Billing Customer

Billing Customer maps the Notrelix Account/customer to the commercial/provider relationship.

It is not the Account itself.

---

# 19. BIL-007 — Billing Customer does not replace Account identity

Accounts owns stable customer/organization Account identity.

Billing can retain:

```text
AccountId
provider customer ID
commercial metadata
```

without becoming Account owner.

---

# 20. Provider customer ID

Provider customer identifiers are external integration references.

They must be scoped to provider and never become the universal Notrelix Account identity.

---

# 21. Subscription

Subscription represents canonical commercial lifecycle for one Billing Customer/Account.

Current source has `Subscription`, `SubscriptionItem`, `SubscriptionStatus`, and `SubscriptionTier`.

---

# 22. BIL-008 — Subscription lifecycle is explicit

Canonical lifecycle should define transitions around concepts such as:

```text
trialing
active
past-due
cancel-at-period-end
canceled
expired
paused where supported
```

Provider-specific states are mapped into canonical Billing semantics rather than written blindly.

---

# 23. Provider state mapping

Provider webhook says what the provider observed.

Application/Billing mapping decides which canonical Subscription transition is valid.

---

# 24. BIL-009 — Provider webhook does not directly set arbitrary Subscription state

Verified provider data is input to Billing transition logic.

It is not unrestricted mutation authority.

---

# 25. Subscription period

Billing period should distinguish:

```text
current period start
current period end
renewal/effective boundary
trial boundary
cancellation effective time
```

where product semantics require them.

---

# 26. Cancel at period end

This is different from immediate cancellation.

Product access/Entitlement should remain consistent with the effective commercial period policy.

---

# 27. BIL-010 — Cancellation request and effective cancellation are distinct facts

A user can request future cancellation while Subscription remains active until the defined boundary.

---

# 28. Trial

Trial semantics must define:

- start;
- end;
- eligible capabilities;
- conversion;
- expiry;
- payment requirement;
- repeat-trial policy.

Do not model trial only as one Account status if Billing owns the commercial lifecycle.

---

# 29. Past due

Past-due state should have explicit grace/degradation policy.

Do not destroy customer work immediately because payment collection failed once.

---

# 30. BIL-011 — Provider/payment failure does not corrupt existing product state

Existing Work Management/Documents/Collaboration state remains owned by those contexts.

Billing may restrict future capability/access according to approved grace/degradation policy.

---

# 31. Subscription change

Upgrade/downgrade must define:

```text
effective time
proration/provider semantics
entitlement change timing
usage-period implications
existing-resource policy
user-visible state
```

---

# 32. Upgrade

Upgrade usually expands entitlements.

It must not bypass Governance permissions.

---

# 33. Downgrade

Downgrade can leave existing product state above new limits.

Product policy must define behavior.

---

# 34. BIL-012 — Downgrade is non-destructive by default

Prefer policy such as:

```text
block new creation
make excess resource read-only
pause premium Automation
prevent new provider sync
scheduled remediation
explicit admin action
```

over automatic deletion of customer work.

Any destructive policy requires explicit product/legal design.

---

# 35. Subscription Item

Subscription Item represents one commercial component/price/quantity inside Subscription where pricing model requires it.

It is Billing state, not direct product-resource ownership.

---

# 36. Entitlement

Current source has first-class `Entitlement`, `EntitlementSource`, `EntitlementStatus`, and `EntitlementTargetScope`.

Entitlement is the canonical answer to feature/limit availability.

---

# 37. BIL-013 — Entitlement is a business contract, not scattered feature flags

Product contexts ask Billing through stable contract:

```text
is feature X available for target scope?
what limit applies?
what usage remains?
which entitlement version/source produced this?
```

They do not hard-code Plan names.

---

# 38. Entitlement target scope

An Entitlement can apply to a defined target such as:

```text
Account
Workspace
another explicitly supported commercial scope
```

Scope must be explicit.

---

# 39. BIL-014 — Entitlement scope is not authorization scope

An Account-level entitlement may make a feature commercially available.

Governance still decides whether this principal may use/manage that feature/resource.

---

# 40. Entitlement source

Entitlement may come from:

- Plan;
- promotional override;
- trial;
- manual enterprise contract;
- migration;
- another explicitly supported source.

Source/provenance should be preserved.

---

# 41. Entitlement precedence

When several sources apply, precedence/merge semantics must be deterministic.

Do not rely on whichever row loads last.

---

# 42. BIL-015 — Entitlement resolution is deterministic and versionable

The same Account/Workspace + feature + effective time should resolve consistently from the same authoritative Billing state.

---

# 43. Entitlement status

Entitlement may be active/inactive/expired/suspended according to product semantics.

Status is Billing-local.

---

# 44. Entitlement cache

Entitlement decisions may be cached if cache key/version preserves:

```text
target scope
FeatureCode
effective entitlement version
usage period/version where limit-sensitive
```

---

# 45. BIL-016 — Entitlement cache cannot outlive authority changes silently

Subscription changes, overrides, usage exhaustion, trial expiry, or Billing corrections must invalidate/re-version affected capability decisions.

---

# 46. Usage

Current source includes:

```text
FeatureUsageLedger
UsageMetric
UsageMetricHistory
UsageMetricKey
UsagePeriod
WorkspaceFeatureUsage
```

This supports durable usage/metering semantics rather than one mutable counter only.

---

# 47. Usage Metric

A Usage Metric defines:

```text
stable metric key
unit
target scope
aggregation
period
source fact
correction behavior
```

---

# 48. BIL-017 — Billing usage metric has commercial semantics

Do not reuse arbitrary operational telemetry counters as billable truth without explicit mapping.

For example:

```text
HTTP request count
CPU seconds
queue messages
```

are not automatically customer-billable usage.

---

# 49. Usage event identity

Billable usage needs stable identity sufficient for deduplication.

Possible dimensions:

```text
Account/Workspace
metric key
source operation/resource
time/period
logical event ID
```

---

# 50. BIL-018 — Usage ingestion is idempotent

Retrying the same logical usage fact must not double-charge or double-consume quota.

---

# 51. Usage ledger

Append-oriented ledger/history is preferred when commercial correctness requires auditability/rebuild.

A derived current total can be materialized.

---

# 52. BIL-019 — Derived usage total is not the only truth

Where financial/commercial correctness matters, current totals must be reproducible from accepted usage/provider truth plus explicit corrections.

---

# 53. Usage correction

Correction should preserve history.

Examples:

```text
reversal
adjustment
credit
replacement measurement
```

Do not overwrite history silently to make a number look right.

---

# 54. Usage Period

Period semantics must define:

- start/end;
- timezone/calendar basis;
- relation to Subscription billing period;
- reset;
- late-arriving usage.

---

# 55. BIL-020 — Usage reset is period transition, not history deletion

Starting a new quota/billing period must not erase historical commercial evidence.

---

# 56. Late usage

A usage fact arriving after period close needs explicit treatment:

```text
apply to original period
adjust next invoice
reject after cutoff
provider-specific correction
```

---

# 57. Usage versus Analytics

Billing Usage answers commercial quantity/limit.

Analytics metrics answer product insight.

The same source event may feed both, but the semantic metric definitions are different.

---

# 58. BIL-021 — Billing Usage and Analytics Metric are distinct authorities

A dashboard count must not automatically become invoice quantity.

A billable usage ledger must not be edited by changing an Analytics widget.

---

# 59. Invoice

Current source has `Invoice`, `InvoiceLineItem`, and `InvoiceStatus`.

Invoice is durable commercial evidence for billed charges.

---

# 60. BIL-022 — Invoice is append/evidence-oriented commercial state

Issued/finalized invoice meaning should not be rewritten casually after the fact.

Corrections should use provider/commercial correction semantics such as:

- credit;
- adjustment;
- replacement;
- void where supported.

---

# 61. Invoice line

Invoice lines should preserve enough product/price/quantity/period meaning to interpret the invoice historically.

---

# 62. Invoice status

Status may map provider lifecycle such as:

```text
draft
open
paid
void
uncollectible
```

or canonical equivalents.

Provider-specific status must be translated deliberately.

---

# 63. Payment Method

Current source has:

```text
PaymentMethod
PaymentMethodStatus
PaymentProvider
```

Payment Method stores safe provider reference and display metadata.

---

# 64. BIL-023 — Billing Domain does not store raw card secrets

Do not persist/log:

```text
PAN
CVC
raw card token secrets
bank credentials
```

Billing can store safe metadata such as:

```text
provider reference
brand
last4
expiry display data
status
```

according to provider/security policy.

---

# 65. Payment authorization

Provider UI/tokenization handles sensitive payment collection.

Notrelix receives safe provider identifiers/results.

---

# 66. Invoice payment failure

Payment failure transitions commercial state.

It does not directly delete or mutate unrelated product resources.

---

# 67. Commercial provider boundary

Billing provider is an external provider, but commercial domain semantics belong to Billing.

Provider SDK/webhook/secret mechanics remain in Infrastructure/Integrations adapters.

---

# 68. BIL-024 — Provider identifiers are references, not Domain ownership

Stripe/other provider:

```text
customer id
subscription id
price id
invoice id
payment-method id
event id
```

remain external references to canonical Billing objects.

---

# 69. Provider webhook

Billing callbacks require:

```text
signature verification
replay protection
provider event identity
mapping
canonical transition
idempotency
```

---

# 70. BIL-025 — Commercial callbacks and commands are idempotent

Duplicate webhook/session/change delivery must not create duplicate:

```text
Subscription
Invoice
Entitlement change
Usage charge
Payment record
```

---

# 71. Out-of-order webhook

Provider events can arrive out of order.

Billing transition logic must use provider revision/time/state and canonical lifecycle constraints rather than blindly applying arrival order.

---

# 72. Provider outage

Provider outage must not corrupt previously accepted Billing state.

When current commercial certainty is unavailable, degrade according to explicit policy.

---

# 73. BIL-026 — Billing uncertainty is represented explicitly

If provider outcome is uncertain:

```text
pending
reconciling
unknown provider result
```

can be safer than pretending success/failure.

---

# 74. Checkout/change operation identity

Creating provider checkout/subscription-change sessions is an external side effect.

Stable operation identity prevents duplicate provider objects under retry.

---

# 75. Account relation

Accounts owns Account identity/lifecycle.

Billing Customer/Subscription reference Account identity.

---

# 76. BIL-027 — Account closure coordinates Billing; Billing does not own Account deletion

Closing an Account may trigger:

```text
cancel Subscription
stop future billing
retain invoices/payment evidence
resolve credits
```

Accounts still owns Account closure.

---

# 77. Workspaces relation

Workspace can be entitlement target or usage source.

Workspaces still owns Workspace lifecycle/membership.

---

# 78. Governance relation

Governance decides who may:

- view Billing;
- update plan;
- manage payment method;
- cancel Subscription;
- export invoices.

Entitlement itself does not authorize administration.

---

# 79. BIL-028 — Billing administration is separately authorized

A User benefiting from a paid feature does not automatically have permission to view invoices or change the Subscription.

---

# 80. Work Management relation

Work Management can consume entitlements/limits and emit usage facts.

Billing cannot directly delete/archive Boards/Items to enforce downgrade.

---

# 81. Automation relation

Automation can be gated by:

- Rule count;
- execution count;
- premium trigger/action types.

Billing owns limit/usage meaning; Automation owns Rule/Execution lifecycle.

---

# 82. Integrations relation

Provider connection count/provider tier may be gated.

Integrations owns Connection lifecycle.

---

# 83. Documents relation

Storage/history/export capabilities may be entitlement/usage inputs.

Billing does not own Page/Block state.

---

# 84. Collaboration relation

Member/guest/notification/storage limits may involve Billing facts, while Collaboration/Workspaces remain source owners.

---

# 85. Analytics relation

Analytics may report Billing metrics.

Analytics projection must not become Billing commercial authority.

---

# 86. Events/facts

Potential stable Billing facts include:

```text
PlanCreated/Deprecated/Archived
PlanLimitChanged
BillingCustomerCreated
SubscriptionCreated/Activated/Changed/Canceled/Expired/PastDue
EntitlementGranted/Changed/Expired/Revoked
UsageAccepted/Corrected
InvoiceIssued/Paid/Voided
PaymentMethodAdded/Removed
```

Only expose logical product facts, not raw provider callbacks.

---

# 87. BIL-029 — Billing public events are canonical commercial facts

A provider event is translated before becoming a Billing event.

Do not expose provider payload as Billing ubiquitous language.

---

# 88. Entitlement propagation

Product contexts may consume entitlement queries/events/projections.

If a projection lags, fail/degrade according to feature risk.

---

# 89. BIL-030 — Security-sensitive entitlement failure never grants capability accidentally

If entitlement is required and current authoritative determination is unavailable, do not default to unlimited/paid access unless explicit grace policy says so.

---

# 90. Grace policy

Grace can preserve availability during:

- provider outage;
- short past-due period;
- delayed webhook;
- entitlement projection lag.

It must be explicit and bounded.

---

# 91. Commercial limits and existing data

Limits primarily govern future operations.

Examples:

```text
cannot create more Workspaces
cannot add new members
cannot execute new premium automation
cannot connect another integration
```

Existing data handling needs separate safe policy.

---

# 92. Usage limit enforcement

Before limit-consuming action, Application can query Billing limit/current usage.

After successful action, accepted usage is recorded idempotently according to the metric contract.

---

# 93. BIL-031 — Usage is recorded from successful source facts

A failed or rolled-back product operation must not consume billable usage unless the commercial metric explicitly counts attempts.

---

# 94. Reservation versus post-commit usage

For hard quotas with concurrency, a reservation/allocation model may be required.

The product must explicitly define:

```text
check
reserve
commit/release
```

rather than assuming a race-prone `count < limit`.

---

# 95. BIL-032 — Hard quota concurrency is designed explicitly

If exceeding a limit is forbidden, concurrent operations must not both pass a stale count and exceed the contract unintentionally.

---

# 96. Invoice/export privacy

Billing surfaces contain commercial/financial metadata.

Governance access and audit may be stricter than ordinary Workspace content.

---

# 97. Retention

Invoice/payment/usage evidence may require longer retention than product resources.

Account/User deletion does not automatically cascade commercial records.

---

# 98. BIL-033 — Commercial retention is independent of ordinary product deletion

Retention follows legal/commercial/privacy policy.

ORM cascade is not the authority.

---

# 99. Anonymization

Where personal data can be removed while commercial evidence is retained, keep only legally/product-required references and safe metadata.

---

# 100. Frontend Billing UX

Billing experience must distinguish:

```text
current Plan
Subscription status
effective next change
entitlement
usage/limit
payment issue
invoice
authorization
```

Do not show every restriction as “upgrade required”.

---

# 101. Permission versus entitlement UX

```text
Not entitled
≠
Not authorized
```

These states require different next actions.

---

# 102. Downgrade UX

Before downgrade, communicate:

- effective time;
- which capabilities become unavailable;
- current over-limit resources;
- whether anything becomes read-only/paused;
- what requires admin action.

---

# 103. Provider pending UX

Do not display final Subscription success before provider/Billing reconciliation is complete.

---

# 104. Current source alignment

Current Billing Domain contains:

```text
BillingEvents
Common
Customers
Entitlements
Payments
Plans
Rules
Subscriptions
Usage
```

Specific current source includes:

```text
Plan
PlanPrice
PlanLimit
FeatureCode
Subscription
SubscriptionItem
SubscriptionTier
Entitlement
EntitlementTargetScope
UsageMetric
UsageMetricHistory
FeatureUsageLedger
WorkspaceFeatureUsage
Invoice
InvoiceLineItem
PaymentMethod
BillingCustomer
```

This supports a broad commercial domain, not a thin provider wrapper.

---

# 105. Current ambiguity watch

Do not normalize:

```text
Account.PlanCode
→ Billing source of truth outside Billing

AccountStatus.Trialing
→ Subscription lifecycle automatically

Plan display name
→ entitlement decision

provider customer ID
→ Account identity

Analytics metric
→ billable usage

mutable usage counter
→ audit-safe commercial ledger

provider invoice object
→ Billing Domain model directly
```

---

# 106. Change impact — Plan/FeatureCode

Review:

```text
Entitlements
Subscriptions
frontend plan display
all gated product contexts
usage metrics
provider product/price mapping
migration
```

---

# 107. Change impact — Subscription lifecycle

Review:

```text
Entitlements
grace/downgrade
Account relationship
frontend
provider mapping/webhooks
retention
```

---

# 108. Change impact — Entitlement

Review every consumer:

```text
Workspaces
Governance
Work Management
Documents
Automation
Integrations
Analytics
frontend capability guards
```

---

# 109. Change impact — Usage

Review:

```text
source fact
idempotency
period
hard-quota concurrency
correction
invoice
Analytics overlap
provider reporting
```

---

# 110. Change impact — Payment/Invoice

Review:

```text
provider adapter
security/PCI boundary
commercial retention
Governance
frontend
audit
Account closure
```

---

# 111. Plan checklist

```text
[ ] stable Plan ID
[ ] stable FeatureCodes
[ ] Price/version semantics
[ ] BillingPeriod
[ ] Plan limits units
[ ] lifecycle
[ ] deprecation behavior
[ ] existing Subscription compatibility
```

---

# 112. Subscription checklist

```text
[ ] Billing Customer/Account
[ ] stable Plan/Price references
[ ] canonical status
[ ] provider mapping
[ ] billing period
[ ] trial/grace
[ ] cancel timing
[ ] upgrade/downgrade
[ ] entitlement effective time
[ ] idempotency
```

---

# 113. Entitlement checklist

```text
[ ] FeatureCode
[ ] target scope
[ ] source/provenance
[ ] value/limit
[ ] status
[ ] effective period
[ ] precedence
[ ] version/cache invalidation
[ ] permission kept separate
```

---

# 114. Usage checklist

```text
[ ] metric key
[ ] unit
[ ] source fact
[ ] target scope
[ ] logical usage identity
[ ] period
[ ] duplicate protection
[ ] hard-quota concurrency
[ ] correction semantics
[ ] reproducible total
```

---

# 115. Payment checklist

```text
[ ] provider reference only
[ ] no raw PAN/CVC/secrets
[ ] Invoice lifecycle
[ ] PaymentMethod lifecycle
[ ] provider webhook verification
[ ] replay/idempotency
[ ] retention/audit
[ ] Account authorization
```

---

# 116. Testing/evidence

Critical evidence should cover:

```text
Plan create/deprecate/archive
FeatureCode/PlanLimit uniqueness
Subscription lifecycle/transitions
cancel-at-period-end
upgrade/downgrade/grace
provider webhook replay/out-of-order
Entitlement resolution/precedence/version
usage dedup/period/correction
hard quota concurrency
Invoice/payment lifecycle
secret non-exposure
Account closure/retention
Governance administration
frontend not-entitled vs not-authorized states
```

---

# 117. Stop conditions

Stop rather than guess if:

- product handlers check plan names directly;
- Accounts becomes a second writable plan/subscription authority;
- Entitlement and Governance permission are merged;
- a provider webhook directly sets arbitrary Billing state;
- duplicate provider callback can create duplicate commercial records;
- usage counters cannot be rebuilt/audited;
- failed product operation consumes usage unintentionally;
- downgrade deletes Boards/Documents automatically;
- raw card/payment secret is persisted/logged;
- current Analytics metric is reused as billable quantity without contract;
- Account deletion cascades invoices/payment evidence;
- hard quota is enforced by race-prone stale count only.

---

# 118. Related canonical owners

```text
PRODUCT.md
docs/product/product-model.md
docs/product/product-experience.md
docs/product/contexts/accounts.md
docs/product/contexts/workspaces.md
docs/product/contexts/governance.md
docs/product/contexts/work-management.md
docs/product/contexts/automation.md
docs/product/contexts/integrations.md
docs/product/contexts/analytics.md
docs/architecture/contract-boundaries.md
docs/architecture/data-ownership-and-consistency.md
docs/architecture/events-realtime-and-delivery-boundary.md
```

---

# 119. Final Billing rule

For every Billing capability, answer:

```text
What canonical commercial fact is this?
Which Account/Billing Customer owns the commercial relationship?
Is this Plan, Subscription, Entitlement, Usage, Invoice, or Payment state?
What stable identity/version applies?
What provider fact is only external input/reference?
What feature/limit scope is affected?
How is entitlement different from authorization?
How are duplicate/replay/out-of-order callbacks handled?
How are Usage identity, period, correction, and quota concurrency handled?
What happens on trial/past-due/downgrade/cancel?
What commercial evidence must survive Account/product deletion?
```

The target is:

> **a stable, provider-independent commercial contract that makes plans, subscriptions, entitlements, limits, usage, and financial evidence explicit without turning Billing into the owner of customer work or resource authorization.**
