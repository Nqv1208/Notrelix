---
title: "Repository Testing Strategy"
document_class: handbook
normative: true
owner: engineering-quality
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Repository Testing Strategy

Testing follows risk and ownership. The suite should fail near the violated contract and still include enough integration/e2e evidence to prove composition.

## QLT-TEST-101 — Test pyramid is contractual, not numeric

- pure business/value behavior: many fast behavior tests;
- application/state orchestration: focused component/slice tests;
- persistence/provider/tenant enforcement: integration tests against realistic dependencies where semantics require them;
- API/contracts/realtime: contract/integration tests;
- critical user journeys: selected e2e.

No fixed percentage is mandated. Coverage targets never replace scenario completeness for critical invariants.

## QLT-TEST-102 — Rejection paths are first class

Critical mutations test success and rejection. Domain tests prove rejected mutation leaves state/version/events unchanged. Application/API tests prove authorization/concurrency/not-found distinctions. Frontend tests prove rejected mutations do not leave optimistic/cache state corrupted.

## QLT-TEST-103 — Architecture is executable

Dependency direction, forbidden imports, project/package boundaries, mobile safety, generated-contract drift and required-suite execution are tested as architecture/CI gates rather than left only to reviewer memory.

## QLT-TEST-104 — Required suite may not pass with zero work

Every required CI job has a non-zero execution assertion or equivalent evidence. Filters/globs that accidentally select no tests cause failure for foundation/critical suites.

## Determinism

Tests control time/random/external facts. Avoid sleeps for ordering when a signal/clock can be controlled. Test data is tenant-isolated and does not depend on execution order. Parallel tests must not share mutable global state without explicit isolation.

## What not to test

Do not assert private implementation shape where public behavior is sufficient. Avoid snapshots as sole evidence for business behavior. Do not mock a dependency so aggressively that the test can pass while real protocol semantics are wrong.

## Change-driven selection

Local development may run focused suites first; completion runs all gates required by the change-impact matrix. CI is final evidence for protected branches.
