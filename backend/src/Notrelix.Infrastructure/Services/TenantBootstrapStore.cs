using System.Data;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Services;

public sealed class TenantBootstrapStore : ITenantBootstrapStore
{
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IAccountDbContext _accountContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ICurrentTenantContext _tenant;

    public TenantBootstrapStore(
        IWorkspaceDbContext workspaceContext,
        IAccountDbContext accountContext,
        IPermissionEvaluator permissionEvaluator,
        ICurrentTenantContext tenant)
    {
        _workspaceContext = workspaceContext;
        _accountContext = accountContext;
        _permissionEvaluator = permissionEvaluator;
        _tenant = tenant;
    }

    public async Task<WorkspaceAccessSnapshot> ResolveWorkspaceAccessAsync(
        Guid workspaceId,
        Guid actorUserId,
        CancellationToken ct)
    {
        await EnsureBootstrapConnectionAsync(ct);

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

    public async Task VerifyAccountAccessAsync(Guid accountId, Guid userId, CancellationToken ct)
    {
        var hasAccess = await _accountContext.AccountMembers
            .AnyAsync(m => m.AccountId == accountId
                           && m.UserId == userId
                           && m.Status == AccountMemberStatus.Active,
                ct);

        if (!hasAccess)
            throw new ForbiddenException($"User {userId} does not have active access to account {accountId}.");
    }

    private async Task EnsureBootstrapConnectionAsync(CancellationToken ct)
    {
        var concreteContext = (ApplicationDbContext)(object)_workspaceContext;
        var connection = (NpgsqlConnection)concreteContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var userId = _tenant.UserId?.ToString() ?? "";
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? "";

        await using var cmd = new NpgsqlCommand($@"
            SELECT set_config('app.current_user_id', @userId, true);
            SELECT set_config('app.request_scope', 'app', true);
            SELECT set_config('app.correlation_id', @correlationId, true);
        ", connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@correlationId", correlationId);
        await cmd.ExecuteScalarAsync(ct);
    }
}
