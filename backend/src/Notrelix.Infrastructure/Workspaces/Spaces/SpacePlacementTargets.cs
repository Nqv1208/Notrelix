using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.SpacePlacement;
using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Infrastructure.Workspaces.Spaces;

/// <summary>Workspaces-owned implementation of the published Space placement-target facts.</summary>
public sealed class SpacePlacementTargets : ISpacePlacementTargets
{
    private readonly IWorkspaceDbContext _context;

    public SpacePlacementTargets(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<SpacePlacementTarget?> FindAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken) =>
        await _context.Spaces
            .AsNoTracking()
            .Where(s => s.Id == spaceId && s.WorkspaceId == workspaceId)
            .Select(s => new SpacePlacementTarget(s.Id, s.WorkspaceId, s.Status == SpaceStatus.Active))
            .FirstOrDefaultAsync(cancellationToken);
}
