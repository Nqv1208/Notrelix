---
title: "Mobile Host Contract"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Mobile Host Contract

The mobile app is an Expo/React Native composition host. Native safety is an architecture invariant, not only a build concern.

## FE-MOB-101 — Production mobile dependency graph is native-safe

Mobile production paths MUST NOT import DOM, ReactDOM, `ui-web`, `runtime-web` or browser-only packages, including transitively through a supposedly shared package. Dependency gates must prove this.

## FE-MOB-102 — Shared semantics, native presentation

Product core and portable state/contracts should be reused where their dependencies are host-neutral. Mobile-specific navigation, gestures, native storage, file/media APIs and presentation adapters live in mobile/runtime-mobile slices.

## FE-MOB-103 — Offline behavior is explicit per capability

Network caching is not automatically offline support. A capability that supports offline mutation/read must define local persistence, conflict/idempotency, queued mutation identity, retry/reconciliation, scope isolation and logout/account-change data disposal. Otherwise the UI should present offline/retry state rather than pretending writes succeeded.

## FE-MOB-104 — Secure local data handling

Tokens/secrets use approved secure storage mechanisms, not generic persistence. Workspace/account-scoped persisted caches must declare whether they survive logout, how they are encrypted/protected as required and how scope transitions prevent cross-account visibility.

## FE-MOB-105 — Mobile lifecycle is handled deliberately

Background/foreground, connectivity, deep links and OS interruptions may invalidate sessions/subscriptions. Runtime-mobile owns lifecycle signals; session/query/realtime owners decide reconciliation through explicit contracts.

## Proof

Run native dependency rules, TypeScript/tests, Expo build-sensitive checks and representative device/emulator validation for navigation, lifecycle and accessibility. A passing web test is not evidence that mobile is safe.
