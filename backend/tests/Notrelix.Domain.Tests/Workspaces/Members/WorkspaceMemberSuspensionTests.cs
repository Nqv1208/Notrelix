using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces;

/// <summary>
/// UC-WSP-F03 / F04 at the Domain: suspend and activate are no-ops on the state they already hold (no version
/// step), the last active Owner cannot be suspended, and a removed member can be neither suspended nor activated.
/// </summary>
public class WorkspaceMemberSuspensionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private WorkspaceMember Member(WorkspaceRole role = WorkspaceRole.Member)
    {
        var member = WorkspaceMember.Create(_accountId, _workspaceId, _userId, role, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();
        return member;
    }

    [Fact]
    public void Suspend_AlreadySuspended_IsANoOpWithNoVersionStep()
    {
        var member = Member();
        member.Suspend(_actorId, _now, 2);
        ((IHasDomainEvents)member).ClearDomainEvents();
        var version = member.Version;

        member.Suspend(_actorId, _now.AddMinutes(1), 2);

        member.Status.Should().Be(WorkspaceMemberStatus.Suspended);
        member.Version.Should().Be(version);
        member.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Suspend_TheLastActiveOwner_IsRefusedAndNothingChanges()
    {
        var owner = Member(WorkspaceRole.Owner);
        var version = owner.Version;

        var act = () => owner.Suspend(_actorId, _now, activeOwnerCount: 1);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Owner_CannotSuspendLastOwner);
        owner.Status.Should().Be(WorkspaceMemberStatus.Active);
        owner.Version.Should().Be(version);
    }

    [Fact]
    public void Suspend_AnOwnerWhenAnotherActiveOwnerExists_Succeeds()
    {
        var owner = Member(WorkspaceRole.Owner);

        owner.Suspend(_actorId, _now, activeOwnerCount: 2);

        owner.Status.Should().Be(WorkspaceMemberStatus.Suspended);
    }

    [Fact]
    public void Suspend_ARemovedMember_IsRefused()
    {
        var member = Member();
        member.Remove(2, _actorId, _now);

        var act = () => member.Suspend(_actorId, _now, 2);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotSuspendRemoved);
        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
    }

    [Fact]
    public void Activate_AlreadyActive_IsANoOpWithNoVersionStep()
    {
        var member = Member();
        var version = member.Version;

        member.Activate(_actorId, _now);

        member.Status.Should().Be(WorkspaceMemberStatus.Active);
        member.Version.Should().Be(version);
        member.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_ARemovedMember_IsRefused()
    {
        var member = Member();
        member.Remove(2, _actorId, _now);

        var act = () => member.Activate(_actorId, _now);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotActivateRemoved);
        member.Status.Should().Be(WorkspaceMemberStatus.Removed);
    }
}
