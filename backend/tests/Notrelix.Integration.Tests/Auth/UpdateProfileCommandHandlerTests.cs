using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// TAC-IA-FLOW-01 evidence. UpdateProfile is a strictly Identity-local flow:
/// authenticated current user -> Identity handler -> Identity persistence ->
/// User.UpdateProfile -> request transaction commit. No Accounts/Workspaces/
/// Governance mutation dependency is introduced.
/// </summary>
[Collection("Database")]
public class UpdateProfileCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public UpdateProfileCommandHandlerTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private (EfRequestDataSession Session, ApplicationDbContext Context) Create()
    {
        var tenant = SystemTenant();
        var context = _db.CreateContext(tenant);
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), tenant),
            NullLogger<EfRequestDataSession>.Instance);
        return (session, context);
    }

    [Fact]
    public async Task Handle_WhenUserExists_CommitsIdentityProfileMutation()
    {
        var (session, context) = Create();
        var now = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero);

        var user = User.Create(
            "avatar@example.com",
            "Old Name",
            "hashed",
            now.AddMinutes(-5),
            hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        var currentUser = new FakeCurrentRequestContext().AsUser(user.Id);
        var handler = new UpdateProfileCommandHandler(
            context,
            currentUser,
            dateTimeProvider.Object);

        var result = await session.ExecuteAsync(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            ct => handler.Handle(new UpdateProfileCommand
            {
                Name = "New Name",
                Avatar = "https://example.com/avatar.png"
            }, ct),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
        result.Data.AvatarUrl.Should().Be("https://example.com/avatar.png");

        await using var verify = _db.CreateContext(SystemTenant());
        var persisted = await verify.Users.SingleAsync(u => u.Id == user.Id);
        persisted.Name.Should().Be("New Name");
        persisted.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailureWithoutCreatingIdentityState()
    {
        var (session, context) = Create();
        var missingUserId = Guid.CreateVersion7();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
        var currentUser = new FakeCurrentRequestContext().AsUser(missingUserId);
        var handler = new UpdateProfileCommandHandler(
            context,
            currentUser,
            dateTimeProvider.Object);

        var result = await session.ExecuteAsync(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            ct => handler.Handle(new UpdateProfileCommand
            {
                Name = "New Name",
                Avatar = null
            }, ct),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenLaterRequestStepFails_RollsBackProfileMutation()
    {
        var (session, context) = Create();
        var now = new DateTimeOffset(2026, 9, 4, 8, 30, 0, TimeSpan.Zero);

        var user = User.Create(
            "rollback-profile@example.com",
            "Before",
            "hashed",
            now.AddMinutes(-10),
            hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        var currentUser = new FakeCurrentRequestContext().AsUser(user.Id);
        var handler = new UpdateProfileCommandHandler(
            context,
            currentUser,
            dateTimeProvider.Object);

        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var result = await handler.Handle(new UpdateProfileCommand
                {
                    Name = "After",
                    Avatar = "https://example.com/after.png"
                }, ct);
                result.Succeeded.Should().BeTrue();

                throw new InvalidOperationException("later request failure");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var verify = _db.CreateContext(SystemTenant());
        var persisted = await verify.Users.SingleAsync(u => u.Id == user.Id);
        persisted.Name.Should().Be("Before");
        persisted.AvatarUrl.Should().BeNull();
    }
}