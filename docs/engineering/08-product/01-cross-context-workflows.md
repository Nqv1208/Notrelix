---
title: "Cross-Context Workflow Contract"
document_class: constitution
normative: true
owner: product
maturity: FROZEN
conformance: CANONICAL
applies_to: product
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Cross-Context Workflow Contract

No bounded context may enforce another context's invariants by reaching into its persistence model. Cross-context behavior is coordinated through Application queries/ports, stable IDs, integration events/outbox or an explicit process manager/saga when a durable multi-step workflow exists.

## PROD-XCTX-101 — One context owns each authoritative fact

Examples: Workspace owns membership/workspace lifecycle; Governance owns policy/ACL/audit semantics; Work Management owns board/item/field/view state; Billing owns subscription/entitlement records; Identity owns authentication/session/credential lifecycle. Consumers may cache/project facts but do not become a second authority.

## PROD-XCTX-102 — Cross-context write is never an object graph mutation

If Work Management action should create a Collaboration activity or trigger Automation, Work Management commits its own state and durable event/outbox fact; downstream owners consume idempotently. Application may synchronously validate required external facts before mutation, but does not update two contexts by sharing one aggregate object graph.

## PROD-XCTX-103 — Strong consistency is explicit and narrow

When one operation truly requires atomic facts from multiple owners, document why eventual consistency is insufficient and where the transaction boundary lives. Do not broaden transactions accidentally because contexts share one database in the modular monolith.

## Scope propagation

Every contract carries enough account/workspace/resource identity for authorization, tenant routing and deduplication. Consumers validate/establish their own tenant scope; they do not trust a naked resource ID to imply scope.

## Failure/retry

Asynchronous consumers are idempotent. Partial external side effects are modeled with operation/provider identities and retry state. Compensation is business-specific; “roll back another context” is not assumed possible.

## Evolution

Producer event/REST contracts use stable logical identities and version/additive compatibility. Renaming CLR types/tables/packages is not a valid reason to break external consumers.
