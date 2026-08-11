---
title: "Foundation and Runtime Boundaries"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Foundation and Runtime Boundaries

Foundation and runtime packages keep low-level mechanisms reusable without smuggling host or product policy into shared code.

## FE-FOUND-101 — Mechanism before convenience

A foundation abstraction is admitted only when it has stable semantics independent of a single product capability. Good candidates include contract primitives, query-key builders, localization/session contracts and platform ports. A one-off helper used by one product remains with that product until reuse and ownership are real.

**Forbidden:** generic `utils`, `common`, `helpers` packages that accept arbitrary business code; importing a product type into Foundation because it is “just a type”.

## FE-FOUND-102 — Foundation contracts are explicit

Ports define only capabilities lower layers need. They do not expose the full browser/native/provider SDK. A storage port, for example, exposes product-neutral storage semantics rather than `localStorage`, AsyncStorage or provider-specific handles.

## FE-RUNTIME-101 — Runtime is an adapter layer

Runtime-web/runtime-mobile packages implement environment capabilities and lifecycle integration. They may depend on Foundation contracts and host libraries. They MUST NOT decide business authorization, query ownership, billing policy or work-management rules.

## FE-RUNTIME-102 — Shared code cannot accidentally execute host globals

Code intended for both web and mobile MUST NOT reference `window`, `document`, DOM types, ReactDOM, browser storage, Expo/native modules or other host globals at import time or execution time. Use a runtime contract supplied by the host.

## FE-RUNTIME-103 — Effects have lifecycle ownership

Subscriptions, connectivity listeners, storage listeners and host event handlers must have explicit installation/disposal ownership. An adapter that registers a listener must return/provide deterministic cleanup and must not leak listeners across account/workspace transitions or hot reload/test cycles.

## Validation

Dependency-rules prove family edges; host-specific tests prove implementations; shared-package tests should run in an environment that would expose accidental DOM/native assumptions where practical.
