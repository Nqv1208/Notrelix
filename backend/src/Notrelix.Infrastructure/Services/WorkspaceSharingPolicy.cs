using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Infrastructure.Services;

/// <summary>Reads the workspace's <c>AllowPublicSharing</c> setting through the request's data session (workspace RLS applies).</summary>
public sealed class WorkspaceSharingPolicy : IWorkspaceSharingPolicy
{
    private readonly IWorkspaceDbContext _context;

    public WorkspaceSharingPolicy(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AllowsShareLinksAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Id == workspaceId)
            .Select(w => w.Settings.AllowPublicSharing)
            .FirstOrDefaultAsync(cancellationToken);
}
