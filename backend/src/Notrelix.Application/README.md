# Notrelix.Application — Layer Structure

> Source of truth: `../../docs/architecture/application-model.md`.

This document describes the **canonical folder structure** the Application layer is
being harmonized to. The scaffolding (empty module folders) mirrors the bounded
contexts/modules of `Notrelix.Domain`.

## Canonical layout

```
Notrelix.Application/
├── Common/                  # cross-cutting concerns (no business use-cases)
│   ├── Activity/            # activity feed abstractions
│   ├── Auditing/            # audit writer abstractions
│   ├── Behaviors/           # MediatR pipeline behaviors
│   ├── Caching/             # cache + invalidation abstractions
│   ├── Context/             # request context (CurrentUser, CurrentTenant, ICurrentRequestContext)
│   ├── Data/                # data access abstractions (unit-of-work, resource versioning)
│   ├── DTOs/                # shared DTO contracts
│   ├── Email/               # email abstractions
│   ├── Entitlements/        # feature/quota checks
│   ├── Events/              # integration event contracts
│   ├── Exceptions/          # application-level exceptions
│   ├── Idempotency/         # idempotency key handling
│   ├── Integrations/        # integration provider abstractions
│   ├── Messaging/           # message broker abstractions
│   ├── Models/              # shared DTOs/models (Result, paging, ...)
│   ├── PostCommit/          # after-commit side-effect queue
│   ├── RateLimiting/        # rate limiting abstractions
│   ├── Requests/            # request markers (ICommand, IQuery, scoping, security, gates, execution, transactions, realtime, caching)
│   │   ├── Scoping/         # IResourceScopedRequest, IWorkspaceRequest, workspace-scoped authorization markers
│   │   ├── Security/        # IRequirePermission, permission action/level markers
│   │   ├── Gates/           # request gate interfaces (feature gates, rate limits)
│   │   ├── Transactions/    # ITransactionalRequest marker
│   │   ├── Caching/         # cache-control request markers
│   │   ├── Execution/       # execution policy markers (offline, background)
│   │   └── Realtime/        # realtime broadcast request markers
│   ├── Security/            # permission context, decisions, version providers
│   ├── Storage/             # file storage abstractions
│   ├── SystemOperations/    # system-level operation abstractions
│   ├── Tenancy/             # workspace/tenant context branching
│   └── Time/                # IDateTimeProvider abstraction
│
└── Features/                # one folder per bounded context
    └── {BoundedContext}/
        └── {Module}/                       # MODULE-FIRST (vertical slice)
            ├── Commands/{UseCase}/         # {UseCase}Command/Handler/Validator/Result
            ├── Queries/{UseCase}/          # {UseCase}Query/Handler/Validator/Result
            ├── DTOs/
            ├── Services/                   # narrow, focused services (optional)
            ├── ReadModels/                 # (optional)
            ├── Mapping/                    # (optional)
            ├── Permissions/                # (optional)
            └── Cache/                      # (optional)
```

### Module-first vs legacy

Canonical (target):

```
Features/WorkManagement/Boards/Commands/CreateBoard/
Features/WorkManagement/Boards/Queries/GetBoard/
```

Legacy (being phased out — do NOT add new use cases here):

```
Features/WorkManagement/Commands/Boards/CreateBoard/
Features/WorkManagement/Queries/Boards/GetBoard/
```

## Bounded contexts (mirror Notrelix.Domain)

| Context         | Modules |
|-----------------|---------|
| Identity        | Auth, Users, Profiles, Sessions, Credentials, OAuth, Security, SSO, ApiTokens |
| Workspaces      | Workspaces, Members, Invitations, Spaces, Teams, Settings, WorkspaceHome |
| Governance      | Permissions, PermissionRules, ResourcePermissions, ShareLinks, Roles, Policies, AuditLogs, SecurityEvents |
| WorkManagement  | Boards, BoardSchema, BoardFields, BoardGroups, BoardItems, BoardViews, FieldOptions, Checklists, Labels, ItemLinks, Relations, Formulas, Rollups, Forms, Templates, Approvals, Workload, MyWork, BoardSearch, Common |
| Documents       | Pages, Blocks, Versions, ResourceLinks, Templates, Export |
| Collaboration   | Comments, Reactions, Mentions, Notifications, Activity, Attachments, Watchers, Presence |
| Automation      | Rules, Engine, Executions, Scheduled, Templates |
| Integrations    | Connections, Webhooks, Providers, Sync, Inbound |
| Billing         | Plans, Subscriptions, Entitlements, Usage, Invoices, Payments, Webhooks |
| Analytics       | Dashboards, Widgets, Snapshots, Metrics |
| Search          | GlobalSearch, BoardSearch, Indexing, Permissions |
| Operations      | ImportExport, Jobs, Idempotency, Admin |

## Migration status

The empty folders are scaffolding for the target architecture. Existing code that
still lives under legacy `Features/{Context}/Commands|Queries/{Module}/` paths must be
migrated module-first incrementally (see migration phases in the enterprise rules doc):

1. **Phase 1** — WorkManagement (Boards, BoardItems, BoardFields, BoardGroups, BoardViews, BoardSchema)
2. **Phase 2** — Governance
3. **Phase 3** — Automation + Billing
4. **Phase 4** — Documents + Collaboration + Integrations

When migrating a use case, update its namespace to
`Notrelix.Application.Features.{Context}.{Module}.Commands.{UseCase}` and remove the
matching legacy folder.
