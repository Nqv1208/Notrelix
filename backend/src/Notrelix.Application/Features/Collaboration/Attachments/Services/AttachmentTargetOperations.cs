using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Abstractions;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Domain.Collaboration.Rules;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The target-independent part of the attachment use cases. Every per-target use case
/// (board item, page) authorizes through the pipeline for its own target first and then calls
/// these operations with that target; the operations scope every row access to the exact
/// target kind and id, so an attachment of another target is never listed, opened or deleted
/// through a route of a different target.
/// </summary>
internal static class AttachmentTargetOperations
{
    public static async Task<List<AttachmentDto>> ListAsync(
        ICollaborationDbContext context,
        IActorLookupService actorLookup,
        ResourceKind kind,
        Guid targetId,
        CancellationToken ct)
    {
        var attachments = await context.Attachments.AsNoTracking()
            .Where(attachment => attachment.Target.Kind == kind && attachment.Target.ResourceId == targetId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .ToListAsync(ct);

        var userIds = attachments.Where(a => a.CreatedBy.HasValue).Select(a => a.CreatedBy!.Value).Distinct().ToList();
        var actors = await actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        return attachments.Select(item =>
        {
            actorMap.TryGetValue(item.CreatedBy ?? Guid.Empty, out var actor);
            return AttachmentDto.From(item, actor?.Name);
        }).ToList();
    }

    public static async Task<AttachmentContentDto> OpenContentAsync(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IStorageService storage,
        ILogger logger,
        ResourceKind kind,
        Guid targetId,
        Guid attachmentId,
        CancellationToken ct)
    {
        var attachment = await context.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId
                && a.Target.Kind == kind
                && a.Target.ResourceId == targetId, ct);

        // A link has no stored content; both cases are reported exactly like an unknown attachment.
        if (attachment is null || !attachment.IsStoredFile)
        {
            throw new NotFoundException(nameof(Attachment), attachmentId);
        }

        var key = attachment.Metadata.StorageKey!;
        if (!AttachmentStorageKey.BelongsTo(key, requestContext.RequireAccountId(), requestContext.RequireWorkspaceId()))
        {
            logger.LogError("Attachment {AttachmentId} has a storage key outside its tenant prefix; refusing to open it.", attachment.Id);
            throw new NotFoundException(nameof(Attachment), attachmentId);
        }

        var content = await storage.OpenReadAsync(key, ct);
        if (content is null)
        {
            logger.LogError("Attachment {AttachmentId} row exists but its stored object is missing.", attachment.Id);
            throw new NotFoundException(nameof(Attachment), attachmentId);
        }

        return new AttachmentContentDto(
            content,
            attachment.Metadata.FileName,
            attachment.Metadata.ContentType,
            attachment.Metadata.Size);
    }

    public static async Task<Result<AttachmentDto>> CreateLinkAsync(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAttachmentTargetLock targetLock,
        AttachmentUploadOptions options,
        ResourceKind kind,
        Guid targetId,
        string filename,
        string url,
        long? sizeBytes,
        string? contentType,
        CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Result<AttachmentDto>.Failure("Attachment URL must be an absolute HTTP(S) URL.");

        var workspaceId = requestContext.RequireWorkspaceId();
        var now = dateTimeProvider.UtcNow;
        var target = ResourceRef.Create(kind, targetId, workspaceId);

        // Links and files share one per-target cap, so a link takes the same short commit-point lock
        // as a file upload and recounts under it; otherwise links and files could jointly exceed it.
        await targetLock.AcquireAsync(target, ct);
        var currentCount = await context.Attachments
            .CountAsync(a => a.Target.Kind == kind && a.Target.ResourceId == targetId, ct);
        AttachmentRules.EnsureMaxAttachments(currentCount, AttachmentTargets.MaxAttachments(kind, options));

        var metadata = FileMetadata.Create(filename, sizeBytes ?? 0, contentType ?? "application/octet-stream", url: url);
        var attachment = Attachment.Create(requestContext.RequireAccountId(), workspaceId, target, AttachmentType.Link, metadata, requestContext.UserId, now);

        context.Attachments.Add(attachment);

        return Result<AttachmentDto>.Success(AttachmentDto.From(attachment));
    }

    public static async Task<Result<AttachmentDto>> UploadFileAsync(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IStorageService storage,
        IRequestRollbackCompensations compensations,
        IAttachmentTargetLock targetLock,
        AttachmentUploadOptions options,
        ResourceKind kind,
        Guid targetId,
        string rawFileName,
        Stream body,
        CancellationToken ct)
    {
        var accountId = requestContext.RequireAccountId();
        var workspaceId = requestContext.RequireWorkspaceId();
        var maxAttachments = AttachmentTargets.MaxAttachments(kind, options);

        var fileName = AttachmentFileName.Sanitize(rawFileName);
        if (AttachmentContentClassifier.IsBlockedExtension(fileName, options.BlockedExtensions))
        {
            throw new UnsupportedMediaTypeException("Files of this type cannot be attached.");
        }

        // Fail fast, before any byte is written. This early check is advisory only: concurrent
        // uploads can all pass it, so the authoritative check is repeated under the target lock
        // right before the row is added (see below).
        var currentCount = await context.Attachments
            .CountAsync(a => a.Target.Kind == kind && a.Target.ResourceId == targetId, ct);
        AttachmentRules.EnsureMaxAttachments(currentCount, maxAttachments);

        var head = new byte[AttachmentContentClassifier.SniffLength];
        var headLength = await body.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
        if (headLength == 0)
        {
            AttachmentRules.EnsureValidFileSize(0, options.MaxFileSizeBytes);
        }

        var headSpan = head.AsSpan(0, headLength);
        if (AttachmentContentClassifier.LooksExecutable(headSpan))
        {
            throw new UnsupportedMediaTypeException("Files of this type cannot be attached.");
        }

        var classified = AttachmentContentClassifier.Classify(headSpan, fileName);
        var storageKey = AttachmentStorageKey.Create(accountId, workspaceId);

        // Register the compensation before the write so a partial or later-rolled-back object
        // is removed by the data session when the request fails before commit.
        compensations.Register(
            $"delete attachment object {storageKey}",
            token => storage.DeleteAsync(storageKey, token));

        using var limited = new SizeLimitedReadStream(
            new PrefixedReadStream(head.AsMemory(0, headLength), body),
            options.MaxFileSizeBytes);
        await storage.WriteAsync(storageKey, limited, classified.ContentType, ct);

        var now = dateTimeProvider.UtcNow;
        var target = ResourceRef.Create(kind, targetId, workspaceId);

        // Authoritative cap check at the commit point. The lock is taken only now, after the
        // (possibly minutes long) stream, and is held until the request transaction ends, so
        // concurrent uploads and links to this target serialize only over this short window.
        // The recount sees every row committed by earlier lock holders. When the cap is exceeded
        // the Domain factory throws; the compensation registered above then deletes the object
        // and no row remains.
        await targetLock.AcquireAsync(target, ct);
        currentCount = await context.Attachments
            .CountAsync(a => a.Target.Kind == kind && a.Target.ResourceId == targetId, ct);

        var attachment = Attachment.CreateStoredFile(
            accountId,
            workspaceId,
            target,
            classified.Type,
            fileName,
            limited.BytesRead,
            classified.ContentType,
            storageKey,
            options.MaxFileSizeBytes,
            currentCount,
            maxAttachments,
            requestContext.UserId,
            now);

        context.Attachments.Add(attachment);

        return Result<AttachmentDto>.Success(AttachmentDto.From(attachment));
    }
}
