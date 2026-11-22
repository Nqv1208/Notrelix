using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

/// <summary>
/// Registers a link on a board item. The size, content type and source of a link are server-owned (size 0,
/// <c>text/uri-list</c>, source Link), so the command carries only the display name and the address; whatever
/// a client sends for the others is dropped at the endpoint. Replayable with an Idempotency-Key.
/// </summary>
[IdempotencyOperation("collaboration.attachments.create-board-item-attachment.v1")]
public record CreateBoardItemAttachmentCommand(Guid BoardItemId, string Filename, string Url) : ICommand<Result<AttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class CreateBoardItemAttachmentCommandHandler : IRequestHandler<CreateBoardItemAttachmentCommand, Result<AttachmentDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAttachmentTargetLock _targetLock;
    private readonly AttachmentUploadOptions _options;
    private readonly IBoardRealtimeNotifier? _realtime;

    public CreateBoardItemAttachmentCommandHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAttachmentTargetLock targetLock,
        IOptions<AttachmentUploadOptions> options,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _targetLock = targetLock;
        _options = options.Value;
    }

    public async Task<Result<AttachmentDto>> Handle(CreateBoardItemAttachmentCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private Task<Result<AttachmentDto>> HandleCore(CreateBoardItemAttachmentCommand request, CancellationToken ct) =>
        AttachmentTargetOperations.CreateLinkAsync(
            _context, _requestContext, _dateTimeProvider, _targetLock, _options,
            AttachmentTargets.BoardItem, request.BoardItemId,
            request.Filename, request.Url, ct);
}
