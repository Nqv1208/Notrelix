using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Commands.Login;
using Notrelix.Domain.Identity.Users;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class LoginCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public LoginCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();

        var passwordHasher = new Mock<IPasswordHasher>();
        var sessionIssuer = new Mock<IAuthSessionIssuer>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);

        var handler = new LoginCommandHandler(context, passwordHasher.Object, sessionIssuer.Object, dateTimeProvider.Object, NullLogger<LoginCommandHandler>.Instance);

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
        await using var context = _db.CreateContext();

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

        var sessionIssuer = new AuthSessionIssuer(jwtService.Object, context, dateTimeProvider.Object);
        var handler = new LoginCommandHandler(context, passwordHasher.Object, sessionIssuer, dateTimeProvider.Object, NullLogger<LoginCommandHandler>.Instance);

        var before = DateTimeOffset.UtcNow;
        var result = await handler.Handle(new LoginCommand
        {
            Email = "login@example.com",
            Password = "Password1!"
        }, CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data!.RefreshToken.Should().Be("refresh-token");

        var updated = await context.Users.FirstAsync(u => u.Id == user.Id);
        updated.LastLoginAt.Should().NotBeNull();
        updated.LastLoginAt!.Value.Should().BeAfter(before);

        (await context.Sessions.ToListAsync()).Should().HaveCount(1);
    }
}
