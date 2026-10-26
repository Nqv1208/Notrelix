using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using global::Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments;

public record GetBoardItemAttachmentsQuery(Guid BoardItemId) : IQuery<Result<List<AttachmentDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class GetBoardItemAttachmentsQueryHandler : IRequestHandler<GetBoardItemAttachmentsQuery, Result<List<AttachmentDto>>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IActorLookupService _actorLookup;
    public GetBoardItemAttachmentsQueryHandler(ICollaborationDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<Result<List<AttachmentDto>>> Handle(GetBoardItemAttachmentsQuery request, CancellationToken ct)
    {
        // Resource existence and tenant ownership are established by
        // ExecutionContextBehavior before this handler runs.
        var result = await AttachmentTargetOperations.ListAsync(
            _context, _actorLookup, AttachmentTargets.BoardItem, request.BoardItemId, ct);
        return Result<List<AttachmentDto>>.Success(result);
    }
}
