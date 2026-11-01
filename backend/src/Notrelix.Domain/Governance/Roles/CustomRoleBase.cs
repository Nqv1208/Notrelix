namespace Notrelix.Domain.Governance.Roles;

/// <summary>
/// The built-in workspace role a custom role is built on. Fixed when the role is created (owner decision
/// 2026-10-31); Owner is never a base because Owner is never replaced.
/// </summary>
public enum CustomRoleBase
{
    Admin,
    Member,
    Guest
}
