using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Security.Auth;
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
    public async Task Handle_WhenSessionUserIsGone_FailsAsAuthenticationSoTheApiAnswers401()
    {
        SetupUsers();

        var sut = CreateSut();
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.auth.session-invalid" && e.Type == ApplicationErrorType.Authentication);
    }

    [Fact]
    public async Task Handle_WhenOtherPeopleExist_ReturnsOnlyTheCaller()
    {
        var caller = CreateUser();
        var other = CreateUser(email: "other@example.com", name: "Other", id: Guid.CreateVersion7());
        SetupUsers(other, caller);

        var sut = CreateSut();
        var result = await sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Data!.Id.Should().Be(TestUserId);
        result.Data!.Email.Should().Be(TestEmail);
        result.Data!.Name.Should().NotBe("Other");
    }

    [Fact]
    public void Answer_CarriesOnlyTheCallersIdentityFacts_NoWorkspaceRoleOrEntitlement()
    {
        typeof(UserDto).GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo("Id", "Email", "Name", "AvatarUrl", "EmailConfirmed");
    }
}
