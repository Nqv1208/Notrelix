using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Domain.Identity.Users;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Auth;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtService = new Mock<IJwtService>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new LoginCommandHandler(context, passwordHasher.Object, jwtService.Object, dateTimeProvider.Object, NullLogger<LoginCommandHandler>.Instance);

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
        using var context = TestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("login@example.com", "User", "hashed", DateTimeOffset.UtcNow);
        // Status default is Active
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new LoginCommandHandler(context, passwordHasher.Object, jwtService.Object, dateTimeProvider.Object, NullLogger<LoginCommandHandler>.Instance);

        var before = DateTimeOffset.UtcNow;
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
