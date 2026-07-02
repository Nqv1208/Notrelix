using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Attachments.DTOs;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments;

public record GetBoardItemAttachmentsQuery(Guid BoardItemId) : IQuery<Result<List<AttachmentDto>>>;

public class GetBoardItemAttachmentsQueryHandler : IRequestHandler<GetBoardItemAttachmentsQuery, Result<List<AttachmentDto>>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly IActorLookupService _actorLookup;
    public GetBoardItemAttachmentsQueryHandler(ICollaborationDbContext context, IResourceReferenceResolver resourceResolver, IActorLookupService actorLookup)
    {
        _context = context;
        _resourceResolver = resourceResolver;
        _actorLookup = actorLookup;
    }

    public async Task<Result<List<AttachmentDto>>> Handle(GetBoardItemAttachmentsQuery request, CancellationToken ct)
    {
        var boardItemExists = await _resourceResolver.ExistsAsync(request.BoardItemId, ResourceTypes.BoardItem, ct);
        if (!boardItemExists) throw new NotFoundException("BoardItem", request.BoardItemId);

        var attachments = await _context.Attachments.AsNoTracking()
            .Where(attachment => attachment.Target.ResourceType == ResourceType.BoardItem && attachment.Target.ResourceId == request.BoardItemId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .ToListAsync(ct);

        var userIds = attachments.Where(a => a.CreatedBy.HasValue).Select(a => a.CreatedBy!.Value).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        var result = attachments.Select(item =>
        {
            actorMap.TryGetValue(item.CreatedBy ?? Guid.Empty, out var actor);
            return new AttachmentDto(
                item.Id,
                item.Target.ResourceId,
                item.Metadata.FileName,
                item.Metadata.Url ?? "",
                item.Metadata.Size,
                item.Metadata.ContentType,
                item.Type.ToString(),
                item.CreatedBy!.Value,
                actor?.Name,
                item.CreatedAt.DateTime
            );
        }).ToList();

        return Result<List<AttachmentDto>>.Success(result);
    }
}
