using FluentAssertions;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceMemberRulesTests
{
    [Fact]
    public void EnsureActorIsNotTarget_WhenActorIsTarget_ShouldThrowWithItsCode()
    {
        var userId = Guid.NewGuid();

        var act = () => WorkspaceMemberRules.EnsureActorIsNotTarget(userId, userId);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_CannotActOnSelf);
        WorkspaceMemberRules.EnsureActorIsNotTarget(userId, Guid.NewGuid());
    }

    [Fact]
    public void EnsureNotLastOwner_WhenLastOwner_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var act = () => WorkspaceMemberRules.EnsureNotLastOwner(workspaceId, userId, ownerCount: 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot perform this action on the last owner of the workspace.");
    }

    [Fact]
    public void EnsureNotLastOwner_WhenMultipleOwners_ShouldNotThrow()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var act = () => WorkspaceMemberRules.EnsureNotLastOwner(workspaceId, userId, ownerCount: 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotLastOwner_WithZeroOwners_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var act = () => WorkspaceMemberRules.EnsureNotLastOwner(workspaceId, userId, ownerCount: 0);
        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Guest)]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Guest)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Member)]
    public void EnsureActorCanAssignRole_WhenAllowed_ShouldNotThrow(WorkspaceRole actor, WorkspaceRole assigned)
    {
        var act = () => WorkspaceMemberRules.EnsureActorCanAssignRole(actor, assigned);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member, WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Guest, WorkspaceRole.Owner)]
    public void EnsureActorCanAssignRole_WhenNonOwnerAssignsOwnerOrAdmin_ShouldThrowStableCode(WorkspaceRole actor, WorkspaceRole assigned)
    {
        var act = () => WorkspaceMemberRules.EnsureActorCanAssignRole(actor, assigned);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole);
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Owner, WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Guest)]
    public void EnsureActorCanActOnMember_WhenAllowed_ShouldNotThrow(WorkspaceRole actor, WorkspaceRole target)
    {
        var act = () => WorkspaceMemberRules.EnsureActorCanActOnMember(actor, target);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member, WorkspaceRole.Owner)]
    public void EnsureActorCanActOnMember_WhenNonOwnerTargetsOwnerOrAdmin_ShouldThrowStableCode(WorkspaceRole actor, WorkspaceRole target)
    {
        var act = () => WorkspaceMemberRules.EnsureActorCanActOnMember(actor, target);
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Member_ActorCannotActOnMember);
    }
}
