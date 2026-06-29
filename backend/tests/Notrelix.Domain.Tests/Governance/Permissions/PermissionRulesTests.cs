using FluentAssertions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Tests.Governance;

public class PermissionRulesTests
{
    [Fact]
    public void CanGrant_GranterLevelHigherThanTarget_ShouldReturnTrue()
    {
        PermissionRules.CanGrant(PermissionLevel.Manager, PermissionLevel.Editor).Should().BeTrue();
    }

    [Fact]
    public void CanGrant_SameLevel_ShouldReturnTrue()
    {
        PermissionRules.CanGrant(PermissionLevel.Editor, PermissionLevel.Editor).Should().BeTrue();
    }

    [Fact]
    public void CanGrant_GranterLevelLowerThanTarget_ShouldReturnFalse()
    {
        PermissionRules.CanGrant(PermissionLevel.Editor, PermissionLevel.Manager).Should().BeFalse();
    }

    [Fact]
    public void CanGrant_TargetIsNone_ShouldReturnFalse()
    {
        PermissionRules.CanGrant(PermissionLevel.Owner, PermissionLevel.None).Should().BeFalse();
    }

    [Fact]
    public void CanAssignOwner_GranterIsOwner_ShouldReturnTrue()
    {
        PermissionRules.CanAssignOwner(PermissionLevel.Owner).Should().BeTrue();
    }

    [Fact]
    public void CanAssignOwner_GranterBelowOwner_ShouldReturnFalse()
    {
        PermissionRules.CanAssignOwner(PermissionLevel.Manager).Should().BeFalse();
        PermissionRules.CanAssignOwner(PermissionLevel.Editor).Should().BeFalse();
        PermissionRules.CanAssignOwner(PermissionLevel.None).Should().BeFalse();
    }
}
