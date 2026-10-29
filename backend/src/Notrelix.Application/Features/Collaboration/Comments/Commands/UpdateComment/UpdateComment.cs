using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

public record UpdateCommentCommand(Guid CommentId, string ContentMd) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    // A coarse workspace-member gate: who may change the comment is decided in the handler from its target.
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("collaboration.comment"), CommentId);
}

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICommentTargetAccess _access;
    public UpdateCommentCommandHandler(ICollaborationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, ICommentTargetAccess access)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _access = access;
    }

    public async Task<Result> Handle(UpdateCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw new NotFoundException(nameof(Comment), request.CommentId);
        await CommentAccessRules.RequireAuthorAsync(comment, _currentUser.UserId, _access, ct);
        comment.UpdateContent(request.ContentMd, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
