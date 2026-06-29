# Test Database Strategy

## Why Not SQLite

Notrelix production uses PostgreSQL. SQLite differs from PostgreSQL in:
- Schema behavior (enums, JSON/JSONB, indexes, constraints)
- Migration semantics
- Transaction isolation and concurrency
- Case sensitivity and collation
- Full-text search (tsvector vs FTS5)
- Array types and extension support

Tests using SQLite may pass locally but fail in production. All database tests must use PostgreSQL.

## Test Categories

| Project | Strategy | DB Required | Docker Required |
|---------|----------|-------------|-----------------|
| Domain.Tests | Domain objects + helpers | None | No |
| Application.Tests | Moq mocks | None | No |
| Architecture.Tests | Reflection-based | None | No |
| Infrastructure.Tests | EF Core InMemory (structural) | No real DB | No |
| API.Tests | WebApplicationFactory + InMemory | No real DB | No |
| Integration.Tests | PostgreSQL via Testcontainers | PostgreSQL 16 | Yes (local) |

## Running Tests

```bash
# Unit tests (no Docker)
dotnet test tests/Notrelix.Domain.Tests
dotnet test tests/Notrelix.Application.Tests
dotnet test tests/Notrelix.Architecture.Tests

# Infrastructure/API tests (no Docker)
dotnet test tests/Notrelix.Infrastructure.Tests
dotnet test tests/Notrelix.API.Tests

# Integration tests (Docker required)
dotnet test tests/Notrelix.Integration.Tests

# Skip Docker-dependent tests locally
dotnet test tests/Notrelix.Integration.Tests --filter "Category!=RequiresDocker"
```

## CI PostgreSQL

The `integration` job in `.github/workflows/ci.yml` provides PostgreSQL 16 as a service container:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    env:
      POSTGRES_USER: notrelix
      POSTGRES_PASSWORD: notrelix
      POSTGRES_DB: notrelix_test
    ports:
      - 5432:5432
```

Migrations are applied via:
```bash
dotnet run --project src/Notrelix.API --no-build --no-launch-profile -- --migrate-only
```

## Package Policy

**Allowed:**
- `Npgsql.EntityFrameworkCore.PostgreSQL` — production and integration tests
- `Microsoft.EntityFrameworkCore.InMemory` — structural tests only (no real query behavior)

**Forbidden:**
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.Data.Sqlite`
- `SQLitePCLRaw.*`
- Any SQLite-related packages

## Vulnerability Scanning

CI runs strict vulnerability scanning:
```bash
dotnet list package --vulnerable --include-transitive --no-restore
```

This scan is never bypassed, skipped, or applied only to production projects. All projects including test projects must pass.

## Architecture Guards

SQLite reintroduction is prevented by:
1. `DatabaseProviderArchitectureTests` — verifies no assembly references SQLite packages
2. CI grep guard — scans source and config files for SQLite patterns
3. `CrossTenantIsolationTests` runs on PostgreSQL to verify query filter behavior matches production
