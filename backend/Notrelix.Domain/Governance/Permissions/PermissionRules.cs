namespace Notrelix.Domain.Governance.Permissions;

/// <summary>
/// Domain-level invariants for permission grants. Pure functions; no side effects.
/// The Application layer hosts the PermissionService that orchestrates these rules.
/// </summary>
public static class PermissionRules
{
    /// <summary>
    /// A subject may not be granted a level higher than the granter's own level.
    /// </summary>
    public static bool CanGrant(PermissionLevel granterLevel, PermissionLevel targetLevel)
        => granterLevel >= targetLevel && targetLevel > PermissionLevel.None;

    /// <summary>
    /// Owner-level grants can only be issued by another Owner.
    /// </summary>
    public static bool CanAssignOwner(PermissionLevel granterLevel)
        => granterLevel >= PermissionLevel.Owner;
}
