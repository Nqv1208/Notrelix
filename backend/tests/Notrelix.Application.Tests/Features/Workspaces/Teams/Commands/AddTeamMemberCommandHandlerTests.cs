using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams.Commands;

public class AddTeamMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private AddTeamMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenThePersonIsAnActiveMember_AddsThem()
    {
        var team = CreateTeam();
        var person = Guid.CreateVersion7();
        SetupTeams(team);
        SetupMembers(CreateMember(WorkspaceRole.Member, person));

        var result = await CreateSut().Handle(new AddTeamMemberCommand(TestWorkspaceId, team.Id, person, "Member", ExpectedVersion: 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        team.Members.Should().Contain(m => m.UserId == person);
    }

    [Fact]
    public async Task Handle_WhenThePersonIsAGuest_IsRefusedAndNothingChanges()
    {
        var team = CreateTeam();
        var guest = Guid.CreateVersion7();
        SetupTeams(team);
        SetupMembers(CreateMember(WorkspaceRole.Guest, guest));
        var before = team.Members.Count;

        Func<Task> act = () => CreateSut().Handle(new AddTeamMemberCommand(TestWorkspaceId, team.Id, guest, "Member", ExpectedVersion: 1), CancellationToken.None);

        var rule = await act.Should().ThrowAsync<BusinessRuleException>();
        rule.Which.RuleCode.Should().Be(Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Team_GuestCannotJoin);
        team.Members.Should().HaveCount(before);
    }

    [Fact]
    public async Task Handle_WhenThePersonIsNotAMemberOfTheWorkspace_IsNotFoundAndNothingChanges()
    {
        var team = CreateTeam();
        SetupTeams(team);
        SetupMembers();
        var before = team.Members.Count;

        Func<Task> act = () => CreateSut().Handle(new AddTeamMemberCommand(TestWorkspaceId, team.Id, Guid.CreateVersion7(), "Member", ExpectedVersion: 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        team.Members.Should().HaveCount(before);
    }

    [Fact]
    public async Task Handle_WhenTheTeamBelongsToAnotherWorkspace_IsNotFound()
    {
        var team = CreateTeam();
        SetupTeams(team);
        SetupMembers(CreateMember(WorkspaceRole.Member, TestUserId));

        Func<Task> act = () => CreateSut().Handle(new AddTeamMemberCommand(Guid.CreateVersion7(), team.Id, TestUserId, "Member", ExpectedVersion: 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
