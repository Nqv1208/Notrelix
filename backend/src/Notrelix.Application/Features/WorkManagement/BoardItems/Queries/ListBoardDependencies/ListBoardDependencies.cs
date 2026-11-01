using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardDependencies;

/// <summary>One dependency of the board: <c>To</c> waits for <c>From</c> (finish to start).</summary>
public record BoardDependencyDto(Guid From, Guid To);

public record BoardDependencyListDto(IReadOnlyList<BoardDependencyDto> Dependencies);

/// <summary>The dependencies between the live items of one board, for the Timeline's lines and critical path.</summary>
public record ListBoardDependenciesQuery(Guid BoardId)
    : IQuery<Result<BoardDependencyListDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardDependenciesQueryHandler : IRequestHandler<ListBoardDependenciesQuery, Result<BoardDependencyListDto>>
{
    private readonly IWorkManagementDbContext _context;

    public ListBoardDependenciesQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<BoardDependencyListDto>> Handle(ListBoardDependenciesQuery request, CancellationToken ct)
    {
        var itemKind = ResourceKind.Create("work-management.board-item");
        var rows = (await _context.BoardItemLinks.AsNoTracking()
                .Where(l => l.BoardId == request.BoardId && l.LinkType == BoardItemLinkType.Dependency)
                .ToListAsync(ct))
            .Where(l => l.Target.Kind == itemKind)
            .ToList();
        if (rows.Count == 0) return Result<BoardDependencyListDto>.Success(new BoardDependencyListDto([]));

        // A link to an item that is deleted draws nothing.
        var live = (await _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == request.BoardId && i.DeletedAt == null)
            .Select(i => i.Id)
            .ToListAsync(ct)).ToHashSet();

        // A Dependency row reads "source is blocked by target": the source waits for the target.
        var dependencies = rows
            .Where(l => live.Contains(l.SourceItemId) && live.Contains(l.Target.ResourceId))
            .Select(l => new BoardDependencyDto(l.Target.ResourceId, l.SourceItemId))
            .ToList();
        return Result<BoardDependencyListDto>.Success(new BoardDependencyListDto(dependencies));
    }
}
