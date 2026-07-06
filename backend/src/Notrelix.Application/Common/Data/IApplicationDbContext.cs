namespace Notrelix.Application.Common.Data;

/// <summary>
/// Minimal application-level DbContext interface after the split refactoring.
/// Only exposes Database (for transactions) and SaveChangesAsync.
/// Bounded-context-specific DbSets are on individual context interfaces.
/// </summary>
public interface IApplicationDbContext
{
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
