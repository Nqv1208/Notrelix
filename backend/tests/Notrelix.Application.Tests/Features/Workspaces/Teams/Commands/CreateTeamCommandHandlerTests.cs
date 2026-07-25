using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams.Commands;

public class CreateTeamCommandHandlerTests : WorkspaceHandlerTestBase
{
    private CreateTeamCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_CreatesTeamSuccessfully()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        SetupTeams();
        SetupTeamMembers();
        var sut = CreateSut();
        var command = new CreateTeamCommand(workspace.Id, "New Team", "A team description");
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        var command = new CreateTeamCommand(TestWorkspaceId, "New Team", null);
        Func<Task> act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
