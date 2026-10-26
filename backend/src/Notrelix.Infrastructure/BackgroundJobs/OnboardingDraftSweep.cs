using Notrelix.Application.Common.SystemOperations;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Sweep of expired onboarding drafts (accepted 2026-10-24). A draft lives 30 days after its last save
/// (<c>OnboardingDraft.Retention</c>) and an expired draft already reads as absent, so deleting it changes
/// nothing a person can see; the sweep only reclaims storage. Bound from
/// <c>Workspaces:Onboarding:DraftSweep</c> and validated on start. See
/// <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class OnboardingDraftSweepOptions
{
    public const string SectionName = "Workspaces:Onboarding:DraftSweep";

    /// <summary>When false the sweep deletes nothing. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between sweeps. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum rows deleted per transaction. Default 500.</summary>
    public int BatchSize { get; init; } = 500;
}

public sealed class OnboardingDraftSweepOptionsValidator : IValidateOptions<OnboardingDraftSweepOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 10_000;

    public ValidateOptionsResult Validate(string? name, OnboardingDraftSweepOptions options)
    {
        var failures = new List<string>();
        var section = OnboardingDraftSweepOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One sweep: deletes drafts whose <c>expires_at</c> has passed, in bounded batches. It runs under the
/// explicit system (worker) tenant context with the RLS worker scope (no RLS bypass) and touches only
/// <c>workspace.onboarding_drafts</c>. Rows are claimed with <c>FOR UPDATE SKIP LOCKED</c>, so several
/// API instances can sweep at once without deleting a row twice or blocking each other, and a draft a
/// person saves at that moment (its expiry moves forward) is simply not matched again.
/// </summary>
internal sealed class OnboardingDraftSweeper
{
    private static readonly SystemOperationReason SweepReason =
        new("Workspaces", "Delete onboarding drafts past their 30-day expiry");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly OnboardingDraftSweepOptions _options;
    private readonly ILogger<OnboardingDraftSweeper> _logger;

    public OnboardingDraftSweeper(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IOptions<OnboardingDraftSweepOptions> options,
        ILogger<OnboardingDraftSweeper> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of drafts deleted.</returns>
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
            _logger.LogInformation("Onboarding draft sweep removed {Count} expired drafts", total);

        return total;
    }

    private async Task<int> DeleteBatchAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "onboarding-draft.sweep", SweepReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var batch = _options.BatchSize;
        var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM workspace.onboarding_drafts
            WHERE id IN (
                SELECT id
                FROM workspace.onboarding_drafts
                WHERE expires_at <= {now}
                ORDER BY expires_at
                LIMIT {batch}
                FOR UPDATE SKIP LOCKED)
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}

/// <summary>Hosted loop of <see cref="OnboardingDraftSweeper"/>; each pass runs in its own DI scope.</summary>
internal sealed class OnboardingDraftSweepService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OnboardingDraftSweepOptions _options;
    private readonly ILogger<OnboardingDraftSweepService> _logger;

    public OnboardingDraftSweepService(
        IServiceScopeFactory scopeFactory,
        IOptions<OnboardingDraftSweepOptions> options,
        ILogger<OnboardingDraftSweepService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("OnboardingDraftSweep is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<OnboardingDraftSweeper>();
                await sweeper.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnboardingDraftSweep pass failed");
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
