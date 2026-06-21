using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Commands.Logout;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Auth;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionExists_ShouldRevokeRefreshToken()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();

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

        var updated = await context.Sessions.FirstAsync(s => s.RefreshTokenHash == RefreshTokenHash.Create(refreshToken));
        updated.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task Handle_WhenSessionDoesNotExist_ShouldStillSucceed()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();

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
