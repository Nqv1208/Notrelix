using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Identity.Commands.Logout;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Tests.Application.Auth;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionExists_ShouldRevokeRefreshToken()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("logout@example.com", "Logout User", "hashed");
        context.Users.Add(user);

        var refreshToken = "logout-token";
        var session = Session.Create(user.Id, refreshToken, DateTime.UtcNow.AddMinutes(10));
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var jwtBlacklist = new Mock<IJwtBlacklistService>();
        var handler = new LogoutCommandHandler(context, jwtBlacklist.Object);

        await handler.Handle(new LogoutCommand
        {
            RefreshToken = refreshToken
        }, CancellationToken.None);

        var updated = await context.Sessions.FirstAsync(s => s.RefreshToken == refreshToken);
        updated.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSessionDoesNotExist_ShouldStillSucceed()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var jwtBlacklist = new Mock<IJwtBlacklistService>();
        var handler = new LogoutCommandHandler(context, jwtBlacklist.Object);

        var result = await handler.Handle(new LogoutCommand
        {
            RefreshToken = "missing-token"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
