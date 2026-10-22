using System.Text.Json.Serialization;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Domain.Collaboration.Rules;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;

/// <summary>
/// Uploads a file as a board item attachment. Authorization is identical to registering a link
/// attachment (UpdateItem on the board item). The request carries the body as a forward-only
/// stream so Application stays free of transport types; the client-declared content type is not
/// part of the request on purpose: the stored content type is derived from the bytes.
/// </summary>
[IdempotencyOperation("collaboration.attachments.upload-board-item-attachment.v1")]
public record UploadBoardItemAttachmentCommand(
    Guid BoardItemId,
    string FileName,
    [property: JsonIgnore, IdempotencyFingerprintIgnore] Stream Content)
    : ICommand<Result<AttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UploadBoardItemAttachmentCommandValidator : AbstractValidator<UploadBoardItemAttachmentCommand>
{
    public UploadBoardItemAttachmentCommandValidator()
    {
        RuleFor(x => x.BoardItemId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.Content).NotNull();
    }
}

public class UploadBoardItemAttachmentCommandHandler : IRequestHandler<UploadBoardItemAttachmentCommand, Result<AttachmentDto>>
{
    private static readonly ResourceKind BoardItemKind = ResourceKind.Create("work-management.board-item");

    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IStorageService _storage;
    private readonly IRequestRollbackCompensations _compensations;
    private readonly AttachmentUploadOptions _options;

    public UploadBoardItemAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IStorageService storage,
        IRequestRollbackCompensations compensations,
        IOptions<AttachmentUploadOptions> options)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _storage = storage;
        _compensations = compensations;
        _options = options.Value;
    }

    public async Task<Result<AttachmentDto>> Handle(UploadBoardItemAttachmentCommand request, CancellationToken ct)
    {
        // Resource existence, tenant ownership and UpdateItem permission were established by
        // the pipeline before this handler runs; nothing below is reached by an unauthorized caller.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();

        var fileName = AttachmentFileName.Sanitize(request.FileName);
        if (AttachmentContentClassifier.IsBlockedExtension(fileName, _options.BlockedExtensions))
        {
            throw new UnsupportedMediaTypeException("Files of this type cannot be attached.");
        }

        // Fail fast, before any byte is written. The Domain factory re-checks with the same rule.
        var currentCount = await _context.Attachments
            .CountAsync(a => a.Target.Kind == BoardItemKind && a.Target.ResourceId == request.BoardItemId, ct);
        AttachmentRules.EnsureMaxAttachments(currentCount, _options.MaxAttachmentsPerItem);

        var head = new byte[AttachmentContentClassifier.SniffLength];
        var headLength = await request.Content.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
        if (headLength == 0)
        {
            AttachmentRules.EnsureValidFileSize(0, _options.MaxFileSizeBytes);
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
        _compensations.Register(
            $"delete attachment object {storageKey}",
            token => _storage.DeleteAsync(storageKey, token));

        using var body = new SizeLimitedReadStream(
            new PrefixedReadStream(head.AsMemory(0, headLength), request.Content),
            _options.MaxFileSizeBytes);
        await _storage.WriteAsync(storageKey, body, classified.ContentType, ct);

        var now = _dateTimeProvider.UtcNow;
        var target = ResourceRef.Create(BoardItemKind, request.BoardItemId, workspaceId);
        var attachment = Attachment.CreateStoredFile(
            accountId,
            workspaceId,
            target,
            classified.Type,
            fileName,
            body.BytesRead,
            classified.ContentType,
            storageKey,
            _options.MaxFileSizeBytes,
            currentCount,
            _options.MaxAttachmentsPerItem,
            _requestContext.UserId,
            now);

        _context.Attachments.Add(attachment);

        return Result<AttachmentDto>.Success(AttachmentDto.From(attachment));
    }
}
