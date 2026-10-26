using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Notrelix.Application.Features.Collaboration.Notifications;
using Notrelix.Application.Features.Documents.Public.PageBlocks;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

public record CreateCommentCommand(ResourceKind ResourceKind, Guid ResourceId, string ContentMd, Guid? ParentCommentId, IReadOnlyList<Guid>? MentionedUserIds = null, CommentAnchorInput? Anchor = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public static CreateCommentCommand ForBoardItem(Guid boardItemId, string contentMd, Guid? parentCommentId, IReadOnlyList<Guid>? mentionedUserIds = null, CommentAnchorInput? anchor = null)
        => new(ResourceKind.Create(BoardItemKind), boardItemId, contentMd, parentCommentId, mentionedUserIds, anchor);

    public static CreateCommentCommand ForPage(Guid pageId, string contentMd, Guid? parentCommentId, IReadOnlyList<Guid>? mentionedUserIds = null, CommentAnchorInput? anchor = null)
        => new(ResourceKind.Create(PageKind), pageId, contentMd, parentCommentId, mentionedUserIds, anchor);

    private const string BoardItemKind = "work-management.board-item";
    private const string PageKind = "documents.page";

    public bool IsPageComment => ResourceKind.Value == PageKind;

    public PermissionAction Action => PermissionAction.CreateComment;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPageBlockFacts _pageBlocks;

    public CreateCommentCommandHandler(ICollaborationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IPageBlockFacts pageBlocks)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _pageBlocks = pageBlocks;
    }

    public async Task<Result<Guid>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();

        var target = ResourceRef.Create(request.ResourceKind, request.ResourceId, workspaceId);
        var now = _dateTimeProvider.UtcNow;
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;

        var anchor = await ResolveAnchorAsync(request, workspaceId, ct);

        Comment comment;
        if (request.ParentCommentId is null)
        {
            comment = Comment.Create(accountId, workspaceId, target, request.ContentMd, userId, now, anchor);
        }
        else
        {
            var parentComment = await _context.Comments
                .AsNoTracking()
                // DeletedAt is the mapped soft-delete column on Comment
                // (IsDeleted is a Domain-only flag ignored in EF mapping).
                .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value && c.DeletedAt == null, ct)
                ?? throw new NotFoundException(nameof(Comment), request.ParentCommentId.Value);

            var parentContext = ParentCommentContext.Create(parentComment.AccountId, parentComment.WorkspaceId, parentComment.Id, parentComment.Target, parentComment.IsDeleted);
            comment = Comment.CreateReply(accountId, workspaceId, target, request.ContentMd, userId, now, parentContext);
        }

        _context.Comments.Add(comment);

        var excerpt = NotificationExcerpt.From(request.ContentMd);
        foreach (var mentionedUserId in DistinctMentionedUsers(request.MentionedUserIds))
        {
            var mention = Mention.Create(
                accountId,
                workspaceId,
                target,
                MentionType.User,
                mentionedUserId,
                userId,
                now,
                excerpt);
            _context.PageMentions.Add(mention);
        }

        return Result<Guid>.Success(comment.Id);
    }

    /// <summary>
    /// A text anchor belongs to a top-level comment on a page and names a live block of that page. The page itself
    /// was authorized by the pipeline; the block is a Documents fact, asked through its published port.
    /// </summary>
    private async Task<CommentAnchor?> ResolveAnchorAsync(CreateCommentCommand request, Guid workspaceId, CancellationToken ct)
    {
        if (request.Anchor is null)
        {
            return null;
        }

        if (!request.IsPageComment)
        {
            throw AnchorRejected("Only a comment on a page can be anchored to text.");
        }

        if (request.ParentCommentId is not null)
        {
            throw AnchorRejected("A reply is not anchored; it follows its thread.");
        }

        var anchor = CommentAnchor.ForTextRange(request.Anchor.BlockId, request.Anchor.Quote, request.Anchor.Start, request.Anchor.End);

        if (!await _pageBlocks.BlockBelongsToPageAsync(workspaceId, request.ResourceId, anchor.BlockId!.Value, ct))
        {
            throw AnchorRejected("The anchored block does not belong to the page.");
        }

        return anchor;
    }

    private static Notrelix.Domain.Common.Exceptions.BusinessRuleException AnchorRejected(string message) =>
        new(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Comment_AnchorInvalid, message);

    private static IEnumerable<Guid> DistinctMentionedUsers(IReadOnlyList<Guid>? mentionedUserIds)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return [];
        }

        return mentionedUserIds
            .Where(id => id != Guid.Empty)
            .Distinct();
    }
}
