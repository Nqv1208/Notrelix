using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Queries;

public class GetCurrentUserTests : IdentityHandlerTestBase
{
    private GetCurrentUserQueryHandler CreateSut() => new(IdentityContextMock.Object, RequestContextMock.Object);

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsUserDto()
    {
        var user = CreateUser(emailConfirmed: true);
        SetupUsers(user);

        var sut = CreateSut();
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(TestUserId);
        result.Data!.Email.Should().Be(TestEmail);
        result.Data!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        SetupUsers();

        var sut = CreateSut();
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }
}
