using Notrelix.Application.Common.SystemOperations;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// The sweep of the Board access-change log. Every row carries its own <c>retention_until</c> (90 days
/// after the change; reads already hide a row past it), so this only removes rows that are already
/// invisible. It deletes history for good, and no owner rule for retention and deletion exists yet
/// (owner decision 3), so it is off by default and no environment turns it on. Bound from
/// <c>Governance:AccessChanges:Retention</c> and validated on start. See
/// <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class ResourceAccessChangeRetentionOptions
{
    public const string SectionName = "Governance:AccessChanges:Retention";

    /// <summary>When false the sweep deletes nothing. Default false until the owner sets the retention rule.</summary>
    public bool Enabled { get; init; } = false;

    /// <summary>Time between sweeps. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum rows deleted per transaction. Default 1000.</summary>
    public int BatchSize { get; init; } = 1000;
}

public sealed class ResourceAccessChangeRetentionOptionsValidator : IValidateOptions<ResourceAccessChangeRetentionOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 10_000;

    public ValidateOptionsResult Validate(string? name, ResourceAccessChangeRetentionOptions options)
    {
        var failures = new List<string>();
        var section = ResourceAccessChangeRetentionOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One sweep: deletes access-change rows past their <c>retention_until</c> in bounded batches. It runs
/// under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass) and
/// touches only <c>governance.resource_access_changes</c>. Rows are claimed with
/// <c>FOR UPDATE SKIP LOCKED</c>, so several API instances can sweep at once.
/// </summary>
internal sealed class ResourceAccessChangeRetentionSweeper
{
    private static readonly SystemOperationReason SweepReason =
        new("Governance", "Delete Board access-change rows past their retention");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly ResourceAccessChangeRetentionOptions _options;
    private readonly ILogger<ResourceAccessChangeRetentionSweeper> _logger;

    public ResourceAccessChangeRetentionSweeper(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IOptions<ResourceAccessChangeRetentionOptions> options,
        ILogger<ResourceAccessChangeRetentionSweeper> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of rows deleted.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var deleted = await DeleteBatchAsync(now, cancellationToken);
            total += deleted;
            if (deleted < _options.BatchSize)
                break;
        }

        if (total > 0)
        {
            _logger.LogInformation("Access change retention removed {Count} rows", total);
        }

        return total;
    }

    private async Task<int> DeleteBatchAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "resource-access-change-retention.sweep", SweepReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var batch = _options.BatchSize;
        var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM governance.resource_access_changes
            WHERE id IN (
                SELECT id
                FROM governance.resource_access_changes
                WHERE retention_until < {now}
                ORDER BY retention_until
                LIMIT {batch}
                FOR UPDATE SKIP LOCKED)
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}

/// <summary>Hosted loop of <see cref="ResourceAccessChangeRetentionSweeper"/>; each pass runs in its own DI scope.</summary>
internal sealed class ResourceAccessChangeRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ResourceAccessChangeRetentionOptions _options;
    private readonly ILogger<ResourceAccessChangeRetentionService> _logger;

    public ResourceAccessChangeRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<ResourceAccessChangeRetentionOptions> options,
        ILogger<ResourceAccessChangeRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ResourceAccessChangeRetention is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<ResourceAccessChangeRetentionSweeper>();
                await sweeper.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResourceAccessChangeRetention pass failed");
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
