using System.Text.Json;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Purge of deleted workspaces (owner decision 2026-11-01). A workspace deleted more than 30 days ago
/// (<see cref="Workspace.RestoreWindow"/>) can no longer be restored, and this job removes its data for good.
/// Bound from <c>Workspaces:Deletion:Purge</c> and validated on start. See
/// <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class DeletedWorkspacePurgeOptions
{
    public const string SectionName = "Workspaces:Deletion:Purge";

    /// <summary>When false nothing is purged and deleted workspaces simply stay restorable-looking. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between passes. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum workspaces purged per pass (each in its own transaction). Default 10.</summary>
    public int BatchSize { get; init; } = 10;
}

public sealed class DeletedWorkspacePurgeOptionsValidator : IValidateOptions<DeletedWorkspacePurgeOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 1_000;

    public ValidateOptionsResult Validate(string? name, DeletedWorkspacePurgeOptions options)
    {
        var failures = new List<string>();
        var section = DeletedWorkspacePurgeOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One purge pass. Each expired workspace is purged in its own transaction under the explicit system (worker)
/// tenant context with the RLS worker scope (no RLS bypass): the workspace row is claimed with
/// <c>FOR UPDATE SKIP LOCKED</c> (several API instances can run the job without purging one workspace twice),
/// every workspace-scoped table is emptied of its rows (children before parents, by the live foreign keys),
/// the workspace row goes last and one audit fact is written, all in the same transaction. A failure rolls the
/// whole workspace back and the next pass retries it; a workspace that is gone is simply not found again, so a
/// retry or a second pass changes nothing. Restore stays possible until the transaction commits, and the
/// claim lock makes a restore that races the purge wait for it, then find NotFound.
/// </summary>
/// <remarks>
/// Kept: the audit trail, billing records (commercial evidence), and the outbox and delivery tables
/// (<see cref="RetainedSchemas"/>). Files in object storage are no longer referenced once their rows are gone and
/// are removed by the attachment orphan sweep. shortcut: one transaction per workspace, upgrade to chunked
/// deletes when a single workspace holds millions of rows.
/// </remarks>
internal sealed class DeletedWorkspacePurger
{
    internal static readonly string[] RetainedSchemas = ["audit", "billing", "messaging", "events", "ops", "operators"];

    private static readonly SystemOperationReason PurgeReason =
        new("Workspaces", "Purge the data of workspaces deleted more than 30 days ago");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly DeletedWorkspacePurgeOptions _options;
    private readonly ILogger<DeletedWorkspacePurger> _logger;
    private IReadOnlyList<string>? _purgeOrder;

    public DeletedWorkspacePurger(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IOptions<DeletedWorkspacePurgeOptions> options,
        ILogger<DeletedWorkspacePurger> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of workspaces purged.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        // Strictly older than the window: a workspace is restorable up to and including its last instant.
        var cutoff = _clock.UtcNow - Workspace.RestoreWindow;
        var purged = 0;

        while (purged < _options.BatchSize && !cancellationToken.IsCancellationRequested)
        {
            if (!await PurgeOneAsync(cutoff, cancellationToken))
                break;
            purged++;
        }

        if (purged > 0)
            _logger.LogInformation("Deleted workspace purge removed {Count} workspaces", purged);

        return purged;
    }

    private async Task<bool> PurgeOneAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "deleted-workspace.purge", PurgeReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var claimed = (await _context.Database.SqlQuery<PurgeTarget>($"""
            SELECT id, account_id, deleted_at, deleted_by
            FROM workspace.workspaces
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

        var order = _purgeOrder ??= await ResolvePurgeOrderAsync(cancellationToken);
        long rows = 0;
        foreach (var table in order)
        {
#pragma warning disable EF1002 // the identifier comes from the database catalog and is quoted; the value is a parameter
            rows += await _context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM {table} WHERE workspace_id = @id",
                [new NpgsqlParameter("id", claimed.Id)],
                cancellationToken);
#pragma warning restore EF1002
        }

        // A person's default-workspace pointer must not outlive the workspace.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE identity.user_profiles SET default_workspace_id = NULL WHERE default_workspace_id = {claimed.Id}",
            cancellationToken);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM workspace.workspaces WHERE id = {claimed.Id}", cancellationToken);

        var metadata = JsonSerializer.SerializeToDocument(new
        {
            accountId = claimed.AccountId,
            deletedAt = claimed.DeletedAt,
            deletedBy = claimed.DeletedBy,
            tablesEmptied = order.Count,
            rowsRemoved = rows,
        });
        _context.EnterpriseAuditLogs.Add(new AuditLog(
            claimed.Id, null, "System", "PurgeWorkspace", "workspaces.workspace", claimed.Id, null, null,
            "Critical", "Succeeded", null, null, null, null, null, null, null, metadata, _clock.UtcNow));
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Purged deleted workspace {WorkspaceId}: {Rows} rows in {Tables} tables", claimed.Id, rows, order.Count);
        return true;
    }

    /// <summary>
    /// Every table outside <see cref="RetainedSchemas"/> that has a uuid <c>workspace_id</c> column, ordered so a
    /// table comes before the tables its foreign keys point at. A cycle between two tables is a stop: the
    /// purge refuses to guess an order.
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolvePurgeOrderAsync(CancellationToken cancellationToken)
    {
        var tables = await _context.Database.SqlQuery<string>($"""
            SELECT format('%I.%I', n.nspname, c.relname) AS "Value"
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid AND c.relkind IN ('r', 'p')
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE a.attname = 'workspace_id' AND NOT a.attisdropped AND a.atttypid = 'uuid'::regtype
              AND n.nspname NOT IN ('audit', 'billing', 'messaging', 'events', 'ops', 'operators')
              AND n.nspname NOT LIKE 'pg\_%' AND n.nspname <> 'information_schema'
            """).ToListAsync(cancellationToken);

        var edges = await _context.Database.SqlQuery<string>($"""
            SELECT format('%I.%I', cn.nspname, cc.relname) || '>' || format('%I.%I', pn.nspname, pc.relname) AS "Value"
            FROM pg_constraint k
            JOIN pg_class cc ON cc.oid = k.conrelid
            JOIN pg_namespace cn ON cn.oid = cc.relnamespace
            JOIN pg_class pc ON pc.oid = k.confrelid
            JOIN pg_namespace pn ON pn.oid = pc.relnamespace
            WHERE k.contype = 'f' AND k.conrelid <> k.confrelid
            """).ToListAsync(cancellationToken);

        // child -> parents, restricted to the tables being emptied
        var set = tables.ToHashSet(StringComparer.Ordinal);
        var parentsOf = tables.ToDictionary(t => t, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            var split = edge.Split('>');
            if (set.Contains(split[0]) && set.Contains(split[1]))
                parentsOf[split[0]].Add(split[1]);
        }

        var ordered = new List<string>();
        var remaining = new HashSet<string>(tables, StringComparer.Ordinal);
        while (remaining.Count > 0)
        {
            // Deletable now: no remaining table still holds a foreign key into it.
            var ready = remaining
                .Where(t => !remaining.Any(other => other != t && parentsOf[other].Contains(t)))
                .Order(StringComparer.Ordinal)
                .ToList();
            if (ready.Count == 0)
                throw new InvalidOperationException(
                    $"Workspace purge found a foreign-key cycle among: {string.Join(", ", remaining.Order(StringComparer.Ordinal))}");
            ordered.AddRange(ready);
            remaining.ExceptWith(ready);
        }

        return ordered;
    }

    private sealed record PurgeTarget(Guid Id, Guid AccountId, DateTimeOffset DeletedAt, Guid? DeletedBy);
}

/// <summary>Hosted loop of <see cref="DeletedWorkspacePurger"/>; each pass runs in its own DI scope.</summary>
internal sealed class DeletedWorkspacePurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeletedWorkspacePurgeOptions _options;
    private readonly ILogger<DeletedWorkspacePurgeService> _logger;

    public DeletedWorkspacePurgeService(
        IServiceScopeFactory scopeFactory,
        IOptions<DeletedWorkspacePurgeOptions> options,
        ILogger<DeletedWorkspacePurgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DeletedWorkspacePurge is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var purger = scope.ServiceProvider.GetRequiredService<DeletedWorkspacePurger>();
                await purger.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletedWorkspacePurge pass failed");
            }

            try
            {
                await Task.Delay(_options.Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
