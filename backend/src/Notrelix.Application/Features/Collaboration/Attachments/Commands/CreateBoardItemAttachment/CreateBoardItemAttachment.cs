using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Domain.Collaboration.Rules;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

public record CreateBoardItemAttachmentCommand(Guid BoardItemId, string Filename, string Url, long? SizeBytes, string? ContentType, string? Source) : ICommand<Result<AttachmentDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
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

    public CreateBoardItemAttachmentCommandHandler(
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

    public async Task<Result<AttachmentDto>> Handle(CreateBoardItemAttachmentCommand request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Result<AttachmentDto>.Failure("Attachment URL must be an absolute HTTP(S) URL.");

        var workspaceId = _requestContext.RequireWorkspaceId();

        var now = _dateTimeProvider.UtcNow;
        var boardItemKind = ResourceKind.Create("work-management.board-item");
        var target = ResourceRef.Create(boardItemKind, request.BoardItemId, workspaceId);

        // Links and files share one per-item cap, so a link takes the same short commit-point lock
        // as a file upload and recounts under it; otherwise links and files could jointly exceed it.
        await _targetLock.AcquireAsync(target, ct);
        var currentCount = await _context.Attachments
            .CountAsync(a => a.Target.Kind == boardItemKind && a.Target.ResourceId == request.BoardItemId, ct);
        AttachmentRules.EnsureMaxAttachments(currentCount, _options.MaxAttachmentsPerItem);

        var metadata = FileMetadata.Create(request.Filename, request.SizeBytes ?? 0, request.ContentType ?? "application/octet-stream", url: request.Url);
        var attachment = Attachment.Create(_requestContext.RequireAccountId(), workspaceId, target, AttachmentType.Link, metadata, _requestContext.UserId, now);

        _context.Attachments.Add(attachment);

        return Result<AttachmentDto>.Success(AttachmentDto.From(attachment));
    }
}
