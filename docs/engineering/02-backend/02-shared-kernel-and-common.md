---
title: "Domain Common and Shared Kernel"
document_class: handbook
normative: true
owner: backend-domain
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/domain
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Domain Common and Shared Kernel

`Common` and `SharedKernel` are high-risk growth points because they can erase bounded-context ownership.

## BE-SHARED-101 — Domain Common contains mechanics, not business semantics

Appropriate examples:

```text
Entity / AggregateRoot base
Domain-event base/contracts
base Domain exception/rule-code mechanics
stable tenant-scope interfaces
cross-cutting lifecycle primitives only when semantics are truly common
```

Forbidden examples: Board helper, billing rule, workspace business service, permission calculator.

## BE-SHARED-102 — Shared Kernel requires identical meaning

A type may be shared only when multiple contexts use the **same concept with the same invariants and lifecycle**, and changes can be coordinated as one contract.

Potential examples include Email/Money/Slug or a globally meaningful identity wrapper. `BoardStatus`, `SubscriptionStatus`, document permission and task priority are context-owned even if their shapes look reusable.

## Admission questions

1. Which contexts already need the concept?
2. Are semantics identical, not merely names/fields?
3. Who owns future changes?
4. Would duplication preserve autonomy better?
5. Does sharing create a dependency cycle or accidental core model?

If answers are weak, keep the concept inside its context.
