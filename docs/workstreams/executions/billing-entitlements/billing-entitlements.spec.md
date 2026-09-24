---
document_id: WRK-SPEC-BILLING-ENTITLEMENTS
document_type: workstream-specification
status: active
owner: billing-entitlements-team
applies_to: [backend, billing, p4c, plans, subscriptions, entitlements, usage, payments, provider-integration]
evidence:
  - docs/product/billing.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/billing-entitlements.md
  - docs/workstreams/cross-team-dependencies.md
  - backend/docs/architecture/application-model.md
  - backend/docs/architecture/security-tenancy-authorization.md
  - backend/docs/architecture/platform-and-messaging.md
  - docs/delivery/contract-first-delivery.md
  - docs/delivery/migration-policy.md
review_on: [billing-model-change, entitlement-contract-change, financial-state-change, provider-contract-change]
---

# SPEC — P4C Billing & Entitlements

## 1. Objective

P4C establishes the commercial authority of Notrelix:

```text
Plan
→ Subscription
→ Entitlement
→ Product entitlement contract
→ Usage
→ Provider/payment integration
→ Billing administration
```

Billing owns commercial state. It does not own Account identity, resource authorization or product-domain state.

## 2. Entry gate

| Contract | Required |
|---|---|
| Account identity | D5 |
| billable Account semantics | D5 |
| billing-admin authorization | D4+ |
| Platform idempotency | D4+ before financial mutations |
| provider secret mechanism | D4+ before provider integration |

Billing core may begin without WorkManagement.

## 3. Plan/catalog

Plan is a global commercial catalog.

Required:

- stable Plan identity;
- versioned commercial meaning;
- price history semantics;
- stable FeatureCode vocabulary;
- explicit limit unit/scope;
- deprecation/archive behavior without rewriting historical subscriptions.

## 4. Subscription

Subscription lifecycle is explicit and separate from Plan lifecycle.

Required states/transitions must represent:

- activation;
- trial;
- renewal period;
- cancel-at-period-end;
- effective cancellation;
- upgrade/downgrade;
- past-due/grace;
- provider uncertainty where applicable.

Provider state cannot directly mutate arbitrary Subscription state without mapping through Billing semantics.

## 5. Entitlement

Entitlement is the stable product-facing commercial contract.

It must be:

- deterministic;
- versionable;
- Account-scoped unless product authority says otherwise;
- separate from Governance authorization;
- fail-closed for security-sensitive capability checks;
- cacheable only with correct invalidation/version semantics.

## 6. Product consumption

Product contexts consume Billing through explicit facts/contracts, never Billing private persistence.

Correct:

```text
Product use case
→ entitlement/capacity contract
→ Billing-owned evaluation
```

Entitlement may deny commercial capability; it never grants resource permission.

## 7. Usage

Usage is commercial evidence from successful source facts.

Required:

- stable metric identity;
- logical usage-event identity;
- idempotent ingestion;
- period semantics;
- late/out-of-order handling;
- corrections without deleting historical authority;
- hard quota concurrency explicitly designed where used.

Analytics metrics are not automatically Billing usage metrics.

## 8. Provider/payment boundary

Provider SDK/schema remains Infrastructure/adapter concern.

Billing Domain/Application owns:

- logical provider operation;
- provider mapping;
- expected semantic outcome;
- unknown/pending outcome;
- reconciliation requirement.

Raw card/payment secrets are never stored in Domain state.

## 9. Webhooks

Provider webhook must be:

- authenticated/verified before business processing;
- routed to known Billing/Account context;
- idempotent by provider delivery identity;
- mapped through Billing semantics;
- resilient to duplicate/out-of-order delivery.

## 10. Unknown outcomes and reconciliation

A timeout/transport failure after provider submission may be UNKNOWN, not failed.

Unknown operations require durable identity and reconciliation before blind retry where duplicate financial effects are possible.

## 11. Financial history

Invoices, payments, prices and usage history are evidence-oriented.

Ordinary Account/product deletion must not accidentally erase required commercial history.

## 12. Authorization

Billing administration is separately authorized through Governance.

Provider credential possession does not grant Billing administration rights.

## 13. Events

Billing events expose canonical commercial facts such as subscription/entitlement/payment transitions, not provider payload dumps.

## 14. Migration

Plan/FeatureCode/Entitlement/Subscription identity changes are persisted contract changes.

Migration must preserve historical meaning and mixed-version compatibility.

## 15. Exit gate

After Entitlement reaches D4+, product teams may consume it while provider/payment depth continues independently.

P4C core is stable only when Plan/Subscription/Entitlement semantics, tenant isolation, idempotency and financial-history guarantees are executable and verified.
