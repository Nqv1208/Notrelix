using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Identity.Commands.UpdateProfile;
using Notrelix.Domain.Entities.Identity;

namespace Notrelix.Tests.Auth;

public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateNameAndAvatar()
    {
        using var context = AuthTestDbContextFactory.CreateInMemoryContext();

        var user = User.Create("avatar@example.com", "Old Name", "hashed");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateProfileCommandHandler(context);

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

        var handler = new UpdateProfileCommandHandler(context);

        var result = await handler.Handle(new UpdateProfileCommand
        {
            UserId = Guid.NewGuid(),
            Name = "New Name",
            Avatar = null
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Không tìm thấy người dùng");
    }
}

