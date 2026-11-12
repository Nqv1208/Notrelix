using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Abstractions;
using global::Notrelix.Application.Features.Collaboration.Public.Attachments;
using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The Collaboration side of the files on a Chat message (CHT-012): the same storage path, file policy, per-target lock and row as the board
/// item and page attachments, under the target kind <c>chat.message</c>. Chat authorizes before it calls.
/// </summary>
public sealed class ChatMessageAttachments(
    ICollaborationDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IStorageService storage,
    IRequestRollbackCompensations compensations,
    IAttachmentTargetLock targetLock,
    IOptions<AttachmentUploadOptions> options,
    ILogger<ChatMessageAttachments> logger) : IChatMessageAttachments
{
    public async Task<ChatMessageAttachmentFact> UploadAsync(Guid messageId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var result = await AttachmentTargetOperations.UploadFileAsync(
            context, requestContext, clock, storage, compensations, targetLock, options.Value,
            AttachmentTargets.ChatMessage, messageId, fileName, content, cancellationToken);

        var dto = result.Data!;
        return new ChatMessageAttachmentFact(
            dto.Id, messageId, dto.Filename, dto.SizeBytes, dto.ContentType, dto.UploadedBy, new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ChatMessageAttachmentFact>>> ListAsync(
        IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<ChatMessageAttachmentFact>>();

        var kind = AttachmentTargets.ChatMessage;
        var ids = messageIds.Distinct().ToList();
        var rows = await context.Attachments.AsNoTracking()
            .Where(a => a.Target.Kind == kind && ids.Contains(a.Target.ResourceId) && a.Metadata.StorageKey != null)
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(a => a.Target.ResourceId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ChatMessageAttachmentFact>)g
                    .Select(a => new ChatMessageAttachmentFact(
                        a.Id, a.Target.ResourceId, a.Metadata.FileName, a.Metadata.Size, a.Metadata.ContentType, a.CreatedBy!.Value, a.CreatedAt))
                    .ToList());
    }

    public async Task<ChatMessageAttachmentContent> OpenAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var content = await AttachmentTargetOperations.OpenContentAsync(
            context, requestContext, storage, logger, AttachmentTargets.ChatMessage, messageId, attachmentId, cancellationToken);
        return new ChatMessageAttachmentContent(content.Content, content.FileName, content.ContentType, content.SizeBytes);
    }

    public async Task<bool> RemoveAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var kind = AttachmentTargets.ChatMessage;
        var attachment = await context.Attachments.FirstOrDefaultAsync(
            a => a.Id == attachmentId && a.Target.Kind == kind && a.Target.ResourceId == messageId, cancellationToken);
        if (attachment is null)
            return false;

        attachment.Delete(requestContext.UserId, clock.UtcNow);
        return true;
    }

    public async Task<int> RemoveAllAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var kind = AttachmentTargets.ChatMessage;
        var attachments = await context.Attachments
            .Where(a => a.Target.Kind == kind && a.Target.ResourceId == messageId)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        foreach (var attachment in attachments)
            attachment.Delete(requestContext.UserId, now);
        return attachments.Count;
    }
}
