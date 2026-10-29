using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.DeletePageAttachment;

/// <summary>
/// Removes an attachment of a Documents page. Its uploader or an editor of the page may delete it, and the
/// attachment must belong to exactly this page. The stored object is retained and purged by the
/// attachment retention job, not here.
/// </summary>
public record DeletePageAttachmentCommand(Guid PageId, Guid AttachmentId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    // A coarse gate; the uploader-or-editor rule is decided in the handler from the attachment's target.
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(AttachmentTargets.Page, PageId);
}

public class DeletePageAttachmentCommandHandler : IRequestHandler<DeletePageAttachmentCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICommentTargetAccess _access;

    public DeletePageAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ICommentTargetAccess access)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _access = access;
    }

    public async Task<Result> Handle(DeletePageAttachmentCommand request, CancellationToken ct)
    {
        var page = AttachmentTargets.Page;
        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId
                && a.Target.Kind == page
                && a.Target.ResourceId == request.PageId, ct);

        if (attachment is null)
            throw new NotFoundException(nameof(Attachment), request.AttachmentId);

        await AttachmentAccessRules.RequireDeleterAsync(attachment, _currentUser.UserId, _access, ct);
        attachment.Delete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
