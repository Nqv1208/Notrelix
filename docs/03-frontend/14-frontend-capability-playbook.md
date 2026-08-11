---
title: "Frontend Capability Implementation Playbook"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Capability Implementation Playbook

Use this playbook for a new or materially changed product capability. The goal is a smallest complete vertical transaction without leaving architecture choices to downstream agents.

## 1. Establish semantic owner

Read the bounded-context document in `../08-product/contexts/`. Define the capability noun, tenant/workspace scope, authorization behavior, REST/realtime contracts and whether web/mobile both participate. If semantics are undecided, stop coding and record the product/architecture decision instead of inventing it in UI.

## 2. Choose package ownership

Decide whether the change belongs to an existing product capability, cross-product feature, foundation mechanism, runtime adapter, UI primitive or host composition. Do not create a package until lifecycle/public API/dependency boundary is independently justified.

For product capability slices, add only needed slices (`core`, `state`, `web`, `mobile`, etc.). No symmetry requirement exists.

## 3. Contract first

For backend data, identify the generated REST/realtime contract. Regenerate types through tooling when changed. Define explicit mapping if capability semantics should not expose transport DTOs directly.

## 4. Server-state owner

Define canonical query keys including account/workspace scope, fetch/query options, mutation adapters, error categories and patch/invalidation strategy. Document any optimistic update including rollback/conflict semantics.

## 5. Realtime convergence

If affected resource emits realtime, define event-to-query consequence: patch, invalidate, refetch or ephemeral collaboration update. Handle duplicate, reconnect and scope transition. Do not build a parallel entity store.

## 6. Host composition

Expose screens/components/adapters through package public exports. Web/mobile apps wire routes/providers/navigation only. Browser/native effects stay in host/runtime slice.

## 7. UX completeness

Implement loading, empty, validation/error, permission denied/read-only, retry, destructive confirmation and concurrency conflict states as applicable. Use accessible UI primitives and keyboard/screen-reader semantics appropriate to host.

## 8. Proof

Run package tests, TypeScript/lint, dependency-rules, generated-contract checks and relevant host integration/e2e suites. For tenant-sensitive work include transition/stale-response tests. For mobile run native-safety gates.

## 9. Change-impact report

Before completion state:
- semantic owner and affected packages;
- public export/dependency changes;
- REST/realtime contract changes;
- query/cache/realtime consequences;
- web/mobile behavior;
- tests/gates executed;
- migration/compatibility or remaining approved exception.

A change is incomplete when the code works only because an app deep-imports an internal package, a component owns an ad-hoc query key, or a generated contract was manually patched.
