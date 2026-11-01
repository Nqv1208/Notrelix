using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree;

/// <summary>
/// One node of the caller's Space tree. <paramref name="ParentSpaceId"/> is null
/// for a root Space and also when the parent is a Space the caller cannot see:
/// such a Space is presented as a root and the hidden parent is never disclosed
/// (GOV-029). Siblings are ordered by <paramref name="Position"/> (ordinal).
/// </summary>
/// <param name="Version">The Space aggregate version, sent back as <c>expectedVersion</c> by move and membership writes.</param>
/// <param name="AccessLevel">The caller's effective level on the Space (<c>Viewer</c>, <c>Editor</c> or <c>Manager</c>; GOV-027). <c>Manager</c> may manage the Space and its members; <c>Editor</c> may create Boards and Docs in it. Derived capability state — every operation is still authorized by the server.</param>
public record SpaceTreeNodeDto(
    Guid Id,
    Guid? ParentSpaceId,
    string Name,
    string? Description,
    string SpaceType,
    string Visibility,
    bool IsArchived,
    string Position,
    long Version,
    DateTimeOffset CreatedAt,
    string AccessLevel);

/// <summary>
/// The Workspace's Space org tree as a flat, parent-linked cursor page (LD-06), restricted to the Spaces
/// the caller may see (single-read Space visibility, GOV-029). Pages run in Space id order, default 50
/// and at most 200; the client reads every page and orders siblings by position. Pass <c>NextCursor</c>
/// back as <c>Cursor</c> for the next page.
/// </summary>
public record GetSpaceTreeQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<SpaceTreeNodeDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceTreeQueryHandler : IRequestHandler<GetSpaceTreeQuery, Result<CursorPage<SpaceTreeNodeDto>>>
{
    private readonly ISpaceStore _spaces;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetSpaceTreeQueryHandler(ISpaceStore spaces, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _spaces = spaces;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<SpaceTreeNodeDto>>> Handle(GetSpaceTreeQuery request, CancellationToken ct)
    {
        var visible = await _visibility.VisibleSpacesAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0)
            return Result<CursorPage<SpaceTreeNodeDto>>.Success(new CursorPage<SpaceTreeNodeDto>([], null));

        var limit = request.Limit ?? SpaceIdCursor.DefaultLimit;
        Guid? after = SpaceIdCursor.TryDecode(request.Cursor, out var cursorId) ? cursorId : null;
        var levels = visible.ToDictionary(v => v.SpaceId, v => v.Level);
        var rows = await _spaces.ListTreeAsync(request.WorkspaceId, levels.Keys.ToList(), after, limit + 1, ct);

        var hasMore = rows.Count > limit;
        var page = hasMore ? rows.Take(limit).ToList() : rows;

        // A parent the caller cannot see is never disclosed: its child reads as a root.
        var nodes = page
            .Select(r => new SpaceTreeNodeDto(
                r.Id,
                r.ParentSpaceId is { } parentId && levels.ContainsKey(parentId) ? parentId : null,
                r.Name,
                r.Description,
                r.SpaceType.ToString(),
                r.Visibility.ToString(),
                r.Status == SpaceStatus.Archived,
                r.Position.Value,
                r.Version,
                r.CreatedAt,
                levels[r.Id]))
            .ToList();

        return Result<CursorPage<SpaceTreeNodeDto>>.Success(
            new CursorPage<SpaceTreeNodeDto>(nodes, hasMore ? SpaceIdCursor.Encode(page[^1].Id) : null));
    }
}
