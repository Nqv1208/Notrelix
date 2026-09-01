using Notrelix.Application.Features.Identity.Ports.Bootstrap;
using Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Queries;

public class GetBootstrapTests : IdentityHandlerTestBase
{
    private GetBootstrapQueryHandler CreateSut(Mock<IIdentityBootstrapReadPort> port)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(TestUserId);
        currentUser.Setup(u => u.IsAuthenticated).Returns(true);

        return new GetBootstrapQueryHandler(port.Object, currentUser.Object);
    }

    private IdentityBootstrapProjection CreateProjection(
        Guid? personalWorkspaceId = null,
        params BootstrapWorkspaceProjection[] workspaces)
    {
        return new IdentityBootstrapProjection(
            new BootstrapUserProjection(TestUserId, "test@example.com", "Test User", null, true),
            workspaces,
            personalWorkspaceId);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsBootstrapData()
    {
        var port = new Mock<IIdentityBootstrapReadPort>();
        port.Setup(p => p.GetAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProjection(personalWorkspaceId: null));

        var sut = CreateSut(port);
        var result = await sut.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.User.Id.Should().Be(TestUserId);
        result.Data!.User.Email.Should().Be("test@example.com");
        result.Data!.Workspaces.Should().BeEmpty();
        result.Data!.PersonalWorkspace.Status.Should().Be("pending");
        result.Data!.PersonalWorkspace.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var port = new Mock<IIdentityBootstrapReadPort>();
        port.Setup(p => p.GetAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityBootstrapProjection?)null);

        var sut = CreateSut(port);
        var result = await sut.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("User not found"));
    }

    [Fact]
    public async Task Handle_MapsWorkspacesAndPersonalWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        var port = new Mock<IIdentityBootstrapReadPort>();
        port.Setup(p => p.GetAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProjection(
                personalWorkspaceId: workspaceId,
                new BootstrapWorkspaceProjection(workspaceId, "My Workspace", "my-workspace", "Admin")));

        var sut = CreateSut(port);
        var result = await sut.Handle(new GetBootstrapQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Workspaces.Should().ContainSingle();
        result.Data.Workspaces[0].Id.Should().Be(workspaceId);
        result.Data.Workspaces[0].Name.Should().Be("My Workspace");
        result.Data.Workspaces[0].Slug.Should().Be("my-workspace");
        result.Data.Workspaces[0].Role.Should().Be("Admin");
        result.Data.PersonalWorkspace.Status.Should().Be("ready");
        result.Data.PersonalWorkspace.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public async Task Handle_QueriesOnlyTheAuthenticatedUserId()
    {
        var port = new Mock<IIdentityBootstrapReadPort>();
        port.Setup(p => p.GetAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProjection());

        var sut = CreateSut(port);
        await sut.Handle(new GetBootstrapQuery(), CancellationToken.None);

        port.Verify(p => p.GetAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
        port.Verify(p => p.GetAsync(It.Is<Guid>(id => id != TestUserId), It.IsAny<CancellationToken>()), Times.Never);
    }
}
