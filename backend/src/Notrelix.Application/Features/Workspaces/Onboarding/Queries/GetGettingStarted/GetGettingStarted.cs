using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.GettingStarted;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Onboarding.Queries.GetGettingStarted;

/// <param name="Key">Stable item key; only <c>first-item</c> is served.</param>
/// <param name="Done"><c>first-item</c>: the caller can read a Board that has an active Item.</param>
/// <param name="BoardId">Done: the Board of the earliest created Item. Not done: the newest Board the caller can read; null when none.</param>
public record GettingStartedItemDto(string Key, bool Done, Guid? BoardId, string? BoardTitle);

/// <param name="Dismissed">The caller hid the card in this workspace.</param>
public record GettingStartedDto(bool Dismissed, IReadOnlyList<GettingStartedItemDto> Items);

/// <summary>
/// The getting-started card of a Workspace for the caller (accepted 2026-10-23: derived, with dismissal
/// stored per user and workspace). Any member may read it (<c>ViewWorkspace</c>). The first-item fact is
/// Work Management's, asked only over the Boards the caller can read (GOV-029), so a hidden Board never
/// makes the card done.
/// </summary>
public record GetGettingStartedQuery(Guid WorkspaceId)
    : IQuery<Result<GettingStartedDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetGettingStartedQueryHandler : IRequestHandler<GetGettingStartedQuery, Result<GettingStartedDto>>
{
    public const string FirstItemKey = "first-item";

    private readonly IWorkspaceDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly IFirstWorkItemFacts _work;
    private readonly ICurrentRequestContext _requestContext;

    public GetGettingStartedQueryHandler(
        IWorkspaceDbContext context,
        IResourceVisibility visibility,
        IFirstWorkItemFacts work,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _work = work;
        _requestContext = requestContext;
    }

    public async Task<Result<GettingStartedDto>> Handle(GetGettingStartedQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var workspaceId = request.WorkspaceId;

        var dismissed = await _context.GettingStartedDismissals
            .AsNoTracking()
            .AnyAsync(d => d.WorkspaceId == workspaceId && d.UserId == userId, ct);

        var readable = await _visibility.VisibleBoardIdsAsync(workspaceId, userId, ct);
        var withItem = await _work.FindEarliestItemBoardAsync(workspaceId, readable, ct);
        var board = withItem ?? await _work.FindNewestBoardAsync(workspaceId, readable, ct);

        return Result<GettingStartedDto>.Success(new GettingStartedDto(
            dismissed,
            [new GettingStartedItemDto(FirstItemKey, withItem is not null, board?.BoardId, board?.Title)]));
    }
}
