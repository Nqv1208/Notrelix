namespace Notrelix.Domain.Workspaces.Members;

public enum WorkspaceRole
{
    Guest,
    Member,
    Admin,
    Owner
}

public static class WorkspaceRoleExtensions
{
    /// <summary>An invitation with this role introduces a Guest, so the workspace setting AllowGuestInvites applies to it.</summary>
    public static bool IntroducesGuest(this WorkspaceRole role) => role == WorkspaceRole.Guest;
}
