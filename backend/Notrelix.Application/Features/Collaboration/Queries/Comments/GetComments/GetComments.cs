using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Comments.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

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
            .Where(c => c.ResourceType == resourceType && c.ResourceId == request.ResourceId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Join(_context.Users.AsNoTracking(), c => c.UserId, u => u.Id,
                (c, u) => new CommentDto(
                    c.Id, c.UserId, u.Name, u.AvatarUrl,
                    c.ContentMd, c.ParentCommentId, c.IsEdited,
                    c.ResolvedAt, c.CreatedAt
                ))
            .ToListAsync(ct);

        return Result<List<CommentDto>>.Success(comments);
    }
}
