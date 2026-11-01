namespace Notrelix.Application.Features.Governance.Authorization;

/// <summary>Where a permission action is decided (owner decision 2026-10-31).</summary>
public static class PermissionLevels
{
    public const string Account = "account";
    public const string Workspace = "workspace";
    public const string Resource = "resource";
}

/// <summary>
/// One product-defined permission action as a tenant sees it when composing a role: its area, its label
/// key (the vi/en catalogs own the words), where it is decided, whether a role may add it (workspace-level
/// actions only) or remove it, and whether it is locked to the Owner (a role never carries it).
/// </summary>
public sealed record PermissionCatalogEntry(
    string Action,
    string Area,
    string LabelKey,
    string Level,
    bool CanAdd,
    bool CanRemove,
    bool Locked);

/// <summary>
/// The permission catalog is product-defined in code; tenants compose roles from it and never invent a
/// permission. It is derived from <see cref="WorkspaceRoleBaseline"/> (every action is classified there)
/// plus the few flags below, so a new action cannot be added to the vocabulary without a place in both.
/// </summary>
public static class PermissionCatalog
{
    // Owner-only by GOV-013: security, lifecycle, billing authority and credentials. A role never carries them.
    private static readonly HashSet<PermissionAction> OwnerLocked =
    [
        PermissionAction.CreateWorkspace,
        PermissionAction.ManageAccount,
        PermissionAction.ChangeMemberRole,
        PermissionAction.ManageApiTokens,
        PermissionAction.ManageWorkspace,
        PermissionAction.ArchiveWorkspace,
        PermissionAction.RestoreWorkspace,
        PermissionAction.DeleteWorkspace,
    ];

    // Reading is decided with the collection reads (lists, search); a role cannot take it away until those
    // reads honour a role (open question recorded in the Governance catalog).
    private static readonly HashSet<PermissionAction> NotRemovable =
    [
        PermissionAction.ViewWorkspace,
        PermissionAction.ViewBoard,
        PermissionAction.ViewPage,
    ];

    private static readonly IReadOnlyDictionary<PermissionAction, string> Areas = new Dictionary<PermissionAction, string>
    {
        [PermissionAction.CreateWorkspace] = "account",
        [PermissionAction.ManageAccount] = "account",
        [PermissionAction.ViewBilling] = "billing",
        [PermissionAction.ViewWorkspace] = "workspace",
        [PermissionAction.ManageWorkspace] = "workspace",
        [PermissionAction.ArchiveWorkspace] = "workspace",
        [PermissionAction.RestoreWorkspace] = "workspace",
        [PermissionAction.DeleteWorkspace] = "workspace",
        [PermissionAction.ManageWorkspaceSettings] = "workspace",
        [PermissionAction.ManageIntegrations] = "workspace",
        [PermissionAction.ManageAutomation] = "automation",
        [PermissionAction.ReadAudit] = "audit",
        [PermissionAction.ViewMembers] = "people",
        [PermissionAction.InviteMember] = "people",
        [PermissionAction.ChangeMemberRole] = "people",
        [PermissionAction.RemoveMember] = "people",
        [PermissionAction.ManageMemberProfile] = "people",
        [PermissionAction.ManageApiTokens] = "security",
        [PermissionAction.ManageSpaces] = "organization",
        [PermissionAction.ManageTeams] = "organization",
        [PermissionAction.CreateBoard] = "boards",
        [PermissionAction.ViewBoard] = "boards",
        [PermissionAction.ManageBoard] = "boards",
        [PermissionAction.ManageBoardPermission] = "boards",
        [PermissionAction.CreateBoardView] = "boards",
        [PermissionAction.UpdateBoardView] = "boards",
        [PermissionAction.ShareBoardView] = "boards",
        [PermissionAction.CreateField] = "boards",
        [PermissionAction.UpdateField] = "boards",
        [PermissionAction.DeleteField] = "boards",
        [PermissionAction.CreateItem] = "boards",
        [PermissionAction.UpdateItem] = "boards",
        [PermissionAction.MoveItem] = "boards",
        [PermissionAction.AssignItem] = "boards",
        [PermissionAction.CreateComment] = "collaboration",
        [PermissionAction.CreatePage] = "pages",
        [PermissionAction.ViewPage] = "pages",
        [PermissionAction.UpdatePage] = "pages",
        [PermissionAction.DeletePage] = "pages",
        [PermissionAction.ArchivePage] = "pages",
        [PermissionAction.SharePage] = "pages",
        [PermissionAction.ManagePagePermission] = "pages",
    };

    public static IReadOnlyList<PermissionCatalogEntry> Entries { get; } = Enum.GetValues<PermissionAction>()
        .Select(Describe)
        .OrderBy(e => e.Area, StringComparer.Ordinal)
        .ThenBy(e => e.Action, StringComparer.Ordinal)
        .ToArray();

    public static bool TryGet(string action, out PermissionCatalogEntry entry)
    {
        entry = Entries.FirstOrDefault(e => string.Equals(e.Action, action, StringComparison.Ordinal))!;
        return entry is not null;
    }

    /// <summary>The Owner never holds a custom role; the others may use any Admin, Member or Guest base.</summary>
    public static bool IsLocked(PermissionAction action) => OwnerLocked.Contains(action);

    private static PermissionCatalogEntry Describe(PermissionAction action)
    {
        var accountLevel = action is PermissionAction.CreateWorkspace or PermissionAction.ManageAccount;
        var resourceLevel = !accountLevel
            && WorkspaceRoleBaseline.Decide("Member", action) == BaselineDecision.LevelBased;
        var level = accountLevel ? PermissionLevels.Account
            : resourceLevel ? PermissionLevels.Resource
            : PermissionLevels.Workspace;
        var locked = OwnerLocked.Contains(action);
        var name = action.ToString();
        return new PermissionCatalogEntry(
            name,
            Areas[action],
            "governance.permission." + char.ToLowerInvariant(name[0]) + name[1..],
            level,
            CanAdd: level == PermissionLevels.Workspace && !locked,
            CanRemove: !locked && !NotRemovable.Contains(action),
            Locked: locked);
    }
}
