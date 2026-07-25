using Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Queries;

public class GetBootstrapTests : IdentityHandlerTestBase
{
    private GetBootstrapQueryHandler CreateSut() => new(
        IdentityContextMock.Object,
        AccountContextMock.Object,
        WorkspaceContextMock.Object);

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsBootstrapData()
    {
        var user = CreateUser(emailConfirmed: true);
        SetupUsers(user);

        var sut = CreateSut();
        var result = await sut.Handle(new GetBootstrapQuery(TestUserId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.User.Id.Should().Be(TestUserId);
        result.Data!.User.Email.Should().Be(TestEmail);
        result.Data!.Workspaces.Should().BeEmpty();
        result.Data!.PersonalWorkspace.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        SetupUsers();

        var sut = CreateSut();
        var result = await sut.Handle(new GetBootstrapQuery(TestUserId), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }
}
