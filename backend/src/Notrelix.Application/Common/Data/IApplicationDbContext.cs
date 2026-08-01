namespace Notrelix.Application.Common.Data;

/// <summary>
/// Minimal application-level DbContext interface.
/// Bounded-context-specific DbSets are on individual context interfaces.
/// Transaction and RLS mechanics are owned by IRequestDataSession (Infrastructure).
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
