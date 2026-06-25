using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Workspaces.Events;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Outbox;

namespace Notrelix.Infrastructure.Tests.Outbox;

public class OutboxMessageCreationTests
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

    private static Mock<IDomainEventDispatchPolicy> CreateOutboxDispatchPolicy()
    {
        var policy = new Mock<IDomainEventDispatchPolicy>();
        policy.Setup(x => x.GetMode(It.IsAny<Type>())).Returns(DomainEventDispatchMode.Outbox);
        return policy;
    }

    [Fact]
    public async Task SaveChanges_WhenEntityHasDomainEvent_CreatesOutboxMessage()
    {
        var mapper = new Mock<IIntegrationEventMapper>();
        mapper.Setup(x => x.Map(It.IsAny<IDomainEvent>()))
            .Returns([CreateMapping()]);
        var registry = new Mock<IEventTypeRegistry>();
        registry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var dispatchPolicy = CreateOutboxDispatchPolicy();
        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, Mock.Of<IMediator>(), dispatchPolicy.Object);
        var options = BuildOptions(interceptor);
        await using var ctx = new TestApplicationDbContext(options);

        var workspace = Workspace.Create(
            Guid.CreateVersion7(), "Test", "test", DateTimeOffset.UtcNow, isPersonal: true);
        ctx.Workspaces.Add(workspace);

        await ctx.SaveChangesAsync();

        var outboxCount = await ctx.Set<OutboxMessage>().CountAsync();
        outboxCount.Should().Be(2); // 1 domain event + 1 integration event
    }

    [Fact]
    public async Task SaveChanges_WhenMultipleAggregatesHaveEvents_CreatesOutboxMessagePerAggregate()
    {
        var mapper = new Mock<IIntegrationEventMapper>();
        mapper.Setup(x => x.Map(It.IsAny<IDomainEvent>()))
            .Returns([CreateMapping()]);
        var registry = new Mock<IEventTypeRegistry>();
        registry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var dispatchPolicy = CreateOutboxDispatchPolicy();
        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, Mock.Of<IMediator>(), dispatchPolicy.Object);
        var options = BuildOptions(interceptor);
        await using var ctx = new TestApplicationDbContext(options);

        var ws1 = Workspace.Create(Guid.CreateVersion7(), "A", "a", DateTimeOffset.UtcNow, isPersonal: true);
        var ws2 = Workspace.Create(Guid.CreateVersion7(), "B", "b", DateTimeOffset.UtcNow, isPersonal: true);
        ctx.Workspaces.AddRange(ws1, ws2);

        await ctx.SaveChangesAsync();

        var outboxCount = await ctx.Set<OutboxMessage>().CountAsync();
        outboxCount.Should().Be(4); // 2 domain events + 2 integration events
    }

    [Fact]
    public async Task SaveChanges_WhenNoDomainEvents_DoesNotCreateOutboxMessage()
    {
        var mapper = new Mock<IIntegrationEventMapper>();
        mapper.Setup(x => x.Map(It.IsAny<IDomainEvent>())).Returns([]);
        var registry = new Mock<IEventTypeRegistry>();
        registry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var dispatchPolicy = CreateOutboxDispatchPolicy();
        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, Mock.Of<IMediator>(), dispatchPolicy.Object);
        var options = BuildOptions(interceptor);
        await using var ctx = new TestApplicationDbContext(options);

        await ctx.SaveChangesAsync();

        var outboxCount = await ctx.Set<OutboxMessage>().CountAsync();
        outboxCount.Should().Be(0);
    }

    [Fact]
    public async Task OutboxMessage_WhenCreated_HasCorrectMessageName()
    {
        var mapper = new Mock<IIntegrationEventMapper>();
        mapper.Setup(x => x.Map(It.IsAny<IDomainEvent>()))
            .Returns([CreateMapping("workspace.created.v1")]);
        var registry = new Mock<IEventTypeRegistry>();
        registry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var dispatchPolicy = CreateOutboxDispatchPolicy();
        var interceptor = new DomainEventInterceptor(
            clock.Object, registry.Object, mapper.Object, Mock.Of<IMediator>(), dispatchPolicy.Object);
        var options = BuildOptions(interceptor);
        await using var ctx = new TestApplicationDbContext(options);

        var workspace = Workspace.Create(
            Guid.CreateVersion7(), "Test", "test", DateTimeOffset.UtcNow, isPersonal: true);
        ctx.Workspaces.Add(workspace);

        await ctx.SaveChangesAsync();

        var message = await ctx.Set<OutboxMessage>()
            .FirstAsync(m => m.MessageType == OutboxMessageType.IntegrationEvent);
        message.MessageName.Should().Be("workspace.created.v1");
    }

    private static DbContextOptions<ApplicationDbContext> BuildOptions(DomainEventInterceptor interceptor)
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-outbox-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
    }

    private class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CustomRole>().Ignore(x => x.Permissions);
        }
    }
}
