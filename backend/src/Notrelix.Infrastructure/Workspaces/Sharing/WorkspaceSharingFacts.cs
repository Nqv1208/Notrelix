using Notrelix.Application.Features.Workspaces.Public.Sharing;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Workspaces.Sharing;

public sealed class WorkspaceSharingFactsReader : IWorkspaceSharingFacts
{
    private readonly ApplicationDbContext _context;

    public WorkspaceSharingFactsReader(ApplicationDbContext context) => _context = context;

    public async Task<WorkspaceSharingFacts?> FindAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var row = await _context.Workspaces.AsNoTracking()
            .Where(w => w.Id == workspaceId && w.DeletedAt == null)
            .Select(w => new { w.Status, w.Settings.AllowPublicSharing })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : new WorkspaceSharingFacts(row.Status == WorkspaceStatus.Active, row.AllowPublicSharing);
    }
}
