using FluentAssertions;
using Notrelix.Domain.Workspaces.Rules;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceOwnerRulesTests
{
    [Fact]
    public void WorkspaceOwnerRules_ShouldNotAllowActionsOnLastOwner()
    {
        // Downgrade
        var actDowngrade = () => WorkspaceOwnerRules.EnsureCanDowngradeOwner(WorkspaceRole.Owner, WorkspaceRole.Admin, 1);
        actDowngrade.Should().Throw<BusinessRuleException>().WithMessage("*Cannot downgrade the last owner*");

        // Suspend
        var actSuspend = () => WorkspaceOwnerRules.EnsureCanSuspendOwner(WorkspaceRole.Owner, 1);
        actSuspend.Should().Throw<BusinessRuleException>().WithMessage("*Cannot suspend the last owner*");

        // Remove
        var actRemove = () => WorkspaceOwnerRules.EnsureCanRemoveOwner(WorkspaceRole.Owner, 1);
        actRemove.Should().Throw<BusinessRuleException>().WithMessage("*Cannot remove the last owner*");
    }

    [Fact]
    public void WorkspaceOwnerRules_ShouldAllowActionsIfMultipleOwners()
    {
        // Downgrade
        var actDowngrade = () => WorkspaceOwnerRules.EnsureCanDowngradeOwner(WorkspaceRole.Owner, WorkspaceRole.Admin, 2);
        actDowngrade.Should().NotThrow();

        // Suspend
        var actSuspend = () => WorkspaceOwnerRules.EnsureCanSuspendOwner(WorkspaceRole.Owner, 2);
        actSuspend.Should().NotThrow();

        // Remove
        var actRemove = () => WorkspaceOwnerRules.EnsureCanRemoveOwner(WorkspaceRole.Owner, 2);
        actRemove.Should().NotThrow();
    }
}
