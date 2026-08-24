using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Requests;
using NpgsqlTypes;
using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class PostgresAccessFactsProvider : IAccessFactsProvider
{
    private const string Sql = """
        SELECT
          EXISTS (SELECT 1 FROM identity.users u WHERE u.id = @user_id AND u.deleted_at IS NULL),
          COALESCE((SELECT u.email_confirmed FROM identity.users u WHERE u.id = @user_id AND u.deleted_at IS NULL), false),
          EXISTS (SELECT 1 FROM account.accounts a WHERE a.id = @account_id AND a.deleted_at IS NULL),
          (SELECT am.role FROM account.account_members am
             WHERE am.account_id = @account_id AND am.user_id = @user_id
               AND am.status = 'Active' AND am.deleted_at IS NULL LIMIT 1),
          EXISTS (SELECT 1 FROM workspace.workspaces w WHERE w.id = @workspace_id AND w.deleted_at IS NULL),
          (SELECT wm.role FROM workspace.workspace_members wm
             WHERE wm.account_id = @account_id AND wm.workspace_id = @workspace_id
               AND wm.user_id = @user_id AND wm.status = 'Active' LIMIT 1),
          CASE
            WHEN @resource_type = 'work-management.board' THEN EXISTS (
              SELECT 1 FROM work.boards b WHERE b.id = @resource_id
                AND b.workspace_id = @workspace_id AND b.deleted_at IS NULL AND b.is_archived = false)
            WHEN @resource_type = 'workspaces.workspace' THEN EXISTS (
              SELECT 1 FROM workspace.workspaces w WHERE w.id = @resource_id AND w.deleted_at IS NULL)
            ELSE @resource_was_located
          END,
          CASE WHEN @resource_type = 'work-management.board' THEN (
            SELECT b.visibility FROM work.boards b WHERE b.id = @resource_id
              AND b.deleted_at IS NULL AND b.is_archived = false LIMIT 1) END,
          CASE WHEN @resource_type = 'work-management.board' THEN (
            SELECT bm.role FROM work.board_members bm
              WHERE bm.board_id = @resource_id AND bm.user_id = @user_id LIMIT 1) END,
          EXISTS (
            SELECT 1 FROM governance.resource_permissions rp
             WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
               AND rp.resource_type = @resource_type AND rp.resource_id = @resource_id
               AND rp.subject_type = 'User' AND rp.subject_id = @user_id
               AND rp.deleted_at IS NULL),
          COALESCE((
            SELECT jsonb_agg(jsonb_build_object('priority', pr.priority, 'effect', pr.effect) ORDER BY pr.priority)
              FROM governance.permission_rules pr
             WHERE @workspace_id IS NOT NULL
               AND pr.account_id = @account_id AND pr.workspace_id = @workspace_id
               AND pr.status = 'Active' AND pr.deleted_at IS NULL
               AND (pr.starts_at IS NULL OR pr.starts_at <= @now)
               AND (pr.expires_at IS NULL OR pr.expires_at > @now)
               AND pr.action = @action
               AND pr.subject_type = 'User' AND pr.subject_id = @user_id
               AND (pr.scope_type = 'Workspace'
                    OR ((pr.resource_type IS NULL OR pr.resource_type = @resource_type)
                        AND (pr.resource_id IS NULL OR pr.resource_id = @resource_id)))
          ), '[]'::jsonb)::text,
          EXISTS (
            SELECT 1 FROM billing.subscriptions s
             WHERE s.account_id = @account_id AND s.status = 'Active' AND s.current_period_end > @now),
          (SELECT s.tier FROM billing.subscriptions s
             WHERE s.account_id = @account_id AND s.status = 'Active' AND s.current_period_end > @now
             ORDER BY CASE s.tier
               WHEN 'Enterprise' THEN 5 WHEN 'Business' THEN 4 WHEN 'Pro' THEN 3
               WHEN 'Starter' THEN 2 ELSE 1 END DESC LIMIT 1),
          CASE WHEN @feature_code IS NULL THEN true ELSE COALESCE((
            SELECT e.status = 'Active'
               AND (e.expires_at IS NULL OR e.expires_at > @now)
               AND (e.limit_value = 0 OR COALESCE((
                   SELECT SUM(f.delta) FROM billing.feature_usage_ledger f
                    WHERE f.account_id = @account_id AND f.feature_code = @feature_code), 0) + @feature_amount <= e.limit_value)
              FROM billing.entitlements e
             WHERE e.account_id = @account_id AND e.feature_code = @feature_code
             ORDER BY e.created_at DESC LIMIT 1
          ), false) END
        """;

    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresAccessFactsProvider(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<AccessFacts> ResolveAsync(
        RequestDescriptor descriptor,
        ExecutionContextSnapshot context,
        object request,
        CancellationToken cancellationToken)
    {
        if (!descriptor.Access.RequiresDatastoreFacts)
        {
            throw new SecurityMisconfigurationException(
                $"Access facts were requested for unprotected request {descriptor.RequestType.Name}.");
        }

        var permission = request as IRequirePermission;
        var resource = permission?.Resource ?? context.Resource;
        if (descriptor.Scope == ApplicationScopeKind.Account && resource is null && context.AccountId.HasValue)
        {
            resource = ResourceRef.Create(ResourceKind.Create("accounts.account"), context.AccountId.Value);
        }

        var feature = request as IRequireFeature;
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            throw new SecurityMisconfigurationException("Access facts require the active data-session connection.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = Sql;
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new SecurityMisconfigurationException("Access facts require the active data-session transaction.");
        Add(command, "user_id", context.UserId, NpgsqlDbType.Uuid);
        Add(command, "account_id", context.AccountId, NpgsqlDbType.Uuid);
        Add(command, "workspace_id", context.WorkspaceId, NpgsqlDbType.Uuid);
        Add(command, "resource_type", resource?.Kind.Value, NpgsqlDbType.Text);
        Add(command, "resource_id", resource?.ResourceId, NpgsqlDbType.Uuid);
        Add(command, "resource_was_located", context.Resource is not null, NpgsqlDbType.Boolean);
        Add(command, "action", permission?.Action.ToString(), NpgsqlDbType.Text);
        Add(command, "feature_code", feature?.FeatureCode, NpgsqlDbType.Text);
        Add(command, "feature_amount", feature?.Amount ?? 0, NpgsqlDbType.Integer);
        Add(command, "now", _timeProvider.GetUtcNow(), NpgsqlDbType.TimestampTz);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new SecurityMisconfigurationException("Access facts query returned no row.");
        }

        var rules = JsonSerializer.Deserialize<AccessPermissionRule[]>(reader.GetString(10), JsonOptions) ?? [];
        return new AccessFacts(
            reader.GetBoolean(0),
            reader.GetBoolean(1),
            reader.GetBoolean(2),
            NullableString(reader, 3),
            reader.GetBoolean(4),
            NullableString(reader, 5),
            reader.GetBoolean(6),
            NullableString(reader, 7),
            NullableString(reader, 8),
            reader.GetBoolean(9),
            rules,
            reader.GetBoolean(11),
            NullableString(reader, 12),
            reader.GetBoolean(13));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string? NullableString(System.Data.Common.DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void Add(
        System.Data.Common.DbCommand command,
        string name,
        object? value,
        NpgsqlDbType type)
    {
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
    }
}
