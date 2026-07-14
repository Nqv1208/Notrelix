using Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams.Commands;

public class ChangeTeamMemberRoleCommandHandlerTests : WorkspaceHandlerTestBase
{
    private ChangeTeamMemberRoleCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenTeamAndMemberExist_ChangesRoleSuccessfully()
    {
        var team = CreateTeam();
        team.AddMember(TestUserId, TeamMemberRole.Member, TestUserId, TestNow);
        SetupTeams(team);
        var sut = CreateSut();
        var command = new ChangeTeamMemberRoleCommand(TestWorkspaceId, team.Id, TestUserId, "Lead");
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTeamNotFound_ThrowsNotFoundException()
    {
        SetupTeams();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new ChangeTeamMemberRoleCommand(TestWorkspaceId, Guid.CreateVersion7(), TestUserId, "Lead"), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsBusinessRuleException()
    {
        var team = CreateTeam();
        SetupTeams(team);
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new ChangeTeamMemberRoleCommand(TestWorkspaceId, team.Id, TestUserId, "Lead"), CancellationToken.None);
        await act.Should().ThrowAsync<BusinessRuleException>();
    }
}
