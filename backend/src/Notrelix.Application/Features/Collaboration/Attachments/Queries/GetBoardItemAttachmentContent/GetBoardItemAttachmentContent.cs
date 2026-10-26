using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachmentContent;

/// <summary>
/// Opens the stored bytes of an uploaded attachment. Authorization is identical to reading the
/// attachment list of the item (ViewBoard on the board item): an item the caller cannot see is
/// not found, and the attachment must belong to that item.
/// </summary>
public record GetBoardItemAttachmentContentQuery(Guid BoardItemId, Guid AttachmentId)
    : IQuery<Result<AttachmentContentDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class GetBoardItemAttachmentContentQueryHandler : IRequestHandler<GetBoardItemAttachmentContentQuery, Result<AttachmentContentDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IStorageService _storage;
    private readonly ILogger<GetBoardItemAttachmentContentQueryHandler> _logger;

    public GetBoardItemAttachmentContentQueryHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IStorageService storage,
        ILogger<GetBoardItemAttachmentContentQueryHandler> logger)
    {
        _context = context;
        _requestContext = requestContext;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<AttachmentContentDto>> Handle(GetBoardItemAttachmentContentQuery request, CancellationToken ct)
    {
        var content = await AttachmentTargetOperations.OpenContentAsync(
            _context, _requestContext, _storage, _logger, AttachmentTargets.BoardItem, request.BoardItemId, request.AttachmentId, ct);
        return Result<AttachmentContentDto>.Success(content);
    }
}
