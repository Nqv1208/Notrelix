---
title: "Product Engineering Semantics"
document_class: constitution
normative: true
owner: product
maturity: FROZEN
conformance: CANONICAL
applies_to: product
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Product Engineering Semantics

This section defines business meaning independently of backend/frontend implementation. A bounded-context document is the canonical owner for nouns, lifecycle, invariants and cross-context responsibility. Backend/frontend handbooks define how those semantics are implemented in each technology.

## Contexts

Accounts, Identity, Workspaces, Governance, Work Management, Documents, Collaboration, Automation, Integrations, Billing and Analytics. Context boundaries are semantic ownership seams and future extraction seams; they are not a requirement to create one deployable service or one project/package per context today.

Read [Product Model](00-product-model.md), [Cross-Context Workflows](01-cross-context-workflows.md), [Product Experience and Brand](02-product-experience-and-brand.md), then the owning context before implementing a feature.

A context doc may describe stable semantics even if the current source has transitional structure. Source/test evidence determines what is implemented; a discrepancy must be classified rather than silently choosing whichever is easier.
