using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

public record CreateBoardItemAttachmentCommand(Guid BoardItemId, string Filename, string Url, long? SizeBytes, string? ContentType, string? Source) : ICommand<Result<AttachmentDto>>, ITransactionalRequest;

public class CreateBoardItemAttachmentCommandHandler : IRequestHandler<CreateBoardItemAttachmentCommand, Result<AttachmentDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentTenantContext _tenant;

    public CreateBoardItemAttachmentCommandHandler(ICollaborationDbContext context, IResourceReferenceResolver resourceResolver, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, ICurrentTenantContext tenant)
    {
        _context = context;
        _resourceResolver = resourceResolver;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _tenant = tenant;
    }

    public async Task<Result<AttachmentDto>> Handle(CreateBoardItemAttachmentCommand request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return Result<AttachmentDto>.Failure("Attachment URL must be an absolute HTTP(S) URL.");

        var workspaceId = await _resourceResolver.GetWorkspaceIdAsync(request.BoardItemId, ResourceTypes.BoardItem, ct)
            ?? throw new NotFoundException("BoardItem", request.BoardItemId);

        var now = _dateTimeProvider.UtcNow;
        var target = ResourceRef.Create(ResourceType.BoardItem, request.BoardItemId, workspaceId);
        var metadata = FileMetadata.Create(request.Filename, request.SizeBytes ?? 0, request.ContentType ?? "application/octet-stream", url: request.Url);
        var attachment = Attachment.Create(_tenant.RequireAccountId(), workspaceId, target, AttachmentType.Link, metadata, _currentUser.UserId, now);

        _context.Attachments.Add(attachment);

        return Result<AttachmentDto>.Success(new AttachmentDto(
            attachment.Id,
            attachment.Target.ResourceId,
            attachment.Metadata.FileName,
            attachment.Metadata.Url ?? "",
            attachment.Metadata.Size,
            attachment.Metadata.ContentType,
            attachment.Type.ToString(),
            attachment.CreatedBy!.Value,
            null,
            attachment.CreatedAt.DateTime
        ));
    }
}
