using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Services;

public sealed class TenantBootstrapStore : ITenantBootstrapStore
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public TenantBootstrapStore(
        IWorkspaceDbContext workspaceContext,
        IAccountDbContext accountContext,
        IPermissionEvaluator permissionEvaluator)
    {
        _workspaceContext = workspaceContext;
        _accountContext = accountContext;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<WorkspaceAccessSnapshot> ResolveWorkspaceAccessAsync(
        Guid workspaceId,
        Guid actorUserId,
        CancellationToken ct)
    {
        var workspace = await _workspaceContext.Workspaces
            .IgnoreQueryFilters()
            .Where(w => w.Id == workspaceId)
            .Select(w => new { w.AccountId, w.Status })
            .FirstOrDefaultAsync(ct);

        if (workspace is null)
        {
            throw new NotFoundException(nameof(Workspace), workspaceId);
        }

        var isActive = workspace.Status == WorkspaceStatus.Active;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(actorUserId, workspace.AccountId, workspaceId, ResourceType.Workspace, null, PermissionAction.ViewWorkspace, Notrelix.Application.Common.Security.PermissionScope.Workspace),
            ct);

        return new WorkspaceAccessSnapshot(
            workspace.AccountId,
            workspaceId,
            actorUserId,
            CanAccess: decision.IsAllowed,
            IsWorkspaceActive: isActive);
    }

    public async Task<bool> HasAccountAccessAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await _accountContext.Accounts
            .AnyAsync(a => a.Id == accountId, cancellationToken);
    }

    public async Task<Guid> ResolveUserAccountAsync(Guid userId, CancellationToken ct)
    {
        var accountId = await _workspaceContext.WorkspaceMembers
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId)
            .Select(m => m.AccountId)
            .FirstOrDefaultAsync(ct);

        if (accountId == Guid.Empty)
            throw new InvalidOperationException($"User {userId} is not a member of any workspace account.");

        return accountId;
    }
}
