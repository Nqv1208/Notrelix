using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Executes the canonical <see cref="ResourceVisibilityQuery"/> statements on the
/// request's data-session connection and transaction, so tenant RLS context and
/// read-only mode apply exactly as for the facts query.
/// </summary>
public sealed class PostgresResourceVisibility : IResourceVisibility
{
    private const string BoardKind = "work-management.board";
    private const string PageKind = "documents.page";

    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresResourceVisibility(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<Guid>> VisibleBoardIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        QueryAsync(ResourceVisibilityQuery.VisibleBoardsSql, workspaceId, userId, BoardKind, ResourceVisibilityQuery.BoardReadAction, cancellationToken);

    public Task<IReadOnlyList<Guid>> VisiblePageIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        QueryAsync(ResourceVisibilityQuery.VisiblePagesSql, workspaceId, userId, PageKind, ResourceVisibilityQuery.PageReadAction, cancellationToken);

    public Task<IReadOnlyList<Guid>> VisibleSpaceIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        QueryAsync(ResourceVisibilityQuery.VisibleSpacesSql, workspaceId, userId, resourceType: null, action: null, cancellationToken);

    private async Task<IReadOnlyList<Guid>> QueryAsync(
        string sql,
        Guid workspaceId,
        Guid userId,
        string? resourceType,
        string? action,
        CancellationToken cancellationToken)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty)
        {
            return [];
        }

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            throw new SecurityMisconfigurationException("Collection visibility requires the active data-session connection.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new SecurityMisconfigurationException("Collection visibility requires the active data-session transaction.");
        Add(command, "workspace_id", workspaceId, NpgsqlDbType.Uuid);
        Add(command, "user_id", userId, NpgsqlDbType.Uuid);
        Add(command, "resource_type", resourceType, NpgsqlDbType.Text);
        Add(command, "action", action, NpgsqlDbType.Text);
        Add(command, "now", _timeProvider.GetUtcNow(), NpgsqlDbType.TimestampTz);

        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object? value, NpgsqlDbType type) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
}
