using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Chat.Calls;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// The call timers (SPEC-CHAT-MEDIA-CALLS section 8, job Chat.Calls.Sweep): ends calls stuck Ringing after <c>Chat:Calls:RingSeconds</c>
/// (Missed) and Active calls with nobody in them after <c>Chat:Calls:EmptyCallSeconds</c>. Bound from <c>Chat:CallSweep</c> and validated
/// on start. It runs on the one hosted-service pattern of the other jobs.
/// </summary>
public sealed class ChatCallSweepOptions
{
    public const string SectionName = "Chat:CallSweep";

    /// <summary>When false the job ends nothing. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Time between passes. Default 5 seconds, so a ring ends within a few seconds of the 45 second limit.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(5);
}

public sealed class ChatCallSweepOptionsValidator : IValidateOptions<ChatCallSweepOptions>
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromMinutes(5);

    public ValidateOptionsResult Validate(string? name, ChatCallSweepOptions options) =>
        options.Interval < MinInterval || options.Interval > MaxInterval
            ? ValidateOptionsResult.Fail($"{ChatCallSweepOptions.SectionName}:Interval must be between {MinInterval} and {MaxInterval}.")
            : ValidateOptionsResult.Success;
}

/// <summary>
/// One pass of the sweep, under the explicit system (worker) tenant context with the RLS worker scope (no RLS bypass). Each call is one
/// transaction: it takes a transaction advisory lock on the call (two instances never end one together), ends it, writes the call message
/// and the realtime fact, and commits. A call that fails rolls back whole and is selected again next pass.
/// </summary>
internal sealed class ChatCallSweepRunner(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    ChatCallSweeper sweeper,
    IDateTimeProvider clock,
    IOptions<ChatCallOptions> calls,
    ILogger<ChatCallSweepRunner> logger)
{
    private static readonly SystemOperationReason Reason =
        new("Chat", "End calls that rang out or stayed empty: Missed after the ring time, Completed after the empty grace period");

    /// <returns>The number of calls ended and people released in this pass.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var ring = TimeSpan.FromSeconds(calls.Value.RingSeconds);
        var empty = TimeSpan.FromSeconds(calls.Value.EmptyCallSeconds);

        List<(Guid CallId, Guid PersonId)> revoked;
        using (new SystemContextScope(tenant, logger, "chat-calls-sweep.revoked", Reason))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);
            revoked = await sweeper.ListRevokedAsync(cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
        }

        var released = 0;
        foreach (var (callId, personId) in revoked)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await EndOneAsync(callId, (c, ct) => sweeper.ApplyRevokedAsync(callId, personId, clock.UtcNow, ct), cancellationToken)) released++;
        }

        List<Guid> due;
        using (new SystemContextScope(tenant, logger, "chat-calls-sweep.list", Reason))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);
            due = await sweeper.ListDueAsync(clock.UtcNow, ring, empty, cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
        }

        var ended = 0;
        foreach (var callId in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await EndOneAsync(callId, (c, ct) => sweeper.ApplyAsync(c, clock.UtcNow, ring, empty, ct), cancellationToken)) ended++;
        }

        if (ended + released > 0)
            logger.LogInformation("Chat call sweep ended {Count} calls and released {Released} people who lost access", ended, released);
        return ended + released;
    }

    private async Task<bool> EndOneAsync(Guid callId, Func<Guid, CancellationToken, Task<bool>> apply, CancellationToken cancellationToken)
    {
        try
        {
            using var system = new SystemContextScope(tenant, logger, "chat-calls-sweep.end", Reason);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);

            var lockKey = LockKey(callId);
            var claimed = await context.Database
                .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({lockKey}) AS \"Value\"")
                .SingleAsync(cancellationToken);
            if (!claimed || !await apply(callId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat call sweep failed for call {CallId}; it is selected again next pass", callId);
            return false;
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }

    private static long LockKey(Guid callId) =>
        System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(callId.ToByteArray())[..8]);
}

/// <summary>Hosted loop of <see cref="ChatCallSweepRunner"/>; each pass runs in its own DI scope.</summary>
internal sealed class ChatCallSweepService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChatCallSweepOptions> options,
    ILogger<ChatCallSweepService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("ChatCallSweep is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateWorkerAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<ChatCallSweepRunner>();
                await runner.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ChatCallSweep pass failed");
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
