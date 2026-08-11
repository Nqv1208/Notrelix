---
title: "Frontend Engineering Handbook"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Engineering Handbook

This handbook is the canonical architecture authority for the pnpm/Turborepo frontend. Scoped `AGENTS.md` files describe how to work in a package family; they do not redefine these rules.

## Read path

1. [Frontend architecture](00-frontend-architecture.md)
2. [Package dependency model](01-package-dependency-model.md)
3. [App composition and routing](02-app-composition-routing.md)
4. [Foundation and runtimes](03-foundation-runtimes.md)
5. [Product vs features](04-product-vs-features.md)
6. [Query, state and cache](05-query-state-cache.md)
7. [Realtime](06-realtime.md)
8. [Contracts and API client](07-contracts-api-client.md)
9. [UI, design system and accessibility](08-ui-design-system-accessibility.md)
10. host-specific contracts for web/mobile/marketing
11. [Testing and gates](12-frontend-testing-gates.md)
12. [Coding standards](13-frontend-coding-standards.md)
13. [Capability playbook](14-frontend-capability-playbook.md)

`frontend/PACKAGE-MAP.md` is current topology evidence. This handbook owns why the package classes exist and what dependencies mean.
