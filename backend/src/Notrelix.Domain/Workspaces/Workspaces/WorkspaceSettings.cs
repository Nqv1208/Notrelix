using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Workspaces;

public sealed class WorkspaceSettings : ValueObject
{
    public bool AllowPublicSharing { get; }
    public bool EnforceMfa { get; }
    public bool AllowGuestInvites { get; }
    public WorkspaceRole DefaultMemberRole { get; }
    public int InvitationExpiryDays { get; }

    /// <summary>
    /// Default expiry, in days, offered for new Guest resource entries (GOV-031, GOV-032). It only
    /// pre-fills new entries and never changes existing ones. Null means no default expiry.
    /// </summary>
    public int? GuestDefaultExpiryDays { get; }

    /// <summary>
    /// The instant <see cref="AllowPublicSharing"/> last changed value, set by the Workspace when the
    /// flag flips (GOV-032 "links off since"). Null when it never changed: a Workspace is created with
    /// links off, so the creation instant is then the answer. It is a fact about the policy history,
    /// not an input, so it is not part of settings equality.
    /// </summary>
    public DateTimeOffset? PublicSharingChangedAt { get; }

    /// <summary>The expiry defaults a Workspace may offer for a new Guest entry.</summary>
    public static readonly IReadOnlyList<int> AllowedGuestDefaultExpiryDays = [30, 90, 180];

    private WorkspaceSettings() { }
    private WorkspaceSettings(
        bool allowPublicSharing,
        bool enforceMfa,
        bool allowGuestInvites,
        WorkspaceRole defaultMemberRole,
        int invitationExpiryDays,
        int? guestDefaultExpiryDays,
        DateTimeOffset? publicSharingChangedAt)
    {
        GuestDefaultExpiryDays = guestDefaultExpiryDays;
        PublicSharingChangedAt = publicSharingChangedAt;
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
        int invitationExpiryDays = 7,
        int? guestDefaultExpiryDays = null)
    {
        if (defaultMemberRole is not WorkspaceRole.Guest and not WorkspaceRole.Member)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Settings_DefaultMemberRoleMustBeGuestOrMember,
                "Default member role must be Guest or Member.");

        if (invitationExpiryDays is < 1 or > 30)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Settings_InvitationExpiryDaysOutOfRange,
                "Invitation expiry days must be between 1 and 30.");

        if (guestDefaultExpiryDays is { } days && !AllowedGuestDefaultExpiryDays.Contains(days))
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Settings_GuestDefaultExpiryDaysInvalid,
                "Guest default expiry must be 30, 90 or 180 days, or none.");

        return new WorkspaceSettings(
            allowPublicSharing,
            enforceMfa,
            allowGuestInvites,
            defaultMemberRole,
            invitationExpiryDays,
            guestDefaultExpiryDays,
            publicSharingChangedAt: null);
    }

    /// <summary>The same settings with the recorded instant of the last sharing-policy change.</summary>
    internal WorkspaceSettings WithPublicSharingChangedAt(DateTimeOffset? changedAt) =>
        new(AllowPublicSharing, EnforceMfa, AllowGuestInvites, DefaultMemberRole, InvitationExpiryDays, GuestDefaultExpiryDays, changedAt);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicSharing;
        yield return EnforceMfa;
        yield return AllowGuestInvites;
        yield return DefaultMemberRole;
        yield return InvitationExpiryDays;
        yield return GuestDefaultExpiryDays;
    }
}
