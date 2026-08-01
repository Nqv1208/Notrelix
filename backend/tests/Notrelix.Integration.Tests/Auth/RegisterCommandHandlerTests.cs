using MediatR;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Testing.Application.Fakes;
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
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_WhenEmailExists_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();

        var existing = User.Create("test@example.com", "Old", "hash", DateTimeOffset.UtcNow);
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");
        var sessionIssuer = new Mock<IAuthSessionIssuer>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();

        var handler = new RegisterCommandHandler(context, context, passwordHasher.Object, sessionIssuer.Object, dateTimeProvider.Object, integrationEventCollector.Object);

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email is already in use");

        passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
        sessionIssuer.Verify(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateUserAndSession()
    {
        await using var context = _db.CreateContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var sessionIssuer = new AuthSessionIssuer(jwtService.Object, context, dateTimeProvider.Object);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();
        var handler = new RegisterCommandHandler(context, context, passwordHasher.Object, sessionIssuer, dateTimeProvider.Object, integrationEventCollector.Object);

        var now = DateTime.UtcNow;
        var result = await handler.Handle(new RegisterCommand
        {
            Email = "test2@example.com",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data!.RefreshToken.Should().Be("refresh-token");
        result.Data!.ExpiresAt.Should().BeAfter(now);
        result.Data!.WorkspaceProvisioning.Should().Be("pending");

        (await context.Users.ToListAsync()).Should().HaveCount(1);
        (await context.Sessions.ToListAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenValidAndDomainEventInterceptorEnabled_ShouldCreateUserAndSession()
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
        await using var context = _db.CreateContext(tenant, interceptor);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var sessionIssuer = new AuthSessionIssuer(jwtService.Object, context, dateTimeProvider.Object);
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new RegisterCommandHandler(context, context, passwordHasher.Object, sessionIssuer, dateTimeProvider.Object, integrationEventCollector.Object);

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "interceptor@example.com",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue($"Handle Succeeded=false. Errors: {string.Join(", ", result.Errors)}");
        var authResult = result.Data;
        authResult.Should().NotBeNull("result.Data is null even though Succeeded=true");
        authResult!.WorkspaceProvisioning.Should().Be("pending");

        await context.SaveChangesAsync();

        (await context.Users.CountAsync()).Should().Be(1);
        (await context.Sessions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldNotCreateWorkspaceSynchronously()
    {
        await using var context = _db.CreateContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var sessionIssuer = new AuthSessionIssuer(jwtService.Object, context, dateTimeProvider.Object);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();
        var handler = new RegisterCommandHandler(context, context, passwordHasher.Object, sessionIssuer, dateTimeProvider.Object, integrationEventCollector.Object);

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
