using FluentAssertions;

namespace Notrelix.Domain.Tests.Workspaces;

/// <summary>A person who leaves the Workspace takes their own Space and Team entries with them, even the last Admin or Lead entry.</summary>
public class MemberDepartureCleanupDomainTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Space NewSpace(Guid creator) =>
        Space.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", SpaceVisibility.Workspace, creator, Now);

    private static Team NewTeam(Guid lead)
    {
        var team = Team.Create(Guid.NewGuid(), Guid.NewGuid(), "ERP", lead, Now);
        team.AddMember(lead, TeamMemberRole.Lead, lead, Now);
        return team;
    }

    [Fact]
    public void TheOnlySpaceAdmin_Departs_TheEntryGoes_AndTheVersionMoves()
    {
        var person = Guid.NewGuid();
        var space = NewSpace(person);
        var version = space.Version;

        var removed = space.RemoveDepartedUser(person, Guid.NewGuid(), Now);

        removed.Should().BeTrue();
        space.Members.Should().NotContain(m => m.SubjectId == person);
        space.Version.Should().Be(version + 1);
        space.DomainEvents.OfType<SpaceMemberRemovedDomainEvent>().Should().ContainSingle(e => e.SubjectId == person);
    }

    [Fact]
    public void AnArchivedSpace_StillLosesTheEntry()
    {
        var person = Guid.NewGuid();
        var space = NewSpace(person);
        space.Archive(Guid.NewGuid(), Now, SpaceContentFact.Empty);

        space.RemoveDepartedUser(person, Guid.NewGuid(), Now).Should().BeTrue();

        space.Members.Should().NotContain(m => m.SubjectId == person);
    }

    [Fact]
    public void ASpaceWithoutTheEntry_IsANoOp_NoVersionNoEvent()
    {
        var space = NewSpace(Guid.NewGuid());
        var version = space.Version;
        var events = space.DomainEvents.Count;

        space.RemoveDepartedUser(Guid.NewGuid(), Guid.NewGuid(), Now).Should().BeFalse();

        space.Version.Should().Be(version);
        space.DomainEvents.Count.Should().Be(events);
    }

    [Fact]
    public void ATeamEntryOfThePerson_IsNotASpaceUserEntry()
    {
        var person = Guid.NewGuid();
        var space = NewSpace(Guid.NewGuid());
        space.AddMember(SpaceMemberSubjectType.Team, person, SpaceRole.Member, Guid.NewGuid(), Now);

        space.RemoveDepartedUser(person, Guid.NewGuid(), Now).Should().BeFalse();

        space.Members.Should().Contain(m => m.SubjectId == person);
    }

    [Fact]
    public void TheOnlyTeamLead_Departs_TheTeamIsLeftWithoutALead()
    {
        var lead = Guid.NewGuid();
        var team = NewTeam(lead);
        var version = team.Version;

        var removed = team.RemoveDepartedMember(lead, Guid.NewGuid(), Now);

        removed.Should().BeTrue();
        team.Members.Should().NotContain(m => m.Status == TeamMemberStatus.Active && m.UserId == lead);
        team.Members.Should().NotContain(m => m.Status == TeamMemberStatus.Active && m.Role == TeamMemberRole.Lead);
        team.Version.Should().Be(version + 1);
        team.DomainEvents.OfType<TeamMemberRemovedDomainEvent>().Should().ContainSingle(e => e.UserId == lead);
    }

    [Fact]
    public void AnArchivedTeam_StillLosesTheMember()
    {
        var lead = Guid.NewGuid();
        var team = NewTeam(lead);
        team.Archive(Guid.NewGuid(), Now);

        team.RemoveDepartedMember(lead, Guid.NewGuid(), Now).Should().BeTrue();

        team.Members.Should().NotContain(m => m.Status == TeamMemberStatus.Active && m.UserId == lead);
    }

    [Fact]
    public void ATeamWithoutTheMember_IsANoOp()
    {
        var team = NewTeam(Guid.NewGuid());
        var version = team.Version;

        team.RemoveDepartedMember(Guid.NewGuid(), Guid.NewGuid(), Now).Should().BeFalse();

        team.Version.Should().Be(version);
    }
}
