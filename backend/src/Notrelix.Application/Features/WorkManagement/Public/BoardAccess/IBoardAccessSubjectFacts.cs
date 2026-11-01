namespace Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

/// <summary>
/// Work Management-published facts about one Board for access summaries: its
/// version and its members. Governance consumes it instead of reading Work
/// persistence; returns null for a Board that is missing, deleted or in
/// another Workspace. An archived Board is returned: it is read like any other
/// (GOV-025 step 1).
/// </summary>
public interface IBoardAccessSubjectFacts
{
    Task<BoardAccessSubject?> FindAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken);
}

/// <param name="Members">Each member with the board role, when they joined and the Board's visibility facts the sharing read needs.</param>
/// <param name="Visibility"><c>Private</c>, <c>Workspace</c> or <c>PublicLink</c>.</param>
public sealed record BoardAccessSubject(
    Guid BoardId,
    long Version,
    IReadOnlyList<Guid> MemberUserIds,
    IReadOnlyList<BoardMemberFact>? Members = null,
    string? Visibility = null);

/// <param name="Role">The Board role name: <c>Owner</c>, <c>Admin</c>, <c>Member</c>, <c>Observer</c> or <c>Guest</c>.</param>
public sealed record BoardMemberFact(Guid UserId, string Role, DateTimeOffset JoinedAt);
