using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment;

public record DeleteAttachmentCommand(Guid AttachmentId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Attachment, AttachmentId);
}

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, ct);

        if (attachment is null)
            throw new NotFoundException(nameof(Attachment), request.AttachmentId);

        var now = _dateTimeProvider.UtcNow;
        attachment.Delete(_currentUser.UserId, now);
        return Result.Success();
    }
}
