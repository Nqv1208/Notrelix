using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces;

/// <summary>Who may become Owner by the handover: an active member who is not a Guest.</summary>
public class WorkspaceMemberPromotionTests
{
    private static WorkspaceMember NewMember(WorkspaceRole role) =>
        WorkspaceMember.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), role, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void AGuest_CannotBecomeOwner_AndNothingChanges()
    {
        var guest = NewMember(WorkspaceRole.Guest);
        var version = guest.Version;

        var act = () => guest.PromoteToOwner(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotPromoteGuestToOwner);
        guest.Role.Should().Be(WorkspaceRole.Guest);
        guest.Version.Should().Be(version);
        guest.DomainEvents.OfType<WorkspaceMemberRoleChangedDomainEvent>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Admin)]
    public void AnActiveMemberOrAdmin_BecomesOwner(WorkspaceRole role)
    {
        var member = NewMember(role);

        member.PromoteToOwner(Guid.NewGuid(), DateTimeOffset.UtcNow);

        member.Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public void ASuspendedMember_CannotBecomeOwner()
    {
        var member = NewMember(WorkspaceRole.Admin);
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);

        var act = () => member.PromoteToOwner(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotPromoteInactiveToOwner);
    }
}
