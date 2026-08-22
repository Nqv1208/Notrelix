using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public class UpdateProfileTests : IdentityHandlerTestBase
{
    private UpdateProfileCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesProfile()
    {
        var user = CreateUser();
        SetupUsers(user);

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateProfileCommand
        {
            Name = "Updated Name",
            Avatar = "https://example.com/avatar.png"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("Updated Name");
        result.Data!.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        SetupUsers();

        var sut = CreateSut();
        var result = await sut.Handle(new UpdateProfileCommand
        {
            Name = "Updated Name"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }
}
