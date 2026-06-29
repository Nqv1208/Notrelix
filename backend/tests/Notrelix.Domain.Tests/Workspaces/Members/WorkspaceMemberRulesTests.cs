using FluentAssertions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceMemberRulesTests
{
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
}
