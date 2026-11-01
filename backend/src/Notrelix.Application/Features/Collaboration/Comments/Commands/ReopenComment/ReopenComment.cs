using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment;

public record ReopenCommentCommand(Guid CommentId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    // A coarse workspace-member gate: who may change the comment is decided in the handler from its target.
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("collaboration.comment"), CommentId);
}

public class ReopenCommentCommandHandler : IRequestHandler<ReopenCommentCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICommentTargetAccess _access;
    private readonly IBoardRealtimeNotifier? _realtime;

    public ReopenCommentCommandHandler(ICollaborationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, ICommentTargetAccess access, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _access = access;
    }

    public async Task<Result> Handle(ReopenCommentCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(ReopenCommentCommand request, CancellationToken ct)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct);
        if (comment is null) throw CommentAccessRules.NotFound(request.CommentId);
        await CommentAccessRules.RequireThreadResolverAsync(comment, _currentUser.UserId, _context, _access, ct);
        comment.Reopen(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
