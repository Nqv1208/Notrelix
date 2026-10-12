namespace Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

/// <summary>
/// Work Management-published facts about one active Board for access
/// summaries: its version and its members. Governance consumes it instead of
/// reading Work persistence; returns null for a Board that is missing,
/// archived, deleted or in another Workspace.
/// </summary>
public interface IBoardAccessSubjectFacts
{
    Task<BoardAccessSubject?> FindAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken);
}

public sealed record BoardAccessSubject(Guid BoardId, long Version, IReadOnlyList<Guid> MemberUserIds);
