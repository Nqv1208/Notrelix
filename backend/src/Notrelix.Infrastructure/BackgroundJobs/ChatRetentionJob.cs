using System.Buffers.Binary;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Chat.Retention.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Chat retention (UC-CHT-H05, CHT-021): the job Chat.Retention.Apply. Bound from <c>Chat:Retention</c> and validated on start. It runs on
/// the one hosted-service pattern of the other retention jobs; there is no second scheduler. Only a Workspace whose Owner set a window is
/// touched, so a deployment where nobody set one removes nothing. See <c>backend/docs/operations/configuration-and-runtime.md</c>.
/// </summary>
public sealed class ChatRetentionOptions
{
    public const string SectionName = "Chat:Retention";

    /// <summary>When false the job removes nothing. Default true (a Workspace still needs a rule an Owner set).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between passes. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>The most batches one space gets in one pass, so a huge backlog is worked off over several passes. Default 50.</summary>
    public int MaxBatchesPerSpace { get; init; } = 50;
}

public sealed class ChatRetentionOptionsValidator : IValidateOptions<ChatRetentionOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatches = 10_000;

    public ValidateOptionsResult Validate(string? name, ChatRetentionOptions options)
    {
        var failures = new List<string>();
        var section = ChatRetentionOptions.SectionName;
        if (options.Interval < MinInterval || options.Interval > MaxInterval)
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        if (options.MaxBatchesPerSpace is < 1 or > MaxBatches)
            failures.Add($"{section}:MaxBatchesPerSpace must be between 1 and {MaxBatches}.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// One pass of the retention job. It runs under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass). Each
/// batch is one transaction: it takes a transaction advisory lock on the space (so two instances never work on one space together), selects
/// the expired messages from the database, applies the rule, writes the Audit fact and the realtime fact, and commits. A batch that fails
/// rolls back whole (no message half removed, no fact without its removal) and the next pass selects the same messages again, so a failed
/// batch resumes by itself and a repeated pass changes nothing.
/// </summary>
internal sealed class ChatRetentionRunner(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    ChatRetentionApplier applier,
    IOptions<ChatRetentionOptions> options,
    ILogger<ChatRetentionRunner> logger)
{
    private static readonly SystemOperationReason Reason =
        new("Chat", "Apply each Workspace's chat retention rule: tombstone or purge messages past the window");

    /// <returns>The number of messages tombstoned or purged in this pass.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RetentionTarget> targets;
        using (new SystemContextScope(tenant, logger, "chat-retention.list", Reason))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);
            targets = await applier.ListTargetsAsync(cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
        }

        var total = 0;
        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += await RunWorkspaceAsync(target, cancellationToken);
        }

        if (total > 0)
            logger.LogInformation("Chat retention removed {Count} messages", total);
        return total;
    }

    private async Task<int> RunWorkspaceAsync(RetentionTarget target, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> spaces;
        using (new SystemContextScope(tenant, logger, "chat-retention.spaces", Reason))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);
            spaces = await applier.ListSpacesWithExpiredAsync(target, cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
        }

        var total = 0;
        foreach (var spaceId in spaces)
        {
            for (var batch = 0; batch < options.Value.MaxBatchesPerSpace; batch++)
            {
                var result = await RunBatchAsync(target, spaceId, cancellationToken);
                if (result is null || result.IsEmpty)
                    break; // failed (resumes next pass), held by another instance, or nothing left
                total += result.Messages;
            }
        }

        return total;
    }

    private async Task<RetentionBatchResult?> RunBatchAsync(RetentionTarget target, Guid spaceId, CancellationToken cancellationToken)
    {
        try
        {
            using var system = new SystemContextScope(tenant, logger, "chat-retention.batch", Reason);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);

            var lockKey = LockKey(spaceId);
            var claimed = await context.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({lockKey}) AS \"Value\"")
                .SingleAsync(cancellationToken);
            if (!claimed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var result = await applier.ApplyBatchAsync(target, spaceId, cancellationToken);
            if (result.IsEmpty)
            {
                await transaction.RollbackAsync(cancellationToken);
                return result;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat retention batch failed for space {SpaceId} of Workspace {WorkspaceId}; it is selected again next pass", spaceId, target.WorkspaceId);
            return null;
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }

    private static long LockKey(Guid spaceId) =>
        BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(spaceId.ToByteArray())[..8]);
}

/// <summary>Hosted loop of <see cref="ChatRetentionRunner"/>; each pass runs in its own DI scope.</summary>
internal sealed class ChatRetentionService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChatRetentionOptions> options,
    ILogger<ChatRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("ChatRetention is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateWorkerAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<ChatRetentionRunner>();
                await runner.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ChatRetention pass failed");
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
