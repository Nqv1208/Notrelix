using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Comments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Comments.Ports;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;

/// <param name="After">Reads the comments that follow this one (oldest first); absent reads from the first.</param>
/// <param name="Limit">Reads at most this many; absent reads them all.</param>
public record GetCommentsQuery(ResourceKind ResourceKind, Guid ResourceId, TimeCursorPosition? After = null, int? Limit = null) : IQuery<Result<List<CommentDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public static GetCommentsQuery ForBoardItem(Guid boardItemId)
        => new(ResourceKind.Create(BoardItemKind), boardItemId);

    public static GetCommentsQuery ForPage(Guid pageId)
        => new(ResourceKind.Create(PageKind), pageId);

    private const string BoardItemKind = "work-management.board-item";
    private const string PageKind = "documents.page";

    // A page's comments are read under the page's own view action (ViewPage); a board item's under ViewBoard.
    public PermissionAction Action => ResourceKind.Value == PageKind ? PermissionAction.ViewPage : PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentUser _currentUser;
    private readonly ICommentTombstoneReader _tombstones;

    public GetCommentsQueryHandler(
        ICollaborationDbContext context,
        IActorLookupService actorLookup,
        ICurrentUser currentUser,
        ICommentTombstoneReader tombstones)
    {
        _context = context;
        _actorLookup = actorLookup;
        _currentUser = currentUser;
        _tombstones = tombstones;
    }

    public async Task<Result<List<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var thread = _context.Comments.AsNoTracking()
            .Where(c => c.Target.Kind == request.ResourceKind && c.Target.ResourceId == request.ResourceId && c.DeletedAt == null);
        if (request.After is { } after)
        {
            thread = thread.Where(c => c.CreatedAt > after.At || (c.CreatedAt == after.At && c.Id.CompareTo(after.Id) > 0));
        }

        var ordered = thread.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);
        var live = await (request.Limit is { } limit ? ordered.Take(limit) : ordered).ToListAsync(ct);

        // A deleted comment with live replies stays in the thread as a tombstone, so the replies keep their parent.
        // Each side is already cut at the same limit and cursor, so the merged first page is exact.
        var tombstones = await _tombstones.ListAsync(request.ResourceKind, request.ResourceId, request.After, request.Limit, ct);
        var rows = live.Concat(tombstones)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToList();
        if (request.Limit is { } take) rows = rows.Take(take).ToList();

        var commentKind = ResourceKind.Create("collaboration.comment");
        var liveIds = rows.Where(c => !c.IsDeleted).Select(c => c.Id).ToList();
        var reactions = await _context.Reactions.AsNoTracking()
            .Where(r => r.Target.Kind == commentKind && liveIds.Contains(r.Target.ResourceId))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);
        var reactionsByComment = reactions.ToLookup(r => r.Target.ResourceId);
        var me = _currentUser.UserId;

        var userIds = rows.Where(c => !c.IsDeleted && c.CreatedBy.HasValue).Select(c => c.CreatedBy!.Value)
            .Concat(reactions.Select(r => r.UserId)).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        var result = rows.Select(c =>
        {
            if (c.IsDeleted) return Tombstone(c);

            ActorSnapshot? actor = null;
            if (c.CreatedBy.HasValue)
                actorMap.TryGetValue(c.CreatedBy.Value, out actor);
            return new CommentDto(
                c.Id, actor?.UserId ?? Guid.Empty, actor?.Name ?? "Unknown", actor?.AvatarUrl,
                c.Content, c.ParentId, c.UpdatedAt != null,
                // The thread's status is the fact; resolving is a change to it, so the last change while resolved is when.
                c.CommentStatus == CommentStatus.Resolved ? (c.UpdatedAt ?? c.CreatedAt).UtcDateTime : null,
                c.CreatedAt.UtcDateTime,
                reactionsByComment[c.Id]
                    .GroupBy(r => r.Emoji.Code)
                    .Select(g => new ReactionSummaryDto(
                        g.Key, g.Count(), g.Any(r => r.UserId == me),
                        g.Select(r => new ReactionUserDto(r.UserId, actorMap.TryGetValue(r.UserId, out var who) ? who.Name : "Unknown")).ToList()))
                    .ToList(),
                // A comment without an anchor has no owned anchor row data at all.
                c.Anchor is { IsTextRange: true } anchor
                    ? new CommentAnchorDto(anchor.BlockId!.Value, anchor.Quote!, anchor.StartOffset!.Value, anchor.EndOffset!.Value)
                    : null,
                c.Version);
        }).ToList();

        return Result<List<CommentDto>>.Success(result);
    }

    /// <summary>What a deleted comment still shows: its id, its place in time and the deleted flag. No author, body, reaction or anchor.</summary>
    private static CommentDto Tombstone(Comment comment) =>
        new(comment.Id, Guid.Empty, string.Empty, null, string.Empty, null, false, null,
            comment.CreatedAt.UtcDateTime, [], null, comment.Version, IsDeleted: true);
}
