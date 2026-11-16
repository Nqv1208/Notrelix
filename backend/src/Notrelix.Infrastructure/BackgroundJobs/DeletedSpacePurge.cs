using System.Text.Json;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Audit;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Purge of deleted Spaces (departments). A Space deleted more than 30 days ago (<see cref="Space.RestoreWindow"/>)
/// can no longer be restored, and this job removes it for good. Bound from <c>Workspaces:Spaces:Purge</c> and
/// validated on start. On by default, like the deleted workspace purge: a deleted Space holds no content (the delete
/// is refused while child Spaces, Boards or Pages exist), only its member and closure rows. See
/// <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class DeletedSpacePurgeOptions
{
    public const string SectionName = "Workspaces:Spaces:Purge";

    /// <summary>When false nothing is purged and the rows of an expired Space stay. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between passes. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum Spaces purged per pass (each in its own transaction). Default 20.</summary>
    public int BatchSize { get; init; } = 20;
}

public sealed class DeletedSpacePurgeOptionsValidator : IValidateOptions<DeletedSpacePurgeOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 1_000;

    public ValidateOptionsResult Validate(string? name, DeletedSpacePurgeOptions options)
    {
        var failures = new List<string>();
        var section = DeletedSpacePurgeOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One purge pass, built like <see cref="DeletedBoardPurger"/>. Each expired Space is purged in its own transaction
/// under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass): the Space row is
/// claimed with <c>FOR UPDATE SKIP LOCKED</c>, so several API instances never purge one Space twice and a restore
/// that races the purge waits for it, then finds NotFound. A Space that still has a child Space row is not claimed
/// (the parent key restricts its delete); the child, deleted earlier, is purged first. In the same transaction it
/// removes the member entries, the closure rows (<c>space_paths</c>, as ancestor or descendant), then the Space row, and writes one audit fact. A failure rolls the Space back and the next
/// pass retries it; a purged Space is gone, so a retry or a second pass changes nothing.
/// </summary>
/// <remarks>Kept: the audit trail, and rows of other contexts that name the Space without a foreign key.</remarks>
internal sealed class DeletedSpacePurger
{
    private static readonly SystemOperationReason PurgeReason =
        new("Workspaces", "Purge the Spaces deleted more than 30 days ago");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly DeletedSpacePurgeOptions _options;
    private readonly ILogger<DeletedSpacePurger> _logger;

    public DeletedSpacePurger(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IOptions<DeletedSpacePurgeOptions> options,
        ILogger<DeletedSpacePurger> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of Spaces purged.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        // Strictly older than the window: a Space is restorable up to and including its last instant.
        var cutoff = _clock.UtcNow - Space.RestoreWindow;
        var purged = 0;

        while (purged < _options.BatchSize && !cancellationToken.IsCancellationRequested)
        {
            if (!await PurgeOneAsync(cutoff, cancellationToken))
                break;
            purged++;
        }

        if (purged > 0)
            _logger.LogInformation("Deleted space purge removed {Count} spaces", purged);

        return purged;
    }

    private async Task<bool> PurgeOneAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "deleted-space.purge", PurgeReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var claimed = (await _context.Database.SqlQuery<PurgeTarget>($"""
            SELECT id, account_id, workspace_id, deleted_at, deleted_by
            FROM workspace.spaces s
            WHERE s.deleted_at IS NOT NULL AND s.deleted_at < {cutoff}
              AND NOT EXISTS (SELECT 1 FROM workspace.spaces c WHERE c.parent_space_id = s.id)
            ORDER BY s.deleted_at
            LIMIT 1
            FOR UPDATE OF s SKIP LOCKED
            """).ToListAsync(cancellationToken)).SingleOrDefault();

        if (claimed is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        long rows = 0;
        rows += await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM workspace.space_members WHERE space_id = {claimed.Id}", cancellationToken);
        rows += await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM workspace.space_paths WHERE ancestor_id = {claimed.Id} OR descendant_id = {claimed.Id}", cancellationToken);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM workspace.spaces WHERE id = {claimed.Id}", cancellationToken);

        var metadata = JsonSerializer.SerializeToDocument(new
        {
            accountId = claimed.AccountId,
            deletedAt = claimed.DeletedAt,
            deletedBy = claimed.DeletedBy,
            rowsRemoved = rows,
        });
        _context.EnterpriseAuditLogs.Add(new AuditLog(
            claimed.WorkspaceId, null, "System", "PurgeSpace", "workspaces.space", claimed.Id, null, null,
            "Critical", "Succeeded", null, null, null, null, null, null, null, metadata, _clock.UtcNow));
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Purged deleted space {SpaceId}: {Rows} rows", claimed.Id, rows);
        return true;
    }

    private sealed record PurgeTarget(Guid Id, Guid AccountId, Guid WorkspaceId, DateTimeOffset DeletedAt, Guid? DeletedBy);
}

/// <summary>Hosted loop of <see cref="DeletedSpacePurger"/>; each pass runs in its own DI scope.</summary>
internal sealed class DeletedSpacePurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeletedSpacePurgeOptions _options;
    private readonly ILogger<DeletedSpacePurgeService> _logger;

    public DeletedSpacePurgeService(
        IServiceScopeFactory scopeFactory,
        IOptions<DeletedSpacePurgeOptions> options,
        ILogger<DeletedSpacePurgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DeletedSpacePurge is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var purger = scope.ServiceProvider.GetRequiredService<DeletedSpacePurger>();
                await purger.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletedSpacePurge pass failed");
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
