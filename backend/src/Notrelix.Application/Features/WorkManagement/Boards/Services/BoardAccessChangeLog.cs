using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// Work Management's one place that reports a change of who can reach a Board: to Governance for the
/// sharing log (<see cref="IResourceAccessChangeRecorder"/>) and to the enterprise audit trail. Called by
/// the handlers that change Board members or visibility, inside the request transaction.
/// </summary>
public sealed class BoardAccessChangeLog
{
    private const string BoardKind = "work-management.board";

    private readonly IResourceAccessChangeRecorder _changes;
    private readonly IAuditService _audit;
    private readonly ICurrentRequestContext _requestContext;

    public BoardAccessChangeLog(IResourceAccessChangeRecorder changes, IAuditService audit, ICurrentRequestContext requestContext)
    {
        _changes = changes;
        _audit = audit;
        _requestContext = requestContext;
    }

    public async Task MemberAddedAsync(Guid boardId, Guid userId, BoardRole role, CancellationToken ct)
    {
        await _changes.RecordAsync(
            new ResourceAccessChangeRecord(BoardKind, boardId, AccessChangeKinds.Added, "User", userId, null, LevelOf(role)), ct);
        await AuditAsync("BoardMemberAdded", boardId, ct);
    }

    public async Task MemberRoleChangedAsync(Guid boardId, Guid userId, BoardRole from, BoardRole to, CancellationToken ct)
    {
        if (LevelOf(from) == LevelOf(to) && from == to)
        {
            return;
        }

        await _changes.RecordAsync(
            new ResourceAccessChangeRecord(BoardKind, boardId, AccessChangeKinds.Changed, "User", userId, LevelOf(from), LevelOf(to)), ct);
        await AuditAsync("BoardMemberRoleChanged", boardId, ct);
    }

    public async Task MemberRemovedAsync(Guid boardId, Guid userId, BoardRole role, CancellationToken ct)
    {
        await _changes.RecordAsync(
            new ResourceAccessChangeRecord(BoardKind, boardId, AccessChangeKinds.Removed, "User", userId, LevelOf(role), null), ct);
        await AuditAsync("BoardMemberRemoved", boardId, ct);
    }

    public async Task VisibilityChangedAsync(Guid boardId, BoardVisibility to, CancellationToken ct)
    {
        await _changes.RecordAsync(
            new ResourceAccessChangeRecord(
                BoardKind,
                boardId,
                to == BoardVisibility.Private ? AccessChangeKinds.MadePrivate : AccessChangeKinds.VisibilityChanged,
                null,
                null,
                null,
                null), ct);
        await AuditAsync("BoardVisibilityChanged", boardId, ct);
    }

    private static string LevelOf(BoardRole role) => AccessLevels.OfRank(BoardRoleLevels.RankOf(role.ToString()));

    private Task AuditAsync(string action, Guid boardId, CancellationToken ct) =>
        _audit.RecordAsync(
            _requestContext.RequireWorkspaceId(),
            _requestContext.UserId,
            action,
            ResourceRef.Create(ResourceKind.Create(BoardKind), boardId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: ct);
}
