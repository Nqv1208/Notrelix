using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

public class TeamVersionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Team), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Rename_ShouldIncrementVersion()
    {
        var team = Team.Create(_accountId, _workspaceId, "Original", _actorId, _now);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var version = team.Version;

        team.Rename("Renamed", _actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamRenamedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Archive(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var team = Team.Create(_accountId, _workspaceId, "Team", _actorId, _now);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var version = team.Version;

        team.Archive(_actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamArchivedDomainEvent);
    }

    [CoversMutation(typeof(Team), "AddMember(System.Guid,Notrelix.Domain.Workspaces.Teams.TeamMemberRole,System.Guid,System.DateTimeOffset,System.Guid?)", MutationScenario.Version)]
    [Fact]
    public void AddMember_ShouldIncrementVersion()
    {
        var team = Team.Create(_accountId, _workspaceId, "Team", _actorId, _now);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var version = team.Version;

        team.AddMember(_userId, TeamMemberRole.Member, _actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamMemberAddedDomainEvent);
    }

    [Fact]
    public void RemoveMember_ShouldIncrementVersion()
    {
        var team = Team.Create(_accountId, _workspaceId, "Team", _actorId, _now);
        team.AddMember(_userId, TeamMemberRole.Member, _actorId, _now);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var version = team.Version;

        team.RemoveMember(_userId, _actorId, _now);

        team.Version.Should().Be(version + 1);
        team.DomainEvents.Should().Contain(e => e is TeamMemberRemovedDomainEvent);
    }

    [CoversMutation(typeof(Team), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion()
    {
        var team = Team.Create(_accountId, _workspaceId, "Team", _actorId, _now);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var version = team.Version;

        team.SoftDelete(_actorId, _now);

        team.Version.Should().Be(version + 1);
        team.IsDeleted.Should().BeTrue();
        team.DomainEvents.Should().Contain(e => e is TeamSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(Team), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var team = Team.Create(_accountId, _workspaceId, "Team", _actorId, _now);
        team.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)team).ClearDomainEvents();
        var version = team.Version;

        team.Restore(_actorId, _now);

        team.Version.Should().Be(version + 1);
        team.IsDeleted.Should().BeFalse();
        team.DomainEvents.Should().Contain(e => e is TeamRestoredDomainEvent);
    }
}
