using System.Text.Json;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Purge of deleted Chat spaces (UC-CHT-H05, CHT-018, CHT-021). A channel deleted more than 30 days ago (<see cref="Space.RestoreWindow"/>)
/// can no longer be restored, and this job removes it and everything of Chat that names it for good. Bound from <c>Chat:Spaces:Purge</c>
/// and validated on start. Off by default, like the deleted board purge: removing a space is irreversible, so an operator turns it on.
/// See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class DeletedChatSpacePurgeOptions
{
    public const string SectionName = "Chat:Spaces:Purge";

    /// <summary>When false nothing is purged and a deleted space stays past its window. Default false.</summary>
    public bool Enabled { get; init; }

    /// <summary>Time between passes. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum spaces purged per pass (each in its own transaction). Default 20.</summary>
    public int BatchSize { get; init; } = 20;
}

public sealed class DeletedChatSpacePurgeOptionsValidator : IValidateOptions<DeletedChatSpacePurgeOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 1_000;

    public ValidateOptionsResult Validate(string? name, DeletedChatSpacePurgeOptions options)
    {
        var failures = new List<string>();
        var section = DeletedChatSpacePurgeOptions.SectionName;
        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One purge pass, built like <see cref="DeletedBoardPurger"/> and sharing its foreign-key plan. Each expired space is purged in its own
/// transaction under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass): the space row is claimed with
/// <c>FOR UPDATE SKIP LOCKED</c>, so several instances never purge one space twice and a restore that races the purge waits, then finds
/// NotFound. It removes every row of a <c>chat</c> table that names the space in its uuid <c>space_id</c> column (messages, members,
/// threads, pins, reactions, mentions, calls and the like), the rows that follow those by foreign key, then the space, and writes one audit
/// fact. A failure rolls the space back and the next pass retries it; a purged space is gone, so a second pass changes nothing.
/// </summary>
/// <remarks>
/// Kept: the audit trail, and rows of other contexts that name the space without a foreign key (Inbox notifications), which follow their own retention.
/// The uploaded files of the space (Collaboration attachment rows and their storage objects) are purged with it. There is no legal hold in the platform, so none can skip a space.
/// shortcut: one transaction per space; upgrade to chunked deletes when one space holds millions of messages.
/// </remarks>
internal sealed class DeletedChatSpacePurger(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    IDateTimeProvider clock,
    IStorageService storage,
    IOptions<DeletedChatSpacePurgeOptions> options,
    ILogger<DeletedChatSpacePurger> logger)
{
    private const string SpacesTable = "chat.spaces";

    private static readonly SystemOperationReason PurgeReason =
        new("Chat", "Purge the channels deleted more than 30 days ago");

    private IReadOnlyList<DeletedBoardPurger.PurgeStep>? _plan;

    /// <returns>The number of spaces purged.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        // Strictly older than the window: a space is restorable up to and including its last instant.
        var cutoff = clock.UtcNow - Space.RestoreWindow;
        var purged = 0;
        while (purged < options.Value.BatchSize && !cancellationToken.IsCancellationRequested)
        {
            if (!await PurgeOneAsync(cutoff, cancellationToken))
                break;
            purged++;
        }

        if (purged > 0)
            logger.LogInformation("Deleted chat space purge removed {Count} spaces", purged);
        return purged;
    }

    private async Task<bool> PurgeOneAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(tenant, logger, "deleted-chat-space.purge", PurgeReason);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await rls.ApplyAsync(cancellationToken);

        var claimed = (await context.Database.SqlQuery<PurgeTarget>($"""
            SELECT id, account_id, workspace_id, deleted_at, deleted_by
            FROM chat.spaces
            WHERE deleted_at IS NOT NULL AND deleted_at < {cutoff}
            ORDER BY deleted_at
            LIMIT 1
            FOR UPDATE SKIP LOCKED
            """).ToListAsync(cancellationToken)).SingleOrDefault();
        if (claimed is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // The files hang on the messages by resource id with no foreign key, so they go first, while the messages still name the space.
        var kind = AttachmentTargets.ChatMessage.ToString();
        var fileKeys = await context.Database.SqlQuery<string>($"""
            SELECT storage_key AS "Value"
            FROM collab.attachments
            WHERE resource_type = {kind} AND storage_key IS NOT NULL AND purged_at IS NULL
              AND resource_id IN (SELECT id FROM chat.messages WHERE space_id = {claimed.Id})
            """).ToListAsync(cancellationToken);
        long rows = await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM collab.attachments
            WHERE resource_type = {kind}
              AND resource_id IN (SELECT id FROM chat.messages WHERE space_id = {claimed.Id})
            """, cancellationToken);

        var plan = _plan ??= await ResolvePlanAsync(cancellationToken);
        foreach (var step in plan)
        {
#pragma warning disable EF1002 // identifiers come from the database catalog and are quoted; the space id is a parameter
            rows += await context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM {step.Table} WHERE {step.Predicate}", [new NpgsqlParameter("board", claimed.Id)], cancellationToken);
#pragma warning restore EF1002
        }

        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM chat.spaces WHERE id = {claimed.Id}", cancellationToken);

        var metadata = JsonSerializer.SerializeToDocument(new
        {
            accountId = claimed.AccountId,
            deletedAt = claimed.DeletedAt,
            deletedBy = claimed.DeletedBy,
            tablesEmptied = plan.Count,
            rowsRemoved = rows,
        });
        context.EnterpriseAuditLogs.Add(new AuditLog(
            claimed.WorkspaceId, null, "System", "PurgeChatSpace", "chat.space", claimed.Id, null, null,
            "Critical", "Succeeded", null, null, null, null, null, null, null, metadata, clock.UtcNow));
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await DeleteObjectsAsync(claimed, fileKeys, cancellationToken);
        logger.LogInformation("Purged deleted chat space {SpaceId}: {Rows} rows in {Tables} tables", claimed.Id, rows, plan.Count);
        return true;
    }

    /// <summary>
    /// After the commit, so a failed commit never leaves rows pointing at missing files. A failed delete is logged and never undoes the purge;
    /// the object is then unreferenced and the orphan sweep of <see cref="AttachmentObjectReaper"/> removes it on a later pass.
    /// </summary>
    private async Task DeleteObjectsAsync(PurgeTarget space, IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        foreach (var key in keys)
        {
            if (!AttachmentStorageKey.BelongsTo(key, space.AccountId, space.WorkspaceId))
            {
                logger.LogWarning("A file of purged chat space {SpaceId} has a storage key outside its tenant prefix; object not deleted", space.Id);
                continue;
            }

            try
            {
                await storage.DeleteAsync(key, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Deleting a file object of purged chat space {SpaceId} failed; the orphan sweep removes it later", space.Id);
            }
        }
    }

    /// <summary>The <c>chat</c> tables (but the spaces table) with a uuid <c>space_id</c> column, and the foreign keys outside the retained schemas.</summary>
    private async Task<IReadOnlyList<DeletedBoardPurger.PurgeStep>> ResolvePlanAsync(CancellationToken cancellationToken)
    {
        var spaceColumns = await context.Database.SqlQuery<DeletedBoardPurger.TableColumn>($"""
            SELECT format('%I.%I', n.nspname, c.relname) AS table_name, format('%I', a.attname) AS column_name
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid AND c.relkind IN ('r', 'p')
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'chat' AND c.relname <> 'spaces'
              AND a.attname = 'space_id'
              AND NOT a.attisdropped AND a.atttypid = 'uuid'::regtype
            """).ToListAsync(cancellationToken);

        var foreignKeys = await context.Database.SqlQuery<DeletedBoardPurger.ForeignKey>($"""
            SELECT format('%I.%I', cn.nspname, cc.relname) AS child,
                   format('%I.%I', pn.nspname, pc.relname) AS parent,
                   (SELECT format('%I', attname) FROM pg_attribute WHERE attrelid = k.conrelid AND attnum = k.conkey[1]) AS child_column,
                   (SELECT format('%I', attname) FROM pg_attribute WHERE attrelid = k.confrelid AND attnum = k.confkey[1]) AS parent_column,
                   cardinality(k.conkey) AS columns,
                   k.confdeltype IN ('c', 'r', 'a') AS follows
            FROM pg_constraint k
            JOIN pg_class cc ON cc.oid = k.conrelid
            JOIN pg_namespace cn ON cn.oid = cc.relnamespace
            JOIN pg_class pc ON pc.oid = k.confrelid
            JOIN pg_namespace pn ON pn.oid = pc.relnamespace
            WHERE k.contype = 'f' AND k.conrelid <> k.confrelid
              AND cn.nspname NOT IN ('audit', 'billing', 'messaging', 'events', 'ops', 'operators')
            """).ToListAsync(cancellationToken);

        return DeletedBoardPurger.BuildPlan(spaceColumns, foreignKeys, SpacesTable);
    }

    private sealed record PurgeTarget(Guid Id, Guid AccountId, Guid WorkspaceId, DateTimeOffset DeletedAt, Guid? DeletedBy);
}

/// <summary>Hosted loop of <see cref="DeletedChatSpacePurger"/>; each pass runs in its own DI scope.</summary>
internal sealed class DeletedChatSpacePurgeService(
    IServiceScopeFactory scopeFactory,
    IOptions<DeletedChatSpacePurgeOptions> options,
    ILogger<DeletedChatSpacePurgeService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("DeletedChatSpacePurge is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateWorkerAsyncScope();
                await scope.ServiceProvider.GetRequiredService<DeletedChatSpacePurger>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeletedChatSpacePurge pass failed");
            }

            try
            {
                await Task.Delay(options.Value.Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
