using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Messaging.Options;

namespace Notrelix.Infrastructure.Messaging;

public sealed class DeduplicationConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IMessageDeduplicationStore _dedupStore;
    private readonly ApplicationDbContext _db;
    private readonly IRlsSessionContext _rls;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DeduplicationConsumeFilter<T>> _logger;
    private readonly MessageDeduplicationOptions _options;

    public DeduplicationConsumeFilter(
        IMessageDeduplicationStore dedupStore,
        ApplicationDbContext db,
        IRlsSessionContext rls,
        IDateTimeProvider dateTimeProvider,
        ILogger<DeduplicationConsumeFilter<T>> logger,
        IOptions<MessageDeduplicationOptions>? options = null)
    {
        _options = options?.Value ?? new MessageDeduplicationOptions();
        _dedupStore = dedupStore;
        _db = db;
        _rls = rls;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Message is not IIntegrationEvent integrationEvent)
        {
            await next.Send(context);
            return;
        }

        var consumerName = ExtractConsumerName(context);

        // Provider-effect durable consumer protocol (e.g. n8n dispatch): the
        // consumer itself owns the claim, RLS transactions, out-of-transaction
        // provider effect, and success marker. Wrapping it would open a second
        // transaction on the shared connection, so these endpoints pass through.
        if (ConsumerOwnedProtocolEndpoints.Contains(consumerName))
        {
            await next.Send(context);
            return;
        }

        // A command-dispatching consumer owns its own data-session transaction:
        // its MediatR command pipeline (DataSessionBehavior -> EfRequestDataSession)
        // opens a transaction (and applies its own RLS) on the shared
        // ApplicationDbContext. Wrapping such a consumer in the dedup transaction
        // below would begin a second transaction on the same connection and throw
        // "already in a transaction". These consumers instead take the
        // command-owned path that never wraps `next`.
        if (CommandOwnedTransactionEndpoints.Contains(consumerName))
        {
            await CommandOwnedSendAsync(context, next, integrationEvent, consumerName);
            return;
        }

        await TransactionalSendAsync(context, next, integrationEvent, consumerName);
    }

    /// <summary>
    /// Consumers that own the complete provider-effect claim protocol and
    /// therefore must NOT be wrapped in the dedup filter's transaction.
    /// Keyed by the receive endpoint name (the same value
    /// <c>N8nDispatchProtocolEndpoints.DispatchEndpointName</c> and the dedup
    /// filter's InputAddress-derived consumer name agree on in production).
    /// </summary>
    private static readonly HashSet<string> ConsumerOwnedProtocolEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        N8nDispatchProtocolEndpoints.DispatchEndpointName,
    };

    /// <summary>
    /// Consumers that run their own MediatR command/data-session transaction and
    /// therefore must NOT be wrapped in the dedup filter's transaction.
    /// Keyed by the receive endpoint name (both the dedup filter and
    /// ConsumerDefinition.EndpointName agree on this exact value).
    /// </summary>
    private static readonly HashSet<string> CommandOwnedTransactionEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        // Must match WorkspaceProvisioningConsumerDefinition.EndpointName.
        "notrelix-identity-registration-completed-workspace-provision-v1",
    };

    /// <summary>
    /// Default path: the dedup claim, the consumer effect and the success marker
    /// commit atomically inside one wrapping transaction, and RLS session context
    /// (set_config(..., true) == SET LOCAL) applies within that transaction.
    /// On failure the transaction rolls back, removing the "Processing" claim so
    /// the message can be retried.
    /// </summary>
    private async Task TransactionalSendAsync(
        ConsumeContext<T> context,
        IPipe<ConsumeContext<T>> next,
        IIntegrationEvent integrationEvent,
        string consumerName)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            // CRITICAL: Apply RLS trong transaction này cho TẤT CẢ events
            // Bao gồm cả system events (khi _tenant.IsSystemContext == true)
            await _rls.ApplyAsync(context.CancellationToken);

            var claimed = await _dedupStore.TryClaimProcessingAsync(
                messageId: integrationEvent.EventId,
                consumerName: consumerName,
                messageName: integrationEvent.MessageName,
                messageVersion: integrationEvent.SchemaVersion,
                sourceEventId: integrationEvent.SourceEventId,
                workspaceId: integrationEvent.WorkspaceId,
                cancellationToken: context.CancellationToken);

            if (!claimed)
            {
                _logger.LogDebug(
                    "Event {EventId} ({MessageName}) already claimed/processed by {ConsumerName}, skipping",
                    integrationEvent.EventId, integrationEvent.MessageName, consumerName);
                await transaction.RollbackAsync(context.CancellationToken);
                return;
            }

            await next.Send(context);

            _dedupStore.MarkSucceeded(
                messageId: integrationEvent.EventId,
                consumerName: consumerName,
                processedAt: _dateTimeProvider.UtcNow);

            await _db.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);

            _logger.LogDebug(
                "Event {EventId} ({MessageName}) processed by {ConsumerName}",
                integrationEvent.EventId, integrationEvent.MessageName, consumerName);
        }
        catch
        {
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Command-owned path: the consumer's MediatR command opens and commits its own
    /// data-session transaction (with its own RLS), so this filter MUST NOT open a
    /// wrapping transaction here. The claim and success marker are persisted as
    /// independent autocommit writes; on failure the "Processing" claim is removed
    /// so the message can be retried (unique-constraint dedup otherwise blocks it).
    /// If the process dies between claim and completion the claim stays "Processing"
    /// until its lease (<see cref="MessageDeduplicationOptions.ProcessingClaimLeaseSeconds"/>)
    /// expires, after which a redelivery takes it over (see TryTakeOverStaleClaimAsync).
    /// The effect itself is idempotent for such consumers (e.g. an existing personal
    /// Workspace is reported as already-existed), bounding retry side effects.
    /// </summary>
    private async Task CommandOwnedSendAsync(
        ConsumeContext<T> context,
        IPipe<ConsumeContext<T>> next,
        IIntegrationEvent integrationEvent,
        string consumerName)
    {
        var claimed = await _dedupStore.TryClaimProcessingAsync(
            messageId: integrationEvent.EventId,
            consumerName: consumerName,
            messageName: integrationEvent.MessageName,
            messageVersion: integrationEvent.SchemaVersion,
            sourceEventId: integrationEvent.SourceEventId,
            workspaceId: integrationEvent.WorkspaceId,
            cancellationToken: context.CancellationToken);

        if (!claimed && !await TryTakeOverStaleClaimAsync(context, integrationEvent, consumerName))
        {
            return;
        }

        try
        {
            await next.Send(context);

            _dedupStore.MarkSucceeded(
                messageId: integrationEvent.EventId,
                consumerName: consumerName,
                processedAt: _dateTimeProvider.UtcNow);

            await _db.SaveChangesAsync(context.CancellationToken);
        }
        catch
        {
            // Failure atomicity: the command/DataSession rollback does NOT clear the
            // shared scoped ApplicationDbContext ChangeTracker, so the failed pipeline's
            // tracked entities (e.g. an Added Workspace/Member and the domain events
            // mapped to outbox rows) are still pending in this same context. The claim
            // removal SaveChanges below must affect ONLY that removal — never silently
            // flush and re-commit the rolled-back writes. Detach every tracked entry
            // first so the cleanup SaveChanges cannot partially persist the failure.
            DetachAllTrackedEntries(_db);

            var claim = await _db.Set<MessagingProcessedEvent>()
                .FirstOrDefaultAsync(e => e.EventId == integrationEvent.EventId
                    && e.ConsumerName == consumerName, context.CancellationToken);
            if (claim is not null)
            {
                _db.Set<MessagingProcessedEvent>().Remove(claim);
                await _db.SaveChangesAsync(context.CancellationToken);
            }
            throw;
        }
    }

    /// <summary>
    /// Called when the claim insert lost to an existing (message, consumer) row.
    /// <list type="bullet">
    /// <item>Succeeded: genuine duplicate, ignored (returns false).</item>
    /// <item>Processing older than the lease: the owning attempt is presumed dead
    /// (crash between claim commit and completion). Exactly one redelivery wins the
    /// compare-and-swap and re-runs the command (returns true).</item>
    /// <item>Processing inside the lease (or row vanished mid-race): NOT acknowledged;
    /// <see cref="MessageClaimInFlightException"/> defers the delivery so a crashed
    /// owner is recovered once its lease expires instead of the message being lost.</item>
    /// </list>
    /// Exactly-once effect on takeover: the command's data-session transaction commits
    /// the business write and its message-keyed idempotency record atomically
    /// (IdempotencyBehavior, key = EventId). If the first attempt committed, the re-run
    /// replays the stored result without executing the handler; if it did not commit,
    /// there is no effect to duplicate. Only the success marker is advanced after
    /// <c>next</c> succeeded (NRX-010).
    /// </summary>
    private async Task<bool> TryTakeOverStaleClaimAsync(
        ConsumeContext<T> context,
        IIntegrationEvent integrationEvent,
        string consumerName)
    {
        var ct = context.CancellationToken;

        if (await _dedupStore.IsProcessedAsync(integrationEvent.EventId, consumerName, ct))
        {
            _logger.LogDebug(
                "Event {EventId} ({MessageName}) already claimed/processed by {ConsumerName}, skipping",
                integrationEvent.EventId, integrationEvent.MessageName, consumerName);
            return false;
        }

        var now = _dateTimeProvider.UtcNow;
        var reclaimed = await _dedupStore.TryReclaimStaleProcessingAsync(
            integrationEvent.EventId,
            consumerName,
            staleBefore: now - TimeSpan.FromSeconds(_options.ProcessingClaimLeaseSeconds),
            reclaimedAt: now,
            ct);

        if (reclaimed)
        {
            _logger.LogWarning(
                "Event {EventId} ({MessageName}) had a stale Processing claim for {ConsumerName}; reclaimed after lease expiry",
                integrationEvent.EventId, integrationEvent.MessageName, consumerName);
            return true;
        }

        throw new MessageClaimInFlightException(integrationEvent.EventId, consumerName);
    }

    private static void DetachAllTrackedEntries(DbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("deduplicationConsumeFilter");
    }

    private static string ExtractConsumerName(ConsumeContext<T> context)
    {
        var inputAddress = context.ReceiveContext.InputAddress;
        if (inputAddress is not null)
        {
            var segments = inputAddress.AbsolutePath.Trim('/').Split('/');
            return segments.Length > 0 ? segments[^1] : $"consumer:{typeof(T).Name}";
        }

        return $"consumer:{typeof(T).Name}";
    }
}

