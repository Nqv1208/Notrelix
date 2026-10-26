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
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IStorageService _storage;
    private readonly IRequestRollbackCompensations _compensations;
    private readonly IAttachmentTargetLock _targetLock;
    private readonly AttachmentUploadOptions _options;

    public UploadBoardItemAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IStorageService storage,
        IRequestRollbackCompensations compensations,
        IAttachmentTargetLock targetLock,
        IOptions<AttachmentUploadOptions> options)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _storage = storage;
        _compensations = compensations;
        _targetLock = targetLock;
        _options = options.Value;
    }

    public Task<Result<AttachmentDto>> Handle(UploadBoardItemAttachmentCommand request, CancellationToken ct) =>
        // Resource existence, tenant ownership and UpdateItem permission were established by
        // the pipeline before this handler runs; nothing below is reached by an unauthorized caller.
        AttachmentTargetOperations.UploadFileAsync(
            _context, _requestContext, _dateTimeProvider, _storage, _compensations, _targetLock, _options,
            AttachmentTargets.BoardItem, request.BoardItemId, request.FileName, request.Content, ct);
}
