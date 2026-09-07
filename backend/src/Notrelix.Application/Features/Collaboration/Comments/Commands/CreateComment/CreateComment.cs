using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

public record CreateCommentCommand(ResourceKind ResourceKind, Guid ResourceId, string ContentMd, Guid? ParentCommentId, IReadOnlyList<Guid>? MentionedUserIds = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public static CreateCommentCommand ForBoardItem(Guid boardItemId, string contentMd, Guid? parentCommentId, IReadOnlyList<Guid>? mentionedUserIds = null)
        => new(ResourceKind.Create(BoardItemKind), boardItemId, contentMd, parentCommentId, mentionedUserIds);

    public static CreateCommentCommand ForPage(Guid pageId, string contentMd, Guid? parentCommentId, IReadOnlyList<Guid>? mentionedUserIds = null)
        => new(ResourceKind.Create(PageKind), pageId, contentMd, parentCommentId, mentionedUserIds);

    private const string BoardItemKind = "work-management.board-item";
    private const string PageKind = "documents.page";

    public PermissionAction Action => PermissionAction.CreateComment;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCommentCommandHandler(ICollaborationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();

        var target = ResourceRef.Create(request.ResourceKind, request.ResourceId, workspaceId);
        var now = _dateTimeProvider.UtcNow;
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;

        Comment comment;
        if (request.ParentCommentId is null)
        {
            comment = Comment.Create(accountId, workspaceId, target, request.ContentMd, userId, now);
        }
        else
        {
            var parentComment = await _context.Comments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value && !c.IsDeleted, ct)
                ?? throw new NotFoundException(nameof(Comment), request.ParentCommentId.Value);

            var parentContext = ParentCommentContext.Create(parentComment.AccountId, parentComment.WorkspaceId, parentComment.Id, parentComment.Target, parentComment.IsDeleted);
            comment = Comment.CreateReply(accountId, workspaceId, target, request.ContentMd, userId, now, parentContext);
        }

        _context.Comments.Add(comment);

        foreach (var mentionedUserId in DistinctMentionedUsers(request.MentionedUserIds, userId))
        {
            var mention = Mention.Create(
                accountId,
                workspaceId,
                target,
                MentionType.User,
                mentionedUserId,
                userId,
                now);
            _context.PageMentions.Add(mention);
        }

        return Result<Guid>.Success(comment.Id);
    }

    private static IEnumerable<Guid> DistinctMentionedUsers(IReadOnlyList<Guid>? mentionedUserIds, Guid actorUserId)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return [];
        }

        return mentionedUserIds
            .Where(id => id != Guid.Empty && id != actorUserId)
            .Distinct();
    }
}
