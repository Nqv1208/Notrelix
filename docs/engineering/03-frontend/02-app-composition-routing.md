---
title: "App Composition and Routing"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# App Composition and Routing

Apps are executable composition roots. They select providers, runtime implementations, top-level routes, shell/layout and product packages. They are not the permanent home of feature/business behavior.

## FE-HOST-101 — Routes are host composition

The host owns route tables, host layout, navigation wiring and lazy boundaries. A product package may export a screen/component/route descriptor contract when needed, but MUST NOT take ownership of the app's router instance or inject arbitrary global routes as a hidden side effect.

## FE-HOST-102 — Provider order is explicit

Global providers are composed intentionally and documented when ordering matters. Typical categories include runtime/session, API/query, account/workspace scope, permissions, realtime, localization/theme and error boundaries. A package MUST NOT silently install a second cache/client/session provider just to make itself work.

## FE-HOST-103 — Bootstrap is fail-closed for scope-sensitive data

Before workspace/account-scoped product content is rendered, the host must establish the authenticated session and active scope needed to form correct queries. During scope change, old scope subscriptions and cache visibility are disposed according to the tenant transition contract before new child data is considered active.

## FE-HOST-104 — Host code may adapt, not duplicate semantics

Host-specific code may translate route params, navigation events, browser/native lifecycle and shell concerns. It MUST NOT reimplement field validation, permission semantics, product state transitions or canonical query key rules already owned by packages.

## Route-level states

Every protected product route must deliberately handle:
- session loading/refresh;
- scope/account/workspace resolution;
- authorization/entitlement denied;
- resource not found vs inaccessible without leaking existence;
- initial loading;
- recoverable network failure;
- empty state;
- stale/realtime reconnect state where relevant.

## Proof

Composition is proven by host integration tests and dependency rules. Critical route guards/tenant transition behavior should have integration/e2e coverage rather than relying only on component snapshots.
