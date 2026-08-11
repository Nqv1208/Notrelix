# Application Model

## Scope

Use cases, commands, queries, pipeline behaviors, authorization, tenant/resource
resolution, transactions, external facts, cache interaction, concurrency and
post-commit orchestration.

## Responsibility / Ownership

Application owns use-case orchestration. It loads aggregates and external facts,
authorizes protected work, opens transactions, coordinates idempotency and maps
Domain/Application results to public contracts.

## Current Architecture

Application source lives under `backend/src/Notrelix.Application`. Tests live in
`backend/tests/Notrelix.Application.Tests` with integration coverage where
persistence or host composition is required.

## Normative Contracts

- Vertical slices are module-first and must keep command/query semantics clear.
- Request marker contracts drive pipeline behavior for authorization, tenant
  scope, transactions, expected-version validation, idempotency and post-commit
  work.
- Authorization is server-side and happens before protected resource effects.
- Resource/tenant scope is resolved before authorization when policy requires
  resource facts.
- Application supplies Domain with actor, time, parent paths, counts and other
  external/cross-aggregate facts.
- Transactions wrap the complete state change and outbox/post-commit enrollment.
- Expected-version conflicts return explicit conflict results and leave partial
  mutations uncommitted.
- Authorization-sensitive cache interaction must include tenant/resource/user
  scope and permission/version invalidation.
- Cross-context writes require explicit use-case/event contracts; handlers do
  not silently mutate another context's aggregate.
- Post-commit work runs only after successful commit through the approved
  post-commit/outbox mechanism.

## Allowed Design

- Handler orchestration that delegates invariant enforcement to Domain.
- Application ports for persistence/provider needs implemented outside
  Application.
- Focused read models when authorized and scoped by the use case.

## Forbidden Design

- Provider SDK calls, EF DbContext access or concrete Infrastructure helpers in
  new handlers without an approved exception.
- Handler-local business invariants that duplicate or bypass Domain.
- Permission decisions embedded as ad-hoc role strings.
- Returning EF entities or provider DTOs as public contracts.
- Updating cache before the transaction outcome is known.

## Failure Modes

- A command validates request shape but omits business authorization.
- Resource scope is trusted from client input.
- A handler writes two aggregates without a single transaction/compensation
  contract.
- A conflict result is reported after partial state has already changed.

## Change Impact Rules

Pipeline order, marker contracts, transaction behavior, authorization policy,
idempotency or result/error semantics changes require focused Application tests
and architecture/contract review.

## Executable Evidence / Tests / Gates

- `backend/src/Notrelix.Application`
- `backend/tests/Notrelix.Application.Tests`
- `backend/tests/Notrelix.Integration.Tests`
- Architecture tests for layer boundaries

## Related ADRs

- `../decisions/ADR-001-pipeline-boundary.md`

## Related Source Manifests

`backend/backend.slnx` and Application project references.

## Non-responsibilities

Application does not own persistence mechanics, provider integration details,
HTTP routing, frontend permissions UX or Product wording by itself.
