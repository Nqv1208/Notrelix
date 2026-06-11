namespace Notrelix.Domain.Governance.Audit;

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
