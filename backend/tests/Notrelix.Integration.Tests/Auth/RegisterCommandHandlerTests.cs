using MediatR;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Common.RateLimiting;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class RegisterCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RegisterCommandHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_WhenEmailExists_ShouldReturnTheSameSuccessAndCreateNothing()
    {
        await using var context = _db.CreateOwnerContext();

        var existing = User.Create("test@example.com", "Old", "hash", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();

        var handler = new RegisterCommandHandler(context, new AccountProvisioningService(context, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))), passwordHasher.Object, dateTimeProvider.Object, integrationEventCollector.Object, Mock.Of<IRateLimitService>());

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);

        // The same neutral success as a new address; the owner is mailed and nothing is created.
        result.Succeeded.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        await context.SaveChangesAsync();

        (await context.Users.CountAsync()).Should().Be(1);
        (await context.Sessions.CountAsync()).Should().Be(0);
        // Only the notice for the owner is raised; its mail is queued after the commit.
        integrationEventCollector.Verify(
            c => c.Add(It.Is<IIntegrationEvent>(e => e is RegistrationAttemptedOnExistingAccountIntegrationEventV1)),
            Times.Once);
        integrationEventCollector.Verify(c => c.Add(It.IsAny<IIntegrationEvent>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateTheUserAndNoSession()
    {
        await using var context = _db.CreateOwnerContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");


        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();
        var handler = new RegisterCommandHandler(context, new AccountProvisioningService(context, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))), passwordHasher.Object, dateTimeProvider.Object, integrationEventCollector.Object, Mock.Of<IRateLimitService>());

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "test2@example.com",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();

        (await context.Users.ToListAsync()).Should().HaveCount(1);
        // Registering opens no session: the person confirms the email and signs in.
        (await context.Sessions.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenValidAndDomainEventInterceptorEnabled_ShouldCreateTheUserAndNoSession()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var eventTypeRegistry = new Mock<IEventTypeRegistry>();
        eventTypeRegistry
            .Setup(x => x.GetMessageName(It.IsAny<Type>()))
            .Returns("test.event");
        var integrationEventMapper = new Mock<IIntegrationEventMapper>();
        integrationEventMapper
            .Setup(x => x.Map(It.IsAny<IDomainEvent>()))
            .Returns(Array.Empty<IntegrationEventMapping>());
        var classificationPolicy = new Mock<IClassificationPolicy>();
        classificationPolicy.Setup(x => x.GetClassification(It.IsAny<Type>()))
            .Returns(new Classification { Value = EventClassification.Business });
        var deliveryPolicy = new Mock<IDeliveryPolicy>();
        deliveryPolicy.Setup(x => x.GetDecision(It.IsAny<Type>()))
            .Returns(new DeliveryDecision { Outbox = true });
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();
        integrationEventCollector.Setup(x => x.CapturePending())
            .Returns(new IntegrationEventBatch(Guid.NewGuid(), []));
        var interceptor = new DomainEventInterceptor(dateTimeProvider.Object, eventTypeRegistry.Object, classificationPolicy.Object, deliveryPolicy.Object, integrationEventMapper.Object, integrationEventCollector.Object);
        await using var context = _db.CreateOwnerContext(tenant, interceptor);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");


        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new RegisterCommandHandler(context, new AccountProvisioningService(context, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))), passwordHasher.Object, dateTimeProvider.Object, integrationEventCollector.Object, Mock.Of<IRateLimitService>());

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "interceptor@example.com",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue($"Handle Succeeded=false. Errors: {string.Join(", ", result.Errors)}");
        await context.SaveChangesAsync();

        (await context.Users.CountAsync()).Should().Be(1);
        (await context.Sessions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldNotCreateWorkspaceSynchronously()
    {
        await using var context = _db.CreateOwnerContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");


        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();
        var handler = new RegisterCommandHandler(context, new AccountProvisioningService(context, new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))), passwordHasher.Object, dateTimeProvider.Object, integrationEventCollector.Object, Mock.Of<IRateLimitService>());

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "noworkspace@example.com",
            Password = "Password1!",
            Name = "No Workspace"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();

        (await context.Workspaces.ToListAsync()).Should().BeEmpty();
    }

    private static Mock<IMediator> CreateMediatorRejectingNonNotifications()
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
            })
            .Returns(Task.CompletedTask);

        return mediator;
    }
}
