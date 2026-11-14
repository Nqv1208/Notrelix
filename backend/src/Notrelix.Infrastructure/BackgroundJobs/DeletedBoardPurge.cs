using System.Text.Json;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Purge of deleted boards (UC-WM-A07, UC-WM-B17, D76.1). A board deleted more than 30 days ago
/// (<see cref="Board.RestoreWindow"/>) can no longer be restored, and this job removes it and its children for
/// good. Bound from <c>WorkManagement:Boards:Purge</c> and validated on start. Off by default: removing a board is
/// irreversible and the first release keeps every deleted board (owner decision pending), so an operator turns it
/// on. See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class DeletedBoardPurgeOptions
{
    public const string SectionName = "WorkManagement:Boards:Purge";

    /// <summary>When false nothing is purged and restore past the window stays refused. Default false.</summary>
    public bool Enabled { get; init; }

    /// <summary>Time between passes. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum boards purged per pass (each in its own transaction). Default 20.</summary>
    public int BatchSize { get; init; } = 20;
}

public sealed class DeletedBoardPurgeOptionsValidator : IValidateOptions<DeletedBoardPurgeOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 1_000;

    public ValidateOptionsResult Validate(string? name, DeletedBoardPurgeOptions options)
    {
        var failures = new List<string>();
        var section = DeletedBoardPurgeOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One purge pass, built like <see cref="DeletedWorkspacePurger"/>. Each expired board is purged in its own
/// transaction under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass): the
/// board row is claimed with <c>FOR UPDATE SKIP LOCKED</c>, so several API instances never purge one board twice
/// and a restore that races the purge waits for it, then finds NotFound. In the same transaction it removes:
/// <list type="number">
/// <item>every row of a <c>work</c> table that names the board in a uuid <c>board_id</c> or <c>*_board_id</c>
/// column (groups, items, fields, views, members, forms, relations from or to the board, ...);</item>
/// <item>every row anywhere (outside the retained schemas) whose foreign key into one of those rows cascades or
/// would block the delete: checklists and field options, an item value, a dependency of another board's item on
/// one of this board's items;</item>
/// <item>the board row;</item>
/// </list>
/// children before parents by the live foreign keys, and writes one audit fact. A failure rolls the whole board
/// back and the next pass retries it; a purged board is gone, so a retry or a second pass changes nothing.
/// </summary>
/// <remarks>
/// Kept: rows of other contexts that name the board without a foreign key (comments and activity in
/// Collaboration, reporting projections, automation rules, field permissions), which follow their own retention
/// (D78.10), and the audit trail. shortcut: one transaction per board; upgrade to chunked deletes when a single
/// board holds millions of rows.
/// </remarks>
internal sealed class DeletedBoardPurger
{
    private const string BoardsTable = "work.boards";

    private static readonly SystemOperationReason PurgeReason =
        new("WorkManagement", "Purge the boards deleted more than 30 days ago");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly DeletedBoardPurgeOptions _options;
    private readonly ILogger<DeletedBoardPurger> _logger;
    private IReadOnlyList<PurgeStep>? _plan;

    public DeletedBoardPurger(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IOptions<DeletedBoardPurgeOptions> options,
        ILogger<DeletedBoardPurger> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of boards purged.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        // Strictly older than the window: a board is restorable up to and including its last instant.
        var cutoff = _clock.UtcNow - Board.RestoreWindow;
        var purged = 0;

        while (purged < _options.BatchSize && !cancellationToken.IsCancellationRequested)
        {
            if (!await PurgeOneAsync(cutoff, cancellationToken))
                break;
            purged++;
        }

        if (purged > 0)
            _logger.LogInformation("Deleted board purge removed {Count} boards", purged);

        return purged;
    }

    private async Task<bool> PurgeOneAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "deleted-board.purge", PurgeReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var claimed = (await _context.Database.SqlQuery<PurgeTarget>($"""
            SELECT id, account_id, workspace_id, deleted_at, deleted_by
            FROM work.boards
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

        var plan = _plan ??= await ResolvePlanAsync(cancellationToken);
        long rows = 0;
        foreach (var step in plan)
        {
#pragma warning disable EF1002 // identifiers come from the database catalog and are quoted; the board id is a parameter
            rows += await _context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM {step.Table} WHERE {step.Predicate}",
                [new NpgsqlParameter("board", claimed.Id)],
                cancellationToken);
#pragma warning restore EF1002
        }

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM work.boards WHERE id = {claimed.Id}", cancellationToken);

        var metadata = JsonSerializer.SerializeToDocument(new
        {
            accountId = claimed.AccountId,
            deletedAt = claimed.DeletedAt,
            deletedBy = claimed.DeletedBy,
            tablesEmptied = plan.Count,
            rowsRemoved = rows,
        });
        _context.EnterpriseAuditLogs.Add(new AuditLog(
            claimed.WorkspaceId, null, "System", "PurgeBoard", "work-management.board", claimed.Id, null, null,
            "Critical", "Succeeded", null, null, null, null, null, null, null, metadata, _clock.UtcNow));
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Purged deleted board {BoardId}: {Rows} rows in {Tables} tables", claimed.Id, rows, plan.Count);
        return true;
    }

    /// <summary>
    /// Reads the live catalog: the <c>work</c> tables (but the boards table) with a uuid column named
    /// <c>board_id</c> or ending in <c>_board_id</c>, and the foreign keys outside the retained schemas.
    /// See <see cref="BuildPlan"/>.
    /// </summary>
    private async Task<IReadOnlyList<PurgeStep>> ResolvePlanAsync(CancellationToken cancellationToken)
    {
        var boardColumns = await _context.Database.SqlQuery<TableColumn>($"""
            SELECT format('%I.%I', n.nspname, c.relname) AS table_name, format('%I', a.attname) AS column_name
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid AND c.relkind IN ('r', 'p')
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'work' AND c.relname <> 'boards'
              AND (a.attname = 'board_id' OR a.attname LIKE '%\_board\_id')
              AND NOT a.attisdropped AND a.atttypid = 'uuid'::regtype
            """).ToListAsync(cancellationToken);

        var foreignKeys = await _context.Database.SqlQuery<ForeignKey>($"""
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

        return BuildPlan(boardColumns, foreignKeys);
    }

    /// <summary>
    /// Builds the ordered deletes (the boards table excluded: its row goes last, separately). A key that sets null
    /// or a default (<see cref="ForeignKey.Follows"/> false) is left to the database; every other key into a removed
    /// row (cascade, restrict, no action) makes its child rows part of the plan, so a blocking key is cleared first
    /// and a cascade is spelled out (and then has nothing left to do).
    /// </summary>
    internal static IReadOnlyList<PurgeStep> BuildPlan(
        IReadOnlyList<TableColumn> boardColumns, IReadOnlyList<ForeignKey> foreignKeys)
    {
        var following = foreignKeys.Where(k => k.Follows).ToList();

        // 1. The tables of the plan: the seeds, the boards table, and every table following a key into one of them.
        var tables = new HashSet<string>(boardColumns.Select(c => c.TableName), StringComparer.Ordinal) { BoardsTable };
        bool grew;
        do
        {
            grew = false;
            foreach (var key in following)
            {
                if (tables.Contains(key.Parent) && tables.Add(key.Child))
                    grew = true;
            }
        } while (grew);

        // 2. Children before parents over every key between tables of the plan; a cycle is a stop.
        var parentsOf = tables.ToDictionary(t => t, _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var key in foreignKeys)
        {
            if (tables.Contains(key.Child) && tables.Contains(key.Parent))
                parentsOf[key.Child].Add(key.Parent);
        }

        var childrenFirst = new List<string>();
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
                    $"Board purge found a foreign-key cycle among: {string.Join(", ", remaining.Order(StringComparer.Ordinal))}");
            childrenFirst.AddRange(ready);
            remaining.ExceptWith(ready);
        }

        // 3. The rows of each table, parents first: its own board columns, or a key into rows being removed.
        var predicates = new Dictionary<string, string>(StringComparer.Ordinal) { [BoardsTable] = "id = @board" };
        for (var index = childrenFirst.Count - 1; index >= 0; index--)
        {
            var table = childrenFirst[index];
            if (table == BoardsTable)
                continue;
            var conditions = boardColumns
                .Where(c => c.TableName == table)
                .Select(c => $"{c.ColumnName} = @board")
                .ToList();
            foreach (var key in following.Where(k => k.Child == table && predicates.ContainsKey(k.Parent)))
            {
                if (key.Columns != 1)
                    throw new InvalidOperationException(
                        $"Board purge found a multi-column foreign key from {key.Child} to {key.Parent}.");
                conditions.Add($"{key.ChildColumn} IN (SELECT {key.ParentColumn} FROM {key.Parent} WHERE {predicates[key.Parent]})");
            }
            predicates[table] = conditions.Count == 1 ? conditions[0] : "(" + string.Join(" OR ", conditions) + ")";
        }

        return childrenFirst
            .Where(t => t != BoardsTable)
            .Select(t => new PurgeStep(t, predicates[t]))
            .ToList();
    }

    internal sealed record TableColumn(string TableName, string ColumnName);

    /// <param name="Follows">The child rows go with the parent row (cascade) or block its delete (restrict, no action).</param>
    internal sealed record ForeignKey(string Child, string Parent, string ChildColumn, string ParentColumn, int Columns, bool Follows);

    internal sealed record PurgeStep(string Table, string Predicate);

    private sealed record PurgeTarget(Guid Id, Guid AccountId, Guid WorkspaceId, DateTimeOffset DeletedAt, Guid? DeletedBy);
}

/// <summary>Hosted loop of <see cref="DeletedBoardPurger"/>; each pass runs in its own DI scope.</summary>
internal sealed class DeletedBoardPurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeletedBoardPurgeOptions _options;
    private readonly ILogger<DeletedBoardPurgeService> _logger;

    public DeletedBoardPurgeService(
        IServiceScopeFactory scopeFactory,
        IOptions<DeletedBoardPurgeOptions> options,
        ILogger<DeletedBoardPurgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DeletedBoardPurge is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var purger = scope.ServiceProvider.GetRequiredService<DeletedBoardPurger>();
                await purger.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletedBoardPurge pass failed");
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
