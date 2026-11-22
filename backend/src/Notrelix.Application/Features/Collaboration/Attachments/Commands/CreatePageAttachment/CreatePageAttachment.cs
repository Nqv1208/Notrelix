using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreatePageAttachment;

/// <summary>
/// Registers a link attachment on a Documents page. Requires edit permission on the page. The size, content type and
/// source of a link are server-owned, so only the display name and the address are carried. Replayable with an Idempotency-Key.
/// </summary>
[IdempotencyOperation("collaboration.attachments.create-page-attachment.v1")]
public record CreatePageAttachmentCommand(Guid PageId, string Filename, string Url)
    : ICommand<Result<AttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(AttachmentTargets.Page, PageId);
}

public class CreatePageAttachmentCommandHandler : IRequestHandler<CreatePageAttachmentCommand, Result<AttachmentDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAttachmentTargetLock _targetLock;
    private readonly AttachmentUploadOptions _options;

    public CreatePageAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAttachmentTargetLock targetLock,
        IOptions<AttachmentUploadOptions> options)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _targetLock = targetLock;
        _options = options.Value;
    }

    public Task<Result<AttachmentDto>> Handle(CreatePageAttachmentCommand request, CancellationToken ct) =>
        AttachmentTargetOperations.CreateLinkAsync(
            _context, _requestContext, _dateTimeProvider, _targetLock, _options,
            AttachmentTargets.Page, request.PageId,
            request.Filename, request.Url, ct);
}
