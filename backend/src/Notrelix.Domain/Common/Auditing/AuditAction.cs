namespace Notrelix.Domain.Common.Auditing;

public enum AuditAction
{
    Create,
    Update,
    Delete,
    Restore,
    Archive,
    Login,
    Logout,
    PermissionChange,
    RoleAssignment,
    Export
}
