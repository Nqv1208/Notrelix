---
title: "Frontend Testing and Architecture Gates"
document_class: handbook
normative: true
owner: frontend-architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: frontend
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Frontend Testing and Architecture Gates

Tests prove behavior; gates prove architecture and generated-contract integrity. A green command that executes zero relevant work is not certification.

## FE-TEST-101 — Test at the owning boundary

- pure capability/core rules: unit/behavior tests close to core;
- state/query/mutations: query-key, adapter and mutation/invalidation tests;
- reusable UI: interaction/accessibility tests;
- host composition: integration tests;
- user-critical flows: targeted browser/native end-to-end tests;
- dependency/public-export boundaries: architecture/dependency-rules gates;
- API/realtime types: generator/drift checks.

Do not compensate for missing low-level semantic tests with huge e2e suites, or claim host correctness from unit tests alone.

## FE-TEST-102 — Tenant transition receives explicit coverage

At least representative workspace/account-sensitive flows must prove old-scope cache/subscriptions cannot surface under the new scope. Include delayed old response/event cases, not only synchronous happy paths.

## FE-TEST-103 — Mutations prove rejection and convergence

Critical mutations cover success, validation/authorization/concurrency rejection, patch/invalidation consequence and realtime/refetch convergence where relevant. Optimistic paths additionally prove rollback/reconciliation.

## FE-TEST-104 — Mobile safety is a gate

Dependency-rules must reject web/DOM packages from production mobile graph. Mobile-specific tests/build checks execute real mobile packages. A workspace glob matching zero packages is failure for required suites.

## FE-TEST-105 — Generated contracts are reproducible

CI regenerates/checks generated API artifacts or performs an equivalent drift check. Hand-edited generated output and stale lockfile/codegen outputs fail.

## Minimum change matrix

| Change | Required evidence |
|---|---|
| package dependency/export | dependency-rules + TypeScript/lint |
| query key/state owner | state tests + impacted consumers |
| workspace/session transition | integration transition tests |
| realtime handler | duplicate/reconnect/scope behavior |
| reusable UI primitive | interaction + accessibility |
| mobile production import | mobile dependency gate/build-sensitive test |
| generated contract | codegen/drift + consumer compile/tests |
| host route/provider | host integration/e2e as risk requires |

Certification records the exact command/gate result and tested SHA in CI; “tests pass locally” is not a substitute for required protected-branch evidence.
