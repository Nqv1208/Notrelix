using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Automatic version when editing goes quiet (owner decision 2026-11-15, DCT-016/017): a page that has had no
/// block save for five minutes gets an Automatic history entry for its unsnapshotted edits. The repo has no
/// delayed-message transport (in-memory and RabbitMQ run without a scheduler), so the delay is a sweep, the
/// same way every other timed rule runs. Bound from <c>Documents:QuietVersionSweep</c> and validated on start.
/// See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class PageQuietVersionSweepOptions
{
    public const string SectionName = "Documents:QuietVersionSweep";

    /// <summary>When false the sweep captures nothing. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between passes. Default 1 minute (the quiet period itself is five minutes).</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How far past the quiet period a page is still looked at, so a pass that did not run is made up. A page
    /// older than that waits for its next save, whose ten-minute rule covers it. Default 30 minutes.
    /// </summary>
    public TimeSpan Lookback { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Maximum pages captured per pass. Default 200.</summary>
    public int BatchSize { get; init; } = 200;
}

public sealed class PageQuietVersionSweepOptionsValidator : IValidateOptions<PageQuietVersionSweepOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaxLookback = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 5_000;

    public ValidateOptionsResult Validate(string? name, PageQuietVersionSweepOptions options)
    {
        var failures = new List<string>();
        var section = PageQuietVersionSweepOptions.SectionName;

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.Lookback < options.Interval || options.Lookback > MaxLookback)
            failures.Add($"{section}:Lookback must be between the Interval and {MaxLookback}.");
        if (options.BatchSize is < 1 or > MaxBatchSize)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One pass: names the quiet pages and captures each in its own transaction, under the explicit system
/// (worker) tenant context with the RLS worker scope. Capturing is idempotent (content equal to the newest
/// entry adds nothing), so a repeated pass, or two instances racing, never adds a second entry for the same
/// content; the loser of a race on the version number is skipped and found equal on the next pass.
/// </summary>
internal sealed class PageQuietVersionSweeper
{
    private static readonly SystemOperationReason SweepReason =
        new("Documents", "Capture an automatic version of a page whose editing went quiet");

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _clock;
    private readonly IPageVersionRecorder _recorder;
    private readonly PageQuietVersionSweepOptions _options;
    private readonly ILogger<PageQuietVersionSweeper> _logger;

    public PageQuietVersionSweeper(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        IDateTimeProvider clock,
        IPageVersionRecorder recorder,
        IOptions<PageQuietVersionSweepOptions> options,
        ILogger<PageQuietVersionSweeper> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _clock = clock;
        _recorder = recorder;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of versions captured.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        IReadOnlyList<Guid> pages;
        using (var system = new SystemContextScope(_tenant, _logger, "page-quiet-version.sweep", SweepReason))
        {
            await using var read = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _rls.ApplyAsync(cancellationToken);
            pages = await _recorder.QuietPagesAsync(now, _options.Lookback, _options.BatchSize, cancellationToken);
            await read.CommitAsync(cancellationToken);
        }

        var captured = 0;
        foreach (var pageId in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var system = new SystemContextScope(_tenant, _logger, "page-quiet-version.sweep", SweepReason);
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                await _rls.ApplyAsync(cancellationToken);
                var version = await _recorder.CaptureWhenQuietAsync(pageId, now, cancellationToken);
                if (version is null) continue;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                captured++;
            }
            catch (DbUpdateException ex)
            {
                // Another instance took the same version number first; the next pass finds the content equal.
                _context.ChangeTracker.Clear();
                _logger.LogInformation(ex, "Quiet version of page {PageId} was taken by another instance", pageId);
            }
        }

        if (captured > 0)
            _logger.LogInformation("Quiet version sweep captured {Count} automatic versions", captured);
        return captured;
    }
}

/// <summary>Hosted loop of <see cref="PageQuietVersionSweeper"/>; each pass runs in its own worker DI scope.</summary>
internal sealed class PageQuietVersionSweepService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PageQuietVersionSweepOptions _options;
    private readonly ILogger<PageQuietVersionSweepService> _logger;

    public PageQuietVersionSweepService(
        IServiceScopeFactory scopeFactory,
        IOptions<PageQuietVersionSweepOptions> options,
        ILogger<PageQuietVersionSweepService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("PageQuietVersionSweep is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateWorkerAsyncScope();
                var sweeper = scope.ServiceProvider.GetRequiredService<PageQuietVersionSweeper>();
                await sweeper.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PageQuietVersionSweep pass failed");
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
