using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.Workspaces.Workspaces.Events;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Interceptors;

namespace Notrelix.Infrastructure.Tests.Data;

public class DomainEventInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenEntityHasDomainEvent_ShouldPublishMediatRNotificationWrapper()
    {
        object? publishedNotification = null;
        var mediator = CreateMediatorRejectingNonNotifications(notification => publishedNotification = notification);
        var interceptor = new DomainEventInterceptor(mediator.Object);
        await using var context = CreateContext(interceptor);

        var workspace = Workspace.CreatePersonal("Personal Workspace", Guid.CreateVersion7());
        context.Workspaces.Add(workspace);

        await context.SaveChangesAsync();

        publishedNotification.Should().NotBeNull();
        publishedNotification.Should().BeAssignableTo<INotification>();
        publishedNotification!.GetType().Name.Should().StartWith("DomainEventNotification");
        publishedNotification.GetType().GenericTypeArguments.Should().ContainSingle()
            .Which.Should().Be(typeof(WorkspaceCreatedEvent));
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

        return new ApplicationDbContext(options);
    }
}
