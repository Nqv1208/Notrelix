using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

public class TeamMemberTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var addedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var member = TeamMember.Create(accountId, workspaceId, teamId, userId, TeamMemberRole.Member, addedBy, now);

        member.AccountId.Should().Be(accountId);
        member.WorkspaceId.Should().Be(workspaceId);
        member.TeamId.Should().Be(teamId);
        member.UserId.Should().Be(userId);
        member.Role.Should().Be(TeamMemberRole.Member);
        member.Status.Should().Be(TeamMemberStatus.Active);
    }

    [Fact]
    public void Reactivate_ShouldSucceed()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.Reactivate(TeamMemberRole.Lead, null, actor, now);

        member.Status.Should().Be(TeamMemberStatus.Active);
        member.Role.Should().Be(TeamMemberRole.Lead);
    }

    [Fact]
    public void Reactivate_AlreadyActive_ShouldThrow()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.Reactivate(TeamMemberRole.Lead, null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Team member is already active.");
    }

    [Fact]
    public void Reactivate_EmptyActor_ShouldNotMutateState()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var originalStatus = member.Status;
        var originalRole = member.Role;

        var act = () => member.Reactivate(TeamMemberRole.Lead, null, Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        member.Status.Should().Be(originalStatus);
        member.Role.Should().Be(originalRole);
    }

    [Fact]
    public void ChangeRole_ShouldSucceed()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.ChangeRole(TeamMemberRole.Lead, actor, now);

        member.Role.Should().Be(TeamMemberRole.Lead);
    }

    [Fact]
    public void ChangeRole_InactiveMember_ShouldThrow()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => member.ChangeRole(TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot change the role of an inactive team member.");
    }

    [Fact]
    public void ChangeRole_SameRole_ShouldBeNoOp()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var originalRole = member.Role;

        member.ChangeRole(TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.Role.Should().Be(originalRole);
    }

    [Fact]
    public void ChangeRole_EmptyActor_ShouldNotMutateRole()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var originalRole = member.Role;

        var act = () => member.ChangeRole(TeamMemberRole.Lead, Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        member.Role.Should().Be(originalRole);
    }

    [Fact]
    public void Remove_ShouldSucceed()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        member.Remove(actor, now);

        member.Status.Should().Be(TeamMemberStatus.Removed);
    }

    [Fact]
    public void Remove_AlreadyRemoved_ShouldBeNoOp()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var originalStatus = member.Status;

        member.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.Status.Should().Be(originalStatus);
    }

    [Fact]
    public void Remove_EmptyActor_ShouldNotMutateStatus()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var originalStatus = member.Status;

        var act = () => member.Remove(Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        member.Status.Should().Be(originalStatus);
    }
}
