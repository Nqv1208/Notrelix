---
document_id: WRK-TESTS-BILLING-ENTITLEMENTS
document_type: workstream-test-plan
status: active
owner: billing-entitlements-team
applies_to: [backend, billing, p4c, testing]
evidence:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.plan.md
review_on: [p4c-requirement-change]
---

# TESTS — P4C Billing & Entitlements

## Catalog

- `BIL-TST-PLAN-001` stable Plan identity.
- `BIL-TST-PLAN-002` Plan rename does not change entitlement identity.
- `BIL-TST-PRICE-001` historical price meaning preserved.
- `BIL-TST-FEAT-001` FeatureCode is stable persisted vocabulary.

## Subscription

- `BIL-TST-SUB-DOM-001` allowed transition matrix.
- `BIL-TST-SUB-DOM-002` invalid transition rejected.
- `BIL-TST-SUB-INT-001` Account isolation.
- `BIL-TST-SUB-CONC-001` competing subscription mutation conflict.
- `BIL-TST-SUB-TIME-001` cancel request vs effective cancellation.

## Entitlement

- `BIL-TST-ENT-001` deterministic evaluation.
- `BIL-TST-ENT-002` entitlement != authorization.
- `BIL-TST-ENT-003` security-sensitive failure fails closed.
- `BIL-TST-ENT-CACHE-001` authority change invalidates stale cache.
- `BIL-TST-ENT-X-001` representative product consumer contract.

## Usage

- `BIL-TST-USG-001` duplicate logical usage event counted once.
- `BIL-TST-USG-002` late/out-of-order usage handled.
- `BIL-TST-USG-003` correction preserves evidence history.
- `BIL-TST-USG-CONC-001` hard quota concurrency when enabled.
- `BIL-TST-USG-PERIOD-001` period transition does not delete history.

## Provider/payment

- `BIL-TST-PROV-001` provider DTO translated at adapter boundary.
- `BIL-TST-PROV-IDEMP-001` provider operation stable identity.
- `BIL-TST-PROV-UNK-001` unknown outcome enters reconciliation path.
- `BIL-TST-WH-SEC-001` invalid webhook signature rejected.
- `BIL-TST-WH-IDEMP-001` duplicate provider delivery processed once.
- `BIL-TST-WH-ORDER-001` out-of-order webhook does not regress state.
- `BIL-TST-SEC-001` raw payment/provider secrets absent from Domain/logs.

## Authorization

- `BIL-TST-AUTHZ-001` billing-admin allowed.
- `BIL-TST-AUTHZ-002` ordinary member denied.
- `BIL-TST-AUTHZ-003` cross-tenant administration denied.

## Migration/history

- `BIL-TST-MIG-001` clean DB.
- `BIL-TST-MIG-002` supported upgrade.
- `BIL-TST-MIG-003` no pending model drift.
- `BIL-TST-HIST-001` Account/product deletion does not accidentally erase retained financial evidence.

## Contract/events

- `BIL-TST-EVT-001` canonical commercial event after commit.
- `BIL-TST-EVT-002` provider payload is not public Billing event.
- `BIL-TST-CON-001` entitlement consumer compatibility.

## Certification rule

Entitlement D4+ requires deterministic evaluation + consumer contract + authorization separation. Financial/provider readiness additionally requires idempotency, unknown-outcome and reconciliation evidence.
