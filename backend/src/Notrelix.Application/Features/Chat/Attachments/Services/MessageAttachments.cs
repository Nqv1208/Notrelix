using Notrelix.Application.Features.Chat.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Public.Attachments;

namespace Notrelix.Application.Features.Chat.Attachments.Services;

/// <summary>
/// The file list of messages (UC-CHT-G08): one Collaboration query serves a whole page of messages, so a history, thread or pin read
/// costs one attachment query, not one per message. A tombstone has none: deleting a message removes its files.
/// </summary>
public static class MessageAttachments
{
    public static string DownloadPath(Guid workspaceId, Guid messageId, Guid attachmentId) =>
        $"/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/attachments/{attachmentId}/content";

    public static string CommitPath(Guid workspaceId, Guid messageId, Guid uploadId) =>
        $"/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/attachments/uploads/{uploadId}/commit";

    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MessageAttachmentDto>>> LoadAsync(
        IChatMessageAttachments attachments, Guid workspaceId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken)
    {
        var ids = messageIds.Distinct().ToList();
        var facts = await attachments.ListAsync(ids, cancellationToken);
        return facts.ToDictionary(
            f => f.Key,
            f => (IReadOnlyList<MessageAttachmentDto>)f.Value.Select(a => ToDto(workspaceId, a)).ToList());
    }

    public static async Task<IReadOnlyList<MessageAttachmentDto>> ForMessageAsync(
        IChatMessageAttachments attachments, Guid workspaceId, Guid messageId, CancellationToken cancellationToken)
    {
        var map = await LoadAsync(attachments, workspaceId, [messageId], cancellationToken);
        return map.TryGetValue(messageId, out var list) ? list : [];
    }

    public static MessageAttachmentDto ToDto(Guid workspaceId, ChatMessageAttachmentFact fact) => new(
        fact.Id,
        fact.MessageId,
        fact.FileName,
        fact.SizeBytes,
        fact.ContentType,
        fact.UploadedBy,
        fact.CreatedAt,
        DownloadPath(workspaceId, fact.MessageId, fact.Id),
        fact.Width,
        fact.Height,
        fact.DurationMs,
        fact.Waveform?.Select(peak => (int)peak).ToArray());
}
