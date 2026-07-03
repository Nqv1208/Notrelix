using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

public record CreateCommentCommand(ResourceType ResourceType, Guid ResourceId, string ContentMd, Guid? ParentCommentId) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentTenantContext _tenant;

    public CreateCommentCommandHandler(ICollaborationDbContext context, IResourceReferenceResolver resourceResolver, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, ICurrentTenantContext tenant)
    {
        _context = context;
        _resourceResolver = resourceResolver;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var workspaceId = await _resourceResolver.GetWorkspaceIdAsync(request.ResourceId, request.ResourceType.ToString(), ct)
            ?? throw new NotFoundException(request.ResourceType.ToString(), request.ResourceId);

        var target = ResourceRef.Create(request.ResourceType, request.ResourceId, workspaceId);
        var comment = Comment.Create(_tenant.RequireAccountId(), workspaceId, target, request.ContentMd, _currentUser.UserId, _dateTimeProvider.UtcNow, parentId: request.ParentCommentId);

        _context.Comments.Add(comment);
        return Result<Guid>.Success(comment.Id);
    }
}
