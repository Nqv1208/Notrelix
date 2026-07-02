using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class RefreshTokenCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RefreshTokenCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_WhenSessionNotFound_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();

        var jwtService = new Mock<IJwtService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new RefreshTokenCommandHandler(context, jwtService.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new RefreshTokenCommand
        {
            RefreshToken = "missing-refresh-token"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Refresh token is invalid or expired");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldRevokeOldSessionAndIssueNewTokens()
    {
        await using var context = _db.CreateContext();

        var user = User.Create("refresh@example.com", "Refresh User", "hashed", DateTimeOffset.UtcNow);
        context.Users.Add(user);

        var oldRefreshToken = "old-refresh";
        var session = UserSession.Create(user.Id, RefreshTokenHash.Create(oldRefreshToken), DateTimeOffset.UtcNow.AddMinutes(10), DateTimeOffset.UtcNow);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("new-access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new RefreshTokenCommandHandler(context, jwtService.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new RefreshTokenCommand
        {
            RefreshToken = oldRefreshToken
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data!.RefreshToken.Should().Be("new-refresh-token");

        // Old session should be revoked
        var old = await context.Sessions.FirstAsync(s => s.RefreshTokenHash.Hash == RefreshTokenHash.Create(oldRefreshToken).Hash);
        old.Status.Should().Be(SessionStatus.Revoked);

        // New session should exist
        (await context.Sessions.ToListAsync()).Should().HaveCount(2);
        (await context.Sessions.AnyAsync(s => s.RefreshTokenHash.Hash == RefreshTokenHash.Create("new-refresh-token").Hash)).Should().BeTrue();
    }
}
