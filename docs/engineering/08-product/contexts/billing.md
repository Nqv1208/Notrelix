---
title: "Billing Context"
document_class: constitution
normative: true
owner: billing
maturity: FROZEN
conformance: CANONICAL
applies_to: billing
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Billing Context

## Mission

Billing owns the global Plan catalog and account/customer commercial state: subscription lifecycle, billing periods, entitlements/limits, usage records where assigned, invoices/payment method references and provider-commercial integration mapping. Product contexts consume entitlements but do not become billing authorities.

## BIL-101 — Plan is a global catalog

Plan/feature definitions are not workspace child records and are not duplicated per tenant merely to represent a subscription. Account/customer subscriptions reference stable Plan/product-price identities and an effective entitlement set/version.

## BIL-102 — Subscription lifecycle is explicit

Trialing/active/past-due/cancel-at-period-end/canceled/expired or provider-specific states map to a canonical lifecycle with validated transitions. Provider webhook payload does not directly set arbitrary internal state; Application maps verified external facts into allowed transitions.

## BIL-103 — Entitlement is a business contract, not scattered feature flags

Product capabilities ask a Billing/entitlement service/contract whether a feature/limit is available for Account/Workspace scope according to canonical mapping. Do not hard-code plan names such as `if Pro` across handlers/frontend. Entitlement-sensitive authorization remains server enforced.

## BIL-104 — Commercial callbacks and commands are idempotent

Checkout/session creation, subscription changes and provider webhooks use stable operation/provider event identities. Duplicate callback delivery does not create duplicate invoices/subscriptions/usage side effects. Store only safe provider identifiers; secret/provider access remains Infrastructure/Integrations boundary.

## BIL-105 — Usage is append/aggregate-safe

Usage facts that affect billing have stable tenant/resource/metric/time identity and duplicate protection. Derived billing totals are reproducible/auditable from accepted usage or provider truth as designed. Do not overwrite usage history casually to “fix” a total; record correction semantics.

## Payment/invoice semantics

PaymentMethod normally stores provider reference/brand/last4/safe metadata, not raw PAN/CVC. Invoice/payment records reflect provider/commercial lifecycle and retention/audit requirements. Billing Domain should not process card secrets.

## Cross-context

Accounts provides customer/account identity. Workspaces/product contexts consume entitlements/limits and may emit usage facts. Governance decides who can view/manage billing. Identity provides actor. Integrations/Infrastructure adapts billing provider SDK/webhooks.

## Failure and downgrade

Provider outage must not corrupt existing entitlements. Define grace/past-due behavior explicitly. Downgrade that exceeds current resource limits requires a product policy (block creation, read-only excess, scheduled enforcement, etc.) rather than destructive deletion of customer work.

## Deletion/retention

Account closure coordinates subscription cancellation and commercial retention. Invoice/payment evidence may have legal retention independent of product data deletion. Never cascade-delete commercial records solely for ORM convenience.

## Forbidden designs

- plan name checks scattered in product code;
- raw card/token secret persistence/logging;
- workspace-specific copies of global Plan as authority;
- webhook update without signature/replay/idempotency;
- Billing directly deleting Boards/Documents to enforce downgrade;
- mutable invoice/usage history without correction/audit semantics.

## Tests/change impact

Cover subscription transitions, webhook replay/out-of-order handling, entitlement resolution/version, usage dedup, downgrade/grace, provider failure and authorization. Plan/entitlement changes require review across gated product capabilities and frontend visibility/upgrade UX.
