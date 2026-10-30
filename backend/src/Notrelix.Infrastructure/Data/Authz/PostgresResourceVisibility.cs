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

    public Task<IReadOnlyList<Guid>> VisibleArchivedBoardIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        QueryAsync(ResourceVisibilityQuery.VisibleArchivedBoardsSql, workspaceId, userId, BoardKind, ResourceVisibilityQuery.BoardReadAction, cancellationToken);

    public Task<IReadOnlyList<Guid>> VisiblePageIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        QueryAsync(ResourceVisibilityQuery.VisiblePagesSql, workspaceId, userId, PageKind, ResourceVisibilityQuery.PageReadAction, cancellationToken);

    public Task<IReadOnlyList<Guid>> VisibleTrashedPageIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        QueryAsync(ResourceVisibilityQuery.VisibleTrashedPagesSql, workspaceId, userId, PageKind, ResourceVisibilityQuery.PageReadAction, cancellationToken);

    public async Task<IReadOnlyList<Guid>> VisibleSpaceIdsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken) =>
        (await VisibleSpacesAsync(workspaceId, userId, cancellationToken)).Select(s => s.SpaceId).ToList();

    public async Task<IReadOnlyList<SpaceAccessEntry>> VisibleSpacesAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var rows = new List<SpaceAccessEntry>();
        await ReadAsync(
            ResourceVisibilityQuery.VisibleSpacesSql, workspaceId, userId, resourceType: null, action: null,
            reader => rows.Add(new SpaceAccessEntry(reader.GetGuid(0), LevelName(reader.GetInt32(1)))),
            cancellationToken);
        return rows;
    }

    private static string LevelName(int level) => level switch
    {
        >= 4 => "Manager",
        3 => "Editor",
        _ => "Viewer",
    };

    private async Task<IReadOnlyList<Guid>> QueryAsync(
        string sql,
        Guid workspaceId,
        Guid userId,
        string? resourceType,
        string? action,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await ReadAsync(sql, workspaceId, userId, resourceType, action, reader => ids.Add(reader.GetGuid(0)), cancellationToken);
        return ids;
    }

    private async Task ReadAsync(
        string sql,
        Guid workspaceId,
        Guid userId,
        string? resourceType,
        string? action,
        Action<System.Data.Common.DbDataReader> readRow,
        CancellationToken cancellationToken)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty)
        {
            return;
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

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            readRow(reader);
        }
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object? value, NpgsqlDbType type) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
}
