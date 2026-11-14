using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Abstractions;
using global::Notrelix.Application.Features.Collaboration.Public.Attachments;
using global::Notrelix.Domain.Collaboration.Rules;
using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The two-step (pre-signed) transfer of a file on a Chat message. The create checks the declared facts against the shared policy and records
/// a pending upload; the commit reads the object that the client wrote and applies the same policy the streaming upload applies, to the bytes
/// that really arrived. Chat authorizes before it calls. Every refusal of the stored bytes deletes the object.
/// </summary>
public sealed class ChatMessageAttachmentTransfers(
    ICollaborationDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IStorageService storage,
    IPresignedObjectStorage presigned,
    IRequestRollbackCompensations compensations,
    IAttachmentTargetLock targetLock,
    IOptions<AttachmentUploadOptions> options,
    IOptions<PresignedUrlOptions> presignOptions) : IChatMessageAttachmentTransfers
{
    /// <summary>Leading bytes read at commit, the same window the streaming upload keeps for image sizes and audio durations.</summary>
    private const int HeaderCapacity = 64 * 1024;

    /// <summary>Image types an authorized download shows inline. Everything else downloads as an attachment, as the streaming endpoint does.</summary>
    private static readonly HashSet<string> InlineContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp",
    };

    private const int DurationMaxMs = FileMetadata.MaxDurationMs;

    public bool SupportsPresignedUrls => presigned.SupportsPresignedUrls;

    public async Task<ChatMessageAttachmentUploadTicket> CreateUploadAsync(
        Guid messageId, string fileName, long declaredSize, string declaredContentType, ChatMessageAudioUpload? audio, CancellationToken cancellationToken)
    {
        RequireSupported();
        var policy = options.Value;
        var accountId = requestContext.RequireAccountId();
        var workspaceId = requestContext.RequireWorkspaceId();
        var kind = AttachmentTargets.ChatMessage;
        var maxAttachments = AttachmentTargets.MaxAttachments(kind, policy);

        var sanitized = AttachmentFileName.Sanitize(fileName);
        if (AttachmentContentClassifier.IsBlockedExtension(sanitized, policy.BlockedExtensions))
            throw new UnsupportedMediaTypeException("Files of this type cannot be attached.");

        var ceiling = audio is null ? policy.MaxFileSizeBytes : Math.Min(policy.MaxFileSizeBytes, audio.MaxBytes);
        if (declaredSize > ceiling)
            throw new PayloadTooLargeException(ceiling);

        // Advisory fail-fast, as the streaming path does; the commit repeats the count under the target lock.
        var currentCount = await context.Attachments.CountAsync(a => a.Target.Kind == kind && a.Target.ResourceId == messageId, cancellationToken);
        AttachmentRules.EnsureMaxAttachments(currentCount, maxAttachments);

        var now = clock.UtcNow;
        var storageKey = AttachmentStorageKey.Create(accountId, workspaceId);
        var pending = PendingAttachmentUpload.Create(
            accountId,
            workspaceId,
            ResourceRef.Create(kind, messageId, workspaceId),
            storageKey,
            sanitized,
            declaredSize,
            string.IsNullOrWhiteSpace(declaredContentType) ? "application/octet-stream" : declaredContentType,
            ceiling,
            audio is not null,
            audio?.DurationHintMs,
            audio?.Waveform,
            requestContext.UserId,
            now,
            now.AddSeconds(policy.PendingUploadLifetimeSeconds));
        context.PendingAttachmentUploads.Add(pending);

        var put = await presigned.CreateUploadUrlAsync(
            storageKey, pending.DeclaredContentType, declaredSize, TimeSpan.FromSeconds(presignOptions.Value.UploadExpirySeconds), cancellationToken);
        return new ChatMessageAttachmentUploadTicket(pending.Id, put.Method, put.Url, put.ContentType, put.ContentLength, put.ExpiresAt);
    }

    public async Task<ChatMessageAttachmentCommit> CommitUploadAsync(Guid messageId, Guid uploadId, CancellationToken cancellationToken)
    {
        RequireSupported();
        var workspaceId = requestContext.RequireWorkspaceId();
        var accountId = requestContext.RequireAccountId();
        var kind = AttachmentTargets.ChatMessage;

        var pending = await context.PendingAttachmentUploads.FirstOrDefaultAsync(
            p => p.Id == uploadId && p.WorkspaceId == workspaceId && p.Target.Kind == kind && p.Target.ResourceId == messageId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(PendingAttachmentUpload), uploadId);

        // A repeated commit of a finished upload answers with its file and adds nothing.
        if (pending.IsCommitted)
            return await ReplayAsync(pending.StorageKey, pending.Voice, cancellationToken);

        var now = clock.UtcNow;
        if (pending.IsExpiredAt(now))
            throw new ConflictException("The upload window has closed. Start a new upload.", "attachment-upload-expired");

        var target = ResourceRef.Create(kind, messageId, workspaceId);
        await targetLock.AcquireAsync(target, cancellationToken);

        // Under the lock: another commit of the same key may have finished while this one waited.
        var key = pending.StorageKey;
        var existing = await context.Attachments.AsNoTracking().AnyAsync(a => a.Metadata.StorageKey == key, cancellationToken);
        if (existing)
            return await ReplayAsync(key, pending.Voice, cancellationToken);

        var head = await presigned.HeadAsync(key, cancellationToken)
            ?? throw new ConflictException("The file has not been uploaded.", "attachment-upload-missing");

        if (head.SizeBytes > pending.CeilingBytes)
        {
            await storage.DeleteAsync(key, cancellationToken);
            throw new PayloadTooLargeException(pending.CeilingBytes);
        }

        if (head.SizeBytes != pending.DeclaredSize)
        {
            await storage.DeleteAsync(key, cancellationToken);
            throw new ConflictException("The uploaded file does not match its declared size.", "attachment-upload-size-mismatch");
        }

        // The stored object is refused, and deleted, on every policy failure (size, executable, blocked type, voice facts).
        var verdict = await ReadPolicyFactsAsync(pending, key, cancellationToken);
        if (verdict.Refusal is not null)
        {
            await storage.DeleteAsync(key, cancellationToken);
            throw verdict.Refusal;
        }

        var currentCount = await context.Attachments.CountAsync(a => a.Target.Kind == kind && a.Target.ResourceId == messageId, cancellationToken);
        compensations.Register($"delete attachment object {key}", token => storage.DeleteAsync(key, token));

        var attachment = Attachment.CreateStoredFile(
            accountId,
            workspaceId,
            target,
            verdict.Type!.Value,
            pending.FileName,
            head.SizeBytes,
            verdict.ContentType,
            key,
            options.Value.MaxFileSizeBytes,
            currentCount,
            AttachmentTargets.MaxAttachments(kind, options.Value),
            requestContext.UserId,
            now,
            verdict.Width,
            verdict.Height,
            verdict.DurationMs,
            verdict.Waveform);
        context.Attachments.Add(attachment);
        pending.MarkCommitted(attachment.Id, now);

        return new ChatMessageAttachmentCommit(ToFact(attachment), pending.Voice, verdict.VerifiedDurationMs, Replayed: false);
    }

    public async Task<ChatMessageAttachmentDownload> CreateDownloadUrlAsync(Guid messageId, Guid attachmentId, CancellationToken cancellationToken)
    {
        RequireSupported();
        var kind = AttachmentTargets.ChatMessage;
        var accountId = requestContext.RequireAccountId();
        var workspaceId = requestContext.RequireWorkspaceId();

        var attachment = await context.Attachments.AsNoTracking().FirstOrDefaultAsync(
            a => a.Id == attachmentId && a.Target.Kind == kind && a.Target.ResourceId == messageId, cancellationToken);
        if (attachment is null || !attachment.IsStoredFile)
            throw new NotFoundException(nameof(Attachment), attachmentId);

        var key = attachment.Metadata.StorageKey!;
        if (!AttachmentStorageKey.BelongsTo(key, accountId, workspaceId))
            throw new NotFoundException(nameof(Attachment), attachmentId);

        var contentType = attachment.Metadata.ContentType;
        var url = await presigned.CreateDownloadUrlAsync(
            key,
            attachment.Metadata.FileName,
            contentType,
            InlineContentTypes.Contains(contentType),
            TimeSpan.FromSeconds(presignOptions.Value.DownloadExpirySeconds),
            cancellationToken);

        return new ChatMessageAttachmentDownload(
            attachment.Id, attachment.Metadata.FileName, contentType, attachment.Metadata.Size, url.Url, url.ExpiresAt);
    }

    private void RequireSupported()
    {
        if (!presigned.SupportsPresignedUrls)
            throw new NotSupportedException("The storage provider does not issue pre-signed URLs.");
    }

    private async Task<ChatMessageAttachmentCommit> ReplayAsync(string key, bool isVoice, CancellationToken cancellationToken)
    {
        var attachment = await context.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Metadata.StorageKey == key, cancellationToken)
            ?? throw new ConflictException("The upload is no longer available.", "attachment-upload-missing");
        return new ChatMessageAttachmentCommit(ToFact(attachment), isVoice, null, Replayed: true);
    }

    private static ChatMessageAttachmentFact ToFact(Attachment a) => new(
        a.Id,
        a.Target.ResourceId,
        a.Metadata.FileName,
        a.Metadata.Size,
        a.Metadata.ContentType,
        a.CreatedBy!.Value,
        a.CreatedAt,
        a.Metadata.Width,
        a.Metadata.Height,
        a.Metadata.DurationMs,
        a.Metadata.Waveform);

    private sealed record PolicyFacts(
        AttachmentType? Type,
        string ContentType,
        int? Width,
        int? Height,
        int? DurationMs,
        int? VerifiedDurationMs,
        byte[]? Waveform,
        Exception? Refusal);

    /// <summary>
    /// Reads the first bytes of the stored object and derives the same facts the streaming path derives: the sniffed type, the image size,
    /// the audio duration (from the header first, the client hint only as a fallback) and the waveform. Returns a refusal instead of
    /// throwing for a policy failure, so the caller can delete the object first.
    /// </summary>
    private async Task<PolicyFacts> ReadPolicyFactsAsync(PendingAttachmentUpload pending, string key, CancellationToken cancellationToken)
    {
        await using var stream = await storage.OpenReadAsync(key, cancellationToken)
            ?? throw new ConflictException("The file has not been uploaded.", "attachment-upload-missing");

        var header = new byte[HeaderCapacity];
        var length = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var span = header.AsSpan(0, length);

        if (length == 0)
            return Refused(new ConflictException("The uploaded file is empty.", "attachment-upload-empty"));
        if (AttachmentContentClassifier.LooksExecutable(span))
            return Refused(new UnsupportedMediaTypeException("Files of this type cannot be attached."));

        var classified = AttachmentContentClassifier.Classify(span, pending.FileName);
        if (AttachmentContentClassifier.IsBlockedExtension(pending.FileName, options.Value.BlockedExtensions))
            return Refused(new UnsupportedMediaTypeException("Files of this type cannot be attached."));

        var image = ImageDimensionReader.TryRead(span, classified.ContentType);
        var isAudio = classified.Type == AttachmentType.Audio;
        var verified = isAudio ? AudioMetadataReader.TryReadDurationMs(span, classified.ContentType) : null;
        var voice = isAudio && pending.Voice;
        var durationMs = voice
            ? Math.Min(verified ?? Math.Clamp(pending.DurationHintMs ?? 0, 0, DurationMaxMs), DurationMaxMs)
            : (int?)null;

        // Voice rules (one audio file, the duration and byte ceilings of a voice message) belong to Chat and are applied to the result.
        return new PolicyFacts(
            classified.Type,
            classified.ContentType,
            image?.Width,
            image?.Height,
            durationMs,
            verified,
            voice ? pending.Waveform : null,
            null);

        static PolicyFacts Refused(Exception refusal) => new(null, string.Empty, null, null, null, null, null, refusal);
    }
}
