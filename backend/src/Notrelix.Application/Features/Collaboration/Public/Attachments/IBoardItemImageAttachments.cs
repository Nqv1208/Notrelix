namespace Notrelix.Application.Features.Collaboration.Public.Attachments;

/// <summary>
/// Collaboration-owned fact about the attachments of a board item, for the Item cover. Work Management
/// keeps only the attachment id; this boundary says whether that id is a stored image of the Item, so
/// Work never reads Collaboration persistence.
/// </summary>
public interface IBoardItemImageAttachments
{
    Task<bool> IsStoredImageOfItemAsync(Guid itemId, Guid attachmentId, CancellationToken cancellationToken);
}
