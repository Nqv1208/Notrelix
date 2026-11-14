using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Collaboration.Notifications.ChatDigest;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Notifications.ChatDigest;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// The missed-mention email digest (UC-CHT-H09, CHT-019): the job Chat.Notifications.Digest. Bound from <c>Chat:Digest</c> and validated on
/// start. It runs on the one hosted-service pattern of the other jobs and the one mail path (the shared email outbox and its dispatcher);
/// there is no second scheduler and no second sender. See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class ChatDigestOptions
{
    public const string SectionName = "Chat:Digest";

    /// <summary>When false the job queues nothing. Default true (a person still has to turn the digest on).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between passes. Default 15 minutes: a pass queues a person's digest at most once per window, so more passes add nothing.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>The length of one digest window; a person gets at most one digest per window. Default 24 hours (a UTC day).</summary>
    public TimeSpan Window { get; init; } = TimeSpan.FromHours(24);

    /// <summary>The most message lines one digest shows; the rest read "and N more". Default 10.</summary>
    public int MaxItems { get; init; } = 10;

    /// <summary>The people looked at per batch. Default 200.</summary>
    public int BatchSize { get; init; } = 200;
}

public sealed class ChatDigestOptionsValidator : IValidateOptions<ChatDigestOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxLines = 50;
    public const int MaxBatch = 5_000;

    public ValidateOptionsResult Validate(string? name, ChatDigestOptions options)
    {
        var failures = new List<string>();
        var section = ChatDigestOptions.SectionName;
        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.Window < ChatDigestWindow.MinLength || options.Window > ChatDigestWindow.MaxLength)
            failures.Add($"{section}:Window must be between {ChatDigestWindow.MinLength} and {ChatDigestWindow.MaxLength}.");
        if (options.MaxItems is < 1 or > MaxLines)
            failures.Add($"{section}:MaxItems must be between 1 and {MaxLines}.");
        if (options.BatchSize is < 1 or > MaxBatch)
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatch}.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One pass of the digest job. It runs under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass), in one
/// transaction that only ever adds email outbox rows, one per person and window, each guarded by a unique key: a pass that fails part way
/// leaves the rows it had saved, and the next pass queues the rest and none twice. The mail itself is sent, retried and given up on by the
/// shared email dispatcher.
/// </summary>
internal sealed class ChatDigestRunner(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    ChatDigestComposer composer,
    IDateTimeProvider clock,
    IOptions<ChatDigestOptions> options,
    ILogger<ChatDigestRunner> logger)
{
    private static readonly SystemOperationReason Reason =
        new("Chat", "Queue the missed-mention email digest of each person who turned it on, once per window");

    public async Task<ChatDigestRunResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var window = ChatDigestWindow.Latest(now, options.Value.Window);

        using var system = new SystemContextScope(tenant, logger, "chat-digest.run", Reason);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await rls.ApplyAsync(cancellationToken);
        var result = await composer.RunAsync(window, options.Value.MaxItems, options.Value.BatchSize, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (result.Queued > 0)
            logger.LogInformation("Chat digest of window {Window} queued {Queued} emails ({AlreadyQueued} already queued)", window.Key, result.Queued, result.AlreadyQueued);
        return result;
    }
}

/// <summary>Hosted loop of <see cref="ChatDigestRunner"/>; each pass runs in its own DI scope.</summary>
internal sealed class ChatDigestService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChatDigestOptions> options,
    ILogger<ChatDigestService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("ChatDigest is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateWorkerAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<ChatDigestRunner>();
                await runner.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ChatDigest pass failed");
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
