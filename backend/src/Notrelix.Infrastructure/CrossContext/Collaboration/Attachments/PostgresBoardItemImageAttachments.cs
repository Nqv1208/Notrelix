using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.Attachments;

/// <summary>Collaboration-owned implementation of <see cref="IBoardItemImageAttachments"/>; runs under the caller's tenant.</summary>
public sealed class PostgresBoardItemImageAttachments(ICollaborationDbContext context) : IBoardItemImageAttachments
{
    public Task<bool> IsStoredImageOfItemAsync(Guid itemId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var kind = AttachmentTargets.BoardItem;
        return context.Attachments.AsNoTracking().AnyAsync(a =>
            a.Id == attachmentId
            && a.Target.Kind == kind && a.Target.ResourceId == itemId
            && a.Type == AttachmentType.Image
            && a.Metadata.StorageKey != null, cancellationToken);
    }
}
