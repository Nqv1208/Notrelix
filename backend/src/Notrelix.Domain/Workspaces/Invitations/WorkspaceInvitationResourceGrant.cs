namespace Notrelix.Domain.Workspaces.Invitations;

/// <summary>
/// The explicit resource entry a resource invitation creates on acceptance
/// (WSP-026 / GOV-030). The level is Viewer by construction: a person outside the
/// workspace reads what was shared with them, so any higher level is rejected when
/// the invitation is created instead of being stored with no effect.
/// </summary>
public sealed record WorkspaceInvitationResourceGrant
{
    public const string BoardKind = "work-management.board";
    public const string PageKind = "documents.page";

    public string ResourceKind { get; }
    public Guid ResourceId { get; }
    public string Level { get; }

    /// <summary>
    /// Loads or builds a grant. It still accepts the levels an invitation could carry before the Viewer cap
    /// (a pending invitation stored earlier must stay readable); a NEW invitation goes through
    /// <see cref="ForNewInvitation"/>, which allows Viewer only.
    /// </summary>
    public WorkspaceInvitationResourceGrant(string resourceKind, Guid resourceId, string level)
    {
        if (resourceId == Guid.Empty)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_ResourceKindNotInvitable,
                "A resource invitation must name a resource.");

        if (resourceKind is not (BoardKind or PageKind))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_ResourceKindNotInvitable,
                "Only a Board or a Page can be shared through a resource invitation.");

        ResourceKind = resourceKind;
        ResourceId = resourceId;
        Level = CanonicalLevel(level);
    }

    public static IReadOnlyList<string> AllowedLevels { get; } = ["Viewer"];

    private static readonly string[] StoredLevels = ["Viewer", "Commenter", "Editor"];

    /// <summary>The grant of a new invitation: view-only (GOV-030).</summary>
    public static WorkspaceInvitationResourceGrant ForNewInvitation(string resourceKind, Guid resourceId, string level)
    {
        if (!AllowedLevels.Contains(level?.Trim(), StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_ResourceLevelAboveViewer,
                "A guest resource entry is view-only: the level must be Viewer.");
        return new WorkspaceInvitationResourceGrant(resourceKind, resourceId, level!);
    }

    private static string CanonicalLevel(string level)
    {
        var match = StoredLevels.FirstOrDefault(l => string.Equals(l, level?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_ResourceLevelAboveViewer,
                "A guest resource entry is view-only: the level must be Viewer.");
        return match;
    }
}
