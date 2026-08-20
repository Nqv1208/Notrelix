using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;
using Notrelix.Domain.Identity.Users;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class UpdateProfileCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public UpdateProfileCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_WhenUserExists_ShouldUpdateNameAndAvatar()
    {
        await using var context = _db.CreateContext();

        var user = User.Create("avatar@example.com", "Old Name", "hashed", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var handler = new UpdateProfileCommandHandler(context, dateTimeProvider.Object);

        var result = await handler.Handle(new UpdateProfileCommand
        {
            UserId = user.Id,
            Name = "New Name",
            Avatar = "https://example.com/avatar.png"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("New Name");
        result.Data!.AvatarUrl.Should().Be("https://example.com/avatar.png");

        var updated = await context.Users.FirstAsync(u => u.Id == user.Id);
        updated.Name.Should().Be("New Name");
        updated.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        await using var context = _db.CreateContext();

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var handler = new UpdateProfileCommandHandler(context, dateTimeProvider.Object);

        var result = await handler.Handle(new UpdateProfileCommand
        {
            UserId = Guid.NewGuid(),
            Name = "New Name",
            Avatar = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found");
    }
}
