---
document_id: WRK-TEAM-BILLING-ENTITLEMENTS
document_type: workstream-team-spec
status: active
owner: billing-entitlements-team
applies_to:
  - billing
  - plans
  - subscriptions
  - entitlements
  - usage
  - invoices
  - payments
  - payment-methods
  - billing-administration
  - provider-webhooks
evidence:
  - docs/product/billing.md
  - docs/architecture/bounded-context-map.md
  - docs/architecture/contract-boundaries.md
  - docs/architecture/data-ownership-and-consistency.md
  - docs/architecture/events-realtime-and-delivery-boundary.md
  - docs/delivery/team-ownership.md
  - docs/workstreams/capability-map.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/domain-modeling.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/infrastructure-and-data.md
  - backend/docs/architecture/api-and-contracts.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/testing-and-quality-gates.md
  - frontend/docs/architecture/api-and-contracts.md
  - frontend/docs/architecture/state-query-mutations.md
  - frontend/docs/generated/package-boundaries.md
review_on:
  - billing-domain-change
  - plan-entitlement-change
  - subscription-lifecycle-change
  - usage-contract-change
  - payment-provider-change
  - webhook-contract-change
  - billing-authorization-change
  - billing-data-ownership-change
---

# Billing & Entitlements Workstream

## 1. Purpose

This workstream defines execution for the Billing bounded context.

Billing owns monetization semantics for:

- Plans;
- Subscriptions;
- Entitlements;
- billable Usage;
- Invoice/Payment/PaymentMethod state where implemented;
- billing administration;
- billing-provider state mapping.

The purpose of this document is to let backend/frontend teams and coding agents implement billing features without inventing:

- plan rules inside product contexts;
- a second Account lifecycle;
- provider-specific status as domain truth;
- direct payment-provider behavior in unrelated contexts;
- client-side entitlement enforcement as security;
- non-idempotent usage ingestion;
- destructive billing cascades based only on database relationships;
- hidden cross-context billing tables.

Canonical product meaning remains in:

```text
docs/product/billing.md
```

This file owns delivery decomposition and coordination only.

## 2. Core ownership

Billing owns business semantics for:

```text
Plan
Subscription
Entitlement
Usage accounting for billing
Invoice
Payment
PaymentMethod
billing provider mapping
billing administration
```

Exact aggregate/entity/value-object boundaries follow Domain source and canonical domain-modeling authority.

Do not infer aggregate-root boundaries from payment-provider object structure.

## 3. Explicit non-ownership

Billing does NOT own:

- Account lifecycle;
- Identity authentication;
- Workspace lifecycle;
- Governance permission semantics;
- WorkManagement feature semantics;
- Automation semantics;
- Integration provider-general semantics;
- Analytics metric semantics;
- generic secret storage;
- generic webhook transport;
- generic idempotency infrastructure.

Billing may consume these capabilities through explicit contracts.

## 4. Foundational monetization invariant

Feature contexts consume:

```text
entitlement decision
limit/value
usage requirement
```

They MUST NOT independently encode plan-tier logic such as:

```text
if plan == "Pro"
if plan == "Enterprise"
```

unless the canonical Billing/product contract explicitly exposes plan identity as the required business concept.

Preferred direction:

```text
Billing
→ owns Plan → Entitlement mapping
→ exposes entitlement outcome
→ product context enforces capability behavior
```

This keeps monetization changes out of unrelated Domain models.

## 5. Capability decomposition

Billing delivery is decomposed into:

```text
BIL-001 Plan/catalog model
BIL-002 Subscription lifecycle
BIL-003 Subscription transition policy
BIL-004 Entitlement model
BIL-005 Entitlement evaluation
BIL-006 Product entitlement contract
BIL-007 Usage event/record contract
BIL-008 Usage aggregation
BIL-009 Usage limit enforcement handoff
BIL-010 Billing administration
BIL-011 Payment-method lifecycle
BIL-012 Invoice lifecycle
BIL-013 Payment lifecycle
BIL-014 Provider customer/subscription mapping
BIL-015 Provider webhook verification
BIL-016 Provider webhook idempotency
BIL-017 Provider state reconciliation
BIL-018 Upgrade/downgrade/cancel
BIL-019 Trial/grace/delinquency if product-defined
BIL-020 Billing frontend
BIL-021 Billing events
BIL-022 Billing observability
BIL-023 Migration/backfill
BIL-024 Hardening/security
```

These are delivery capabilities, not independent services.

## 6. Delivery waves

### Billing Wave A — internal commercial model

```text
BIL-001 Plan/catalog
BIL-002 Subscription lifecycle
BIL-003 Subscription transition policy
BIL-004 Entitlement model
BIL-005 Entitlement evaluation
BIL-006 Product entitlement contract
```

### Billing Wave B — usage and product enforcement

```text
BIL-007 Usage contract
BIL-008 Usage aggregation
BIL-009 Usage limit enforcement handoff
```

### Billing Wave C — provider/payment integration

```text
BIL-011 Payment method
BIL-012 Invoice
BIL-013 Payment
BIL-014 Provider mapping
BIL-015 Webhook verification
BIL-016 Webhook idempotency
BIL-017 Reconciliation
```

### Billing Wave D — lifecycle and UX

```text
BIL-018 Upgrade/downgrade/cancel
BIL-019 Trial/grace/delinquency
BIL-010 Billing administration
BIL-020 Frontend
BIL-021 Events
BIL-022 Observability
```

### Billing Wave E — migration/hardening

```text
BIL-023 Migration/backfill
BIL-024 Security/hardening
```

Provider work may begin earlier, but provider state MUST NOT define the internal Billing model.

# Plan and entitlement model

## 7. Plan/catalog model (BIL-001)

### Responsibilities

Define:

- Plan identity;
- display name;
- commercial state;
- included capabilities;
- configurable limits where product-defined;
- provider-price mapping where applicable;
- availability/retirement state.

### Stable semantic rule

A Plan is an internal Billing concept.

Provider "price", "product", or SKU identifiers are external mappings.

Do not make a provider price ID the canonical Plan identity.

## 8. Plan versioning

Plan semantics may evolve.

Before changing an existing Plan define:

- whether existing subscribers retain old terms;
- whether the Plan is versioned or mutated;
- entitlement impact;
- migration behavior;
- provider-price mapping;
- frontend display behavior.

Do not silently change entitlements for existing subscriptions without explicit product semantics.

## 9. Entitlement model (BIL-004)

An entitlement represents a capability outcome owned by Billing.

Possible conceptual forms:

```text
boolean capability
numeric limit
quota
tiered limit
feature configuration
```

Exact types should remain minimal and product-backed.

Do not build a generic policy language unless product needs it.

## 10. Entitlement identity

Entitlement IDs/names should be stable enough for consumers.

Avoid:

```text
provider SKU as entitlement ID
frontend component name as entitlement ID
```

Consumer contracts should use Billing-owned identifiers.

## 11. Entitlement evaluation (BIL-005)

Evaluation may depend on:

- active subscription;
- Plan;
- subscription status;
- trial/grace state;
- account-level overrides if product-defined;
- usage state where limit-based.

Evaluation MUST be deterministic for the same canonical Billing state.

## 12. Entitlement contract

A consumer should ask conceptually:

```text
Can Account X use capability Y?
What limit applies?
What current usage applies?
```

rather than:

```text
Which Plan string does Account X have?
```

unless Plan identity itself is the intended product contract.

## 13. Entitlement caching

If entitlement results are cached:

- account scope must be explicit;
- invalidation on subscription/entitlement change must be defined;
- stale-window risk must be acceptable;
- backend enforcement must remain correct.

Do not let stale frontend entitlement data become the security/business enforcement authority.

# Subscription lifecycle

## 14. Subscription lifecycle (BIL-002)

Define canonical states according to product semantics.

Possible concepts may include:

```text
none
trialing
active
past-due
grace
cancel-scheduled
cancelled
expired
```

Exact state names follow source/product authority.

Do not mirror provider status values one-to-one without mapping.

## 15. Subscription invariants

A subscription must:

- belong to a valid billable Account;
- preserve tenant isolation;
- have internally valid state transitions;
- map to at most the supported active commercial state according to product semantics;
- not depend on Workspace ownership unless product defines workspace billing.

## 16. Transition policy (BIL-003)

Every transition must define:

- source state;
- target state;
- actor/system cause;
- provider interaction;
- entitlement effect;
- effective time;
- event;
- idempotency;
- failure/reconciliation behavior.

Examples:

```text
trial → active
active → cancel scheduled
cancel scheduled → active
active → past due
past due → active
active → cancelled
```

## 17. Effective timing

Upgrade/downgrade/cancel semantics may be:

- immediate;
- end-of-period;
- prorated;
- scheduled.

These are product/commercial rules.

Do not infer them from provider defaults.

## 18. Subscription and Account lifecycle

Account deletion/disable interaction must be explicit.

Possible questions:

```text
Does disabling Account cancel Subscription?
Does deleting Account require cancellation first?
Can historical invoices remain?
Can billing data outlive account access?
```

Do not let FK cascade or provider cleanup define the product lifecycle.

# Product entitlement consumption

## 19. Product entitlement contract (BIL-006)

Consumers may include:

- WorkManagement;
- Documents;
- Automation;
- Integrations;
- Analytics;
- Workspace administration.

Each consumer dependency should define:

```text
capability/entitlement ID
boolean/limit/value
enforcement location
frontend UX
error contract
usage reporting requirement
```

## 20. Enforcement split

Backend/Application:

```text
authoritative enforcement
```

Frontend:

```text
UX gating / explanation
```

Frontend-only gating is insufficient.

## 21. Limit enforcement

If an entitlement imposes a limit, define:

- counted resource;
- counting scope;
- current usage source;
- race behavior;
- pre-check vs transaction-time enforcement;
- over-limit behavior;
- downgrade behavior;
- existing resource behavior after limit reduction.

Example:

```text
max boards = 10
```

requires a concurrency-safe definition.

Two simultaneous creates must not both pass a stale "9/10" check if the product contract requires hard enforcement.

## 22. Consumer error contract

When entitlement blocks an operation, distinguish it from:

```text
unauthorized
forbidden by Governance
validation error
capacity/system failure
```

A stable application/API error lets frontend show correct upgrade/limit UX.

# Usage

## 23. Usage record contract (BIL-007)

A usage fact must define:

- producer context;
- Account;
- entitlement/metric identity;
- quantity;
- unit;
- occurrence time;
- idempotency identity;
- optional resource identity;
- correction/reversal semantics.

## 24. Producer ownership

The source context owns the business fact that happened.

Billing owns interpretation as billable usage.

Example:

```text
Automation
→ owns execution fact

Billing
→ owns whether that execution counts toward billable usage
```

Do not force source contexts to encode provider billing semantics.

## 25. Usage idempotency

Duplicate delivery MUST NOT double-count usage.

Usage identity must distinguish:

- same source event retried;
- different source events;
- different Account;
- correction/reversal.

## 26. Late and out-of-order usage

Define behavior for:

- delayed event;
- replay;
- event after billing period closed;
- correction;
- duplicated period aggregation.

Do not assume perfect event ordering.

## 27. Usage aggregation (BIL-008)

Aggregation must define:

- period boundary;
- timezone;
- unit;
- reset behavior;
- account scope;
- late event policy;
- correction policy;
- precision.

If Billing periods are provider-aligned, internal semantics still need explicit mapping.

## 28. Usage backfill

If usage pipeline is rebuilt or replayed:

- projection must be idempotent;
- aggregate must be repairable;
- replay window understood;
- provider already-reported state considered.

## 29. Product usage-limit handoff (BIL-009)

Product contexts may need fast current-usage checks.

Preferred models include:

- Billing entitlement/usage query;
- approved local projection;
- event-driven local limit projection.

Do not let each product context independently calculate "billing usage" from raw tables.

# Billing administration and authorization

## 30. Billing administration (BIL-010)

Administrative flows may include:

- view subscription;
- change Plan;
- cancel;
- resume;
- manage payment method;
- view invoices;
- manage billing contact;
- provider portal handoff if product-defined.

## 31. Governance boundary

Governance owns:

```text
who may administer billing
```

Billing owns:

```text
what billing operation means
```

Platform/Application owns enforcement.

## 32. Actor/account separation

A billing request must distinguish:

```text
authenticated actor
billable Account
billing authorization
```

Do not infer billing Account solely from a client-provided ID.

## 33. Frontend security

Frontend may hide/disable billing admin controls for UX.

Backend MUST reject unauthorized operations.

# Payment methods / invoices / payments

## 34. PaymentMethod lifecycle (BIL-011)

Where implemented, define:

- provider-backed reference;
- default method;
- add/update/remove;
- expired/invalid state;
- sensitive-data handling.

Raw payment credentials/card data should not be stored by Notrelix unless explicitly required and compliant.

Prefer provider tokens/references.

## 35. PaymentMethod security

Never log or expose:

- full card data;
- secret provider tokens;
- raw payment credentials.

Frontend should consume provider-safe representations only.

## 36. Invoice lifecycle (BIL-012)

Invoice semantics should define:

- internal identity;
- provider identity mapping;
- amount/currency;
- period;
- status;
- immutable historical fields;
- download/view behavior where product-defined.

Provider invoice status must be mapped into internal semantics.

## 37. Payment lifecycle (BIL-013)

Payment semantics should define:

- attempted;
- succeeded;
- failed;
- refunded if supported;
- provider mapping;
- relation to invoice/subscription.

Do not conflate a provider HTTP success response with final payment settlement unless provider contract guarantees it.

# Provider integration

## 38. Provider mapping (BIL-014)

External provider concepts may include:

```text
customer
product
price
subscription
invoice
payment method
payment intent/payment
```

Billing must map them into internal concepts.

Provider IDs are references, not canonical domain identities.

## 39. Provider abstraction depth

Do not over-generalize for multiple providers before a real second-provider requirement exists.

At the same time, keep provider-specific DTO/status logic out of Domain.

A practical boundary is:

```text
Billing Domain/Application
→ provider-neutral business semantics

Infrastructure/provider adapter
→ provider SDK/API mapping
```

## 40. Provider customer mapping

Define:

- one provider customer per Account or another explicit mapping;
- creation idempotency;
- duplicate customer reconciliation;
- migration if mapping changes.

## 41. Provider request idempotency

For provider operations that support idempotency, define stable keys for:

- subscription create/change;
- payment operation;
- customer create;
- checkout session if applicable.

Technical provider idempotency is separate from application command idempotency but should correlate safely.

# Webhooks

## 42. Webhook verification (BIL-015)

Inbound billing webhooks must verify:

- provider authenticity/signature;
- expected endpoint;
- timestamp/replay window if supported;
- raw-body requirements;
- provider event type;
- observability.

Unverified webhooks MUST NOT mutate Billing state.

## 43. Webhook idempotency (BIL-016)

Provider retries are expected.

The same provider event MUST NOT apply state changes twice.

Store/recognize provider event identity according to architecture.

## 44. Unknown events

Unknown/unhandled events should:

- not crash the whole webhook pipeline;
- be observable;
- be safely ignored or retained according to policy.

## 45. Event ordering

Provider webhook ordering may not be guaranteed.

Billing MUST NOT assume:

```text
subscription.updated
always arrives before
invoice.paid
```

unless provider guarantees it.

Use provider timestamps/version/state reconciliation where required.

# Reconciliation

## 46. Provider state reconciliation (BIL-017)

Webhooks alone may be insufficient.

Define reconciliation for cases such as:

- webhook missed;
- webhook delayed;
- local DB update failed after provider success;
- provider API succeeded but request timed out;
- duplicate customer/subscription;
- state divergence detected.

## 47. Source of truth

Internal Billing state is canonical for Notrelix behavior, but provider state may be authoritative for external financial settlement.

The mapping/reconciliation rule must be explicit.

Do not treat one side as universally authoritative for every field.

## 48. Repair operations

Operational tooling may need:

- refetch provider state;
- replay webhook;
- reconcile subscription;
- repair mapping;
- regenerate entitlement projection.

Repair operations require strong authorization and auditability.

# Upgrade / downgrade / cancel

## 49. Upgrade (BIL-018)

Define:

- effective time;
- prorating;
- immediate entitlement changes;
- provider operation;
- idempotency;
- failure;
- frontend confirmation.

## 50. Downgrade

Define:

- immediate vs period-end;
- resource/usage above new limits;
- entitlement transition;
- scheduled state;
- cancellation of pending downgrade;
- provider mapping.

Do not delete user data automatically merely because a lower Plan has a smaller limit unless product policy explicitly requires it.

## 51. Cancel

Define:

- immediate vs period-end;
- entitlement grace;
- data retention;
- reactivate/resume;
- provider state.

## 52. Trial/grace/delinquency (BIL-019)

If product supports these, define:

- entry condition;
- duration;
- entitlement effect;
- payment retry interaction;
- UX;
- final transition.

Do not invent a grace period because the payment provider happens to expose one.

# Billing frontend

## 53. Billing UI (BIL-020)

Frontend capabilities may include:

- current Plan;
- subscription state;
- entitlement/usage;
- upgrade/downgrade;
- cancel/resume;
- payment method;
- invoices;
- billing contact/admin.

## 54. Query/state scope

Billing queries must be Account-scoped.

Account switching MUST NOT reuse Billing state from another Account.

If frontend foundation relies on hard reset rather than Account ID in keys, that dependency must already be proven.

## 55. Sensitive UI

Do not expose:

- provider secrets;
- raw webhook data;
- hidden provider internal IDs unless needed for support/admin tooling;
- full payment credential data.

## 56. Provider redirect/portal flows

If using hosted checkout/portal:

- return URL validation;
- session state;
- Account binding;
- post-return reconciliation;
- stale browser state

must be handled.

A redirect success page is not proof that provider state committed.

# Billing events

## 57. Internal Billing events (BIL-021)

Possible Billing facts:

- SubscriptionCreated;
- SubscriptionChanged;
- SubscriptionCancelled;
- EntitlementChanged;
- UsageThresholdReached;
- InvoiceStateChanged;
- PaymentStateChanged.

Exact names/payloads follow current source/ADR authority.

## 58. Consumer ownership

Consumers may include:

- product contexts;
- Analytics;
- notification/supporting capabilities.

Billing event payloads should expose stable Billing facts, not provider-specific raw payloads.

## 59. Entitlement change propagation

When entitlements change, define:

- consumer delivery;
- cache invalidation;
- local projection update;
- eventual consistency window;
- backend enforcement behavior during transition.

# Data ownership

## 60. Billing persistence ownership

Billing owns:

- Plans;
- Subscriptions;
- Entitlements;
- Usage billing state;
- invoice/payment/payment-method references;
- provider mapping;
- webhook processing state.

## 61. Forbidden cross-context access

Product contexts MUST NOT:

- write Billing tables;
- depend on Billing EF entities;
- calculate entitlement from Billing private tables.

Billing MUST NOT:

- mutate WorkManagement/Documents/etc. private tables to enforce limits.

## 62. Historical financial data

Historical invoice/payment records may require retention independent of active product access.

Do not delete historical financial records solely because an Account is deactivated unless retention/legal/product policy explicitly says so.

# Migration and compatibility

## 63. Plan migration (BIL-023)

Changing Plan model may require:

- mapping old Plan;
- grandfathering;
- provider-price migration;
- entitlement migration;
- frontend compatibility;
- event compatibility.

## 64. Subscription migration

Define:

- existing local subscription state;
- provider mapping;
- invalid/missing mappings;
- duplicate mappings;
- repair strategy.

## 65. Entitlement migration

If entitlement identifiers/semantics change:

- enumerate consumers;
- support compatibility period if needed;
- update backend/frontend consumers;
- migrate persisted overrides/config;
- regenerate docs/contracts.

## 66. Usage migration/backfill

Usage schema changes require:

- replay strategy;
- idempotent rebuild;
- already-billed period handling;
- duplicate prevention;
- reconciliation.

## 67. Provider migration

Changing provider requires a dedicated migration/architecture plan.

Do not treat it as a normal adapter replacement because:

- subscriptions;
- payment methods;
- invoices;
- customer IDs;
- webhook history

may be externally owned state.

# Security

## 68. Security hardening (BIL-024)

Billing is security-sensitive.

Required concerns:

- least privilege;
- billing-admin authorization;
- secret protection;
- webhook verification;
- provider token protection;
- tenant isolation;
- auditability;
- no raw payment credential logging.

## 69. Rate limiting / abuse

Sensitive billing operations may require abuse controls:

- checkout creation;
- payment-method operations;
- provider callbacks;
- portal generation.

Rate limiting is a platform mechanism.

Billing owns which operation is sensitive.

## 70. Auditability

High-impact billing actions should be traceable:

- actor;
- Account;
- operation;
- old/new subscription state;
- provider correlation;
- result.

Audit logs must not include secrets.

# Observability

## 71. Critical signals (BIL-022)

Measure:

- subscription transition success/failure;
- provider API latency/failure;
- webhook verification failure;
- webhook lag;
- reconciliation mismatch;
- duplicate events;
- usage ingestion lag;
- entitlement evaluation errors;
- payment failures.

## 72. Correlation

A billing flow should be traceable across:

```text
user request
→ Billing command
→ provider request
→ provider webhook
→ reconciliation
→ entitlement update
```

without exposing secret payloads.

# Performance and scalability

## 73. Entitlement evaluation performance

Entitlement checks may sit on hot product paths.

Measure:

- evaluation latency;
- cache hit/miss;
- invalidation lag;
- DB query count.

Optimize without sacrificing correctness or tenant isolation.

## 74. Usage ingestion scalability

Measure:

- usage events/sec;
- duplicate rate;
- aggregation lag;
- replay throughput;
- per-Account hot spots.

Avoid global locks or single-tenant contention across all Accounts.

## 75. Provider webhook throughput

Webhook handlers should:

- verify quickly;
- persist safely;
- defer heavy work where architecture permits;
- remain idempotent;
- expose backlog/lag.

# Testing

## 76. Domain tests

Cover:

- Plan invariants;
- Subscription transitions;
- Entitlement evaluation;
- trial/grace/delinquency if modeled;
- invalid transitions;
- usage semantics where Domain-owned.

## 77. Application tests

Cover:

- commands/queries;
- authorization;
- Account association;
- entitlement checks;
- idempotency;
- provider failure mapping;
- transition policy;
- usage ingestion.

## 78. Infrastructure tests

Cover:

- persistence mappings;
- provider adapter mapping;
- webhook verification;
- webhook idempotency;
- provider request idempotency;
- secret handling;
- migration compatibility;
- reconciliation.

## 79. API tests

Cover:

- billing admin endpoints;
- unauthorized vs forbidden;
- entitlement API/contract;
- usage API if exposed;
- provider callback/webhook;
- OpenAPI;
- validation/error mapping.

## 80. Frontend tests

Cover:

- current Plan;
- subscription status;
- usage display;
- upgrade/downgrade/cancel;
- billing admin permission;
- Account switching;
- provider redirect return;
- stale/reconciliation state.

## 81. Critical E2E — subscription

```text
authorized billing admin
→ starts subscription
→ provider operation succeeds
→ webhook/reconciliation updates Billing
→ entitlement becomes active
→ product capability becomes available
```

## 82. Critical E2E — duplicate provider event

```text
same webhook delivered twice
→ Billing state changes once effectively
→ no duplicate entitlement change
```

## 83. Critical E2E — downgrade

```text
active higher Plan
→ schedule downgrade
→ current entitlement follows product timing
→ period transition
→ new entitlement takes effect
→ over-limit resources follow defined policy
```

## 84. Critical E2E — unauthorized

```text
non-billing-admin
→ attempts subscription/payment operation
→ denied by backend
```

# Dependency readiness

## 85. Readiness matrix

| Capability | Required upstream readiness |
|---|---|
| Plan/catalog | product commercial semantics D5 |
| Subscription | Account identity D5 |
| Billing admin | Governance authz D5 |
| Entitlement | Plan/subscription model D5 |
| Product enforcement | entitlement contract D5 |
| Usage ingestion | producer event contract D4+ |
| Provider customer/subscription | provider adapter/secret mechanism D4+ |
| Webhook | verification/idempotency D5 |
| Reconciliation | provider mapping D4+ |
| Billing frontend | API/Account isolation D4+ |
| Analytics handoff | Billing event contract D4+ |

# Parallelization

## 86. Safe parallel work

After Plan/Subscription/Entitlement contracts stabilize:

- provider adapter;
- billing frontend;
- usage pipeline;
- invoice read model;
- payment-method UI;
- Analytics consumption

may proceed in parallel.

## 87. Unsafe parallelization

Do not allow independent teams to invent:

- entitlement IDs;
- subscription states;
- provider status mapping;
- usage idempotency;
- provider customer mapping;
- webhook replay semantics.

# Cross-team handoffs

## 88. Product entitlement handoff template

```text
Consumer context:
Entitlement ID:
Decision type:
Limit/value:
Enforcement point:
Frontend UX:
Usage requirement:
Current readiness:
Required readiness:
Tests:
```

## 89. Usage producer handoff template

```text
Producer context:
Business fact:
Usage metric:
Quantity:
Account scope:
Source event ID:
Idempotency:
Late-event behavior:
Correction behavior:
Tests:
```

## 90. Provider handoff template

```text
Provider operation:
Internal Billing operation:
Idempotency:
Provider customer/subscription mapping:
Expected webhook:
Failure mapping:
Reconciliation:
Secrets:
Tests:
```

# Decision authority

## 91. Team-local decisions

May decide locally:

- internal aggregate helpers;
- private provider mapper structure;
- billing UI component composition;
- local query optimization;
- test fixture design;
- measured caching preserving correctness.

## 92. Decisions requiring escalation

Escalate:

- new pricing/monetization model;
- new Plan semantics affecting product contexts;
- new entitlement type requiring cross-system contract;
- direct product-context Billing-table access;
- new payment/secret architecture;
- provider migration;
- new service boundary;
- major financial retention-policy change;
- new source of truth between provider and internal Billing.

## 93. Stop conditions

Stop and escalate when:

- Account billable-owner semantics are unclear;
- Governance cannot represent billing-admin authorization;
- provider status cannot map cleanly to internal subscription semantics;
- entitlement change requires consumer-breaking behavior without migration;
- usage identity cannot prevent double counting;
- webhook authenticity cannot be verified;
- retry can duplicate financial effects;
- downgrade data policy is undefined;
- provider/local state diverges without reconciliation strategy;
- sensitive data would need to be logged/exposed;
- service split is proposed only to simplify ownership.

# Completion criteria

## 94. Capability Definition of Done

A Billing slice is `DONE` only when:

- canonical Billing/product semantics are identified;
- Account ownership is explicit;
- authorization is server-enforced;
- internal state is provider-neutral enough to preserve domain ownership;
- provider mappings are explicit;
- idempotency prevents duplicate financial/business effects;
- entitlement contract is stable;
- usage counting is tenant-safe;
- migrations/reconciliation are handled;
- security/secret rules hold;
- observability exists;
- relevant tests pass;
- architecture gates remain green.

## 95. Billing foundation exit criteria

Billing supports broad product delivery when:

- Plan/Subscription/Entitlement semantics are stable;
- Account billing ownership is D5;
- billing-admin authorization is D5;
- product entitlement contract is D5;
- usage identity/aggregation are D4+;
- provider webhook verification/idempotency are D5;
- provider reconciliation exists;
- frontend Billing state is Account-isolated;
- consumers do not duplicate plan rules.

## 96. Service extraction readiness

Billing is a strong future extraction candidate because it has:

- independent commercial semantics;
- provider/security concerns;
- independent operational failure modes;
- potentially different compliance/retention requirements.

That still does not justify extraction now.

Before extraction prove:

- Billing data ownership is private;
- Account/Governance contracts are stable;
- entitlement consumer contract is stable;
- usage producer contracts are stable;
- provider webhooks can route independently;
- secret management crosses boundary safely;
- reconciliation/observability are mature;
- no product context directly reads Billing tables;
- migration/deployment sequencing is understood;
- independent deployment provides measurable value.
