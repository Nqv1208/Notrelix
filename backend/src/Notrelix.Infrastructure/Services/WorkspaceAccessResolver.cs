using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Services;

public sealed class WorkspaceAccessResolver : IWorkspaceAccessResolver
{
    private readonly IWorkspaceDbContext _context;

    public WorkspaceAccessResolver(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<WorkspaceAccessSnapshot> ResolveAsync(
        Guid workspaceId,
        Guid actorUserId,
        CancellationToken ct)
    {
        // Load workspace from DB — single source of truth for AccountId
        var workspace = await _context.Workspaces
            .IgnoreQueryFilters()
            .Where(w => w.Id == workspaceId)
            .Select(w => new { w.AccountId, w.Status })
            .FirstOrDefaultAsync(ct);

        if (workspace is null)
        {
            throw new NotFoundException(nameof(Workspace), workspaceId);
        }

        var isActive = workspace.Status == WorkspaceStatus.Active;

        // Workspace access is established by an active membership grant. The
        // request-scoped permission/rule evaluation remains owned by
        // AccessControlBehavior; this resolver answers only the narrower
        // "is this actor an active member of this workspace" fact.
        var canAccess = await _context.WorkspaceMembers
            .IgnoreQueryFilters()
            .AnyAsync(m => m.AccountId == workspace.AccountId
                           && m.WorkspaceId == workspaceId
                           && m.UserId == actorUserId
                           && m.Status == WorkspaceMemberStatus.Active, ct);

        return new WorkspaceAccessSnapshot(
            workspace.AccountId,
            workspaceId,
            actorUserId,
            CanAccess: canAccess,
            IsWorkspaceActive: isActive);
    }

    public async Task<WorkspaceBySlugSnapshot?> ResolveBySlugAsync(string slug, CancellationToken ct)
    {
        return await _context.Workspaces
            .Where(w => w.Slug == slug)
            .Select(w => new WorkspaceBySlugSnapshot(w.Id, w.Slug, w.AccountId))
            .FirstOrDefaultAsync(ct);
    }
}
