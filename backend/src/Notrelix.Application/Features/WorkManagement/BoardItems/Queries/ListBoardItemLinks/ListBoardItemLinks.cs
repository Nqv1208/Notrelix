using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemLinks;

public record BoardItemLinkDto(Guid Id, Guid SourceItemId, Guid TargetItemId, Guid TargetBoardId, string? LinkType);

public record ListBoardItemLinksQuery(Guid BoardItemId) : IQuery<Result<List<BoardItemLinkDto>>>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class ListBoardItemLinksQueryHandler : IRequestHandler<ListBoardItemLinksQuery, Result<List<BoardItemLinkDto>>>
{
    private readonly IWorkManagementDbContext _context;

    public ListBoardItemLinksQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BoardItemLinkDto>>> Handle(ListBoardItemLinksQuery request, CancellationToken ct)
    {
        var links = await _context.BoardItemLinks
            .AsNoTracking()
            .Where(l => l.SourceItemId == request.BoardItemId)
            .Select(l => new BoardItemLinkDto(
                l.Id,
                l.SourceItemId,
                l.Target.ResourceId,
                l.Target.Kind == ResourceKind.Create("work-management.board") ? l.Target.ResourceId : Guid.Empty,
                l.LinkType.ToString()))
            .ToListAsync(ct);

        return Result<List<BoardItemLinkDto>>.Success(links);
    }
}
