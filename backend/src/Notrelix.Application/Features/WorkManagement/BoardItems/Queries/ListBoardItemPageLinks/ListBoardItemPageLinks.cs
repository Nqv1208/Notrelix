using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Public.PageTitles;
using Notrelix.Application.Features.Documents.Public.PageViewAccess;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemPageLinks;

/// <summary>A Documents page linked to the item that the caller may open.</summary>
public record LinkedPageDto(Guid PageId, string Title, DateTimeOffset LinkedAt);

/// <summary>The linked pages the caller sees, and how many more are linked that they may not see (shown without a name).</summary>
public record BoardItemPageLinkListDto(IReadOnlyList<LinkedPageDto> Pages, int HiddenCount);

public record ListBoardItemPageLinksQuery(Guid BoardItemId) : IQuery<Result<BoardItemPageLinkListDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

/// <summary>
/// Work Management owns the link; Documents owns whether a page can be seen and what it is called. A page the
/// caller cannot see is counted, never named; a page that no longer exists is left out.
/// </summary>
public class ListBoardItemPageLinksQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IPageViewAccess pageAccess,
    IPageTitleFacts pageTitles) : IRequestHandler<ListBoardItemPageLinksQuery, Result<BoardItemPageLinkListDto>>
{
    public async Task<Result<BoardItemPageLinkListDto>> Handle(ListBoardItemPageLinksQuery request, CancellationToken ct)
    {
        var item = await context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var pageKind = ResourceKind.Create("documents.page");
        var links = (await context.BoardItemLinks.AsNoTracking()
                .Where(l => l.SourceItemId == item.Id)
                .ToListAsync(ct))
            .Where(l => l.Target.Kind == pageKind)
            .OrderBy(l => l.CreatedAt)
            .ToList();
        if (links.Count == 0)
            return Result<BoardItemPageLinkListDto>.Success(new BoardItemPageLinkListDto([], 0));

        var accountId = requestContext.RequireAccountId();
        var viewable = new List<BoardItemLink>();
        foreach (var link in links)
        {
            if (await pageAccess.CanViewAsync(accountId, item.WorkspaceId, link.Target.ResourceId, requestContext.UserId, ct))
                viewable.Add(link);
        }

        var titles = viewable.Count == 0
            ? new Dictionary<Guid, string>()
            : (await pageTitles.TitlesAsync(item.WorkspaceId, viewable.Select(l => l.Target.ResourceId).ToList(), ct)).ToDictionary(p => p.Key, p => p.Value);

        var pages = viewable
            .Where(l => titles.ContainsKey(l.Target.ResourceId))
            .Select(l => new LinkedPageDto(l.Target.ResourceId, titles[l.Target.ResourceId], l.CreatedAt))
            .ToList();

        return Result<BoardItemPageLinkListDto>.Success(new BoardItemPageLinkListDto(pages, links.Count - viewable.Count));
    }
}
