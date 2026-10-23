using Notrelix.Application.Common.SystemOperations;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Retention of the user-visible security activity feed. Bound from <c>Security:Activity:Retention</c>
/// and validated on start. See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class SecurityActivityRetentionOptions
{
    public const string SectionName = "Security:Activity:Retention";

    /// <summary>When false the sweep deletes nothing. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>How long an entry is kept, counted from when it occurred. Default 90 days.</summary>
    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(90);

    /// <summary>Time between sweeps. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum rows deleted per transaction. Default 1000.</summary>
    public int BatchSize { get; init; } = 1000;
}

public sealed class SecurityActivityRetentionOptionsValidator : IValidateOptions<SecurityActivityRetentionOptions>
{
    public static readonly TimeSpan MinRetention = TimeSpan.FromDays(1);
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 10_000;

    public ValidateOptionsResult Validate(string? name, SecurityActivityRetentionOptions options)
    {
        var failures = new List<string>();
        var section = SecurityActivityRetentionOptions.SectionName;

        if (options.Retention < MinRetention)
            failures.Add($"{section}:Retention must be at least {MinRetention}.");
        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One retention sweep: deletes entries older than the retention in bounded batches. It runs under
/// the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass) and touches
/// only <c>identity.user_security_activity</c>. Rows are claimed with <c>FOR UPDATE SKIP LOCKED</c>, so
/// several API instances can sweep at once without deleting a row twice or blocking each other.
/// </summary>
internal sealed class SecurityActivityRetentionSweeper
{
    private static readonly SystemOperationReason SweepReason =
        new("Identity", "Delete security activity entries older than the retention period");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly SecurityActivityRetentionOptions _options;
    private readonly ILogger<SecurityActivityRetentionSweeper> _logger;

    public SecurityActivityRetentionSweeper(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IOptions<SecurityActivityRetentionOptions> options,
        ILogger<SecurityActivityRetentionSweeper> logger)
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
        var cutoff = _clock.UtcNow - _options.Retention;
        var total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var deleted = await DeleteBatchAsync(cutoff, cancellationToken);
            total += deleted;
            if (deleted < _options.BatchSize)
                break;
        }

        if (total > 0)
        {
            _logger.LogInformation("Security activity retention removed {Count} entries", total);
        }

        return total;
    }

    private async Task<int> DeleteBatchAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "security-activity-retention.sweep", SweepReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var batch = _options.BatchSize;
        var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM identity.user_security_activity
            WHERE id IN (
                SELECT id
                FROM identity.user_security_activity
                WHERE occurred_at < {cutoff}
                ORDER BY occurred_at
                LIMIT {batch}
                FOR UPDATE SKIP LOCKED)
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}

/// <summary>Hosted loop of <see cref="SecurityActivityRetentionSweeper"/>; each pass runs in its own DI scope.</summary>
internal sealed class SecurityActivityRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SecurityActivityRetentionOptions _options;
    private readonly ILogger<SecurityActivityRetentionService> _logger;

    public SecurityActivityRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<SecurityActivityRetentionOptions> options,
        ILogger<SecurityActivityRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SecurityActivityRetention is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<SecurityActivityRetentionSweeper>();
                await sweeper.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityActivityRetention pass failed");
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
