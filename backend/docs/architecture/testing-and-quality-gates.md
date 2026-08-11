# Testing and Quality Gates

## Scope

Backend test topology, critical production-graph evidence, architecture/OpenAPI
drift, non-zero-work guarantees and focused vs certification command selection.

## Responsibility / Ownership

Tests prove executable contracts. Documentation describes which evidence is
required; it does not replace tests or CI.

## Current Architecture

`backend/backend.slnx` lists production projects and test projects:

- `Notrelix.Domain.Tests`
- `Notrelix.Application.Tests`
- `Notrelix.Infrastructure.Tests`
- `Notrelix.Platform.Tests`
- `Notrelix.API.Tests`
- `Notrelix.Integration.Tests`
- `Notrelix.Architecture.Tests`
- Testing support libraries under `Notrelix.Testing.*`

## Normative Contracts

- Domain tests prove aggregate/rule behavior without provider bootstrapping.
- Application tests prove use-case orchestration, authorization, transactions,
  idempotency and result semantics.
- Infrastructure tests prove persistence mappings, RLS, migrations, adapters and
  cache/provider mechanics.
- Platform tests prove outbox, post-commit, idempotency, retry/dead-letter and
  background execution mechanisms.
- API tests prove binding, authentication integration, result/error translation
  and public contract behavior.
- Integration tests prove production graph behavior across layers.
- Architecture tests enforce dependency direction and forbidden references.
- OpenAPI/contract drift checks are required when public API shape changes.
- Critical gates must do non-zero work; empty success is not proof.

## Allowed Design

- Focused tests while implementing, followed by broader gates for public,
  persistence, architecture or cross-layer changes.
- Test fixtures/support packages that do not weaken production boundaries.

## Forbidden Design

- Skipping or weakening architecture tests to land a dependency shortcut.
- Deleting a valid failing test instead of fixing the regression.
- Claiming certification from compile-only success.
- Manual snapshots regenerated without reviewing diff meaning.

## Failure Modes

- A test command passes because it selected zero tests.
- Integration-only behavior is claimed from unit tests.
- Architecture docs point to stale projects not in `backend.slnx`.

## Change Impact Rules

Run the narrowest useful test first, then the required broader gate for the
changed contract. Public API, migrations, RLS, messaging and architecture changes
require broader validation than local unit tests.

## Executable Evidence / Tests / Gates

```bash
cd backend
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

## Related ADRs

See `../decisions/README.md`.

## Related Source Manifests

`backend/backend.slnx`.

## Non-responsibilities

This document does not define frontend validation or issue/project workflow.
