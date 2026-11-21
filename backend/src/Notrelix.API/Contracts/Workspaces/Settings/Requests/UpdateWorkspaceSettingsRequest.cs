namespace Notrelix.API.Contracts.Workspaces.Settings.Requests;

public sealed record UpdateWorkspaceSettingsRequest(
    bool AllowPublicSharing,
    bool AllowGuestInvites,
    string DefaultMemberRole,
    int InvitationExpiryDays,
    long ExpectedVersion,
    int? GuestDefaultExpiryDays = null,
    string? Size = null,
    string? Language = null,
    string? TimeZone = null,
    string? WeekStart = null
);
