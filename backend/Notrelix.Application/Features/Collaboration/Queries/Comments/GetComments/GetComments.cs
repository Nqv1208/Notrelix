using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Comments.DTOs;

namespace Notrelix.Application.Features.Shared.Queries.Comments.GetComments;

public record GetCommentsQuery(string ResourceType, Guid ResourceId) : IRequest<Result<List<CommentDto>>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<List<CommentDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetCommentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var resourceType = Enum.Parse<ResourceType>(request.ResourceType, ignoreCase: true);

        var comments = await _context.Comments.AsNoTracking()
            .Where(c => c.Target.ResourceType == resourceType && c.Target.ResourceId == request.ResourceId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var userIds = comments.Select(c => c.CreatedBy).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var users = await _context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var result = comments.Select(c =>
        {
            var user = c.CreatedBy.HasValue && users.TryGetValue(c.CreatedBy.Value, out var u) ? u : null;
            return new CommentDto(
                c.Id, user?.Id ?? Guid.Empty, user?.Name ?? "Unknown", user?.AvatarUrl,
                c.Content, c.ParentId, c.UpdatedAt != null,
                null,
                c.CreatedAt.DateTime);
        }).ToList();

        return Result<List<CommentDto>>.Success(result);
    }
}
