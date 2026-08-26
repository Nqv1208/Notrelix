using System.Text.Json;
using Notrelix.Application.Common.Realtime;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// FZ-INF-03 — crash-after-claim: the outbox claim query mirrors the
/// OutboxDispatcher contract (Processing lease of 60s, per-stream ordering
/// guard, bounded retry). A row claimed by a crashed dispatcher must be
/// reclaimable once its lease expires, while an unexpired lease must never be
/// stolen, and a later stream version must never be claimed while an earlier
/// version of the same stream remains undispatched.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class OutboxClaimReclaimTests : IAsyncLifetime
{
    private const int ProcessingTimeoutSeconds = 60;

    private readonly PostgresTestContainer _fixture;
    private DatabaseReset _reset = null!;

    public OutboxClaimReclaimTests(PostgresTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_fixture.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ApplicationDbContext CreateContext() =>
        _fixture.CreateContext(new FakeCurrentTenantContext());

    private static MessagingOutboxMessage CreateMessage(
        Guid eventId,
        string status,
        DateTimeOffset? processingStartedAt,
        DateTimeOffset now,
        string? streamKey = null,
        long? streamVersion = null)
    {
        MessagingOutboxMessage message;
        if (streamKey is not null && streamVersion.HasValue)
        {
            var change = new RealtimeResourceChangedV1(
                eventId, accountId: null, workspaceId: null, actorUserId: null,
                Guid.NewGuid(), causationId: null, now,
                topicNamespace: "work", resourceKind: "board-item",
                resourceId: Guid.NewGuid(), streamKey, streamVersion.Value,
                changeKind: "updated", payloadContract: "test.v1",
                JsonDocument.Parse("{}").RootElement);
            message = MessagingOutboxMessage.FromIntegrationEvent(change, now);
        }
        else
        {
            message = new MessagingOutboxMessage(
                eventId: eventId,
                sourceEventId: null,
                sourceContext: "test",
                messageName: "test.reclaim.v1",
                schemaVersion: 1,
                destination: null,
                subjectType: null,
                subjectId: null,
                aggregateType: null,
                aggregateId: null,
                workspaceId: null,
                accountId: null,
                actorUserId: null,
                correlationId: Guid.NewGuid().ToString(),
                causationId: null,
                partitionKey: null,
                payloadJson: JsonDocument.Parse("{}"),
                headersJson: null,
                metadataJson: null,
                createdAt: now);
        }

        switch (status)
        {
            case "Pending":
                break;
            case "Processing":
                message.MarkProcessing(processingStartedAt ?? now, Guid.NewGuid());
                break;
            case "Failed":
                message.MarkFailed("DispatchFailed", "boom", now);
                break;
        }

        return message;
    }

    [Fact]
    public async Task Claim_ReclaimsExpiredLease_ButNeverActiveLease()
    {
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var pendingEventId = Guid.NewGuid();
        var expiredLeaseEventId = Guid.NewGuid();
        var activeLeaseEventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().AddRange(
                CreateMessage(pendingEventId, "Pending", null, now),
                CreateMessage(expiredLeaseEventId, "Processing", now.AddSeconds(-ProcessingTimeoutSeconds - 10), now),
                CreateMessage(activeLeaseEventId, "Processing", now.AddSeconds(-10), now));
            await context.SaveChangesAsync();
        }

        var lockId = Guid.NewGuid();
        var claimed = await ClaimAsync(now, lockId);
        claimed.Select(m => m.EventId).Should().BeEquivalentTo(
            [pendingEventId, expiredLeaseEventId],
            "a crashed claim (expired lease) and a pending row are reclaimable; the active lease is not");

        var all = await ReadAllAsync();

        var expired = all.Single(m => m.EventId == expiredLeaseEventId);
        expired.Status.Should().Be("Processing");
        expired.LockId.Should().Be(lockId, "the reclaiming dispatcher owns the reclaimed lease");
        expired.ProcessingStartedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));

        var active = all.Single(m => m.EventId == activeLeaseEventId);
        active.ProcessingStartedAt.Should().Be(now.AddSeconds(-10));
        active.LockId.Should().NotBe(lockId, "an unexpired lease must never be stolen");
    }

    [Fact]
    public async Task Claim_SecondRun_DoesNotReclaim_AlreadyClaimedRows()
    {
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().Add(
                CreateMessage(Guid.NewGuid(), "Pending", null, now));
            await context.SaveChangesAsync();
        }

        var firstClaim = await ClaimAsync(now, Guid.NewGuid());
        firstClaim.Should().HaveCount(1, "the first claim takes the pending row");

        var secondClaim = await ClaimAsync(now, Guid.NewGuid());
        secondClaim.Should().BeEmpty("a fresh claim must not double-take rows claimed moments ago");
    }

    [Fact]
    public async Task Claim_ExpiredFailedRow_IsReclaimable()
    {
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var failedEventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().Add(
                CreateMessage(failedEventId, "Failed", null, now));
            await context.SaveChangesAsync();
        }

        // Move next_attempt_at into the past, as the dispatcher's retry backoff does.
        await using (var rewrite = CreateContext())
        {
            await rewrite.Database.ExecuteSqlRawAsync(
                "UPDATE messaging.outbox_messages SET next_attempt_at = {0} WHERE event_id = {1}",
                now.AddMinutes(-5).UtcDateTime, failedEventId);
        }

        var claimed = await ClaimAsync(now, Guid.NewGuid());
        claimed.Select(m => m.EventId).Should().Contain(failedEventId,
            "a failed message whose retry window has elapsed stays retryable");
    }

    [Fact]
    public async Task Claim_FailedEarlierVersion_BlocksLaterVersionOfSameStream()
    {
        // Ordered-stream invariant: N+1 of a stream may only be claimed after every
        // earlier version is Processed — a Failed (retry-exhausted or not) v1 must
        // hold back Pending v2 of the same stream.
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var seededAt = now.AddMinutes(-1);
        var streamKey = $"board-item:{Guid.NewGuid():N}";
        var v1EventId = Guid.NewGuid();
        var v2EventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().AddRange(
                CreateMessage(v1EventId, "Failed", null, seededAt, streamKey, 1),
                CreateMessage(v2EventId, "Pending", null, seededAt.AddMilliseconds(1), streamKey, 2));
            await context.SaveChangesAsync();
        }

        var claimed = await ClaimAsync(now, Guid.NewGuid());

        claimed.Should().NotContain(m => m.EventId == v2EventId,
            "a later stream version must never be claimed while an earlier version is undispatched");
    }

    [Fact]
    public async Task Claim_ProcessedEarlierVersion_ReleasesLaterVersion_AndUnrelatedStreams()
    {
        // Once v1 is Processed the stream head advances: v2 becomes claimable and an
        // unrelated blocked stream does not prevent independent progress elsewhere.
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var seededAt = now.AddMinutes(-1);
        var releasedStream = $"board-item:{Guid.NewGuid():N}";
        var unrelatedStream = $"board-item:{Guid.NewGuid():N}";
        var processedV1Id = Guid.NewGuid();
        var pendingV2Id = Guid.NewGuid();
        var unrelatedV1Id = Guid.NewGuid();
        var unrelatedV2Id = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().AddRange(
                CreateMessage(processedV1Id, "Pending", null, seededAt, releasedStream, 1),
                CreateMessage(pendingV2Id, "Pending", null, seededAt.AddMilliseconds(1), releasedStream, 2),
                CreateMessage(unrelatedV1Id, "Failed", null, seededAt, unrelatedStream, 1),
                CreateMessage(unrelatedV2Id, "Pending", null, seededAt.AddMilliseconds(2), unrelatedStream, 2));
            await context.SaveChangesAsync();

            // Mark the first stream's v1 as fully dispatched.
            var processed = await context.Set<MessagingOutboxMessage>()
                .SingleAsync(m => m.EventId == processedV1Id);
            processed.MarkProcessed(now);
            await context.SaveChangesAsync();
        }

        var claimed = await ClaimAsync(now, Guid.NewGuid());

        claimed.Select(m => m.EventId).Should().BeEquivalentTo(
            [pendingV2Id, unrelatedV1Id],
            "Processed v1 releases same-stream v2; the failed v1 of an unrelated stream stays retryable by itself while blocking only its own later version");

        claimed.Should().NotContain(m => m.EventId == unrelatedV2Id,
            "a failed earlier version blocks later versions of its own stream");
    }

    private async Task<List<MessagingOutboxMessage>> ClaimAsync(DateTimeOffset now, Guid lockId)
    {
        await using var context = CreateContext();
        var processingCutoff = now.AddSeconds(-ProcessingTimeoutSeconds);

        await using var transaction = await context.Database.BeginTransactionAsync();
        var claimed = await context.Set<MessagingOutboxMessage>()
            .FromSqlRaw("""
                SELECT * FROM messaging.outbox_messages
                WHERE (
                    (status = 'Pending' AND next_attempt_at <= {0})
                    OR
                    (status = 'Processing' AND processing_started_at <= {1})
                    OR
                    (status = 'Failed' AND retry_count < max_retries AND next_attempt_at <= {0})
                )
                AND (
                    stream_key IS NULL
                    OR NOT EXISTS (
                        SELECT 1
                        FROM messaging.outbox_messages earlier
                        WHERE earlier.stream_key = outbox_messages.stream_key
                          AND earlier.stream_version < outbox_messages.stream_version
                          AND earlier.status <> 'Processed'
                    )
                )
                ORDER BY created_at
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """, now.UtcDateTime, processingCutoff.UtcDateTime)
            .ToListAsync();

        foreach (var message in claimed)
        {
            message.MarkProcessing(now, lockId);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return claimed;
    }

    private async Task<List<MessagingOutboxMessage>> ReadAllAsync()
    {
        await using var context = CreateContext();
        return await context.Set<MessagingOutboxMessage>().ToListAsync();
    }
}
