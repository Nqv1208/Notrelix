using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;

public record GetCommentsQuery(ResourceKind ResourceKind, Guid ResourceId) : IQuery<Result<List<CommentDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public static GetCommentsQuery ForBoardItem(Guid boardItemId)
        => new(ResourceKind.Create(BoardItemKind), boardItemId);

    public static GetCommentsQuery ForPage(Guid pageId)
        => new(ResourceKind.Create(PageKind), pageId);

    private const string BoardItemKind = "work-management.board-item";
    private const string PageKind = "documents.page";

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IActorLookupService _actorLookup;
    public GetCommentsQueryHandler(ICollaborationDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<Result<List<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var comments = await _context.Comments.AsNoTracking()
            .Where(c => c.Target.Kind == request.ResourceKind && c.Target.ResourceId == request.ResourceId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var userIds = comments.Select(c => c.CreatedBy).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        var result = comments.Select(c =>
        {
            ActorSnapshot? actor = null;
            if (c.CreatedBy.HasValue)
                actorMap.TryGetValue(c.CreatedBy.Value, out actor);
            return new CommentDto(
                c.Id, actor?.UserId ?? Guid.Empty, actor?.Name ?? "Unknown", actor?.AvatarUrl,
                c.Content, c.ParentId, c.UpdatedAt != null,
                // The thread's status is the fact; resolving is a change to it, so the last change while resolved is when.
                c.CommentStatus == CommentStatus.Resolved ? (c.UpdatedAt ?? c.CreatedAt).DateTime : null,
                c.CreatedAt.DateTime,
                // A comment without an anchor has no owned anchor row data at all.
                c.Anchor is { IsTextRange: true } anchor
                    ? new CommentAnchorDto(anchor.BlockId!.Value, anchor.Quote!, anchor.StartOffset!.Value, anchor.EndOffset!.Value)
                    : null);
        }).ToList();

        return Result<List<CommentDto>>.Success(result);
    }
}
