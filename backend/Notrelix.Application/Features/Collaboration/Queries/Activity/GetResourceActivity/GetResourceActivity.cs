using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Shared.Activity.DTOs;

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
            .CountAsync(a => a.Target.ResourceType == resourceType && a.Target.ResourceId == request.ResourceId, ct);

        var logs = await _context.ActivityLogs.AsNoTracking()
            .Where(a => a.Target.ResourceType == resourceType && a.Target.ResourceId == request.ResourceId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(_context.Users.AsNoTracking(), a => a.ActorId, u => u.Id,
                (a, u) => new ActivityLogDto(
                    a.Id, a.ActorId, u.Name, a.Type.ToString(),
                    a.Target.ResourceType.ToString(), a.Target.ResourceId,
                    null, a.Timestamp.DateTime
                ))
            .ToListAsync(ct);

        return Result<object>.Success(new { data = logs, total, page = request.Page, pageSize = request.PageSize });
    }
}
