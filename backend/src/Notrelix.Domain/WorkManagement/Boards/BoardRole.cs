namespace Notrelix.Domain.WorkManagement.Boards;

public enum BoardRole
{
    Guest,
    Member,
    Observer,
    Admin,
    Owner
}

/// <summary>
/// A board Viewer holds a read-only role: Observer or Guest. A Viewer reads the Board and acts on nothing,
/// is not a member of it, is not counted or listed with its members, and is never offered as an assignee.
/// </summary>
public static class BoardRoles
{
    /// <summary>The read-only roles, for queries that filter by role.</summary>
    public static readonly BoardRole[] Viewers = [BoardRole.Guest, BoardRole.Observer];

    public static bool IsViewer(this BoardRole role) => role is BoardRole.Guest or BoardRole.Observer;
}
