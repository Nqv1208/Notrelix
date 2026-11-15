using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Activity.DTOs;
using global::Notrelix.Application.Features.Collaboration.Activity.Ports;
using global::Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;

namespace Notrelix.Application.Features.Collaboration.Activity.Queries.GetPageActivity;

/// <summary>
/// The short feed of who renamed, moved, trashed or restored a page (product rule DCT-031). Anyone who may
/// view the page reads it; an unreadable page is the same NotFound as any other page read.
/// </summary>
public record GetPageActivityQuery(Guid PageId, int Page = 1, int PageSize = 20)
    : IQuery<Result<ResourceActivityPageDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create(PageActivityTypes.ResourceKind), PageId);
}

public class GetPageActivityQueryHandler : IRequestHandler<GetPageActivityQuery, Result<ResourceActivityPageDto>>
{
    private readonly GetResourceActivityQueryHandler _feed;

    public GetPageActivityQueryHandler(
        IResourceActivityReader reader, IActorLookupService actorLookup, ICurrentRequestContext requestContext) =>
        _feed = new GetResourceActivityQueryHandler(reader, actorLookup, requestContext);

    // The page permission was checked on this query; the shared handler only pages and words the rows.
    public Task<Result<ResourceActivityPageDto>> Handle(GetPageActivityQuery request, CancellationToken ct) =>
        _feed.Handle(
            new GetResourceActivityQuery(ResourceKind.Create(PageActivityTypes.ResourceKind), request.PageId, request.Page, request.PageSize),
            ct);
}
