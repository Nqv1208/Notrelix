using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Storage;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachmentContent;

/// <summary>
/// Opens the stored bytes of an attachment of a Documents page. Authorization is the page view
/// permission, and the attachment must belong to exactly this page: an attachment id of another
/// page or of a board item is reported as not found, so a foreign id referenced from a block
/// never leaks content.
/// </summary>
public record GetPageAttachmentContentQuery(Guid PageId, Guid AttachmentId)
    : IQuery<Result<AttachmentContentDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(AttachmentTargets.Page, PageId);
}

public class GetPageAttachmentContentQueryHandler : IRequestHandler<GetPageAttachmentContentQuery, Result<AttachmentContentDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IStorageService _storage;
    private readonly ILogger<GetPageAttachmentContentQueryHandler> _logger;

    public GetPageAttachmentContentQueryHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IStorageService storage,
        ILogger<GetPageAttachmentContentQueryHandler> logger)
    {
        _context = context;
        _requestContext = requestContext;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<AttachmentContentDto>> Handle(GetPageAttachmentContentQuery request, CancellationToken ct) =>
        Result<AttachmentContentDto>.Success(await AttachmentTargetOperations.OpenContentAsync(
            _context, _requestContext, _storage, _logger, AttachmentTargets.Page, request.PageId, request.AttachmentId, ct));
}
