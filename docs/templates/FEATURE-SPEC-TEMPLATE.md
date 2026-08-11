---
title: "Product Feature Specification Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Product Feature Specification Template

## Problem and outcome
User/business problem, observable successful outcome and explicit non-goals.

## Semantic owner
Bounded context, ubiquitous language and affected lifecycle/aggregate/resource.

## Scope / authorization
Account/workspace/global scope, subject/resource permission, guest/share behavior, entitlement.

## Invariants and state transitions
Preconditions, success transition, no-op, rejection, concurrency/version, deletion/archive.

## Cross-context facts
Which facts are loaded synchronously, which events/contracts are emitted/consumed, consistency choice.

## API / realtime / frontend state
Use cases/endpoints, contract/error semantics, realtime convergence, query-key/cache owner, web/mobile states.

## Data/migration
Schema/index/RLS/backfill/compatibility/retention.

## Failure/operations
Retries/idempotency/provider failure/observability/runbook impact.

## Acceptance proof
Behavior/integration/architecture/contract/e2e tests and exact success criteria.
