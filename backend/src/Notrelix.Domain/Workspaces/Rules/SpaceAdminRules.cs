using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Domain.Workspaces.Rules;

/// <summary>
/// The last Admin entry of a Space cannot be removed or downgraded (mirrors
/// <see cref="TeamLeadRules"/>; WSP-024). User and Team Admin entries both count.
/// </summary>
public static class SpaceAdminRules
{
    public static void EnsureCanRemove(SpaceRole role, int adminCount)
    {
        if (role == SpaceRole.Admin && adminCount <= 1)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotRemoveLastAdmin,
                "Cannot remove the last admin of a space.");
    }

    public static void EnsureCanChangeRole(SpaceRole currentRole, SpaceRole newRole, int adminCount)
    {
        if (currentRole == SpaceRole.Admin && newRole != SpaceRole.Admin && adminCount <= 1)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Space_CannotDowngradeLastAdmin,
                "Cannot downgrade the last admin of a space.");
    }
}
