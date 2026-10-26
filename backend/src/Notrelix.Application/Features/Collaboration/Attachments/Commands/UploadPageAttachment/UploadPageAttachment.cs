using System.Text.Json.Serialization;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadPageAttachment;

/// <summary>
/// Uploads a file as a Documents page attachment (image and file blocks). Requires edit permission
/// on the page; the file policy (size cap, blocked extensions, sniffed content type) is the same as
/// for board item attachments.
/// </summary>
[IdempotencyOperation("collaboration.attachments.upload-page-attachment.v1")]
public record UploadPageAttachmentCommand(
    Guid PageId,
    string FileName,
    [property: JsonIgnore, IdempotencyFingerprintIgnore] Stream Content)
    : ICommand<Result<AttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(AttachmentTargets.Page, PageId);
}

public class UploadPageAttachmentCommandValidator : AbstractValidator<UploadPageAttachmentCommand>
{
    public UploadPageAttachmentCommandValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.Content).NotNull();
    }
}

public class UploadPageAttachmentCommandHandler : IRequestHandler<UploadPageAttachmentCommand, Result<AttachmentDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IStorageService _storage;
    private readonly IRequestRollbackCompensations _compensations;
    private readonly IAttachmentTargetLock _targetLock;
    private readonly AttachmentUploadOptions _options;

    public UploadPageAttachmentCommandHandler(
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

    public Task<Result<AttachmentDto>> Handle(UploadPageAttachmentCommand request, CancellationToken ct) =>
        AttachmentTargetOperations.UploadFileAsync(
            _context, _requestContext, _dateTimeProvider, _storage, _compensations, _targetLock, _options,
            AttachmentTargets.Page, request.PageId, request.FileName, request.Content, ct);
}
