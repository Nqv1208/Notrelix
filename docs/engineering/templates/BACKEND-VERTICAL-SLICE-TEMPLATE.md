---
title: "Backend Vertical Slice Plan Template"
document_class: template
normative: false
owner: architecture
maturity: FROZEN
conformance: CANONICAL
applies_to: repository
last_verified_sha: "4b60c332a36227b80cb0c19b385de8ed3c23ebf8"
---
# Backend Vertical Slice Plan Template

## Owning use case
Context/module/command-or-query and business result.

## Domain
Aggregate/method/value objects, supplied external facts, invariants/no-op/version/events. List exact mutation tests.

## Application
Request contract/markers, validator, authorization/resource, handler orchestration, ports, transaction/idempotency/cache/realtime requirements.

## Infrastructure
Repository/query implementation, mapping, constraint/index/RLS/migration, provider adapters.

## API
Endpoint/route/version, request/response/errors, generated OpenAPI impact.

## Async consequences
Outbox/integration events/consumers/background work; dedup/order/poison semantics.

## Proof matrix
Project tests + architecture/API/integration gates. Include negative authorization/tenant/concurrency paths.

## Files to change
Explicit paths when implementation plan is being handed to an agent; do not leave structural choices unresolved.
