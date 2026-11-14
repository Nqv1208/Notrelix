using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.ListDeletedBoards;

/// <summary>
/// The boards of one Workspace deleted inside the 30-day restore window that the caller can restore,
/// newest first, optionally narrowed to the boards placed in one Space (<c>SpaceId</c>). Listed: the boards
/// the caller deleted, the ones where the caller is a board Owner or Admin, and every other one the access
/// engine lets the caller restore, decided by the same ManageBoard request restore itself is authorized with
/// (a deleted board is reachable only by a lifecycle request): a workspace Owner, and a Space manager for a
/// Workspace-visible board placed in their Space. A deleted board is NotFound to everyone else, so it is
/// absent here, never marked. A deleter who has since lost the board's Space and holds no role on the board
/// no longer sees it; a deleted board at the Workspace root stays listed for its deleter while it is visible
/// to the whole Workspace. Restore itself is authorized again on the board (ManageBoard).
/// </summary>
public record ListDeletedBoardsQuery(Guid WorkspaceId, Guid? SpaceId = null)
    : IQuery<Result<List<RetiredBoardDto>>>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListDeletedBoardsQueryHandler : IRequestHandler<ListDeletedBoardsQuery, Result<List<RetiredBoardDto>>>
{
    /// <summary>
    /// Upper bound of the deleted boards read and decided in one list. shortcut: the engine decides each board
    /// the caller did not delete and holds no manager role on one at a time; a workspace that deletes more boards
    /// than this in 30 days lists the newest ones. Upgrade to a set-based Governance read when that happens.
    /// </summary>
    internal const int MaxListed = 200;

    private readonly IWorkManagementDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly IExecutionContextReader _executionContext;
    private readonly IAccessFactsProvider _facts;
    private readonly IAccessPolicyEvaluator _policy;

    public ListDeletedBoardsQueryHandler(
        IWorkManagementDbContext context,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock,
        IExecutionContextReader executionContext,
        IAccessFactsProvider facts,
        IAccessPolicyEvaluator policy)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
        _clock = clock;
        _executionContext = executionContext;
        _facts = facts;
        _policy = policy;
    }

    /// <summary>The request restore is authorized with: ManageBoard on the board, reaching a deleted board.</summary>
    private sealed record RestoreProbe(Guid BoardId) : IRequirePermission, IBoardReachesDeletedRequest
    {
        public PermissionAction Action => PermissionAction.ManageBoard;
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
    }

    private static readonly RequestDescriptor RestoreProbeDescriptor = new(
        typeof(RestoreProbe),
        ApplicationRequestKind.Command,
        ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource,
        ApplicationDataAccessKind.Write,
        new AccessRequirements(RequiresPermission: true, RequiresVerifiedEmail: false, RequiresSubscription: false, RequiresFeature: false),
        IsIdempotent: false,
        RequiresExpectedVersion: false);

    public async Task<Result<List<RetiredBoardDto>>> Handle(ListDeletedBoardsQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var accountId = _requestContext.RequireAccountId();
        var since = _clock.UtcNow - Board.RestoreWindow;
        var managerRoles = new[] { BoardRole.Owner, BoardRole.Admin };
        var spaces = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, userId, ct)).ToList();

        // The global filter hides soft-deleted rows, and bypassing it also drops its tenant predicate,
        // so the resolved tenant scope is applied here.
        var inWindow = _context.Boards.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.AccountId == accountId && b.WorkspaceId == request.WorkspaceId
                && b.DeletedAt != null && b.DeletedAt >= since);
        if (request.SpaceId is { } spaceFilter)
            inWindow = inWindow.Where(b => b.SpaceId == spaceFilter);

        var candidates = await inWindow
            .OrderByDescending(b => b.DeletedAt)
            .Take(MaxListed)
            .Select(b => new
            {
                Board = b,
                Own = (b.DeletedBy == userId
                        && ((b.SpaceId != null && spaces.Contains(b.SpaceId.Value))
                            || (b.SpaceId == null && b.Visibility == BoardVisibility.Workspace)
                            || _context.BoardMembers.Any(m => m.BoardId == b.Id && m.UserId == userId)))
                    || _context.BoardMembers.Any(m => m.BoardId == b.Id && m.UserId == userId && managerRoles.Contains(m.Role)),
            })
            .ToListAsync(ct);

        var listed = new List<Board>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (candidate.Own || await CanRestoreAsync(candidate.Board.Id, ct))
                listed.Add(candidate.Board);
        }

        return Result<List<RetiredBoardDto>>.Success(listed
            .Select(b => new RetiredBoardDto(
                b.Id, b.WorkspaceId, b.Title, b.Background, b.Visibility.ToString(), b.Version,
                b.SpaceId is { } spaceId && spaces.Contains(spaceId) ? spaceId : null,
                string.IsNullOrWhiteSpace(b.ItemKeyPrefix) ? null : b.ItemKeyPrefix,
                b.DeletedAt!.Value.UtcDateTime,
                (b.DeletedAt.Value + Board.RestoreWindow).UtcDateTime))
            .ToList());
    }

    // The same decision the pipeline gives RestoreBoardCommand: ManageBoard on the deleted board.
    private async Task<bool> CanRestoreAsync(Guid boardId, CancellationToken ct)
    {
        var context = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Execution context is not resolved for listing deleted boards.");
        var probe = new RestoreProbe(boardId);
        var facts = await _facts.ResolveAsync(RestoreProbeDescriptor, context, probe, ct);
        return _policy.Evaluate(RestoreProbeDescriptor, context, facts, probe).Kind == AccessDecisionKind.Allowed;
    }
}
