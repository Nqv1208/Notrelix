---
title: "Backend Testing and Architecture Gates"
document_class: handbook
normative: true
owner: quality
maturity: FROZEN
conformance: CANONICAL
applies_to: backend/tests
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Backend Testing and Architecture Gates

## Test taxonomy

### Domain

Pure behavior/invariants, mutation success/rejection/no-op, version/events, lifecycle, value semantics. No database/provider needed.

### Application

Handlers/validators/pipeline/request classification/authorization/external-fact orchestration using focused fakes/ports.

### Infrastructure

EF mappings, adapter behavior, serializers, provider translation, persistence-specific components.

### Platform

Reliable mechanism state machines: ordering, idempotency identity, poison/retry/delivery.

### API

HTTP transport, error mapping, OpenAPI, idempotency/concurrency headers, host registration.

### Integration

Production composition with database/RLS/outbox/idempotency/transaction/consumer graph and external boundary substitutes as required.

### Architecture

Project dependencies, forbidden references, placement/naming/marker rules where stable enough to automate.

## BE-TEST-101 — Test the protected contract, not implementation trivia

Prefer scenario assertions around externally meaningful state/result/event/contract. A mock interaction is insufficient proof of a business invariant unless interaction order is itself the contract.

## BE-TEST-102 — Required suites must execute non-zero relevant work

CI should guard critical projects/suites against filters that accidentally execute zero tests.

## Rule traceability

Architecture/security/tenant/reliability tests should reference rule IDs in names/traits/comments where useful so a future failure explains which invariant was violated.
