using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Services;

public sealed class WorkspaceAccessResolver : IWorkspaceAccessResolver
{
    private readonly IWorkspaceDbContext _context;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public WorkspaceAccessResolver(
        IWorkspaceDbContext context,
        IPermissionEvaluator permissionEvaluator)
    {
        _context = context;
        _permissionEvaluator = permissionEvaluator;
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

        // Check if actor can access this workspace
        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(
                actorUserId,
                workspace.AccountId,
                workspaceId,
                ResourceType.Workspace,
                null,
                PermissionAction.ViewWorkspace,
                Notrelix.Application.Common.Security.PermissionScope.Workspace),
            ct);

        return new WorkspaceAccessSnapshot(
            workspace.AccountId,
            workspaceId,
            actorUserId,
            CanAccess: decision.IsAllowed,
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