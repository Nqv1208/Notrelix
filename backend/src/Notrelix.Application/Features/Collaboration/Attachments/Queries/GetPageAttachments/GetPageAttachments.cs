using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachments;

/// <summary>
/// Lists the attachments of a Documents page. Authorization is the page view permission: a page
/// the caller cannot see is not found, and only attachments whose target is exactly this page are returned.
/// </summary>
public record GetPageAttachmentsQuery(Guid PageId) : IQuery<Result<List<AttachmentDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(AttachmentTargets.Page, PageId);
}

public class GetPageAttachmentsQueryHandler : IRequestHandler<GetPageAttachmentsQuery, Result<List<AttachmentDto>>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IActorLookupService _actorLookup;

    public GetPageAttachmentsQueryHandler(ICollaborationDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<Result<List<AttachmentDto>>> Handle(GetPageAttachmentsQuery request, CancellationToken ct) =>
        Result<List<AttachmentDto>>.Success(await AttachmentTargetOperations.ListAsync(
            _context, _actorLookup, AttachmentTargets.Page, request.PageId, ct));
}
