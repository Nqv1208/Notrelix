using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Commands.Logout;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class LogoutCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public LogoutCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_WhenSessionExists_ShouldRevokeRefreshToken()
    {
        await using var context = _db.CreateContext();

        var user = User.Create("logout@example.com", "Logout User", "hashed", DateTimeOffset.UtcNow);
        context.Users.Add(user);

        var refreshToken = "logout-token";
        var session = UserSession.Create(user.Id, RefreshTokenHash.Create(refreshToken), DateTimeOffset.UtcNow.AddMinutes(10), DateTimeOffset.UtcNow);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var jwtBlacklist = new Mock<IJwtBlacklistService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new LogoutCommandHandler(context, jwtBlacklist.Object, dateTimeProvider.Object);

        await handler.Handle(new LogoutCommand
        {
            RefreshToken = refreshToken
        }, CancellationToken.None);

        var updated = await context.Sessions.FirstAsync(s => s.RefreshTokenHash.Hash == RefreshTokenHash.Create(refreshToken).Hash);
        updated.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task Handle_WhenSessionDoesNotExist_ShouldStillSucceed()
    {
        await using var context = _db.CreateContext();

        var jwtBlacklist = new Mock<IJwtBlacklistService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var handler = new LogoutCommandHandler(context, jwtBlacklist.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new LogoutCommand
        {
            RefreshToken = "missing-token"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
