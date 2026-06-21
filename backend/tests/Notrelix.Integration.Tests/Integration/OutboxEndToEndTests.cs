using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Integration.Tests.Integration;

public class OutboxEndToEndTests
{
    private sealed record TestIntegrationEvent : IIntegrationEvent
    {
        public Guid EventId { get; init; }
        public Guid? SourceEventId { get; init; }
        public string MessageName { get; init; } = "test.event";
        public int SchemaVersion { get; init; } = 1;
        public Guid? WorkspaceId { get; init; }
        public Guid? ActorUserId { get; init; }
        public string? CorrelationId { get; init; }
        public string? CausationId { get; init; }
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    }

    private static IntegrationEventMapping CreateMapping(string messageName = "test.event")
    {
        return new IntegrationEventMapping(new TestIntegrationEvent
        {
            EventId = Guid.CreateVersion7(),
            MessageName = messageName,
        });
    }

    [Fact]
    public async Task DomainEventFlow_WhenAggregateSaved_CreatesOutboxAndPublishesNotification()
    {
        object? publishedNotification = null;
        var mediator = CreateMediatorCapturingNotification(n => publishedNotification = n);
        var clock = MockClock(DateTimeOffset.UtcNow);
        var registry = MockEventRegistry("workspace.created");
        var mapper = MockIntegrationMapper("workspace.created");

        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, mediator.Object);

        await using var context = CreateContext(interceptor);

        var workspace = Workspace.Create(
            Guid.CreateVersion7(), "E2E Outbox Test", "e2e-outbox",
            clock.Object.UtcNow, isPersonal: true);
        context.Workspaces.Add(workspace);

        await context.SaveChangesAsync();

        publishedNotification.Should().NotBeNull("mediator should publish domain event notification");
        publishedNotification.Should().BeAssignableTo<INotification>();

        var outboxCount = await context.Set<OutboxMessage>().CountAsync();
        outboxCount.Should().Be(1, "one domain event should create one outbox message");
    }

    [Fact]
    public async Task DomainEventFlow_WhenMultipleAggregatesSaved_CreatesOneOutboxPerEvent()
    {
        object? publishedNotification = null;
        var mediator = CreateMediatorCapturingNotification(n => publishedNotification = n);
        var clock = MockClock(DateTimeOffset.UtcNow);
        var registry = MockEventRegistry("workspace.created");
        var mapper = MockIntegrationMapper("workspace.created");

        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, mediator.Object);

        await using var context = CreateContext(interceptor);

        var ws1 = Workspace.Create(Guid.CreateVersion7(), "A", "a", clock.Object.UtcNow, isPersonal: true);
        var ws2 = Workspace.Create(Guid.CreateVersion7(), "B", "b", clock.Object.UtcNow, isPersonal: true);
        context.Workspaces.AddRange(ws1, ws2);

        await context.SaveChangesAsync();

        publishedNotification.Should().NotBeNull();
        var outboxCount = await context.Set<OutboxMessage>().CountAsync();
        outboxCount.Should().Be(2, "two aggregates with events should create two outbox messages");
    }

    [Fact]
    public async Task DomainEventFlow_WhenNoDomainEvents_CreatesNoOutboxMessages()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clock = MockClock(DateTimeOffset.UtcNow);
        var registry = MockEventRegistry("test.event");
        var mapper = MockIntegrationMapper("test.event");

        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, mediator.Object);

        await using var context = CreateContext(interceptor);

        await context.SaveChangesAsync();

        var outboxCount = await context.Set<OutboxMessage>().CountAsync();
        outboxCount.Should().Be(0, "no domain events should result in no outbox messages");
    }

    private static Mock<IMediator> CreateMediatorCapturingNotification(Action<object> onPublish)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((notification, _) =>
            {
                if (notification is not INotification)
                    throw new ArgumentException("notification is not INotification", nameof(notification));
                onPublish(notification);
            })
            .Returns(Task.CompletedTask);
        return mediator;
    }

    private static Mock<IDateTimeProvider> MockClock(DateTimeOffset now)
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.Setup(x => x.UtcNow).Returns(now);
        return mock;
    }

    private static Mock<IEventTypeRegistry> MockEventRegistry(string messageName)
    {
        var mock = new Mock<IEventTypeRegistry>();
        mock.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns(messageName);
        return mock;
    }

    private static Mock<IIntegrationEventMapper> MockIntegrationMapper(string messageName)
    {
        var mock = new Mock<IIntegrationEventMapper>();
        mock.Setup(x => x.Map(It.IsAny<IDomainEvent>()))
            .Returns([CreateMapping(messageName)]);
        return mock;
    }

    private static ApplicationDbContext CreateContext(DomainEventInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-outbox-e2e-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        return new TestApplicationDbContext(options);
    }

    private sealed class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
        }
    }
}
