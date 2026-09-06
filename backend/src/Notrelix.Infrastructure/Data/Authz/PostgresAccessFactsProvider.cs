using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Requests.Gates;
using Notrelix.Application.Common.Requests.Security;

namespace Notrelix.Infrastructure.Data.Authz;

public sealed class PostgresAccessFactsProvider : IAccessFactsProvider
{
    private const string Sql = AccessFactsQuery.Sql;

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
        var target = request as IRequirePermissionTarget;
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
        Add(command, "target_subject_type", target?.TargetSubjectType, NpgsqlDbType.Text);
        Add(command, "target_subject_id", target?.TargetSubjectId, NpgsqlDbType.Uuid);
        Add(command, "target_permission_id", target?.TargetPermissionId, NpgsqlDbType.Uuid);
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
            reader.GetBoolean(13),
            NullableInt(reader, 14),
            NullableInt(reader, 15));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string? NullableString(System.Data.Common.DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? NullableInt(System.Data.Common.DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static void Add(
        System.Data.Common.DbCommand command,
        string name,
        object? value,
        NpgsqlDbType type)
    {
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
    }
}
