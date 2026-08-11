---
title: "Frontend Coding Standards"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Coding Standards

Coding standards protect architecture legibility and predictable state behavior rather than prescribe cosmetic preferences already handled by formatter/linter.

## Modules and exports

Prefer small cohesive modules with explicit named exports. Public package API is curated; internal files are not part of the contract merely because TypeScript can resolve them. Avoid barrel files that accidentally export internals or create cycles.

## Types

Use generated transport types at API boundary and capability-owned semantic types internally. Avoid `any`; narrow `unknown` at boundaries. Model discriminated states explicitly rather than combinations of booleans such as `isLoading + hasError + noAccess` that permit impossible states.

## React behavior

Components render/coordinate presentation. Network calls, cache policy and product orchestration belong to owned hooks/adapters/state. Effects synchronize with external systems; do not use effects as a substitute for derived state. Every subscription/timer/listener effect has deterministic cleanup.

## State

Prefer server-state owner APIs over copying entities into local stores. Local state is for ephemeral UI/draft interaction. Memoization is a measured optimization, not default ceremony. Stable callbacks/derived values matter when required by subscription/component contracts, not to satisfy folklore.

## Error handling

Do not swallow errors or convert typed contract failures to `console.error` plus success-looking UI. Map technical error categories into capability recovery states. Logging/telemetry must redact secrets and sensitive payloads.

## Naming

Use product vocabulary consistently: Board, BoardItem, BoardField, BoardView, Document, Workspace, etc. Do not rename a concept in one package because a UI library uses a different noun. Generic abstractions use product-neutral names only when truly generic.

## Comments

Comments explain non-obvious invariant, compatibility reason or trade-off. They do not narrate code. Temporary workaround comments reference the tracked exception/debt and removal condition.

## Generated/vendor code

Do not reformat or hand-edit generated files or third-party copied components merely to satisfy unrelated preferences. Configure exclusions/ownership intentionally while keeping architecture/security checks on surrounding integration.
