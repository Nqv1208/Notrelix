# 04 — Persistence, DbContext, Tenant Filter and RLS Rules

## 1. ApplicationDbContext role

`ApplicationDbContext` is the single EF Core runtime DbContext mapping multiple bounded-context interfaces.

It may implement:

```txt
IApplicationDbContext
IWorkspaceDbContext
IWorkManagementDbContext
IIdentityDbContext
IAccountDbContext
IDocumentDbContext
ICollaborationDbContext
IAutomationDbContext
IGovernanceDbContext
IIntegrationDbContext
IBillingDbContext
IReportingDbContext
ISearchProjectionDbContext
IMessagingDbContext
IAuditDbContext
IOpsDbContext
...
```

Rule:

- Application handlers inject bounded-context interfaces, not `ApplicationDbContext`.
- Infrastructure services may inject `ApplicationDbContext` only when implementing persistence/runtime concerns.
- API endpoints must never inject `ApplicationDbContext`.

## 2. DbContext registration

`PersistenceRegistration` owns:

- DbContext registration.
- EF interceptors.
- bounded-context interface mapping.
- RLS services.
- outbox persistence infra.

Do not register DbContext in feature folders.

## 3. Query filter rule

Workspace-scoped entities must implement Domain `IWorkspaceScoped` and expose `AccountId` + `WorkspaceId`.

The global query filter must enforce:

```txt
SystemContext OR (entity.AccountId == current.AccountId AND entity.WorkspaceId == current.WorkspaceId)
```

Soft-deletable entities must be filtered by `DeletedAt == null`.

Do not add ad-hoc tenant filters manually as a replacement for global filter/RLS. Manual filters may be added as defense-in-depth, not as the only layer.

## 4. Null tenant behavior

If `ApplicationDbContext` is created without tenant context, workspace-scoped access must be blocked by default.

Rule:

```txt
No tenant context -> no tenant-scoped data access.
```

Only design-time factory/system jobs may create context without normal HTTP/request tenant, and they must use explicit system context or migration context.

## 5. RLS session rule

`IRlsSessionContext.ApplyAsync(...)` must set PostgreSQL session variables before querying tenant-scoped data.

Required session keys:

```txt
app.current_user_id
app.current_account_id
app.current_workspace_id
app.request_scope
app.correlation_id
```

Rule:

- Non-system request requires AccountId.
- Workspace request requires WorkspaceId.
- If RLS session context is disabled, non-system request must fail fast.

## 6. Transaction/RLS order

For request pipeline:

```txt
Begin transaction
Apply RLS session
Run handler/query
SaveChanges if transactional
Commit
```

For consumer/background worker:

```txt
Create scope
Set tenant/system context
Begin transaction if writing
Apply RLS session
Execute work
SaveChanges
Commit
Clear tenant context
```

## 7. System context rule

System context bypasses tenant filter. It is dangerous.

Allowed only for:

- migrations/initialization/seed where appropriate.
- outbox dispatcher infrastructure.
- background jobs designed for global/system work.
- maintenance jobs with explicit audit/log.

Forbidden:

- use case handler bypass.
- API endpoint bypass.
- shortcut for failing tests.

Every `SetSystem()` or `SystemContextScope` usage must be documented or allowlisted in architecture tests.

## 8. Raw SQL rule

Raw SQL is allowed only in Infrastructure persistence services when EF cannot express the operation safely or efficiently.

Required:

- Parameterized SQL only.
- Tenant/account/workspace condition must be explicit unless system context is required.
- RLS session must be applied before execution.
- Method name must state intent.
- Add tests for tenant isolation.

Forbidden:

```csharp
FromSqlRaw($"... {userInput} ...")
ExecuteSqlRaw($"... {userInput} ...")
```

Allowed:

```csharp
FromSqlRaw("... WHERE workspace_id = {0}", workspaceId)
ExecuteSqlInterpolatedAsync($"SELECT set_config(...)")
```

## 9. IgnoreQueryFilters rule

`IgnoreQueryFilters()` is forbidden by default.

Allowed only in:

- resource scope resolver needing to discover tenant for a resource.
- admin/system maintenance job.
- restore/soft-delete lifecycle service.

Requirements:

- Must have explicit tenant/account/resource guard.
- Must be covered by tests.
- Must be allowlisted in architecture tests.

## 10. SaveChanges rule

Application handlers implementing `ITransactionalRequest` must not call `SaveChangesAsync` directly.

Infrastructure services may call `SaveChangesAsync` only when they own their transaction boundary:

- Background worker.
- Consumer filter/executor.
- Outbox dispatcher.
- Seed/initializer.
- Test fixture.

## 11. Concurrency token rule

All `AggregateRoot.Version` columns must be mapped as concurrency tokens.

Requests implementing `IExpectedVersionRequest` must fail fast if version cannot be verified. Do not silently skip concurrency.

## 12. Persistence tests

Minimum tests:

- Soft-deleted entity not returned.
- Tenant A cannot query tenant B entity.
- System context can access only in allowlisted test path.
- RLS disabled for non-system request throws.
- Raw SQL service respects tenant boundary.
- IgnoreQueryFilters usage is allowlisted and guarded.
