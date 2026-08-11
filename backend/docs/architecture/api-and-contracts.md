# API and Contracts

## Scope

HTTP endpoint responsibility, authentication integration, Application
authorization relationship, request/result translation, OpenAPI/contracts,
versioning, errors, idempotency input, pagination/filter/sort and realtime
contract relationship.

## Responsibility / Ownership

API is a thin transport and composition boundary. It binds requests,
authenticates, maps transport context, calls Application, translates results and
exposes generated public contracts.

## Current Architecture

API source lives under `backend/src/Notrelix.API`. Contract evidence includes
endpoint registrations, OpenAPI output/contracts and API/integration tests.

## Normative Contracts

- Endpoints do not mutate Domain or query persistence for workflows directly.
- Authentication happens at the host/API boundary; authorization remains an
  Application/use-case responsibility.
- Request DTOs map to Application commands/queries; result contracts avoid EF
  entities and provider DTOs.
- Errors are translated into stable HTTP/problem-detail shapes.
- Versioning must be explicit when public contracts change incompatibly.
- Idempotency keys are accepted and forwarded only for use cases that support
  idempotent semantics.
- Pagination/filter/sort conventions must preserve authorization and tenant
  scope.
- REST and realtime contracts must describe the same business facts; realtime
  is not a competing source of truth.
- Route inventories are generated or tested, not hand-maintained.

## Allowed Design

- Endpoint groups/minimal APIs/controllers as transport adapters.
- OpenAPI drift checks as public contract evidence.
- Host composition for auth, CSRF, rate limiting, CORS and serialization.

## Forbidden Design

- API-local permission checks as the only enforcement.
- Direct DbContext/provider calls to complete a business workflow.
- Handwritten DTOs that drift from generated public contracts.
- Manual route catalogs used as authority.

## Failure Modes

- A hidden endpoint bypasses Application authorization.
- Error/result mapping differs by endpoint for the same use case.
- Realtime emits a fact not represented by the committed use case.

## Change Impact Rules

Public endpoint, auth integration, error, OpenAPI, versioning or idempotency
changes require API tests and contract drift checks.

## Executable Evidence / Tests / Gates

- `backend/src/Notrelix.API`
- `backend/tests/Notrelix.API.Tests`
- `backend/tests/Notrelix.Integration.Tests`
- OpenAPI/contract generation checks where available

## Related ADRs

- `../decisions/ADR-003-csrf-protection.md`
- `../decisions/ADR-004-rate-limiting-architecture.md`

## Related Source Manifests

`backend/backend.slnx`, API project file, generated contracts.

## Non-responsibilities

API does not own business invariants, persistence model, frontend state rules or
background delivery.
