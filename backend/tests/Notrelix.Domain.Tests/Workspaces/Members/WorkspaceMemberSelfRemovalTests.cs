using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces;

/// <summary>Leaving a Workspace is the membership removal with the member as its own actor.</summary>
public class WorkspaceMemberSelfRemovalTests
{
    private static WorkspaceMember NewMember(Guid userId, WorkspaceRole role) =>
        WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), userId, role, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Theory]
    [InlineData(WorkspaceRole.Guest)]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Admin)]
    public void AMemberWhoIsNotAnOwner_CanLeave(WorkspaceRole role)
    {
        var userId = Guid.NewGuid();
        var member = NewMember(userId, role);

        member.Remove(1, userId, DateTimeOffset.UtcNow);

        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
        member.DomainEvents.OfType<WorkspaceMemberRemovedDomainEvent>().Should().ContainSingle()
            .Which.ActorId.Should().Be(userId);
    }

    [Fact]
    public void AnOwner_CanLeave_WhenAnotherActiveOwnerRemains()
    {
        var userId = Guid.NewGuid();
        var member = NewMember(userId, WorkspaceRole.Owner);

        member.Remove(2, userId, DateTimeOffset.UtcNow);

        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
    }

    [Fact]
    public void TheLastOwner_CannotLeave_AndNothingChanges()
    {
        var userId = Guid.NewGuid();
        var member = NewMember(userId, WorkspaceRole.Owner);
        var version = member.Version;
        var events = member.DomainEvents.Count;

        var leave = () => member.Remove(1, userId, DateTimeOffset.UtcNow);

        leave.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Owner_CannotRemoveLastOwner);
        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.Version.Should().Be(version);
        member.DomainEvents.Count.Should().Be(events);
    }

    [Fact]
    public void LeavingTwice_IsASemanticNoOp()
    {
        var userId = Guid.NewGuid();
        var member = NewMember(userId, WorkspaceRole.Member);
        member.Remove(1, userId, DateTimeOffset.UtcNow);
        var version = member.Version;

        member.Remove(1, userId, DateTimeOffset.UtcNow);

        member.Version.Should().Be(version);
    }
}
