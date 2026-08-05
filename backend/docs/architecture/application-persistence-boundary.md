# Application Persistence Boundary

> Stable. New code must follow these rules. Legacy debt is controlled via baseline.

## Freeze rules

1. **Handlers never call SaveChanges.** Transaction ownership belongs to
   `IRequestDataSession` (Infrastructure). The only exception is the
   documented `IAutomationDbContext.SaveChangesAsync` in the Automation
   post-commit action, which is part of the legacy baseline.

2. **Handlers never access DatabaseFacade or transactions.**
   No `BeginTransaction`, `CommitTransaction`, `RollbackTransaction`
   in Application code.

3. **Application never catches Npgsql or DbUpdate exceptions.**
   Provider exception translation is Infrastructure responsibility.

4. **New use cases must prefer context-owned query/repository ports.**
   When touching an existing handler, migrate its EF queries to the
   bounded-context `I*DbContext` abstraction or a dedicated read store.

5. **New shared DbSet/IQueryable interfaces require architecture review.**
   Do not add new `DbSet<T>` properties to `IApplicationDbContext` or
   create new generic repository abstractions.

## Legacy EF baseline

File: `tests/Notrelix.Architecture.Tests/Baselines/application-legacy-ef-usage.approved.txt`

- 246 files currently use EF-related types (IQueryable, DbSet, I*DbContext)
- Baseline may only **shrink** — new entries fail the architecture test
- Migrated entries are removed from the baseline
- Migration occurs when a use case is modified for a feature

## Target pattern

```csharp
// Application owns the port
public interface IBoardReadStore
{
    Task<BoardDetailsDto?> GetAsync(
        Guid workspaceId, Guid boardId, CancellationToken ct);
}

// Infrastructure implements with EF
public sealed class EfBoardReadStore : IBoardReadStore
{
    private readonly ApplicationDbContext _context;
    // ...
}
```

## Accepted transitional debt

- Existing handlers using `I*DbContext` with `IQueryable` remain in the
  approved shrinking baseline.
- `IAutomationDbContext.SaveChangesAsync` is the only handler-level
  SaveChanges in the baseline.
- Full provider independence is NOT claimed while the baseline exists.
