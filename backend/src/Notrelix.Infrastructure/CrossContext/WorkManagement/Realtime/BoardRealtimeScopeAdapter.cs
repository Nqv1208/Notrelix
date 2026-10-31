using Notrelix.Infrastructure.Data;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ports.Realtime;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;

namespace Notrelix.Infrastructure.CrossContext.WorkManagement.Realtime;

/// <summary>
/// Work Management-owned implementation of the Board a changed resource lives on. Board children resolve
/// through the published Board-child authorization fact; approval requests, comments and attachments
/// carry a target, which resolves the same way when it is a Board item. One indexed lookup per request on
/// the request's data session; Board children resolve while soft-deleted (restore); a deleted approval request, comment or attachment is no longer found.
/// </summary>
public sealed class BoardRealtimeScopeAdapter : IBoardRealtimeScopeFacts
{
    private const string BoardItemKind = "work-management.board-item";

    private readonly IBoardChildAuthorizationFacts _boardChildren;
    private readonly IWorkManagementDbContext _work;
    private readonly ICollaborationDbContext _collaboration;
    private readonly ApplicationDbContext _session;

    public BoardRealtimeScopeAdapter(
        IBoardChildAuthorizationFacts boardChildren,
        IWorkManagementDbContext work,
        ICollaborationDbContext collaboration,
        ApplicationDbContext session)
    {
        _session = session;
        _boardChildren = boardChildren;
        _work = work;
        _collaboration = collaboration;
    }

    public bool HasPendingChanges() => _session.ChangeTracker.HasChanges();

    public async Task<Guid?> ResolveBoardIdAsync(string resourceKind, Guid resourceId, CancellationToken cancellationToken)
    {
        switch (resourceKind)
        {
            case "work-management.approval-request":
                return await ResolveTargetAsync(await _work.ApprovalRequests.AsNoTracking()
                    .Where(r => r.Id == resourceId)
                    .Select(r => new TargetRow(r.Target.Kind, r.Target.ResourceId))
                    .FirstOrDefaultAsync(cancellationToken), cancellationToken);
            case "collaboration.comment":
                return await ResolveTargetAsync(await _collaboration.Comments.AsNoTracking()
                    .Where(r => r.Id == resourceId)
                    .Select(r => new TargetRow(r.Target.Kind, r.Target.ResourceId))
                    .FirstOrDefaultAsync(cancellationToken), cancellationToken);
            case "collaboration.attachment":
                return await ResolveTargetAsync(await _collaboration.Attachments.AsNoTracking()
                    .Where(r => r.Id == resourceId)
                    .Select(r => new TargetRow(r.Target.Kind, r.Target.ResourceId))
                    .FirstOrDefaultAsync(cancellationToken), cancellationToken);
            default:
                return await ResolveBoardChildAsync(resourceKind, resourceId, cancellationToken);
        }
    }

    private Task<Guid?> ResolveTargetAsync(TargetRow? target, CancellationToken cancellationToken) =>
        target is { } row && row.Kind.Value == BoardItemKind
            ? ResolveBoardChildAsync(BoardItemKind, row.ResourceId, cancellationToken)
            : Task.FromResult<Guid?>(null);

    private async Task<Guid?> ResolveBoardChildAsync(string resourceKind, Guid resourceId, CancellationToken cancellationToken)
    {
        var scope = await _boardChildren.ResolveAsync(resourceKind, resourceId, cancellationToken);
        return scope.IsBoardChild ? scope.BoardId : null;
    }

    private sealed record TargetRow(ResourceKind Kind, Guid ResourceId);
}
