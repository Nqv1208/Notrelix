using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Executes <see cref="ResourceAccessEntriesQuery"/> on the request's
/// data-session connection and transaction, so tenant RLS context and
/// read-only mode apply exactly as for the access facts query.
/// </summary>
public sealed class PostgresResourceAccessEntriesReader : IResourceAccessEntriesReader
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresResourceAccessEntriesReader(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ResourceAccessEntries> ReadAsync(ResourceAccessEntriesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grants = new List<ResourceGrantEntry>();
        await ReadAsync(ResourceAccessEntriesQuery.GrantsSql, request, reader => grants.Add(new ResourceGrantEntry(
            reader.GetString(0),
            reader.GetGuid(1),
            reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetGuid(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.IsDBNull(7) ? null : reader.GetInt32(7),
            reader.GetInt64(8))), cancellationToken);

        var reach = new List<AccessReachEntry>();
        await ReadAsync(ResourceAccessEntriesQuery.ReachSql, request, reader => reach.Add(new AccessReachEntry(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetBoolean(3))), cancellationToken);

        string? spaceName = null;
        if (request.SpaceId is not null)
        {
            await ReadAsync(ResourceAccessEntriesQuery.SpaceNameSql, request, reader => spaceName = reader.GetString(0), cancellationToken);
        }

        var spaceRoles = new List<SpaceRoleEntry>();
        if (request.SpaceId is not null)
        {
            await ReadAsync(ResourceAccessEntriesQuery.SpaceRolesSql, request, reader => spaceRoles.Add(new SpaceRoleEntry(
                reader.GetGuid(0),
                reader.GetInt32(1))), cancellationToken);
        }

        var hasReadRules = false;
        await ReadAsync(ResourceAccessEntriesQuery.HasReadRulesSql, request, reader => hasReadRules = reader.GetBoolean(0), cancellationToken);

        return new ResourceAccessEntries(grants, reach, spaceName, hasReadRules, spaceRoles);
    }

    private async Task ReadAsync(
        string sql,
        ResourceAccessEntriesRequest request,
        Action<System.Data.Common.DbDataReader> readRow,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            throw new SecurityMisconfigurationException("Access entries require the active data-session connection.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new SecurityMisconfigurationException("Access entries require the active data-session transaction.");
        Add(command, "account_id", request.AccountId, NpgsqlDbType.Uuid);
        Add(command, "workspace_id", request.WorkspaceId, NpgsqlDbType.Uuid);
        Add(command, "resource_type", request.ResourceType, NpgsqlDbType.Text);
        Add(command, "resource_id", request.ResourceId, NpgsqlDbType.Uuid);
        Add(command, "space_id", request.SpaceId, NpgsqlDbType.Uuid);
        Add(command, "member_ids", request.MemberUserIds.Distinct().ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Uuid);
        Add(command, "action", request.ReadAction, NpgsqlDbType.Text);
        Add(command, "now", _timeProvider.GetUtcNow(), NpgsqlDbType.TimestampTz);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            readRow(reader);
        }
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object? value, NpgsqlDbType type) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
}
