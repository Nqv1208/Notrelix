using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Identity.Commands.Register;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Tests.Application.Auth;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailExists_ShouldReturnFailure()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var existing = User.Create("test@example.com", "Old", "hash");
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtService = new Mock<IJwtService>();

        var handler = new RegisterCommandHandler(context, passwordHasher.Object, jwtService.Object);

        var result = await handler.Handle(new RegisterCommand
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email is already in use");

        passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
        jwtService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        jwtService.Verify(x => x.GenerateRefreshToken(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateUserWorkspaceAndSession()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var handler = new RegisterCommandHandler(context, passwordHasher.Object, jwtService.Object);

        var now = DateTime.UtcNow;
        var result = await handler.Handle(new RegisterCommand
        {
            Email = "test2@example.com",
            Password = "Password1!",
            Name = "New Name"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data!.RefreshToken.Should().Be("refresh-token");
        result.Data!.ExpiresAt.Should().BeAfter(now);

        (await context.Users.ToListAsync()).Should().HaveCount(1);
        (await context.Workspaces.ToListAsync()).Should().HaveCount(1);
        (await context.Sessions.ToListAsync()).Should().HaveCount(1);
    }
}
