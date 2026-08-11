---
title: "Frontend Capability Plan Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Capability Plan Template

## Semantic owner / hosts
Product or feature owner; web/mobile/marketing applicability.

## Contract
Generated REST/realtime types and mapping; backend dependency/blocker.

## Package placement/public API
Existing/new package slices, dependency edges and exports. Explain any new package.

## Server state
Canonical scoped query keys, query owner, mutation/error categories, patch/invalidation/optimistic rollback.

## Realtime
Event consequence, duplicate/out-of-order/reconnect/scope transition.

## UX states
Loading/empty/error/retry/denied/read-only/concurrency/offline/destructive confirmation.

## Accessibility/host behavior
Keyboard/focus/screen reader; mobile native-safe concerns; route/composition integration.

## Proof
Package tests, dependency rules, codegen, transition/realtime, host integration/e2e, accessibility.
