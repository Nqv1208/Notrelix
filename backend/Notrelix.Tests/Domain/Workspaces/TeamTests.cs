using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Teams.Events;
using Xunit;

namespace Notrelix.Domain.Tests.Workspaces;

public class TeamTests
{
    [Fact]
    public void AddMember_ShouldAddToList_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var team = Team.Create(workspaceId, "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.ClearDomainEvents();

        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Members.Should().HaveCount(1);
        team.Members.First().UserId.Should().Be(userId);
        team.Members.First().Status.Should().Be(TeamMemberStatus.Active);
        team.DomainEvents.Should().ContainSingle(e => e is TeamMemberAddedEvent);
    }

    [Fact]
    public void SoftDelete_ShouldSetStatusToSoftDeleted_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.ClearDomainEvents();

        team.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Status.Should().Be(TeamStatus.SoftDeleted);
        team.IsDeleted.Should().BeTrue();
        team.DomainEvents.Should().Contain(e => e is TeamSoftDeletedEvent);
    }

    [Fact]
    public void Restore_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.ClearDomainEvents();

        team.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Status.Should().Be(TeamStatus.Active);
        team.IsDeleted.Should().BeFalse();
        team.DomainEvents.Should().Contain(e => e is TeamRestoredEvent);
    }

    [Fact]
    public void AddMember_ShouldThrow_WhenArchived()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.AddMember(Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void RemoveMember_ShouldThrow_WhenArchived()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.RemoveMember(userId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [Fact]
    public void AddMember_ShouldThrow_WhenDeleted()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.AddMember(Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void RemoveMember_ShouldSetMemberStatusToRemoved()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.RemoveMember(userId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var member = team.Members.First(m => m.UserId == userId);
        member.Status.Should().Be(TeamMemberStatus.Removed);
    }

    [Fact]
    public void AddMember_DuplicateActiveMember_ShouldThrow()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.AddMember(userId, TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*already a member of this team*");
    }

    [Fact]
    public void AddMember_ReAddRemovedMember_ShouldReactivateMember()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        
        // Add member
        team.AddMember(userId, TeamMemberRole.Member, actor, DateTimeOffset.UtcNow);
        
        // Remove member
        team.RemoveMember(userId, actor, DateTimeOffset.UtcNow);
        team.Members.First(m => m.UserId == userId).Status.Should().Be(TeamMemberStatus.Removed);
        team.ClearDomainEvents();

        // Re-add/reactivate
        var reactivateTime = DateTimeOffset.UtcNow.AddMinutes(5);
        team.AddMember(userId, TeamMemberRole.Lead, actor, reactivateTime);

        var member = team.Members.First(m => m.UserId == userId);
        team.Members.Should().HaveCount(1, "reactivated member should not create a duplicate row");
        member.Status.Should().Be(TeamMemberStatus.Active);
        member.Role.Should().Be(TeamMemberRole.Lead);
        member.UpdatedBy.Should().Be(actor);
        member.UpdatedAt.Should().Be(reactivateTime);
        team.DomainEvents.Should().ContainSingle(e => e is TeamMemberAddedEvent);
    }

    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.ClearDomainEvents();

        team.Rename("QA Team", Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Name.Should().Be("QA Team");
        team.DomainEvents.Should().ContainSingle(e => e is TeamRenamedEvent);
    }

    [Fact]
    public void Rename_ShouldThrow_WhenArchived()
    {
        var team = Team.Create(Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*archived*");
    }
}
