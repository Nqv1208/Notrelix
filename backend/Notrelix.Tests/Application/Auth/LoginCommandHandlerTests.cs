using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Identity.Commands.Login;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtService = new Mock<IJwtService>();

        var handler = new LoginCommandHandler(context, passwordHasher.Object, jwtService.Object);

        var result = await handler.Handle(new LoginCommand
        {
            Email = "missing@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldGenerateTokensAndUpdateLastLogin()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("login@example.com", "User", "hashed");
        // Status default is Active
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var handler = new LoginCommandHandler(context, passwordHasher.Object, jwtService.Object);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(new LoginCommand
        {
            Email = "login@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data!.RefreshToken.Should().Be("refresh-token");

        var updated = await context.Users.FirstAsync(u => u.Id == user.Id);
        updated.LastLoginAt.Should().NotBeNull();
        updated.LastLoginAt!.Value.Should().BeAfter(before);

        (await context.Sessions.ToListAsync()).Should().HaveCount(1);
    }
}
