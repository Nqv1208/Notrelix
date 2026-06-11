using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Activity.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Shared.Queries.Activity.GetResourceActivity;

public record GetResourceActivityQuery(string ResourceType, Guid ResourceId, int Page = 1, int PageSize = 20)
    : IRequest<Result<object>>;

public class GetResourceActivityQueryHandler : IRequestHandler<GetResourceActivityQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;
    public GetResourceActivityQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<object>> Handle(GetResourceActivityQuery request, CancellationToken ct)
    {
        var resourceType = Enum.Parse<ResourceType>(request.ResourceType, ignoreCase: true);

        var total = await _context.ActivityLogs
            .CountAsync(a => a.ResourceType == resourceType && a.ResourceId == request.ResourceId, ct);

        var logs = await _context.ActivityLogs.AsNoTracking()
            .Where(a => a.ResourceType == resourceType && a.ResourceId == request.ResourceId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(_context.Users.AsNoTracking(), a => a.ActorId, u => u.Id,
                (a, u) => new ActivityLogDto(
                    a.Id, a.ActorId, u.Name, a.Action,
                    a.ResourceType.ToString(), a.ResourceId,
                    a.ResourceTitle, a.CreatedAt
                ))
            .ToListAsync(ct);

        return Result<object>.Success(new { data = logs, total, page = request.Page, pageSize = request.PageSize });
    }
}
