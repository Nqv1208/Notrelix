using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// FZ-INF-03 — outbox failure matrix: business state and the outbox row are
/// written by the DomainEventInterceptor inside the same SaveChanges, so they
/// commit and roll back together on PostgreSQL.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class OutboxAtomicityTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid ActorUserId = Guid.Parse("00000000-0000-0000-0000-0000000000A1");

    private readonly PostgresTestContainer _fixture;
    private DatabaseReset _reset = null!;

    public OutboxAtomicityTests(PostgresTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_fixture.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

    private static TestIntegrationEvent CreatePendingEvent() => new()
    {
        EventId = Guid.NewGuid(),
        MessageName = "test.atomicity.v1",
        SchemaVersion = 1,
        CorrelationId = Guid.NewGuid(),
        ActorUserId = ActorUserId,
        OccurredAt = FixedTime,
    };

    [Fact]
    public async Task BusinessRowAndOutboxRow_CommitInOneSaveChanges()
    {
        var now = FixedTime.AddHours(1);
        var pendingEvent = CreatePendingEvent();
        var collector = new Mock<IIntegrationEventCollector>();
        collector.Setup(x => x.CapturePending())
            .Returns(new IntegrationEventBatch(Guid.NewGuid(), [pendingEvent]));

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _fixture.CreateContext(tenant, CreateInterceptor(collector, now));

        context.Workspaces.Add(Workspace.Create(
            Guid.NewGuid(), ActorUserId, "Atomic Workspace", "atomic-workspace", now));
        await context.SaveChangesAsync();

        (await context.Workspaces.CountAsync()).Should().Be(1);
        var outbox = await context.Set<MessagingOutboxMessage>().ToListAsync();
        outbox.Should().ContainSingle("the interceptor writes the outbox row in the same SaveChanges");
        outbox[0].MessageName.Should().Be("test.atomicity.v1");
        outbox[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Rollback_RemovesBusinessAndOutboxRowsTogether()
    {
        var now = FixedTime.AddHours(1);
        var pendingEvent = CreatePendingEvent();
        var collector = new Mock<IIntegrationEventCollector>();
        collector.Setup(x => x.CapturePending())
            .Returns(new IntegrationEventBatch(Guid.NewGuid(), [pendingEvent]));

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _fixture.CreateContext(tenant, CreateInterceptor(collector, now));

        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Workspaces.Add(Workspace.Create(
            Guid.NewGuid(), ActorUserId, "Rollback Workspace", "rollback-workspace", now));
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        (await context.Workspaces.CountAsync()).Should().Be(0);
        (await context.Set<MessagingOutboxMessage>().CountAsync()).Should().Be(0,
            "rolling back the transaction must remove the outbox row with the business row");
    }

    [Fact]
    public async Task FailedSaveChanges_RollsBackBoth_AndRestoresRetryIntent()
    {
        var now = FixedTime.AddHours(1);
        var duplicateEvent = CreatePendingEvent();
        var collector = new Mock<IIntegrationEventCollector>();
        collector.Setup(x => x.CapturePending())
            .Returns(new IntegrationEventBatch(Guid.NewGuid(), [duplicateEvent, duplicateEvent]));

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _fixture.CreateContext(tenant, CreateInterceptor(collector, now));

        context.Workspaces.Add(Workspace.Create(
            Guid.NewGuid(), ActorUserId, "Failing Workspace", "failing-workspace", now));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>(
            "duplicate outbox event ids violate the unique constraint, simulating a mid-commit failure");

        (await context.Workspaces.CountAsync()).Should().Be(0);
        (await context.Set<MessagingOutboxMessage>().CountAsync()).Should().Be(0);

        collector.Verify(x => x.Restore(It.IsAny<IntegrationEventBatch>()), Times.Once,
            "a failed SaveChanges must restore the pending integration events so a retry keeps the intent");
    }

    private sealed class TestIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; set; }
        public string MessageName { get; set; } = "TestAtomicityEvent";
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
