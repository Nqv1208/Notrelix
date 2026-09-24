---
document_id: WRK-PLAN-BILLING-ENTITLEMENTS
document_type: workstream-plan
status: active
owner: billing-entitlements-team
applies_to: [backend, billing, p4c]
evidence:
  - docs/workstreams/executions/billing-entitlements/billing-entitlements.spec.md
  - docs/workstreams/backend-roadmap.md
  - docs/workstreams/teams/billing-entitlements.md
review_on: [p4c-sequence-change, entitlement-handoff-change]
---

# PLAN — P4C Billing & Entitlements

## 1. Sequence

```text
Phase 0 source/commercial inventory
Phase 1 Plan + FeatureCode
Phase 2 Subscription lifecycle
Phase 3 Entitlement contract
Phase 4 Product consumption/capacity
Phase 5 Usage ledger
Phase 6 Provider/payment adapter
Phase 7 Webhook/idempotency/reconciliation
Phase 8 Billing administration
Phase 9 migration/security/observability
Phase 10 certification
```

## 2. Phase 0

Record candidate SHA and classify current Billing source as KEEP/HARDEN/REFACTOR/MIGRATE/RETIRE/BLOCKED.

Inventory existing Plan, Subscription, Entitlement, Usage, invoice/payment/provider surfaces and test evidence.

## 3. Phase 1 — Plan/catalog

Lock stable IDs, FeatureCode vocabulary, Plan limits and historical price behavior.

Preferred PR: `PR-BIL-01 catalog`.

## 4. Phase 2 — Subscription

Build explicit transition table and reject provider-driven arbitrary state assignment.

Prove Account containment and lifecycle timing.

Preferred PR: `PR-BIL-02 subscription`.

## 5. Phase 3 — Entitlement

Define minimal product-facing contract and deterministic precedence.

Add cache only with version/invalidation proof.

Preferred PR: `PR-BIL-03 entitlement`.

This is the first major downstream handoff.

## 6. Phase 4 — Product consumption

Integrate one representative product use case.

Prove:

- authorization remains separate;
- entitlement failure semantics are stable;
- hard limit race is handled if applicable.

Preferred PR: `PR-BIL-04 product handoff`.

## 7. Phase 5 — Usage

Implement append/evidence-oriented usage ingestion with stable idempotency key and period semantics.

Preferred PR: `PR-BIL-05 usage`.

## 8. Phase 6 — Provider/payment

Introduce provider anti-corruption adapter, logical operation identity, secret reference and result taxonomy.

Preferred PR: `PR-BIL-06 provider adapter`.

## 9. Phase 7 — Webhooks/reconciliation

Verify signature/authentication, idempotency, out-of-order events, unknown outcome and repair path.

Preferred PR: `PR-BIL-07 reconciliation`.

## 10. Phase 8 — Administration

Protect subscription/payment/admin operations with Governance billing-admin actions.

## 11. Phase 9 — Hardening

Close:

- clean/upgrade migrations;
- financial-history retention;
- secret/log redaction;
- observability/correlation;
- duplicate-provider-event tests;
- provider outage behavior.

## 12. Stop conditions

Stop if:

- entitlement and authorization are merged;
- provider payload becomes Domain schema;
- provider timeout is treated as safe retry without outcome classification;
- raw payment secrets enter Domain/Application state;
- historical commercial facts would be destructively rewritten;
- usage cannot be made idempotent.
