# Infrastructure and Data

## Scope

EF Core, PostgreSQL, migrations, RLS, Redis/cache adapters, repositories,
provider adapters, storage/search adapters, indexes and data-shape mechanisms.

## Responsibility / Ownership

Infrastructure implements technical ports and persistence contracts. It supports
Domain/Application correctness but does not own business rules.

## Current Architecture

Infrastructure source lives under `backend/src/Notrelix.Infrastructure`.
Persistence evidence is the EF model and migration chain, not manually
maintained schema snapshots.

## Normative Contracts

- DbContext ownership, EF mappings, converters, query filters and migrations
  belong to Infrastructure.
- PostgreSQL schema is changed through reviewed migrations; generated/current
  schema is authority.
- RLS is defense in depth and must complement Application authorization.
- Tenant-scoped persistence must include enough account/workspace/resource
  predicates or session context to prevent cross-tenant leakage.
- Repositories/adapters implement Application ports and preserve aggregate
  version/concurrency semantics.
- Cache/Redis keys include tenant/resource/user/permission scope when data is
  authorization-sensitive.
- External provider, storage and search clients remain adapters; provider DTOs
  do not leak into Domain.
- Indexes and data shapes serve query/runtime needs without redefining product
  semantics.

## Allowed Design

- EF configuration classes and migrations that map approved Domain/Application
  contracts.
- Adapter-specific retries where idempotency and transaction boundaries are
  preserved.
- Generated schema/OpenAPI evidence when linked to a producer.

## Forbidden Design

- Business invariants only in EF configuration, SQL constraints or RLS.
- Reflection/private-field mutation to bypass Domain APIs.
- Blanket policy generation that guesses tenant columns.
- Dropping legacy storage before old readers/writers and rollback are known.
- Provider-specific terminology becoming canonical Domain language.

## Failure Modes

- RLS/session context is missing in background consumers.
- A migration changes persisted meaning without deterministic backfill.
- Cache serves data after permission/resource scope changes.
- EF model drift is suppressed to allow startup.

## Change Impact Rules

Persistence, migration, RLS, index, cache or provider adapter changes require
source inspection, migration review, round-trip/integration tests where
relevant, and rollback/roll-forward planning for destructive changes.

## Executable Evidence / Tests / Gates

- `backend/src/Notrelix.Infrastructure`
- Infrastructure migrations and configurations
- `backend/tests/Notrelix.Infrastructure.Tests`
- `backend/tests/Notrelix.Integration.Tests`

## Related ADRs

- `../decisions/ADR-002-rls-bootstrap-connection-lifecycle.md`

## Related Source Manifests

`backend/backend.slnx`, EF model/migrations, docker compose files.

## Non-responsibilities

Infrastructure does not decide aggregate boundaries, authorization semantics,
product lifecycle or API shapes.
