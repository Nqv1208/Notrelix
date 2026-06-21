using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Auth;

public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateNameAndAvatar()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("avatar@example.com", "Old Name", "hashed", DateTimeOffset.UtcNow);
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
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

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
