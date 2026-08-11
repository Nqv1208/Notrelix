# Backend Overview

## Scope

Durable backend architecture for the Notrelix modular monolith.

## Responsibility / Ownership

The backend owns server-authoritative business behavior, persistence, delivery,
security enforcement, public API contracts, and background processing.

## Current Architecture

`backend/backend.slnx` defines five production projects:

- `Notrelix.Domain`
- `Notrelix.Application`
- `Notrelix.Infrastructure`
- `Notrelix.Platform`
- `Notrelix.API`

Dependency direction is closed: API, Infrastructure, and Platform depend inward
through Application/Domain contracts. Domain does not depend on outer layers.

## Normative Contracts

- Domain owns local business invariants and state transitions.
- Application owns use-case orchestration, authorization, transactions and
  external facts.
- Infrastructure owns EF Core, PostgreSQL, Redis/cache adapters, provider
  clients, search/storage adapters, migrations and RLS mechanisms.
- Platform owns reusable delivery, messaging, idempotency, post-commit and
  runtime mechanisms.
- API is a transport/composition boundary.
- Bounded contexts are product ownership seams, not automatic project seams.
- Future extraction must preserve current contracts instead of introducing
  premature service boundaries.

## Allowed Design

- Vertical slices may cross projects when the business transaction requires it.
- Cross-context coordination goes through Application contracts and explicit
  events/messages.
- Shared abstractions are admitted only when semantics and dependency direction
  are stable.

## Forbidden Design

- Moving provider/runtime logic into Domain.
- Handler-local authorization strings that bypass the canonical model.
- Direct DbContext/provider access in new Application code without an approved
  boundary decision.
- New broad `Common`/`Shared` dumping grounds.
- Documentation that claims a target topology not present in `backend.slnx`.

## Failure Modes

- Source and docs disagree on project topology.
- A convenience dependency reverses ownership.
- A roadmap/freeze artifact is treated as current architecture.

## Change Impact Rules

Changes to project references, package ownership, public API contracts, events,
migrations, RLS, or pipeline ordering require architecture tests and a linked
ADR when the decision is consequential.

## Executable Evidence / Tests / Gates

- `backend/backend.slnx`
- `backend/src/**/*.csproj`
- `backend/tests/Notrelix.Architecture.Tests`
- `dotnet build backend.slnx`
- `dotnet test backend.slnx`

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

`backend/backend.slnx` is the production/test project inventory authority.

## Non-responsibilities

This document does not define product semantics, endpoint shapes, migration
steps, or coding-agent execution order.
