namespace Notrelix.API.Contracts.Workspaces.Settings.Requests;

public sealed record UpdateWorkspaceSettingsRequest(
    bool AllowPublicSharing,
    bool EnforceMfa,
    bool AllowGuestInvites,
    string DefaultMemberRole,
    int InvitationExpiryDays,
    long ExpectedVersion
);
