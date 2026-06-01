using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Identity.Commands.RefreshToken;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Tests.Application.Auth;

public class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldReturnFailure()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var jwtService = new Mock<IJwtService>();
        var handler = new RefreshTokenCommandHandler(context, jwtService.Object);

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
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("refresh@example.com", "Refresh User", "hashed");
        context.Users.Add(user);

        var oldRefreshToken = "old-refresh";
        var session = Session.Create(user.Id, oldRefreshToken, DateTime.UtcNow.AddMinutes(10));
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("new-access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");

        var handler = new RefreshTokenCommandHandler(context, jwtService.Object);

        var result = await handler.Handle(new RefreshTokenCommand
        {
            RefreshToken = oldRefreshToken
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data!.RefreshToken.Should().Be("new-refresh-token");

        // Old session should be revoked
        var old = await context.Sessions.FirstAsync(s => s.RefreshToken == oldRefreshToken);
        old.IsRevoked.Should().BeTrue();

        // New session should exist
        (await context.Sessions.ToListAsync()).Should().HaveCount(2);
        (await context.Sessions.AnyAsync(s => s.RefreshToken == "new-refresh-token")).Should().BeTrue();
    }
}
