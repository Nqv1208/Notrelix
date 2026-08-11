# backend/tests — Agent Instructions

Tests are executable proof for canonical rules; they must not invent a competing architecture.

## Place tests by proof responsibility

- Domain.Tests → pure behavior, invariants, no-op/failure atomicity/event/version semantics.
- Application.Tests → handlers, validators, behaviors, authorization/request classification.
- Infrastructure.Tests → mappings/adapters/provider/persistence-unit behavior.
- Platform.Tests → reusable runtime/messaging mechanisms.
- API.Tests → transport/OpenAPI/error/idempotency/host contracts.
- Integration.Tests → production graph, database/RLS/outbox/idempotency/cross-layer behavior.
- Architecture.Tests → dependencies, placement, forbidden references, structural invariants.

Testing support projects provide reusable fixtures/builders; they must not hide product assertions in generic helpers.

## Required quality

A required suite that executes zero relevant tests is a failure. Prefer scenario/contract assertions over implementation-coupled mocks. When a canonical MUST is automatable, its test/gate should name or reference the rule ID where practical.
