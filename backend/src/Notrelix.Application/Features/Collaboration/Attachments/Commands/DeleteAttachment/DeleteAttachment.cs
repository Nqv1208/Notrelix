using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment;

public record DeleteAttachmentCommand(Guid BoardItemId, Guid AttachmentId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    // A coarse gate; the editor-only rule is decided in the handler from the attachment's target.
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("collaboration.attachment"), AttachmentId);
}

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICommentTargetAccess _access;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DeleteAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ICommentTargetAccess access,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _access = access;
    }

    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(DeleteAttachmentCommand request, CancellationToken ct)
    {
        // This use case is authorised as a board item attachment; an attachment of any other
        // target (a Documents page) is removed only through that target's own use case.
        var boardItem = AttachmentTargets.BoardItem;
        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.Target.Kind == boardItem && a.Target.ResourceId == request.BoardItemId, ct);

        if (attachment is null)
            throw AttachmentAccessRules.NotFound(request.Resource);

        var now = _dateTimeProvider.UtcNow;
        await AttachmentAccessRules.RequireDeleterAsync(attachment, request.Resource, _access, ct);
        attachment.Delete(_currentUser.UserId, now);
        return Result.Success();
    }
}
