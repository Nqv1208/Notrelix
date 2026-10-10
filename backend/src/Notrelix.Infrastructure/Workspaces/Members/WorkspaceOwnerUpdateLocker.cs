using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Workspaces.Members;

/// <summary>
/// PostgreSQL implementation of <see cref="IWorkspaceOwnerUpdateLocker"/> that
/// takes an exclusive <c>SELECT ... FOR UPDATE</c> row lock on the active
/// workspace row. The lock is held until the owning request transaction commits
/// or rolls back (the transaction is open for the full handler execution), so a
/// concurrent owner-affecting membership operation that reaches the same
/// workspace is serialized and re-reads the owner count after the first
/// transaction commits instead of using a stale pre-commit count.
/// </summary>
public sealed class WorkspaceOwnerUpdateLocker : IWorkspaceOwnerUpdateLocker
{
    private readonly ApplicationDbContext _context;

    public WorkspaceOwnerUpdateLocker(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Workspace?> LockWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return (await _context.Workspaces
            .FromSqlInterpolated($"""
                SELECT *
                FROM workspace.workspaces
                WHERE id = {workspaceId}
                  AND status = 'Active'
                  AND deleted_at IS NULL
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken))
            .SingleOrDefault();
    }
}