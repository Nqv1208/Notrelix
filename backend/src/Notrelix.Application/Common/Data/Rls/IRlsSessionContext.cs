namespace Notrelix.Application.Common.Data.Rls;

/// <summary>
/// Applies Row-Level Security session context for tenant isolation.
/// Infrastructure owns the database-specific implementation.
/// </summary>
public interface IRlsSessionContext
{
    Task ApplyAsync(CancellationToken cancellationToken);
}
