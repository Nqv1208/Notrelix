# Notrelix Backend

The backend is a .NET modular monolith for the Notrelix enterprise work
management platform. It owns server-authoritative product behavior, persistence,
security enforcement, public API contracts and background delivery.

## Projects

`backend/backend.slnx` is the project inventory authority. Production projects:

- `src/Notrelix.Domain`
- `src/Notrelix.Application`
- `src/Notrelix.Infrastructure`
- `src/Notrelix.Platform`
- `src/Notrelix.API`

Tests live under `tests/` and are split by layer plus integration,
architecture, and shared testing support projects.

## Prerequisites

- .NET SDK compatible with the solution target frameworks.
- Docker for local PostgreSQL/Redis and dependent services.
- Environment values from `.env.example` or the selected `.env.*` file.

## Commands

```bash
dotnet restore backend.slnx
dotnet build backend.slnx
dotnet test backend.slnx
```

From the repository root, Docker-backed helpers are available through `make`:

```bash
make dev-up
make be-build
make be-test
make db-init
```

## Runtime and Configuration

Read [configuration and runtime](docs/operations/configuration-and-runtime.md)
for environment precedence, Docker dependencies, options validation and safe
local reset/seed guidance.

## Documentation

- [Backend agent contract](AGENTS.md)
- [Backend documentation index](docs/README.md)
- [Backend overview](docs/architecture/backend-overview.md)
- [Testing and quality gates](docs/architecture/testing-and-quality-gates.md)
