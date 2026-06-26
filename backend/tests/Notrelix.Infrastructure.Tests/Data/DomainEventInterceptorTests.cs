using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance.Roles;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;

namespace Notrelix.Infrastructure.Tests.Data;

public class DomainEventInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenEntityHasInlineDomainEvent_ShouldPublishMediatRNotification()
    {
        object? publishedNotification = null;
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
        var eventTypeRegistry = new Mock<IEventTypeRegistry>();
        eventTypeRegistry.Setup(x => x.GetMessageName(It.IsAny<Type>())).Returns("test.event");
        var integrationEventMapper = new Mock<IIntegrationEventMapper>();
        integrationEventMapper.Setup(x => x.Map(It.IsAny<IDomainEvent>())).Returns([]);
        var mediator = CreateMediatorRejectingNonNotifications(notification => publishedNotification = notification);
        var dispatchPolicy = new Mock<IDomainEventDispatchPolicy>();
        dispatchPolicy.Setup(x => x.GetMode(typeof(WorkspaceCreatedDomainEvent)))
            .Returns(DomainEventDispatchMode.Inline);
        var interceptor = new DomainEventInterceptor(
            dateTimeProvider.Object, eventTypeRegistry.Object, integrationEventMapper.Object, mediator.Object, dispatchPolicy.Object);
        await using var context = CreateContext(interceptor);

        var workspace = Workspace.Create(
            Guid.CreateVersion7(), "Personal Workspace", "personal-workspace",
            DateTimeOffset.UtcNow, isPersonal: true);
        context.Workspaces.Add(workspace);

        await context.SaveChangesAsync();

        publishedNotification.Should().NotBeNull();
        publishedNotification.Should().BeAssignableTo<INotification>();
        publishedNotification!.GetType().Name.Should().StartWith("DomainEventNotification");
        publishedNotification.GetType().GenericTypeArguments.Should().ContainSingle()
            .Which.Should().Be(typeof(WorkspaceCreatedDomainEvent));
    }

    private static Mock<IMediator> CreateMediatorRejectingNonNotifications(Action<object> onPublish)
    {
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((notification, _) =>
            {
                if (notification is not INotification)
                {
                    throw new ArgumentException("notification does not implement INotification", nameof(notification));
                }

                onPublish(notification);
            })
            .Returns(Task.CompletedTask);

        return mediator;
    }

    private static ApplicationDbContext CreateContext(DomainEventInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notrelix-domain-events-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        return new TestApplicationDbContext(options);
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
