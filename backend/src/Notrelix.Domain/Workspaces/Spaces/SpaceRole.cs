namespace Notrelix.Domain.Workspaces.Spaces;

/// <summary>
/// Role of a Space member entry (docs/product/workspaces.md WSP-024). The
/// meaning of a role for a protected operation is Governance-owned (GOV-027);
/// the Space only stores who holds which role.
/// </summary>
public enum SpaceRole
{
    Viewer,
    Member,
    Admin
}
