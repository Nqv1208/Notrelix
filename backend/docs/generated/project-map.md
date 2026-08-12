---
document_id: BE-GEN-PROJECT-MAP
document_type: generated
status: generated
owner: backend-architecture
applies_to:
  - backend-project-inventory
  - backend-project-references
  - backend-test-project-relationships
evidence:
  - backend/backend.slnx
  - backend/src/**/*.csproj
  - backend/tests/**/*.csproj
review_on:
  - generated
---

# Backend Project Map

> **GENERATED FILE — DO NOT EDIT.**
>
> Source of truth: `backend/backend.slnx` and the `.csproj` files listed by that solution.
>
> Regenerate:
>
> `node scripts/docs/generate-backend-project-map.mjs`
>
> Check drift without writing:
>
> `node scripts/docs/generate-backend-project-map.mjs --check`

This file is a **source-derived inventory**, not normative architecture.

For project roles, allowed dependency direction, bounded-context placement, and rules for adding a production project, read:

- `backend/docs/architecture/backend-overview.md`
- `backend/docs/architecture/testing-and-quality-gates.md`

## Generated summary

| Type | Count |
|---|---:|
| Production | 5 |
| Test | 7 |
| Testing support | 4 |
| **Total** | **16** |

The generator derives project type from source location plus `<IsTestProject>` and uses recognized solution comments only as consistency hints. It fails if a project cannot be classified, if a solution hint conflicts with the project manifest, if a referenced project is missing, or if a direct `ProjectReference` points to a project outside the solution inventory.

## Production projects

| Project | SDK | Project references | Package references | Test relationship |
|---|---|---|---|---|
| `Notrelix.Domain` | `Microsoft.NET.Sdk` | — | — | Directly referenced by: `Notrelix.Architecture.Tests`, `Notrelix.Domain.Tests`, `Notrelix.Application.Tests`, `Notrelix.Infrastructure.Tests`, `Notrelix.API.Tests`, `Notrelix.Integration.Tests`, `Notrelix.Platform.Tests` |
| `Notrelix.Application` | `Microsoft.NET.Sdk` | `Notrelix.Domain` | `AutoMapper`<br>`FluentValidation`<br>`FluentValidation.DependencyInjectionExtensions`<br>`MediatR`<br>`Microsoft.EntityFrameworkCore`<br>`Microsoft.Extensions.Hosting` | Directly referenced by: `Notrelix.Architecture.Tests`, `Notrelix.Application.Tests`, `Notrelix.Infrastructure.Tests`, `Notrelix.API.Tests`, `Notrelix.Integration.Tests`, `Notrelix.Platform.Tests` |
| `Notrelix.Infrastructure` | `Microsoft.NET.Sdk` | `Notrelix.Application`<br>`Notrelix.Domain` | `BCrypt.Net-Next`<br>`EFCore.NamingConventions`<br>`MassTransit`<br>`MassTransit.RabbitMQ`<br>`Microsoft.AspNetCore.Authentication.JwtBearer`<br>`Microsoft.AspNetCore.Identity.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore.Design`<br>`Microsoft.EntityFrameworkCore.Tools`<br>`Microsoft.Extensions.Caching.StackExchangeRedis`<br>`Npgsql.EntityFrameworkCore.PostgreSQL`<br>`Resend`<br>`Serilog.AspNetCore`<br>`Serilog.Sinks.Console`<br>`Serilog.Sinks.File`<br>`StackExchange.Redis`<br>`System.IdentityModel.Tokens.Jwt` | Directly referenced by: `Notrelix.Architecture.Tests`, `Notrelix.Infrastructure.Tests`, `Notrelix.API.Tests`, `Notrelix.Integration.Tests` |
| `Notrelix.API` | `Microsoft.NET.Sdk.Web` | `Notrelix.Infrastructure`<br>`Notrelix.Application` | `Asp.Versioning.Mvc`<br>`Microsoft.AspNetCore.Authentication.JwtBearer`<br>`Microsoft.AspNetCore.OpenApi`<br>`Microsoft.EntityFrameworkCore.Design`<br>`Swashbuckle.AspNetCore` | Directly referenced by: `Notrelix.Architecture.Tests`, `Notrelix.API.Tests`, `Notrelix.Integration.Tests` |
| `Notrelix.Platform` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Application` | `Microsoft.Extensions.DependencyInjection.Abstractions`<br>`Microsoft.Extensions.Logging.Abstractions` | Directly referenced by: `Notrelix.Platform.Tests` |

## Test projects

| Project | SDK | Project references | Package references | Test relationship |
|---|---|---|---|---|
| `Notrelix.Architecture.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Application`<br>`Notrelix.Infrastructure`<br>`Notrelix.API` | `coverlet.collector`<br>`FluentAssertions`<br>`Microsoft.CodeAnalysis.CSharp`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Domain`, `Notrelix.Application`, `Notrelix.Infrastructure`, `Notrelix.API` |
| `Notrelix.Domain.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain` | `coverlet.collector`<br>`FluentAssertions`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Domain` |
| `Notrelix.Application.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Application`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain`<br>`Notrelix.Testing.Application` | `coverlet.collector`<br>`FluentAssertions`<br>`Microsoft.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore.InMemory`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Domain`, `Notrelix.Application` |
| `Notrelix.Infrastructure.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Application`<br>`Notrelix.Infrastructure`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain`<br>`Notrelix.Testing.Application` | `coverlet.collector`<br>`FluentAssertions`<br>`Microsoft.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore.InMemory`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`SSH.NET`<br>`Testcontainers.PostgreSQL`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Domain`, `Notrelix.Application`, `Notrelix.Infrastructure` |
| `Notrelix.API.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Application`<br>`Notrelix.Infrastructure`<br>`Notrelix.API`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain`<br>`Notrelix.Testing.Application` | `coverlet.collector`<br>`FluentAssertions`<br>`Microsoft.AspNetCore.Mvc.Testing`<br>`Microsoft.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore.InMemory`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Domain`, `Notrelix.Application`, `Notrelix.Infrastructure`, `Notrelix.API` |
| `Notrelix.Integration.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Application`<br>`Notrelix.Infrastructure`<br>`Notrelix.API`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain`<br>`Notrelix.Testing.Application`<br>`Notrelix.Testing.Integration` | `coverlet.collector`<br>`FluentAssertions`<br>`MediatR`<br>`Microsoft.AspNetCore.Mvc.Testing`<br>`Microsoft.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore.InMemory`<br>`Microsoft.EntityFrameworkCore.Relational`<br>`Microsoft.Extensions.Caching.StackExchangeRedis`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`SSH.NET`<br>`Testcontainers`<br>`Testcontainers.PostgreSQL`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Domain`, `Notrelix.Application`, `Notrelix.Infrastructure`, `Notrelix.API` |
| `Notrelix.Platform.Tests` | `Microsoft.NET.Sdk` | `Notrelix.Platform`<br>`Notrelix.Domain`<br>`Notrelix.Application` | `coverlet.collector`<br>`FluentAssertions`<br>`Microsoft.NET.Test.Sdk`<br>`Moq`<br>`xunit`<br>`xunit.runner.visualstudio` | Directly exercises: `Notrelix.Platform`, `Notrelix.Domain`, `Notrelix.Application` |

## Testing support projects

| Project | SDK | Project references | Package references | Used directly by tests |
|---|---|---|---|---|
| `Notrelix.Testing.Core` | `Microsoft.NET.Sdk` | — | — | `Notrelix.Domain.Tests`<br>`Notrelix.Application.Tests`<br>`Notrelix.Infrastructure.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests` |
| `Notrelix.Testing.Domain` | `Microsoft.NET.Sdk` | `Notrelix.Domain`<br>`Notrelix.Testing.Core` | — | `Notrelix.Domain.Tests`<br>`Notrelix.Application.Tests`<br>`Notrelix.Infrastructure.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests` |
| `Notrelix.Testing.Application` | `Microsoft.NET.Sdk` | `Notrelix.Application`<br>`Notrelix.Domain`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain` | — | `Notrelix.Application.Tests`<br>`Notrelix.Infrastructure.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests` |
| `Notrelix.Testing.Integration` | `Microsoft.NET.Sdk` | `Notrelix.Infrastructure`<br>`Notrelix.Application`<br>`Notrelix.Domain`<br>`Notrelix.Testing.Core`<br>`Notrelix.Testing.Domain`<br>`Notrelix.Testing.Application` | `Microsoft.EntityFrameworkCore`<br>`Microsoft.EntityFrameworkCore.InMemory` | `Notrelix.Integration.Tests` |

## Production-to-test relationship

This matrix is derived only from **direct** `ProjectReference` edges from test projects.

It does not claim that every test project proves every behavior in the referenced production assembly.

| Production project | Direct test-project relationships |
|---|---|
| `Notrelix.Domain` | `Notrelix.Architecture.Tests`<br>`Notrelix.Domain.Tests`<br>`Notrelix.Application.Tests`<br>`Notrelix.Infrastructure.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests`<br>`Notrelix.Platform.Tests` |
| `Notrelix.Application` | `Notrelix.Architecture.Tests`<br>`Notrelix.Application.Tests`<br>`Notrelix.Infrastructure.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests`<br>`Notrelix.Platform.Tests` |
| `Notrelix.Infrastructure` | `Notrelix.Architecture.Tests`<br>`Notrelix.Infrastructure.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests` |
| `Notrelix.API` | `Notrelix.Architecture.Tests`<br>`Notrelix.API.Tests`<br>`Notrelix.Integration.Tests` |
| `Notrelix.Platform` | `Notrelix.Platform.Tests` |

## Direct project-reference edges

| From | Type | Directly references |
|---|---|---|
| `Notrelix.Application` | Production | `Notrelix.Domain` |
| `Notrelix.Infrastructure` | Production | `Notrelix.Application` |
| `Notrelix.Infrastructure` | Production | `Notrelix.Domain` |
| `Notrelix.API` | Production | `Notrelix.Infrastructure` |
| `Notrelix.API` | Production | `Notrelix.Application` |
| `Notrelix.Platform` | Production | `Notrelix.Domain` |
| `Notrelix.Platform` | Production | `Notrelix.Application` |
| `Notrelix.Architecture.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.Architecture.Tests` | Test | `Notrelix.Application` |
| `Notrelix.Architecture.Tests` | Test | `Notrelix.Infrastructure` |
| `Notrelix.Architecture.Tests` | Test | `Notrelix.API` |
| `Notrelix.Domain.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.Domain.Tests` | Test | `Notrelix.Testing.Core` |
| `Notrelix.Domain.Tests` | Test | `Notrelix.Testing.Domain` |
| `Notrelix.Application.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.Application.Tests` | Test | `Notrelix.Application` |
| `Notrelix.Application.Tests` | Test | `Notrelix.Testing.Core` |
| `Notrelix.Application.Tests` | Test | `Notrelix.Testing.Domain` |
| `Notrelix.Application.Tests` | Test | `Notrelix.Testing.Application` |
| `Notrelix.Infrastructure.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.Infrastructure.Tests` | Test | `Notrelix.Application` |
| `Notrelix.Infrastructure.Tests` | Test | `Notrelix.Infrastructure` |
| `Notrelix.Infrastructure.Tests` | Test | `Notrelix.Testing.Core` |
| `Notrelix.Infrastructure.Tests` | Test | `Notrelix.Testing.Domain` |
| `Notrelix.Infrastructure.Tests` | Test | `Notrelix.Testing.Application` |
| `Notrelix.API.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.API.Tests` | Test | `Notrelix.Application` |
| `Notrelix.API.Tests` | Test | `Notrelix.Infrastructure` |
| `Notrelix.API.Tests` | Test | `Notrelix.API` |
| `Notrelix.API.Tests` | Test | `Notrelix.Testing.Core` |
| `Notrelix.API.Tests` | Test | `Notrelix.Testing.Domain` |
| `Notrelix.API.Tests` | Test | `Notrelix.Testing.Application` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Application` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Infrastructure` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.API` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Testing.Core` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Testing.Domain` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Testing.Application` |
| `Notrelix.Integration.Tests` | Test | `Notrelix.Testing.Integration` |
| `Notrelix.Platform.Tests` | Test | `Notrelix.Platform` |
| `Notrelix.Platform.Tests` | Test | `Notrelix.Domain` |
| `Notrelix.Platform.Tests` | Test | `Notrelix.Application` |
| `Notrelix.Testing.Domain` | Testing support | `Notrelix.Domain` |
| `Notrelix.Testing.Domain` | Testing support | `Notrelix.Testing.Core` |
| `Notrelix.Testing.Application` | Testing support | `Notrelix.Application` |
| `Notrelix.Testing.Application` | Testing support | `Notrelix.Domain` |
| `Notrelix.Testing.Application` | Testing support | `Notrelix.Testing.Core` |
| `Notrelix.Testing.Application` | Testing support | `Notrelix.Testing.Domain` |
| `Notrelix.Testing.Integration` | Testing support | `Notrelix.Infrastructure` |
| `Notrelix.Testing.Integration` | Testing support | `Notrelix.Application` |
| `Notrelix.Testing.Integration` | Testing support | `Notrelix.Domain` |
| `Notrelix.Testing.Integration` | Testing support | `Notrelix.Testing.Core` |
| `Notrelix.Testing.Integration` | Testing support | `Notrelix.Testing.Domain` |
| `Notrelix.Testing.Integration` | Testing support | `Notrelix.Testing.Application` |

## Explicit internal test seams

| Project | `InternalsVisibleTo` |
|---|---|
| `Notrelix.Domain` | `Notrelix.Domain.Tests` |
| `Notrelix.Platform` | `Notrelix.Platform.Tests` |

## Source inputs

The current generation read:

- `backend/backend.slnx`
- `backend/src/Notrelix.Domain/Notrelix.Domain.csproj`
- `backend/src/Notrelix.Application/Notrelix.Application.csproj`
- `backend/src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj`
- `backend/src/Notrelix.API/Notrelix.API.csproj`
- `backend/src/Notrelix.Platform/Notrelix.Platform.csproj`
- `backend/tests/Notrelix.Architecture.Tests/Notrelix.Architecture.Tests.csproj`
- `backend/tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj`
- `backend/tests/Notrelix.Application.Tests/Notrelix.Application.Tests.csproj`
- `backend/tests/Notrelix.Infrastructure.Tests/Notrelix.Infrastructure.Tests.csproj`
- `backend/tests/Notrelix.API.Tests/Notrelix.API.Tests.csproj`
- `backend/tests/Notrelix.Integration.Tests/Notrelix.Integration.Tests.csproj`
- `backend/tests/Notrelix.Platform.Tests/Notrelix.Platform.Tests.csproj`
- `backend/tests/Notrelix.Testing.Core/Notrelix.Testing.Core.csproj`
- `backend/tests/Notrelix.Testing.Domain/Notrelix.Testing.Domain.csproj`
- `backend/tests/Notrelix.Testing.Application/Notrelix.Testing.Application.csproj`
- `backend/tests/Notrelix.Testing.Integration/Notrelix.Testing.Integration.csproj`

## Generation contract

The generator MUST derive this document from the solution and project manifests.

It MUST NOT:

- infer product or layer ownership from project names;
- invent a human-authored "current role" column;
- treat package presence as architecture permission;
- infer transitive references as direct references;
- silently ignore projects outside the recognized backend source/test locations;
- treat solution comments as stronger evidence than project path/`<IsTestProject>`;
- leave placeholder rows such as "inspect csproj later".

Architecture semantics remain in canonical authored documents.

Source inventory remains here.

---

Generated by `scripts/docs/generate-backend-project-map.mjs`.
