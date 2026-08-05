using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// FZ-INF-03 — dispatcher contract matrix that cannot run through the internal
/// BackgroundService directly: retry/backoff and dead-letter transitions of the
/// outbox row, at-least-once dedupe through MessagingProcessedEvent, atomicity
/// of the completion transaction, and MVCC ordering that guarantees the
/// dispatcher never observes an event before the business transaction commits.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class OutboxDispatchContractTests : IAsyncLifetime
{
    private const string DispatcherConsumerName = "OutboxDispatcher";
    private const int MaxRetries = 5;

    private static readonly DateTimeOffset FixedTime = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ActorUserId = Guid.Parse("00000000-0000-0000-0000-0000000000A1");

    private readonly PostgresTestContainer _fixture;
    private DatabaseReset _reset = null!;

    public OutboxDispatchContractTests(PostgresTestContainer fixture)
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

    private static MessagingOutboxMessage CreateMessage(Guid eventId, DateTimeOffset now)
    {
        var message = new MessagingOutboxMessage(
            eventId: eventId,
            sourceEventId: null,
            sourceContext: "test",
            messageName: "test.dispatch.v1",
            schemaVersion: 1,
            destination: null,
            subjectType: null,
            subjectId: null,
            aggregateType: null,
            aggregateId: null,
            workspaceId: null,
            actorUserId: ActorUserId,
            correlationId: Guid.NewGuid().ToString(),
            causationId: null,
            partitionKey: null,
            payloadJson: System.Text.Json.JsonDocument.Parse("{}"),
            headersJson: null,
            metadataJson: null,
            createdAt: now);

        return message;
    }

    [Fact]
    public async Task PublisherFailure_StaysRetryable_WithBackoff_ThenDeadLetters()
    {
        var eventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().Add(CreateMessage(eventId, FixedTime));
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var message = await context.Set<MessagingOutboxMessage>().SingleAsync();

            message.MarkFailed("DispatchFailed", "broker unavailable", FixedTime.AddSeconds(1));
            message.Status.Should().Be("Failed", "a transient publish failure must not dead-letter immediately");
            message.RetryCount.Should().Be(1);
            message.NextAttemptAt.Should().Be(FixedTime.AddSeconds(3),
                "backoff doubles per attempt (2^retry), so the retry window starts in the future");

            message.MarkFailed("DispatchFailed", "broker unavailable", FixedTime.AddSeconds(2));
            message.MarkFailed("DispatchFailed", "broker unavailable", FixedTime.AddSeconds(4));
            message.MarkFailed("DispatchFailed", "broker unavailable", FixedTime.AddSeconds(8));

            message.Status.Should().Be("Failed");
            message.RetryCount.Should().Be(4);
            message.NextAttemptAt.Should().Be(FixedTime.AddSeconds(24),
                "backoff is relative to the failure time of the fourth attempt: 8s + 2^4");

            message.MarkFailed("DispatchFailed", "broker unavailable", FixedTime.AddSeconds(16));
            message.Status.Should().Be("DeadLetter", "the final retry exhausts the budget and dead-letters");

            await context.SaveChangesAsync();
        }

        await using (var verify = CreateContext())
        {
            var stored = await verify.Set<MessagingOutboxMessage>().SingleAsync();
            stored.Status.Should().Be("DeadLetter");
            stored.RetryCount.Should().Be(MaxRetries);
            stored.LastErrorCode.Should().Be("DispatchFailed");
        }
    }

    [Fact]
    public async Task FailedRetryWindow_ExpiresAndRowIsClaimableAgain()
    {
        var eventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            var message = CreateMessage(eventId, FixedTime);
            message.MarkFailed("DispatchFailed", "broker unavailable", FixedTime);
            context.Set<MessagingOutboxMessage>().Add(message);
            await context.SaveChangesAsync();
        }

        var lockId = Guid.NewGuid();
        var claimed = await ClaimAsync(FixedTime.AddSeconds(3), lockId);

        claimed.Should().ContainSingle("the retry window elapsed, so the row is claimable again");
        claimed[0].LockId.Should().Be(lockId);
        claimed[0].Status.Should().Be("Processing");
    }

    [Fact]
    public async Task AlreadySucceededEvent_Deduplicates_AndAddsNoDeliveryAttempt()
    {
        var eventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            context.Set<MessagingOutboxMessage>().Add(CreateMessage(eventId, FixedTime));
            var processed = new MessagingProcessedEvent(
                eventId, DispatcherConsumerName, "test", "test.dispatch.v1", 1,
                null, null, null, null, ActorUserId,
                Guid.NewGuid().ToString(), null, FixedTime);
            processed.MarkSucceeded(FixedTime);
            context.Set<MessagingProcessedEvent>().Add(processed);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var message = await context.Set<MessagingOutboxMessage>().SingleAsync();
            var alreadyProcessed = await context.Set<MessagingProcessedEvent>()
                .AnyAsync(x => x.EventId == message.EventId
                    && x.ConsumerName == DispatcherConsumerName
                    && x.Status == "Succeeded");

            alreadyProcessed.Should().BeTrue();

            message.MarkProcessed(FixedTime);
            await context.SaveChangesAsync();
        }

        await using (var verify = CreateContext())
        {
            var stored = await verify.Set<MessagingOutboxMessage>().SingleAsync();
            stored.Status.Should().Be("Processed");
            stored.PublishedAt.Should().Be(FixedTime);

            (await verify.Set<OutboxDeliveryAttempt>().CountAsync()).Should().Be(0,
                "an at-least-once redelivery must not create a second attempt record");
            (await verify.Set<MessagingProcessedEvent>().CountAsync()).Should().Be(1,
                "the dedupe marker stays unique per event id");
        }
    }

    [Fact]
    public async Task CompletionTransaction_IsAtomic_AllOrNothing()
    {
        var eventId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            var message = CreateMessage(eventId, FixedTime);
            message.MarkProcessing(FixedTime, Guid.NewGuid());
            context.Set<MessagingOutboxMessage>().Add(message);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var message = await context.Set<MessagingOutboxMessage>().SingleAsync();
            message.MarkProcessed(FixedTime.AddSeconds(5));

            context.Set<OutboxDeliveryAttempt>().Add(new OutboxDeliveryAttempt(
                message.Id, message.EventId, 1,
                "test-host", "MassTransit", null, "Started", FixedTime.AddSeconds(5)));

            var processed = new MessagingProcessedEvent(
                message.EventId, DispatcherConsumerName, "test", "test.dispatch.v1", 1,
                null, null, null, null, ActorUserId,
                Guid.NewGuid().ToString(), null, FixedTime.AddSeconds(5));
            processed.MarkSucceeded(FixedTime.AddSeconds(5));
            context.Set<MessagingProcessedEvent>().Add(processed);

            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using (var verify = CreateContext())
        {
            var stored = await verify.Set<MessagingOutboxMessage>().SingleAsync();
            stored.Status.Should().Be("Processing",
                "the completion transaction must be atomic: rollback keeps the row Processing, never half-published");
            (await verify.Set<OutboxDeliveryAttempt>().CountAsync()).Should().Be(0);
            (await verify.Set<MessagingProcessedEvent>().CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task UncommittedOutboxRow_IsInvisibleToOtherSessions_UntilCommit()
    {
        var now = FixedTime.AddHours(1);
        var eventId = Guid.NewGuid();
        var pendingEvent = new TestIntegrationEvent
        {
            EventId = eventId,
            MessageName = "test.postcommit.v1",
            SchemaVersion = 1,
            CorrelationId = Guid.NewGuid(),
            ActorUserId = ActorUserId,
            OccurredAt = now,
        };

        var collector = new Mock<IIntegrationEventCollector>();
        collector.Setup(x => x.CapturePending())
            .Returns(new IntegrationEventBatch(Guid.NewGuid(), [pendingEvent]));
        var interceptor = CreateInterceptor(collector, now);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();

        await using (var context = _fixture.CreateContext(tenant, interceptor))
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            context.Workspaces.Add(Workspace.Create(
                Guid.NewGuid(), ActorUserId, "PostCommit Workspace", "postcommit-workspace", now));
            await context.SaveChangesAsync();

            await using var otherSession = CreateContext();
            (await otherSession.Set<MessagingOutboxMessage>().CountAsync()).Should().Be(0,
                "an uncommitted outbox row must never be visible to the dispatcher's connection (MVCC)");

            await transaction.CommitAsync();
        }

        await using (var afterCommit = CreateContext())
        {
            (await afterCommit.Set<MessagingOutboxMessage>().CountAsync()).Should().Be(1,
                "only after the business transaction commits does the outbox row become publishable");
        }
    }

    private async Task<List<MessagingOutboxMessage>> ClaimAsync(DateTimeOffset now, Guid lockId)
    {
        await using var context = CreateContext();

        await using var transaction = await context.Database.BeginTransactionAsync();
        var claimed = await context.Set<MessagingOutboxMessage>()
            .FromSqlRaw("""
                SELECT * FROM messaging.outbox_messages
                WHERE (
                    (status = 'Pending' AND next_attempt_at <= {0})
                    OR
                    (status = 'Processing' AND processing_started_at <= {1})
                    OR
                    (status = 'Failed' AND next_attempt_at <= {0})
                )
                ORDER BY created_at
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """, now.UtcDateTime, now.AddSeconds(-60).UtcDateTime)
            .ToListAsync();

        foreach (var message in claimed)
        {
            message.MarkProcessing(now, lockId);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return claimed;
    }

    private static DomainEventInterceptor CreateInterceptor(
        Mock<IIntegrationEventCollector> collector,
        DateTimeOffset now)
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        var eventTypeRegistry = new Mock<IEventTypeRegistry>();
        eventTypeRegistry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var integrationEventMapper = new Mock<IIntegrationEventMapper>();
        integrationEventMapper.Setup(x => x.Map(It.IsAny<IDomainEvent>())).Returns([]);
        var classificationPolicy = new Mock<IClassificationPolicy>();
        classificationPolicy.Setup(x => x.GetClassification(It.IsAny<Type>()))
            .Returns(new Classification { Value = EventClassification.Business });
        var deliveryPolicy = new Mock<IDeliveryPolicy>();
        deliveryPolicy.Setup(x => x.GetDecision(It.IsAny<Type>()))
            .Returns(new DeliveryDecision { Outbox = true });

        return new DomainEventInterceptor(
            dateTimeProvider.Object, eventTypeRegistry.Object,
            classificationPolicy.Object, deliveryPolicy.Object,
            integrationEventMapper.Object, collector.Object);
    }

    private sealed class TestIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; set; }
        public string MessageName { get; set; } = "TestDispatchEvent";
        public int SchemaVersion { get; set; } = 1;
        public Guid? SourceEventId { get; set; }
        public Guid? WorkspaceId { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid? CausationId { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? AccountId { get; set; }
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
