using System.Text.Json;
using Notrelix.Infrastructure.Data;

using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Messaging.Options;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class OutboxDispatcher : BackgroundService
{
    private const string DispatcherConsumerName = "OutboxDispatcher";
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private const int ProcessingTimeoutSeconds = 60;
    private const int MaxRetries = 5;
    private const int MaxBackoffSeconds = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly IOutboxWakeSignal _wakeSignal;
    private readonly MetricsService _metrics;
    private readonly OutboxDispatchOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private DateTimeOffset _lastCountRefresh;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger,
        IOutboxWakeSignal wakeSignal,
        MetricsService metrics,
        IOptions<OutboxDispatchOptions> options)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _wakeSignal = wakeSignal;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcessBatchAsync(stoppingToken))
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher failed");
            }

            await _wakeSignal.WaitAsync(PollInterval, stoppingToken);
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await RefreshOutboxCountsIfDueAsync(cancellationToken);

        // Phase 1: Short claim transaction — select + mark Processing + commit
        var pollTimer = System.Diagnostics.Stopwatch.StartNew();
        var claim = await ClaimBatchAsync(cancellationToken);
        if (claim.IsEmpty) return false;
        var claimMs = pollTimer.ElapsedMilliseconds;

        // Phase 2: Publish outside database transaction
        var startedRuns = 0;
        try
        {
            if (claim.Unordered.Count > 0)
            {
                await DispatchUnorderedAsync(claim.Unordered, claim.LockId, cancellationToken);
            }

            // Ordered streams: each run is dispatched strictly in stream version order, stopping at its first failure.
            foreach (var run in claim.StreamRuns)
            {
                startedRuns++;
                await DispatchStreamRunAsync(run, claim.LockId, cancellationToken);
            }
        }
        finally
        {
            // A failure in an earlier part must not hold the runs that never started until the lease expires.
            foreach (var run in claim.StreamRuns.Skip(startedRuns))
            {
                await ReleaseAsync(run, claim.LockId);
            }
        }

        _logger.LogDebug(
            "Outbox poll: {Unordered} unordered + {Streams} stream run(s) of {Ordered} message(s); claim {ClaimMs} ms, dispatch {DispatchMs} ms",
            claim.Unordered.Count, claim.StreamRuns.Count, claim.StreamRuns.Sum(r => r.Length), claimMs, pollTimer.ElapsedMilliseconds - claimMs);
        return claim.MoreLikely;
    }

    private async Task DispatchUnorderedAsync(List<Guid> claimed, Guid lockId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateWorkerAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var integrationEventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var eventTypeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
        var eventCatalog = scope.ServiceProvider.GetRequiredService<IIntegrationEventCatalog>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // Re-attach claimed messages to this scope's context
        var messages = await context.Set<MessagingOutboxMessage>()
            .Where(m => claimed.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            // Verify lease ownership before processing
            if (message.LockId != lockId)
            {
                _logger.LogWarning(
                    "Outbox {MsgId}: lease lost (expected {ExpectedLock}, found {ActualLock}). Skipping.",
                    message.Id, lockId, message.LockId);
                continue;
            }

            await ProcessMessageAsync(message, context, eventTypeRegistry, eventCatalog, integrationEventBus, dateTimeProvider, cancellationToken);
        }

        // Phase 3: Short completion transaction — mark Processed/Failed + commit
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Dispatches one stream's claimed run in stream version order. Message k+1 is handed to the bus only after
    /// message k was saved Processed; the first message that does not end Processed (failed, dead-lettered, lease
    /// lost) stops the run and the rest is released unattempted. Each message commits on its own, so a crash
    /// redelivers at most the message that was in flight (consumer dedup absorbs it).
    /// </summary>
    private async Task DispatchStreamRunAsync(IReadOnlyList<Guid> ids, Guid lockId, CancellationToken cancellationToken)
    {
        var next = 0;
        try
        {
            await using var scope = _scopeFactory.CreateWorkerAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var integrationEventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
            var eventTypeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
            var eventCatalog = scope.ServiceProvider.GetRequiredService<IIntegrationEventCatalog>();
            var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

            while (next < ids.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = ids[next];

                // Fence: renew the lease on the message about to run, only while this dispatcher still owns it.
                // A row another instance reclaimed (or that is already Processed) matches nothing, so two
                // dispatchers can never work the same stream at once.
                var now = dateTimeProvider.UtcNow;
                var renewed = await context.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE messaging.outbox_messages
                    SET processing_started_at = {0}, locked_until = {1}, updated_at = {0}
                    WHERE id = {2} AND lock_id = {3} AND status = 'Processing'
                    """,
                    [now.UtcDateTime, now.AddSeconds(ProcessingTimeoutSeconds).UtcDateTime, id, lockId],
                    cancellationToken);
                if (renewed == 0)
                {
                    _logger.LogWarning(
                        "Outbox {MsgId}: lease lost (lock {LockId}); stopping this stream run, {Left} message(s) released.",
                        id, lockId, ids.Count - next);
                    return;
                }

                context.ChangeTracker.Clear();
                var message = await context.Set<MessagingOutboxMessage>()
                    .SingleAsync(m => m.Id == id, cancellationToken);

                await ProcessMessageAsync(message, context, eventTypeRegistry, eventCatalog, integrationEventBus, dateTimeProvider, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                next++;

                if (message.Status != "Processed")
                {
                    _logger.LogWarning(
                        "Outbox {MsgId}: stream {StreamKey} stopped at version {Version} ({Status}); {Left} later message(s) released.",
                        id, message.StreamKey, message.StreamVersion, message.Status, ids.Count - next);
                    return;
                }
            }
        }
        finally
        {
            if (next < ids.Count)
            {
                await ReleaseAsync(ids.Skip(next).ToArray(), lockId);
            }
        }
    }

    /// <summary>Puts claimed-but-unattempted messages back to Pending, only the ones this lock still owns.</summary>
    private async Task ReleaseAsync(Guid[] ids, Guid lockId)
    {
        try
        {
            await using var scope = _scopeFactory.CreateWorkerAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow;
            await context.Database.ExecuteSqlRawAsync(
                """
                UPDATE messaging.outbox_messages
                SET status = 'Pending', lock_id = NULL, processing_started_at = NULL, locked_until = NULL, updated_at = {0}
                WHERE lock_id = {1} AND status = 'Processing' AND id = ANY({2})
                """,
                [now.UtcDateTime, lockId, ids],
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Not released: the rows return after the processing timeout, in order, like after a crash.
            _logger.LogWarning(ex, "Outbox: failed to release {Count} claimed message(s) of lock {LockId}", ids.Length, lockId);
        }
    }

    private async Task RefreshOutboxCountsIfDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastCountRefresh < TimeSpan.FromSeconds(30))
        {
            return;
        }

        _lastCountRefresh = now;

        try
        {
            await using var scope = _scopeFactory.CreateWorkerAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Dead-letter is the exhausted 'Failed' terminal state, not a status of
            // its own: retry_count >= max_retries marks rows no dispatcher will claim.
            var counts = await context.Set<MessagingOutboxMessage>()
                .GroupBy(m => new { m.Status, Exhausted = m.RetryCount >= m.MaxRetries })
                .Select(g => new { g.Key.Status, g.Key.Exhausted, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var oldestUndispatched = await context.Set<MessagingOutboxMessage>()
                .Where(m => m.Status == "Pending" || m.Status == "Processing" || m.Status == "Failed")
                .MinAsync(m => (DateTimeOffset?)m.CreatedAt, cancellationToken);

            _metrics.UpdateOutboxCounts(
                counts.Where(c => c.Status == "Pending").Sum(c => c.Count),
                counts.Where(c => c.Status == "Failed").Sum(c => c.Count),
                counts.Where(c => c.Status == "Failed" && c.Exhausted).Sum(c => c.Count),
                oldestUndispatched.HasValue ? (now - oldestUndispatched.Value).TotalMilliseconds : null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh outbox gauge counts");
        }
    }

    // A row is claimable when it is due Pending, a Processing row whose lease expired (crashed dispatcher), or a
    // Failed row inside its retry budget whose backoff elapsed. {0} = now, {1} = lease cutoff.
    private const string Claimable = """
        (
            (status = 'Pending' AND next_attempt_at <= {0})
            OR
            (status = 'Processing' AND processing_started_at <= {1})
            OR
            (status = 'Failed' AND retry_count < max_retries AND next_attempt_at <= {0})
        )
        """;

    private const string UnorderedClaimSql = $$"""
        SELECT * FROM messaging.outbox_messages
        WHERE stream_key IS NULL AND {{Claimable}}
        ORDER BY created_at
        LIMIT {2}
        FOR UPDATE SKIP LOCKED
        """;

    // Ordered streams. A stream's head is its earliest message that is not Processed; it is claimable only when it
    // is itself claimable, which is the strict-order guard (nothing earlier is outstanding). Streams with outstanding
    // messages are enumerated by a skip scan over the partial index of unprocessed messages, so the head probe costs
    // one index lookup per stream instead of growing with the backlog. Heads are taken oldest first, capped at {3}.
    // Each head brings the consecutive run behind it, at most {2} messages, cut at the first message that is not
    // itself claimable (e.g. a Failed one in backoff) so a run never jumps over an outstanding message. The whole poll
    // is capped at {3} messages, oldest head first, so one hot stream cannot starve the others.
    private const string RunCandidatesSql = $$"""
        WITH RECURSIVE streams(stream_key) AS (
            (SELECT stream_key FROM messaging.outbox_messages
             WHERE stream_key IS NOT NULL AND status <> 'Processed'
             ORDER BY stream_key LIMIT 1)
            UNION ALL
            SELECT (SELECT o.stream_key FROM messaging.outbox_messages o
                    WHERE o.stream_key > s.stream_key AND o.status <> 'Processed'
                    ORDER BY o.stream_key LIMIT 1)
            FROM streams s WHERE s.stream_key IS NOT NULL
        ),
        heads AS (
            SELECT hd.stream_key, hd.stream_version, hd.created_at
            FROM streams s
            CROSS JOIN LATERAL (
                SELECT o.stream_key, o.stream_version, o.created_at, o.status, o.next_attempt_at,
                       o.processing_started_at, o.retry_count, o.max_retries
                FROM messaging.outbox_messages o
                WHERE o.stream_key = s.stream_key AND o.status <> 'Processed'
                ORDER BY o.stream_version LIMIT 1
            ) hd
            WHERE s.stream_key IS NOT NULL AND {{Claimable}}
            ORDER BY hd.created_at
            LIMIT {3}
        )
        SELECT x.id, x.stream_key, x.stream_version, x.head_created_at
        FROM (
            SELECT r.id, h.stream_key, r.stream_version, h.created_at AS head_created_at,
                   bool_and(r.claimable) OVER (PARTITION BY h.stream_key ORDER BY r.stream_version) AS run_ok
            FROM heads h
            CROSS JOIN LATERAL (
                SELECT m.id, m.stream_version, {{Claimable}} AS claimable
                FROM messaging.outbox_messages m
                WHERE m.stream_key = h.stream_key AND m.stream_version >= h.stream_version
                  AND m.status <> 'Processed'
                ORDER BY m.stream_version
                LIMIT {2}
            ) r
        ) x
        WHERE x.run_ok
        ORDER BY x.head_created_at, x.stream_key, x.stream_version
        LIMIT {3}
        """;

    // SKIP LOCKED may skip a row another claimer holds, and the claimable test is repeated on the locked row because
    // another claimer may have taken it since the candidate read.
    private const string LockRunSql = $$"""
        SELECT * FROM messaging.outbox_messages
        WHERE id = ANY({2}) AND {{Claimable}}
        ORDER BY stream_key, stream_version
        FOR UPDATE SKIP LOCKED
        """;

    private sealed record ClaimedWork(List<Guid> Unordered, List<Guid[]> StreamRuns, Guid LockId, bool MoreLikely)
    {
        public bool IsEmpty => Unordered.Count == 0 && StreamRuns.Count == 0;
    }

    private sealed record RunRow(Guid Id, string StreamKey, long StreamVersion, DateTimeOffset HeadCreatedAt);

    private async Task<ClaimedWork> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateWorkerAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var processingCutoff = now.AddSeconds(-ProcessingTimeoutSeconds);
        var lockId = Guid.NewGuid();
        var streamBatch = _options.StreamBatchSize;
        var streamCap = _options.MaxStreamMessagesPerPoll;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Messages with no stream have no order contract: oldest first, as before.
            var unordered = await context.Set<MessagingOutboxMessage>()
                .FromSqlRaw(UnorderedClaimSql, now.UtcDateTime, processingCutoff.UtcDateTime, BatchSize)
                .ToListAsync(cancellationToken);

            // Ordered streams: see RunCandidatesSql.
            var candidates = await context.Database
                .SqlQueryRaw<RunRow>(RunCandidatesSql, now.UtcDateTime, processingCutoff.UtcDateTime, streamBatch, streamCap)
                .OrderBy(r => r.HeadCreatedAt).ThenBy(r => r.StreamKey).ThenBy(r => r.StreamVersion)
                .ToListAsync(cancellationToken);

            // Lock the run. A run is only valid as a prefix, so each stream keeps its messages up to the first
            // one that could not be locked.
            var ordered = new List<Guid[]>();
            MessagingOutboxMessage[] lockedStreamRows = [];
            if (candidates.Count > 0)
            {
                lockedStreamRows = (await context.Set<MessagingOutboxMessage>()
                    .FromSqlRaw(LockRunSql, now.UtcDateTime, processingCutoff.UtcDateTime, candidates.Select(c => c.Id).ToArray())
                    .ToListAsync(cancellationToken)).ToArray();

                var lockedIds = lockedStreamRows.Select(m => m.Id).ToHashSet();
                foreach (var stream in candidates.GroupBy(c => c.StreamKey))
                {
                    var run = stream.Select(c => c.Id).TakeWhile(lockedIds.Contains).ToArray();
                    if (run.Length > 0) ordered.Add(run);
                }
            }

            var kept = ordered.SelectMany(r => r).ToHashSet();
            if (unordered.Count == 0 && kept.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ClaimedWork([], [], Guid.Empty, false);
            }

            var claimedRows = unordered.Concat(lockedStreamRows.Where(m => kept.Contains(m.Id))).ToList();
            foreach (var message in claimedRows)
            {
                message.MarkProcessing(now, lockId);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            foreach (var message in claimedRows)
            {
                _metrics.CommitToClaim.Record((now - message.CreatedAt).TotalMilliseconds);
            }

            _logger.LogDebug(
                "Claimed {Unordered} unordered and {Ordered} ordered outbox messages in {Streams} stream run(s) (lock={LockId})",
                unordered.Count, kept.Count, ordered.Count, lockId);

            // More is likely waiting when a bound was reached: the unordered limit, the poll cap, or a full run.
            var more = unordered.Count == BatchSize
                || kept.Count >= streamCap
                || ordered.Any(r => r.Length == streamBatch);
            return new ClaimedWork(unordered.Select(m => m.Id).ToList(), ordered, lockId, more);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ProcessMessageAsync(
        MessagingOutboxMessage message,
        ApplicationDbContext context,
        IEventTypeRegistry eventTypeRegistry,
        IIntegrationEventCatalog eventCatalog,
        IIntegrationEventBus integrationEventBus,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        var alreadyProcessed = await context.Set<MessagingProcessedEvent>()
            .AnyAsync(x => x.EventId == message.EventId
                && x.ConsumerName == DispatcherConsumerName
                && x.Status == "Succeeded", cancellationToken);

        if (alreadyProcessed)
        {
            message.MarkProcessed(now);
            _logger.LogDebug("V5 outbox {MsgId}: {MsgName} already processed, skipping",
                message.Id, message.MessageName);
            return;
        }

        var attempt = new OutboxDeliveryAttempt(
            message.Id, message.EventId, message.RetryCount + 1,
            Environment.MachineName, "MassTransit", null, "Started", now);
        context.Set<OutboxDeliveryAttempt>().Add(attempt);

        Type integrationEventType;
        try
        {
            // Compound contract identity: the outbox row carries both the logical
            // message name and its schema version (IAREQ131). No name-only fallback.
            integrationEventType = eventCatalog.Resolve(new EventContractKey(message.MessageName, message.SchemaVersion));
        }
        catch (UnknownIntegrationEventTypeException ex)
        {
            _logger.LogCritical(ex, "V5 outbox {MsgId}: unknown integration event type {MsgName} — dead-lettering permanently",
                message.Id, message.MessageName);
            message.MarkDeadLetter("UnknownEventType", ex.Message, now);
            attempt.MarkFailed("UnknownEventType", ex.Message, now);
            var processedEvent = new MessagingProcessedEvent(
                message.EventId, DispatcherConsumerName,
                message.SourceContext, message.MessageName, message.SchemaVersion,
                message.SourceEventId, message.SubjectType, message.SubjectId,
                message.WorkspaceId, message.ActorUserId,
                message.CorrelationId, message.CausationId, now);
            processedEvent.MarkFailed(now, ex.Message);
            context.Set<MessagingProcessedEvent>().Add(processedEvent);
            return;
        }

        try
        {
            var integrationEvent = message.PayloadJson.Deserialize(integrationEventType, JsonOptions) as IIntegrationEvent;

            if (integrationEvent is null)
            {
                _logger.LogError("V5 outbox {MsgId}: failed to deserialize payload as {MsgName}",
                    message.Id, message.MessageName);
                FailMessage(message, attempt, "DeserializationFailed", "Deserialization returned null or unexpected type", dateTimeProvider);
                return;
            }

            // Publish OUTSIDE database transaction — broker latency does not hold DB locks
            var publishStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await integrationEventBus.PublishAsync(integrationEvent, cancellationToken);
            publishStopwatch.Stop();

            var publishedAt = dateTimeProvider.UtcNow;
            _metrics.OutboxPublishDuration.Record(publishStopwatch.Elapsed.TotalMilliseconds);
            _metrics.CommitToPublish.Record((publishedAt - message.CreatedAt).TotalMilliseconds);
            _metrics.OutboxDispatchedCount.Add(1);

            message.MarkProcessed(now);
            attempt.MarkSucceeded(now);

            var processedEvent = new MessagingProcessedEvent(
                message.EventId, DispatcherConsumerName,
                message.SourceContext, message.MessageName, message.SchemaVersion,
                message.SourceEventId, message.SubjectType, message.SubjectId,
                message.WorkspaceId, message.ActorUserId,
                message.CorrelationId, message.CausationId, now);
            processedEvent.MarkSucceeded(now);
            context.Set<MessagingProcessedEvent>().Add(processedEvent);

            _logger.LogDebug("V5 outbox {MsgId}: {MsgName} dispatched (attempt {Retry})",
                message.Id, message.MessageName, message.RetryCount + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V5 outbox {MsgId}: {MsgName} failed (attempt {Retry})",
                message.Id, message.MessageName, message.RetryCount + 1);
            _metrics.PublishFailures.Add(1);
            FailMessage(message, attempt, "DispatchFailed", ex.ToString(), dateTimeProvider);
        }
    }

    private static void FailMessage(
        MessagingOutboxMessage message,
        OutboxDeliveryAttempt attempt,
        string errorCode,
        string errorMessage,
        IDateTimeProvider dateTimeProvider)
    {
        message.MarkFailed(errorCode, errorMessage, dateTimeProvider.UtcNow);
        attempt.MarkFailed(errorCode, errorMessage, dateTimeProvider.UtcNow);
    }
}
