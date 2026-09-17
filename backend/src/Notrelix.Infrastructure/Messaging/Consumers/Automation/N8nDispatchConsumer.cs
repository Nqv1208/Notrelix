using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

/// <summary>
/// Inbound adapter for a durable <see cref="N8nDispatchRequestedV1"/> under the
/// prepare/effect/settle protocol (TAC-PF-FLOW-04 / M11):
///
/// <list type="bullet">
///   <item>Tx1: the claim (Processing) and the attempt evidence (execution
///   Running) commit atomically, so a crash before the effect leaves a
///   reconcilable Running residue.</item>
///   <item>The provider call runs OUTSIDE any database transaction.</item>
///   <item>Tx2: the outcome evidence (Succeed / terminal Fail / retryable
///   re-queue) commits atomically with the claim lifecycle — claim → Succeeded,
///   or the claim is RELEASED for a retryable outcome so the delivery mechanism
///   can redeliver and re-acquire under the same execution identity.</item>
/// </list>
///
/// A redelivery whose claim is still Processing never re-fires the provider: a
/// FRESH claim means another attempt is actively in flight (the duplicate is
/// ignored — the execution and claim are left untouched), while a STALE claim
/// indicates an interrupted attempt that is reconciled from the durable
/// execution state (Running → Failed with a reconciliation marker, settled claim
/// → skip, stale Queued → operator sweep). The consumer owns no Automation
/// business state.
/// </summary>
public sealed class N8nDispatchConsumer : IConsumer<N8nDispatchRequestedV1>
{
    private readonly N8nDispatchUseCase _useCase;
    private readonly ApplicationDbContext _db;
    private readonly IRlsSessionContext _rls;
    private readonly IProviderEffectClaimStore _claims;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<N8nDispatchConsumer> _logger;
    private readonly PipelineMetrics _metrics;
    private readonly TimeSpan _providerEffectClaimStaleAfter;

    public N8nDispatchConsumer(
        N8nDispatchUseCase useCase,
        ApplicationDbContext db,
        IRlsSessionContext rls,
        IProviderEffectClaimStore claims,
        IDateTimeProvider clock,
        ILogger<N8nDispatchConsumer> logger,
        IOptions<N8nOptions>? n8nOptions = null,
        PipelineMetrics? metrics = null)
    {
        _useCase = useCase;
        _db = db;
        _rls = rls;
        _claims = claims;
        _clock = clock;
        _logger = logger;
        _metrics = metrics ?? new PipelineMetrics();
        _providerEffectClaimStaleAfter = TimeSpan.FromSeconds(
            (n8nOptions?.Value.ProviderEffectClaimStaleAfterSeconds
                ?? new N8nOptions().ProviderEffectClaimStaleAfterSeconds));
    }

    public async Task Consume(ConsumeContext<N8nDispatchRequestedV1> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var preparation = await AcquireAndPrepareAsync(context, message, ct);
        if (preparation is null)
            return; // consumed / settled / reconciled inside the first transaction

        var dispatchStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _useCase.CallWebhookAsync(
            preparation.WebhookPath!, preparation.Payload!, ct);
        _metrics.N8nDispatchDuration.Record(dispatchStopwatch.Elapsed.TotalMilliseconds);

        await SettleAsync(context, message, result, ct);
    }

    /// <summary>
    /// Tx1: acquire the claim, progress the attempt, and commit the durable
    /// intent atomically. Returns the ready webhook call, or null when nothing
    /// requires a provider effect.
    /// </summary>
    private async Task<N8nDispatchPreparation?> AcquireAndPrepareAsync(
        ConsumeContext<N8nDispatchRequestedV1> context,
        N8nDispatchRequestedV1 message,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _rls.ApplyAsync(ct);

            var claimed = await _claims.TryAcquireClaimAsync(
                messageId: message.EventId,
                consumerName: N8nDispatchProtocolEndpoints.DispatchEndpointName,
                messageName: message.MessageName,
                messageVersion: message.SchemaVersion,
                sourceEventId: message.SourceEventId,
                workspaceId: message.WorkspaceId,
                cancellationToken: ct);

            if (!claimed)
            {
                // A claim already exists. The unique violation aborts this
                // transaction, so roll back and reconcile from durable state in
                // a fresh transaction — never  re-issue the provider effect.
                await transaction.RollbackAsync(ct);
                await HandleClaimResidueAsync(context, message, ct);
                return null;
            }

            var preparation = await _useCase.PrepareAttemptAsync(message, ct);

            if (preparation.Outcome == N8nDispatchPrepareOutcome.ReadyForDispatch)
            {
                // Claim Processing + execution Running commit atomically.
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return preparation;
            }

            // Nothing to progress, or a precondition settled terminally:
            // normalize the claim and finish inside the first transaction.
            var settledEarly = await _claims.TryMarkClaimSucceededAsync(
                message.EventId, N8nDispatchProtocolEndpoints.DispatchEndpointName, _clock.UtcNow, ct);
            if (!settledEarly)
            {
                throw new InvalidOperationException(
                    "Provider-effect claim was not transitioned to Succeeded before the terminal settle commit.");
            }
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _metrics.N8nDispatchSucceeded.Add(1);
            return null;
        }
        catch (N8nDispatchRetryableException)
        {
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Tx2: settle the outcome evidence atomically with the claim lifecycle.
    /// Throws <see cref="N8nDispatchRetryableException"/> after the retryable
    /// evidence and claim release are committed, signalling the delivery
    /// mechanism to redeliver.
    /// </summary>
    private async Task SettleAsync(
        ConsumeContext<N8nDispatchRequestedV1> context,
        N8nDispatchRequestedV1 message,
        N8nWebhookDispatchResult result,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _rls.ApplyAsync(ct);

            var handled = await _useCase.SettleAttemptAsync(message, result, ct);

            if (handled)
            {
                var settled = await _claims.TryMarkClaimSucceededAsync(
                    message.EventId, N8nDispatchProtocolEndpoints.DispatchEndpointName, _clock.UtcNow, ct);
                if (!settled)
                {
                    throw new InvalidOperationException(
                        "Provider-effect claim was not transitioned to Succeeded before the terminal settle commit.");
                }
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _metrics.N8nDispatchSucceeded.Add(1);
                return;
            }

            // Retryable provider outcome: the evidence (re-queued + attempt count)
            // and the claim release commit atomically so the invariant
            // "Queued-for-retry IFF the claim no longer blocks retry" always holds.
            var released = await _claims.TryReleaseProcessingClaimAsync(
                message.EventId, N8nDispatchProtocolEndpoints.DispatchEndpointName, ct);
            if (!released)
            {
                throw new InvalidOperationException(
                    "Provider-effect claim ownership was lost before retry settlement.");
            }
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _metrics.N8nDispatchFailed.Add(1);
            _metrics.N8nDispatchRetries.Add(1);
            throw new N8nDispatchRetryableException(message.ExecutionId);
        }
        catch (N8nDispatchRetryableException)
        {
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Reconciles a residual claim after a failed acquire. Normally never throws
    /// (the message is fully consumed) and never re-fires the provider effect:
    /// settled claim → skip; FRESH Processing claim → another attempt is in
    /// flight, the duplicate is ignored without touching execution or claim;
    /// STALE Processing + Running → interrupted attempt → settle Failed with a
    /// reconciliation marker; STALE Processing + Queued → operator sweep. A
    /// claim that vanished between inspection and settlement is left for a later
    /// redelivery (never re-issued here).
    /// </summary>
    private async Task HandleClaimResidueAsync(
        ConsumeContext<N8nDispatchRequestedV1> context,
        N8nDispatchRequestedV1 message,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _rls.ApplyAsync(ct);

            var inspection = await _claims.InspectClaimAsync(
                message.EventId, N8nDispatchProtocolEndpoints.DispatchEndpointName, ct);

            if (inspection.State is MessageClaimState.Succeeded or MessageClaimState.Missing)
            {
                // Already settled (normal duplicate) or torn — nothing to do.
                await transaction.CommitAsync(ct);
                _logger.LogDebug(
                    "Event {EventId} already {State} for {ConsumerName}; skipping",
                    message.EventId, inspection.State, N8nDispatchProtocolEndpoints.DispatchEndpointName);
                return;
            }

            if (IsClaimFresh(inspection.ClaimedAt))
            {
                // A fresh Processing claim means another consumer is actively
                // handling the provider effect. The duplicate must NOT touch the
                // execution or the claim, and must NOT call the provider — the
                // active attempt will settle the outcome itself.
                await transaction.CommitAsync(ct);
                _logger.LogDebug(
                    "Event {EventId} claim Processing and fresh for {ConsumerName}; "
                    + "an attempt is in flight, duplicate not acted on",
                    message.EventId, N8nDispatchProtocolEndpoints.DispatchEndpointName);
                return;
            }

            var residue = await _useCase.SettleResidueAsync(message, ct);
            if (residue == N8nDispatchResidueOutcome.RequiresOperatorSweep)
            {
                // Queued execution with a stale Processing claim: re-running the
                // effect blindly could duplicate provider side-effects. Leave it
                // for the operator sweep instead.
                await transaction.CommitAsync(ct);
                _logger.LogWarning(
                    "Event {EventId} has a stale Processing claim with a Queued execution; operator sweep required",
                    message.EventId);
                return;
            }

            var settled = await _claims.TryMarkClaimSucceededAsync(
                message.EventId, N8nDispatchProtocolEndpoints.DispatchEndpointName, _clock.UtcNow, ct);
            if (!settled)
            {
                // The claim vanished/changed between inspection and settlement.
                // Keep the "never throws" residue contract: leave the state for
                // the next redelivery, which re-inspects from scratch.
                await transaction.CommitAsync(ct);
                _logger.LogWarning(
                    "Event {EventId} residue claim disappeared before reconciliation ({Outcome}); "
                    + "leaving for a later redelivery",
                    message.EventId, residue);
                return;
            }
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            _logger.LogInformation(
                "Event {EventId} interrupted attempt reconciled ({Outcome}); provider NOT re-fired",
                message.EventId, residue);
        }
        catch (N8nDispatchRetryableException)
        {
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private bool IsClaimFresh(DateTimeOffset? claimedAt) =>
        claimedAt is not null && _clock.UtcNow - claimedAt.Value < _providerEffectClaimStaleAfter;
}

/// <summary>
/// Signals the delivery mechanism to redeliver the dispatch intent. Carries the
/// stable execution identity for diagnostics only.
/// </summary>
public sealed class N8nDispatchRetryableException(Guid executionId)
    : InvalidOperationException($"n8n dispatch for execution {executionId} requires another delivery attempt.");