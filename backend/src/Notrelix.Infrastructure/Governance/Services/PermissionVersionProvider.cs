using System.Data;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Governance.Services;

public sealed class PermissionVersionProvider : IPermissionVersionProvider
{
    private const string Sql = """
        SELECT GREATEST(
            COALESCE((SELECT MAX(wm.updated_at) FROM workspace.workspace_members wm WHERE wm.account_id = @accountId AND wm.workspace_id = @workspaceId AND wm.user_id = @userId), '1970-01-01'::timestamp),
            COALESCE((SELECT MAX(mra.assigned_at) FROM governance.member_role_assignments mra WHERE mra.account_id = @accountId AND mra.workspace_id = @workspaceId), '1970-01-01'::timestamp),
            COALESCE((SELECT MAX(cr.updated_at) FROM governance.custom_roles cr WHERE cr.account_id = @accountId AND cr.workspace_id = @workspaceId), '1970-01-01'::timestamp),
            COALESCE((SELECT MAX(rp.updated_at) FROM governance.resource_permissions rp WHERE rp.account_id = @accountId AND rp.workspace_id = @workspaceId), '1970-01-01'::timestamp),
            COALESCE((SELECT MAX(pr.updated_at) FROM governance.permission_rules pr WHERE pr.account_id = @accountId AND pr.workspace_id = @workspaceId), '1970-01-01'::timestamp)
        )
        """;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<PermissionVersionProvider> _logger;

    public PermissionVersionProvider(
        ApplicationDbContext db,
        ILogger<PermissionVersionProvider> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async ValueTask<string> GetVersionAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Sql;

        var accountParam = cmd.CreateParameter();
        accountParam.ParameterName = "accountId";
        accountParam.Value = accountId;
        accountParam.DbType = DbType.Guid;
        cmd.Parameters.Add(accountParam);

        var workspaceParam = cmd.CreateParameter();
        workspaceParam.ParameterName = "workspaceId";
        workspaceParam.Value = workspaceId;
        workspaceParam.DbType = DbType.Guid;
        cmd.Parameters.Add(workspaceParam);

        var userParam = cmd.CreateParameter();
        userParam.ParameterName = "userId";
        userParam.Value = userId;
        userParam.DbType = DbType.Guid;
        cmd.Parameters.Add(userParam);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);

        if (result is DateTime maxUpdatedAt)
        {
            var version = $"perm:{accountId}:{workspaceId}:{userId}:{maxUpdatedAt.Ticks}";
            _logger.LogTrace("Permission version for {UserId} in account {AccountId} workspace {WorkspaceId}: {Version} (maxUpdate={MaxUpdate})",
                userId, accountId, workspaceId, version, maxUpdatedAt);
            return version;
        }

        throw new InvalidOperationException(
            $"Cannot compute permission version for user {userId} in account {accountId} workspace {workspaceId}. " +
            "Permissioned cache scope requires a valid permission version.");
    }
}
