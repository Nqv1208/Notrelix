using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Workspaces;

public sealed class WorkspaceSettings : ValueObject
{
    public bool AllowPublicSharing { get; }
    public bool EnforceMfa { get; }
    public bool AllowGuestInvites { get; }
    public WorkspaceRole DefaultMemberRole { get; }
    public int InvitationExpiryDays { get; }

    private WorkspaceSettings() { }
    private WorkspaceSettings(
        bool allowPublicSharing,
        bool enforceMfa,
        bool allowGuestInvites,
        WorkspaceRole defaultMemberRole,
        int invitationExpiryDays)
    {
        AllowPublicSharing = allowPublicSharing;
        EnforceMfa = enforceMfa;
        AllowGuestInvites = allowGuestInvites;
        DefaultMemberRole = defaultMemberRole;
        InvitationExpiryDays = invitationExpiryDays;
    }

    public static WorkspaceSettings Create(
        bool allowPublicSharing = false,
        bool enforceMfa = false,
        bool allowGuestInvites = false,
        WorkspaceRole defaultMemberRole = WorkspaceRole.Member,
        int invitationExpiryDays = 7)
    {
        if (defaultMemberRole is not WorkspaceRole.Guest and not WorkspaceRole.Member)
            throw new BusinessRuleException(
                BusinessRuleCodes.Common_DefaultMemberRoleMustBeGuestOrMember,
                "Default member role must be Guest or Member.");

        if (invitationExpiryDays is < 1 or > 30)
            throw new BusinessRuleException(
                BusinessRuleCodes.Common_InvitationExpiryDaysOutOfRange,
                "Invitation expiry days must be between 1 and 30.");

        return new WorkspaceSettings(
            allowPublicSharing,
            enforceMfa,
            allowGuestInvites,
            defaultMemberRole,
            invitationExpiryDays);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicSharing;
        yield return EnforceMfa;
        yield return AllowGuestInvites;
        yield return DefaultMemberRole;
        yield return InvitationExpiryDays;
    }
}
