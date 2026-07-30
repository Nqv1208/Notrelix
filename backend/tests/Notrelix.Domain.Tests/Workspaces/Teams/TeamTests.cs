using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

[CoversAggregate(typeof(Team))]
public class TeamTests
{
    [CoversMutation(typeof(Team), "AddMember(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset,System.Guid?)", MutationScenario.Event)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void AddMember_ShouldAddToList_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var team = Team.Create(Guid.NewGuid(), workspaceId, "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Members.Should().HaveCount(1);
        team.Members.First().UserId.Should().Be(userId);
        team.Members.First().Status.Should().Be(TeamMemberStatus.Active);
        team.DomainEvents.Should().ContainSingle(e => e is TeamMemberAddedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void Delete_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.IsDeleted.Should().BeTrue();
        team.DomainEvents.Should().Contain(e => e is TeamDeletedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldSetIsDeleted_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.IsDeleted.Should().BeFalse();
        team.DomainEvents.Should().Contain(e => e is TeamRestoredDomainEvent);
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void AddMember_ShouldThrow_WhenArchived()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.AddMember(Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void RemoveMember_ShouldThrow_WhenArchived()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.RemoveMember(userId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [CoversMutation(typeof(Team), "AddMember(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset,System.Guid?)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void AddMember_ShouldThrow_WhenDeleted()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.AddMember(Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void RemoveMember_ShouldSetMemberStatusToRemoved()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.RemoveMember(userId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var member = team.Members.First(m => m.UserId == userId);
        member.Status.Should().Be(TeamMemberStatus.Removed);
    }

    [CoversMutation(typeof(Team), "AddMember(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset,System.Guid?)", MutationScenario.NoOp)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void AddMember_DuplicateActiveMember_ShouldBeNoOp()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.AddMember(userId, TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Members.Should().HaveCount(1);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void AddMember_ReAddRemovedMember_ShouldReactivateMember()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        var actor = Guid.NewGuid();

        team.AddMember(userId, TeamMemberRole.Member, actor, DateTimeOffset.UtcNow);
        team.RemoveMember(userId, actor, DateTimeOffset.UtcNow);
        team.Members.First(m => m.UserId == userId).Status.Should().Be(TeamMemberStatus.Removed);
        ((IHasDomainEvents)team).ClearDomainEvents();

        var reactivateTime = DateTimeOffset.UtcNow.AddMinutes(5);
        team.AddMember(userId, TeamMemberRole.Lead, actor, reactivateTime);

        var member = team.Members.First(m => m.UserId == userId);
        team.Members.Should().HaveCount(1, "reactivated member should not create a duplicate row");
        member.Status.Should().Be(TeamMemberStatus.Active);
        member.Role.Should().Be(TeamMemberRole.Lead);
        member.UpdatedBy.Should().Be(actor);
        member.UpdatedAt.Should().Be(reactivateTime);
        team.DomainEvents.Should().ContainSingle(e => e is TeamMemberAddedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.Rename("QA Team", Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Name.Should().Be("QA Team");
        team.DomainEvents.Should().ContainSingle(e => e is TeamRenamedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Rename_ShouldThrow_WhenArchived()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename an archived team.");
    }

    [CoversMutation(typeof(Team), "Unarchive(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Unarchive_ShouldSetStatusToActive_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var actor = Guid.NewGuid();

        team.Unarchive(actor, DateTimeOffset.UtcNow);

        team.Status.Should().Be(TeamStatus.Active);
        team.DomainEvents.Should().ContainSingle(e => e is TeamUnarchivedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Unarchive(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Unarchive_WhenAlreadyActive_ShouldBeNoOp()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Status.Should().Be(TeamStatus.Active);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Invalid)]
    [Fact]
    public void Unarchive_Deleted_ShouldThrow()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.Unarchive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(Team), "UpdateDescription(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void UpdateDescription_ShouldSucceed_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var actor = Guid.NewGuid();

        team.UpdateDescription("Core product team", actor, DateTimeOffset.UtcNow);

        team.Description.Should().Be("Core product team");
        team.DomainEvents.Should().ContainSingle(e => e is TeamDescriptionUpdatedDomainEvent);
        var evt = (TeamDescriptionUpdatedDomainEvent)team.DomainEvents.First();
        evt.OldDescription.Should().BeNull();
        evt.NewDescription.Should().Be("Core product team");
        evt.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(Team), "UpdateDescription(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UpdateDescription_ShouldClearDescription_WhenSetToNull()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.UpdateDescription("Initial desc", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.UpdateDescription(null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.Description.Should().BeNull();
        team.DomainEvents.Should().ContainSingle(e => e is TeamDescriptionUpdatedDomainEvent);
    }

    [CoversMutation(typeof(Team), "UpdateDescription(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void UpdateDescription_WhenSameValue_ShouldBeNoOp()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.UpdateDescription("Same", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.UpdateDescription("Same", Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void UpdateDescription_ArchivedTeam_ShouldThrow()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.UpdateDescription("New desc", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [CoversMutation(typeof(Team), "ChangeMemberRole(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void ChangeMemberRole_ShouldSucceed_AndRaiseEvent()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var actor = Guid.NewGuid();

        team.ChangeMemberRole(userId, TeamMemberRole.Lead, actor, DateTimeOffset.UtcNow);

        team.Members.First(m => m.UserId == userId).Role.Should().Be(TeamMemberRole.Lead);
        team.DomainEvents.Should().ContainSingle(e => e is TeamMemberRoleChangedDomainEvent);
        var evt = (TeamMemberRoleChangedDomainEvent)team.DomainEvents.First();
        evt.OldRole.Should().Be(TeamMemberRole.Member);
        evt.NewRole.Should().Be(TeamMemberRole.Lead);
        evt.UpdatedBy.Should().Be(actor);
    }

    [CoversMutation(typeof(Team), "ChangeMemberRole(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void ChangeMemberRole_WhenSameRole_ShouldBeNoOp()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();

        team.ChangeMemberRole(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "ChangeMemberRole(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ChangeMemberRole_NonMember_ShouldThrow()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.ChangeMemberRole(Guid.NewGuid(), TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*not an active member*");
    }

    [CoversMutation(typeof(Team), "ChangeMemberRole(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ChangeMemberRole_DowngradeLastLead_ShouldThrow()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var leadId = Guid.NewGuid();
        team.AddMember(leadId, TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.AddMember(Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.ChangeMemberRole(leadId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot downgrade the last lead of a team.");
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void ChangeMemberRole_ArchivedTeam_ShouldThrow()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.ChangeMemberRole(userId, TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*archived*");
    }

    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void CreateWithLead_ShouldCreateTeamAndLeadMember()
    {
        var workspaceId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var (team, lead) = TeamFactory.CreateWithLead(
            Guid.NewGuid(), workspaceId, "Dev Team", creatorUserId, now);

        team.WorkspaceId.Should().Be(workspaceId);
        team.Name.Should().Be("Dev Team");
        team.Status.Should().Be(TeamStatus.Active);
        lead.UserId.Should().Be(creatorUserId);
        lead.Role.Should().Be(TeamMemberRole.Lead);
        team.Members.Should().HaveCount(1);
    }

    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void RemoveMember_ShouldThrow_WhenRemovingLastLead()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var leadId = Guid.NewGuid();
        team.AddMember(leadId, TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => team.RemoveMember(leadId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot remove the last lead from a team.");
    }

    [CoversMutation(typeof(Team), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Rename_ArchivedTeam_ShouldNotMutateName()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalName = team.Name;

        var act = () => team.Rename("New Name", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Name.Should().Be(originalName);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void UpdateDescription_ArchivedTeam_ShouldNotMutateDescription()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalDescription = team.Description;

        var act = () => team.UpdateDescription("New description", Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Description.Should().Be(originalDescription);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void AddMember_ArchivedTeam_ShouldNotAddMember()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalCount = team.Members.Count;

        var act = () => team.AddMember(Guid.NewGuid(), TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Members.Count.Should().Be(originalCount);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void RemoveMember_ArchivedTeam_ShouldNotRemoveMember()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalCount = team.Members.Count;

        var act = () => team.RemoveMember(userId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Members.Count.Should().Be(originalCount);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(Team), "RemoveMember(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ChangeMemberRole_ArchivedTeam_ShouldNotMutateRole()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamMemberRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);
        team.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalRole = team.Members.First(m => m.UserId == userId).Role;

        var act = () => team.ChangeMemberRole(userId, TeamMemberRole.Lead, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Members.First(m => m.UserId == userId).Role.Should().Be(originalRole);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Rename_EmptyActor_ShouldNotMutateName()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalName = team.Name;

        var act = () => team.Rename("New Name", Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Name.Should().Be(originalName);
        team.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Archive_EmptyActor_ShouldNotMutateStatus()
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "Dev Team", Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var originalStatus = team.Status;

        var act = () => team.Archive(Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        team.Status.Should().Be(originalStatus);
        team.DomainEvents.Should().BeEmpty();
    }
}
