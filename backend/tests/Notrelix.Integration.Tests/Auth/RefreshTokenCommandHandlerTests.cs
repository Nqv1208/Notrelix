using Notrelix.Application.Features.Identity.Auth.Commands.RefreshToken;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Security.Abstractions;
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
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static RefreshTokenCommandHandler CreateHandler(IIdentityDbContext context, IJwtService jwtService, IDateTimeProvider dateTimeProvider)
    {
        var ledger = new Mock<IRefreshRotationLedger>();
        ledger.Setup(l => l.TryClaimAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return new RefreshTokenCommandHandler(
            context, jwtService, new Mock<IJwtBlacklistService>().Object, ledger.Object,
            new Mock<ISecurityActivityRecorder>().Object, dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldReturnFailure()
    {
        await using var context = _db.CreateOwnerContext();

        var jwtService = new Mock<IJwtService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = CreateHandler(context, jwtService.Object, dateTimeProvider.Object);

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
        await using var context = _db.CreateOwnerContext();

        var user = User.Create("refresh@example.com", "Refresh User", "hashed", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        context.Users.Add(user);

        var oldRefreshToken = "old-refresh";
        var session = UserSession.Create(user.Id, RefreshTokenHash.Create(oldRefreshToken), DateTimeOffset.UtcNow.AddMinutes(10), DateTimeOffset.UtcNow);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid?>())).Returns("new-access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = CreateHandler(context, jwtService.Object, dateTimeProvider.Object);

        var result = await handler.Handle(new RefreshTokenCommand
        {
            RefreshToken = oldRefreshToken
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data!.RefreshToken.Should().Be("new-refresh-token");

        // The session keeps its id and stays active; its credential is replaced and the old one is remembered.
        var rotated = await context.Sessions.SingleAsync();
        rotated.Id.Should().Be(session.Id);
        rotated.Status.Should().Be(SessionStatus.Active);
        rotated.RefreshTokenHash.Hash.Should().Be(RefreshTokenHash.Create("new-refresh-token").Hash);
        rotated.PreviousRefreshTokenHash.Should().Be(RefreshTokenHash.Create(oldRefreshToken).Hash);
    }
}
