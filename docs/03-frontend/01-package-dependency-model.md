---
title: "Package Dependency Model"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Package Dependency Model

Package boundaries are the frontend equivalent of service/module seams. They must communicate ownership and permitted dependency direction, not merely shorten import paths.

## FE-PKG-101 — Foundation is capability-agnostic

Foundation packages may own contracts, query primitives, platform abstractions, localization/session primitives and other low-level mechanisms that are reusable without knowing Work Management, Documents, Billing or another business capability. Foundation MUST NOT import `product`, `features` or `apps`.

Admission to Foundation requires all of:
- stable semantic usefulness across more than one capability or an explicitly platform-wide responsibility;
- no business-specific naming or policy;
- no host-specific global access unless that is abstracted behind a runtime port;
- a deliberately small public surface.

## FE-PKG-102 — Product packages own bounded product capability

A product capability may split internally by responsibility, for example:

```text
core    domain-facing types, pure rules/adapters
state   server-state/query/mutation ownership
web     web-specific composition and UI integration
mobile  native-specific composition and UI integration
testing reusable capability test fixtures when justified
plugins explicit extension boundary when the product owns one
```

Subpackages are optional. Do not create the same directory set mechanically for every capability.

## FE-PKG-103 — Features coordinate; they do not steal product ownership

A feature package is justified for cross-product/application behavior such as account bootstrap, activity aggregation or an application workflow whose semantics span capability owners. If behavior can be named unambiguously as “Work Management board mutation” or “Document block editing”, it normally remains in the product capability.

## FE-PKG-104 — UI packages contain reusable presentation primitives

UI packages may own design tokens, accessible primitives, composition helpers and host-appropriate reusable presentation. They MUST NOT know tenant selection, board permissions, billing entitlements or product-specific query state.

## FE-PKG-105 — Runtime packages adapt environment capability

Runtime-web/mobile packages provide browser/native implementations of ports such as storage, connectivity, file picking or host lifecycle. They must not implement product policy.

## Import review

For every new edge, review both directions:

- Why does the consumer require this owner?
- Would moving the symbol avoid a reverse dependency?
- Is the symbol public for a stable reason?
- Does the dependency pull a web-only/native-only package into shared code?
- Does the edge make a supposedly lower package understand product vocabulary?

Architecture checks MUST reject cycles and forbidden family edges. ESLint/TypeScript alone are not sufficient because syntactically valid imports can still violate ownership.
