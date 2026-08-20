using System.Data;
using System.Diagnostics;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Services;

public sealed class TenantBootstrapStore : ITenantBootstrapStore
{
    private static readonly Notrelix.Domain.SharedKernel.ResourceKind WorkspaceKind = Notrelix.Domain.SharedKernel.ResourceKind.Create("workspaces.workspace");
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
        var concreteContext = (ApplicationDbContext)(object)_workspaceContext;
        var connection = (NpgsqlConnection)concreteContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        // Bootstrap resolution runs outside the pipeline's DbRequestScope (the
        // tenant context must be established before that scope can apply RLS),
        // so the RLS session variables are set inside an explicit transaction.
        // RLS policies for workspace membership/access grants read
        // app.current_user_id (and account/workspace scopes) and are only
        // honored while a transaction is active, because set_config(..., true)
        // is LOCAL-scoped.
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var correlationId = Activity.Current?.Id ?? "";
            await using (var command = new NpgsqlCommand(
                """
                SELECT set_config('app.current_user_id', @userId, true);
                SELECT set_config('app.request_scope', 'app', true);
                SELECT set_config('app.correlation_id', @correlationId, true);
                """,
                connection,
                transaction))
            {
                command.Parameters.AddWithValue("@userId", actorUserId.ToString());
                command.Parameters.AddWithValue("@correlationId", correlationId);
                await command.ExecuteScalarAsync(ct);
            }

            var workspace = await _workspaceContext.Workspaces
                .IgnoreQueryFilters()
                .Where(w => w.Id == workspaceId)
                .Select(w => new { w.AccountId, w.Status })
                .FirstOrDefaultAsync(ct);

            if (workspace is null)
            {
                throw new NotFoundException(nameof(Workspace), workspaceId);
            }

            // Establish the account/workspace RLS scopes for the permission
            // evaluation that follows inside this same transaction.
            await using (var scopeCommand = new NpgsqlCommand(
                """
                SELECT set_config('app.current_account_id', @accountId, true);
                SELECT set_config('app.current_workspace_id', @workspaceId, true);
                """,
                connection,
                transaction))
            {
                scopeCommand.Parameters.AddWithValue("@accountId", workspace.AccountId.ToString());
                scopeCommand.Parameters.AddWithValue("@workspaceId", workspaceId.ToString());
                await scopeCommand.ExecuteScalarAsync(ct);
            }

            // Establish the application-level tenant scope as well. The
            // ApplicationDbContext applies a model query filter keyed on the
            // current tenant (AccountId/WorkspaceId); bootstrap runs before the
            // pipeline's DbRequestScope has set that tenant, so permission
            // evaluation would otherwise be filtered to zero rows even for an
            // authorized member.
            _tenant.SetWorkspace(workspace.AccountId, workspaceId, actorUserId);

            var isActive = workspace.Status == WorkspaceStatus.Active;

            var decision = await _permissionEvaluator.EvaluateAsync(
                new PermissionContext(
                    actorUserId, workspace.AccountId, workspaceId, WorkspaceKind, null,
                    PermissionAction.ViewWorkspace,
                    Notrelix.Application.Common.Security.PermissionScope.Workspace),
                ct);

            await transaction.CommitAsync(ct);

            return new WorkspaceAccessSnapshot(
                workspace.AccountId,
                workspaceId,
                actorUserId,
                CanAccess: decision.IsAllowed,
                IsWorkspaceActive: isActive);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
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
}