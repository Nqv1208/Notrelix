using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

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
        var comment = Comment.Create(_requestContext.RequireAccountId(), workspaceId, target, request.ContentMd, _requestContext.UserId, _dateTimeProvider.UtcNow, parentId: request.ParentCommentId);

        _context.Comments.Add(comment);
        return Result<Guid>.Success(comment.Id);
    }
}
