# backend/PROJECT-MAP.md — Verified Project Inventory

This document is source-derived and should be regenerated/verified from `backend/backend.slnx` and project files. It is not a normative architecture source.

## Production

| Project | Direct project references | Current role |
|---|---|---|
| `Notrelix.Domain` | none | pure business model/invariants |
| `Notrelix.Application` | Domain | use cases, ports, pipeline/orchestration |
| `Notrelix.Infrastructure` | Application, Domain | EF/persistence/providers/cache/security implementation |
| `Notrelix.Platform` | Application, Domain | reusable runtime/messaging mechanisms |
| `Notrelix.API` | inspect `.csproj` during change | host/HTTP composition |

## Tests/support in solution

```text
Notrelix.Architecture.Tests
Notrelix.Domain.Tests
Notrelix.Application.Tests
Notrelix.Infrastructure.Tests
Notrelix.API.Tests
Notrelix.Integration.Tests
Notrelix.Platform.Tests
Notrelix.Testing.Core
Notrelix.Testing.Domain
Notrelix.Testing.Application
Notrelix.Testing.Integration
```

## Drift policy

CI/docs validation should compare this inventory with `backend.slnx`. A new production project is an architecture change and requires an explicit ownership/dependency decision; adding a project to make local organization easier is insufficient justification.
