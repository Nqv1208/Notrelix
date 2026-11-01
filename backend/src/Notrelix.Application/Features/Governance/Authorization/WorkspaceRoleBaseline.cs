namespace Notrelix.Application.Features.Governance.Authorization;

/// <summary>How a baseline cell decides a request.</summary>
public enum BaselineDecision
{
    /// <summary>The role holds the action by default.</summary>
    Allow,

    /// <summary>The role does not hold the action by default.</summary>
    Deny,

    /// <summary>
    /// Decided on the resource level (GOV-025/027): Board, Page or Space evaluation, never by role
    /// alone. The workspace role only seeds the level (Owner → Owner, Admin/Member on an unplaced
    /// Workspace-visible resource → Editor, Admin in a Space → Manager, Guest → explicit access).
    /// </summary>
    LevelBased,
}

/// <summary>
/// The workspace-role baseline (WG-DEBT-002, docs/product/governance.md §45): one typed table from
/// (workspace role name, <see cref="PermissionAction"/>) to a <see cref="BaselineDecision"/>, replacing
/// string-literal role branches in <see cref="AccessPolicyEngine"/>. Every action must be classified
/// (the exhaustive test fails otherwise), so no action reaches "Owner only" by fall-through.
/// Explicit permission rules, resource permissions and Space evaluation compose on top of it in the
/// existing GOV-025 order and are not part of this table. Account-scoped decisions use
/// <see cref="AccountRoleBaseline"/>.
/// </summary>
public static class WorkspaceRoleBaseline
{
    private const BaselineDecision A = BaselineDecision.Allow;
    private const BaselineDecision D = BaselineDecision.Deny;
    private const BaselineDecision L = BaselineDecision.LevelBased;

    private readonly record struct Row(
        BaselineDecision Owner,
        BaselineDecision Admin,
        BaselineDecision Member,
        BaselineDecision Guest);

    private static readonly IReadOnlyDictionary<PermissionAction, Row> Rows = new Dictionary<PermissionAction, Row>
    {
        // Account-scoped actions are decided on the Account role; the workspace cells are the
        // workspace-request default (never requested against a workspace today).
        [PermissionAction.CreateWorkspace] = new(A, D, D, D),
        [PermissionAction.ManageAccount] = new(A, D, D, D),

        // Workspace shell and membership.
        [PermissionAction.ViewWorkspace] = new(A, A, A, A),
        // The member list is not guest-safe: a Guest reads only their own row (GET members/me,
        // ViewWorkspace) and names from the resources shared with them (WG-DEBT-002 item 6).
        [PermissionAction.ViewMembers] = new(A, A, A, D),
        [PermissionAction.InviteMember] = new(A, A, D, D),
        [PermissionAction.ChangeMemberRole] = new(A, D, D, D),
        [PermissionAction.RemoveMember] = new(A, A, D, D),
        [PermissionAction.ManageWorkspaceSettings] = new(A, A, D, D),
        // API tokens are credentials: issuing, listing and revoking them is security
        // configuration and stays Owner (GOV-013), never inherited from workspace settings.
        [PermissionAction.ManageApiTokens] = new(A, D, D, D),

        // Lifecycle and security stay Owner (GOV-013).
        [PermissionAction.ManageWorkspace] = new(A, D, D, D),
        [PermissionAction.ArchiveWorkspace] = new(A, D, D, D),
        [PermissionAction.RestoreWorkspace] = new(A, D, D, D),
        [PermissionAction.DeleteWorkspace] = new(A, D, D, D),

        // Organization and integrations (GOV-028, D15, M8).
        [PermissionAction.ManageSpaces] = new(A, A, D, D),
        [PermissionAction.ManageTeams] = new(A, A, D, D),
        [PermissionAction.ManageMemberProfile] = new(A, A, D, D),
        [PermissionAction.ManageIntegrations] = new(A, A, D, D),
        // Automation was decided by ManageWorkspaceSettings (Owner, Admin); the split keeps who may do what.
        [PermissionAction.ManageAutomation] = new(A, A, D, D),
        // Reading the audit trail is a tenant-admin right; billing is Owner only unless a role adds it (owner decision 2026-10-31).
        [PermissionAction.ReadAudit] = new(A, A, D, D),
        [PermissionAction.ViewBilling] = new(A, D, D, D),

        // Creating at the workspace root is a usage right; Guests are excluded. In a Space the
        // Space level decides (Editor on that Space).
        [PermissionAction.CreateBoard] = new(A, A, A, D),
        [PermissionAction.CreatePage] = new(A, A, A, D),

        // Board and Page vocabulary: decided on the resource level (GOV-025/027).
        [PermissionAction.ViewBoard] = new(A, L, L, L),
        [PermissionAction.ManageBoardPermission] = new(A, L, L, L),
        [PermissionAction.CreateBoardView] = new(A, L, L, L),
        [PermissionAction.UpdateBoardView] = new(A, L, L, L),
        [PermissionAction.ShareBoardView] = new(A, L, L, L),
        [PermissionAction.CreateField] = new(A, L, L, L),
        [PermissionAction.UpdateField] = new(A, L, L, L),
        [PermissionAction.DeleteField] = new(A, L, L, L),
        [PermissionAction.CreateItem] = new(A, L, L, L),
        [PermissionAction.UpdateItem] = new(A, L, L, L),
        [PermissionAction.MoveItem] = new(A, L, L, L),
        [PermissionAction.AssignItem] = new(A, L, L, L),
        [PermissionAction.ManageBoard] = new(A, L, L, L),
        [PermissionAction.ViewPage] = new(A, L, L, L),
        [PermissionAction.UpdatePage] = new(A, L, L, L),
        [PermissionAction.DeletePage] = new(A, L, L, L),
        [PermissionAction.ArchivePage] = new(A, L, L, L),
        [PermissionAction.SharePage] = new(A, L, L, L),
        [PermissionAction.ManagePagePermission] = new(A, L, L, L),

        // Commenting is decided on the target resource (board item / page); a Guest needs explicit
        // access evidence on it.
        [PermissionAction.CreateComment] = new(A, L, L, L),
    };

    /// <summary>The actions the table classifies (every <see cref="PermissionAction"/> must be one).</summary>
    public static IReadOnlyCollection<PermissionAction> ClassifiedActions => Rows.Keys.ToArray();

    /// <summary>
    /// The workspace roles the table classifies, by the role name carried in the access facts
    /// (Governance reads roles as facts and does not depend on the Workspaces Domain model).
    /// </summary>
    public static IReadOnlyList<string> RoleNames { get; } = ["Owner", "Admin", "Member", "Guest"];

    /// <summary>
    /// The baseline cell for a workspace role name and action; an unknown role is denied and an
    /// unclassified action throws.
    /// </summary>
    public static BaselineDecision Decide(string role, PermissionAction action)
    {
        if (!Rows.TryGetValue(action, out var row))
        {
            throw new InvalidOperationException($"PermissionAction '{action}' has no workspace role baseline row.");
        }

        return role switch
        {
            "Owner" => row.Owner,
            "Admin" => row.Admin,
            "Member" => row.Member,
            "Guest" => row.Guest,
            _ => BaselineDecision.Deny,
        };
    }

    /// <summary>
    /// Whether an Allow cell applies to a request against this resource kind. A default granted on
    /// the workspace never silently widens to another kind: the reads (ViewWorkspace, ViewMembers)
    /// and workspace settings (which target Automation, Analytics and Integrations resources) apply
    /// to any kind, ManageIntegrations also to the calendar integration and to Automation rules
    /// (a Webhook rule reaches the n8n integration), every other Allow cell only to the workspace resource itself. Outside that, the request is denied.
    /// </summary>
    public static bool AppliesToResourceKind(PermissionAction action, string? resourceKind) => action switch
    {
        PermissionAction.ViewWorkspace or PermissionAction.ViewMembers or PermissionAction.ManageWorkspaceSettings
            or PermissionAction.ManageAutomation or PermissionAction.ViewBilling => true,
        PermissionAction.ManageIntegrations =>
            resourceKind is "workspaces.workspace" or "integrations.calendar-integration" or "automation.rule",
        _ => resourceKind == "workspaces.workspace",
    };
}

/// <summary>
/// The Account-role baseline for Account-scoped requests (workspace roles do not apply): every
/// Account member may view a workspace; an Account Admin may also create workspaces and manage the
/// Account; an Account Owner passes earlier in the engine.
/// </summary>
public static class AccountRoleBaseline
{
    public static BaselineDecision Decide(string role, PermissionAction action) => action switch
    {
        PermissionAction.ViewWorkspace => BaselineDecision.Allow,
        PermissionAction.CreateWorkspace or PermissionAction.ManageAccount =>
            role is "Owner" or "Admin" ? BaselineDecision.Allow : BaselineDecision.Deny,
        _ => role == "Owner" ? BaselineDecision.Allow : BaselineDecision.Deny,
    };
}
