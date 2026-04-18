using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Auth.Commands.Logout;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Tests.Auth;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionExists_ShouldRevokeRefreshToken()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var user = Notrelix.Domain.Entities.User.Create("logout@example.com", "Logout User", "hashed");
        context.Users.Add(user);

        var refreshToken = "logout-token";
        var session = Notrelix.Domain.Entities.Session.Create(user.Id, refreshToken, DateTime.UtcNow.AddMinutes(10));
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new LogoutCommandHandler(context);

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

        var handler = new LogoutCommandHandler(context);

        var result = await handler.Handle(new LogoutCommand
        {
            RefreshToken = "missing-token"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}

