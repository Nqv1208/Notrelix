using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;

namespace Notrelix.Domain.Workspaces.Invitations;

public enum WorkspaceInvitationEntryKind
{
    Team,
    Space
}

/// <summary>
/// One initial Team or Space entry named by a Workspace invitation (WSP-026).
/// It grants nothing until the invitation is accepted; on acceptance every
/// entry is applied in the same transaction as the Workspace membership.
/// The role is stored as the canonical name of <see cref="TeamMemberRole"/>
/// (Team) or <see cref="SpaceRole"/> (Space).
/// </summary>
public sealed record WorkspaceInvitationInitialEntry
{
    public const int MaxEntries = 20;

    public WorkspaceInvitationEntryKind Kind { get; }
    public Guid TargetId { get; }
    public string Role { get; }

    public WorkspaceInvitationInitialEntry(WorkspaceInvitationEntryKind kind, Guid targetId, string role)
    {
        if (targetId == Guid.Empty)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_InitialEntryInvalid,
                "An initial entry must name a Team or Space.");

        var canonical = CanonicalRole(kind, role);
        Kind = kind;
        TargetId = targetId;
        Role = canonical;
    }

    /// <summary>Builds an entry from client strings; an unknown kind or role is a rule violation.</summary>
    public static WorkspaceInvitationInitialEntry Parse(string kind, Guid targetId, string role)
    {
        if (!Enum.TryParse<WorkspaceInvitationEntryKind>(kind, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_InitialEntryInvalid,
                "The initial entry has an unknown kind or role.");
        return new WorkspaceInvitationInitialEntry(parsed, targetId, role);
    }

    private static string CanonicalRole(WorkspaceInvitationEntryKind kind, string role)
    {
        var ok = false;
        string canonical = string.Empty;
        switch (kind)
        {
            case WorkspaceInvitationEntryKind.Team:
                ok = Enum.TryParse<TeamMemberRole>(role, true, out var teamRole) && Enum.IsDefined(teamRole);
                canonical = ok ? Enum.Parse<TeamMemberRole>(role, true).ToString() : string.Empty;
                break;
            case WorkspaceInvitationEntryKind.Space:
                ok = Enum.TryParse<SpaceRole>(role, true, out var spaceRole) && Enum.IsDefined(spaceRole);
                canonical = ok ? Enum.Parse<SpaceRole>(role, true).ToString() : string.Empty;
                break;
        }

        if (!ok)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Invitation_InitialEntryInvalid,
                "The initial entry has an unknown kind or role.");
        return canonical;
    }
}
