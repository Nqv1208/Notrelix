using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey;

/// <param name="ItemKey">The key the Item holds now; it differs from the asked key when the Item moved.</param>
/// <param name="Moved">True when the asked key is an old one, kept as an alias after a move to another board.</param>
public sealed record ResolvedBoardItemKeyDto(Guid ItemId, Guid BoardId, string ItemKey, bool Moved);

/// <summary>
/// Opens an Item by the key it has on this board, or by the key it had here before it moved to another board
/// (D76.3, UC-WM-A10): a moved Item keeps its old key as a stored alias, so a link or a mention with the old key
/// still lands on it. The Item's current board must be one the caller may view; otherwise, and for a key the board
/// never issued, the answer is the same NotFound.
/// </summary>
public record ResolveBoardItemKeyQuery(Guid BoardId, string Key)
    : IQuery<ResolvedBoardItemKeyDto>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ResolveBoardItemKeyQueryHandler : IRequestHandler<ResolveBoardItemKeyQuery, ResolvedBoardItemKeyDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IResourceVisibility _visibility;
    private readonly IBoardItemKeyAliases _aliases;

    public ResolveBoardItemKeyQueryHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IResourceVisibility visibility, IBoardItemKeyAliases aliases)
    {
        _context = context;
        _aliases = aliases;
        _requestContext = requestContext;
        _visibility = visibility;
    }

    public async Task<ResolvedBoardItemKeyDto> Handle(ResolveBoardItemKeyQuery request, CancellationToken ct)
    {
        var key = request.Key.Trim();
        var current = await _context.BoardItems.AsNoTracking()
            .Where(i => i.BoardId == request.BoardId && i.ItemKey == key)
            .Select(i => new { i.Id, i.BoardId, i.ItemKey })
            .FirstOrDefaultAsync(ct);
        if (current is not null) return new ResolvedBoardItemKeyDto(current.Id, current.BoardId, current.ItemKey!, false);

        var movedId = await _aliases.FindItemAsync(request.BoardId, key, ct);
        var moved = movedId is null
            ? null
            : await _context.BoardItems.AsNoTracking()
                .Where(i => i.Id == movedId.Value)
                .Select(i => new { i.Id, i.BoardId, i.ItemKey, i.WorkspaceId })
                .FirstOrDefaultAsync(ct);
        if (moved is null || moved.ItemKey is null) throw new NotFoundException("BoardItemKey", key);

        // The Item lives on another board now: the caller must be able to see that one too.
        var visible = await _visibility.VisibleBoardIdsAsync(moved.WorkspaceId, _requestContext.UserId, ct);
        if (!visible.Contains(moved.BoardId)) throw new NotFoundException("BoardItemKey", key);

        return new ResolvedBoardItemKeyDto(moved.Id, moved.BoardId, moved.ItemKey, true);
    }
}

public class ResolveBoardItemKeyQueryValidator : AbstractValidator<ResolveBoardItemKeyQuery>
{
    public ResolveBoardItemKeyQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(64);
    }
}
