using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

public record CreateCommentCommand(ResourceType ResourceType, Guid ResourceId, string ContentMd, Guid? ParentCommentId) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType, ResourceId);
}

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCommentCommandHandler(ICollaborationDbContext context, IResourceReferenceResolver resourceResolver, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _resourceResolver = resourceResolver;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var workspaceId = await _resourceResolver.GetWorkspaceIdAsync(request.ResourceId, request.ResourceType.ToString(), ct)
            ?? throw new NotFoundException(request.ResourceType.ToString(), request.ResourceId);

        var target = ResourceRef.Create(request.ResourceType, request.ResourceId, workspaceId);
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

            var parentContext = ParentCommentContext.Create(parentComment.Id, parentComment.Target);
            comment = Comment.CreateReply(accountId, workspaceId, target, request.ContentMd, userId, now, parentContext);
        }

        _context.Comments.Add(comment);
        return Result<Guid>.Success(comment.Id);
    }
}
