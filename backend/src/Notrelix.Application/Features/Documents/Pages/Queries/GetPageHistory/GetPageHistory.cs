using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPageHistory;

public record GetPageHistoryQuery(Guid PageId) : IQuery<Result<List<PageHistoryDto>>>;

public class GetPageHistoryQueryHandler : IRequestHandler<GetPageHistoryQuery, Result<List<PageHistoryDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetPageHistoryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageHistoryDto>>> Handle(GetPageHistoryQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var history = await _context.ActivityLogs.AsNoTracking()
            .Where(activity => activity.Target.ResourceType == ResourceType.Page && activity.Target.ResourceId == request.PageId)
            .OrderByDescending(activity => activity.Timestamp)
            .Select(activity => new PageHistoryDto(
                activity.Id,
                activity.ActorId,
                activity.Type.ToString(),
                null,
                activity.Timestamp.DateTime
            ))
            .ToListAsync(ct);

        return Result<List<PageHistoryDto>>.Success(history);
    }
}
