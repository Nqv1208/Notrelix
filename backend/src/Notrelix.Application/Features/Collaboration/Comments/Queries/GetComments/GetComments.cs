using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;

public record GetCommentsQuery(ResourceType ResourceType, Guid ResourceId) : IQuery<Result<List<CommentDto>>>, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType, ResourceId);
}

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>
{
    private readonly ICollaborationDbContext _context;
    private readonly IActorLookupService _actorLookup;
    public GetCommentsQueryHandler(ICollaborationDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<Result<List<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var comments = await _context.Comments.AsNoTracking()
            .Where(c => c.Target.ResourceType == request.ResourceType && c.Target.ResourceId == request.ResourceId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var userIds = comments.Select(c => c.CreatedBy).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        var result = comments.Select(c =>
        {
            ActorSnapshot? actor = null;
            if (c.CreatedBy.HasValue)
                actorMap.TryGetValue(c.CreatedBy.Value, out actor);
            return new CommentDto(
                c.Id, actor?.UserId ?? Guid.Empty, actor?.Name ?? "Unknown", actor?.AvatarUrl,
                c.Content, c.ParentId, c.UpdatedAt != null,
                null,
                c.CreatedAt.DateTime);
        }).ToList();

        return Result<List<CommentDto>>.Success(result);
    }
}
