namespace Notrelix.Domain.Workspaces.Invitations;

/// <summary>
/// The explicit resource entry a resource invitation creates on acceptance
/// (WSP-026 / GOV-030). The level is capped at Editor by construction: a
/// Guest never holds Manager, so a Manager-or-higher level is rejected when
/// the invitation is created instead of being stored with no effect.
/// </summary>
public sealed record WorkspaceInvitationResourceGrant
{
    public const string BoardKind = "work-management.board";
    public const string PageKind = "documents.page";

    public string ResourceKind { get; }
    public Guid ResourceId { get; }
    public string Level { get; }

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

    public static IReadOnlyList<string> AllowedLevels { get; } = ["Viewer", "Commenter", "Editor"];

    private static string CanonicalLevel(string level)
    {
        var match = AllowedLevels.FirstOrDefault(l => string.Equals(l, level?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_ResourceLevelAboveEditor,
                "A guest resource entry is capped at Editor: choose Viewer, Commenter or Editor.");
        return match;
    }
}
