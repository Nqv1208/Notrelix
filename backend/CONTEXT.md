# backend/CONTEXT.md — Backend Current Snapshot

Non-normative current-state summary. Do not copy package/project inventories into additional `CONTEXT.md` files.

## Production projects

`backend/backend.slnx` currently includes:

```text
src/Notrelix.Domain
src/Notrelix.Application
src/Notrelix.Infrastructure
src/Notrelix.Platform
src/Notrelix.API
```

The solution also includes architecture, Domain, Application, Infrastructure, Platform, API and integration test projects plus shared testing-support projects.

## Current dependency evidence

- Domain currently has no package references and exposes internals to Domain tests only.
- Application references Domain and currently references MediatR, FluentValidation, AutoMapper, EF Core and hosting abstractions. EF package presence is current source evidence; new persistence ownership remains constrained by backend architecture.
- Infrastructure references Application + Domain and contains EF/Npgsql, Redis, authentication/security providers, logging and MassTransit/provider packages.
- Platform references Application + Domain and keeps test-only internal visibility for Platform tests.

## Current structural direction

Application feature placement is module-first within a bounded context:

```text
Features/{BoundedContext}/{Module}/Commands/{UseCase}
Features/{BoundedContext}/{Module}/Queries/{UseCase}
```

Legacy/alternate layouts are not a reason to add new code outside the canonical structure.

## Canonical bounded contexts

Accounts, Identity, Workspaces, Governance, Work Management, Documents, Collaboration, Automation, Integrations, Billing and Analytics/Reporting. Supporting technical modules such as Search/Operations do not automatically become business bounded contexts.
