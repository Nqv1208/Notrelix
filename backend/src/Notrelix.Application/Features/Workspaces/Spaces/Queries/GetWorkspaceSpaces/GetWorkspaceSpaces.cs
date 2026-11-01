using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces;

/// <summary>
/// The Spaces the caller may see as a cursor page (LD-06): ordered by name, id on ties, default 50 and at
/// most 200 per page. Pass <c>NextCursor</c> back as <c>Cursor</c> for the next page.
/// </summary>
public record GetWorkspaceSpacesQuery(
    Guid WorkspaceId,
    string? Cursor = null,
    int? Limit = null
) : IQuery<Result<CursorPage<SpaceDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceSpacesQueryHandler : IRequestHandler<GetWorkspaceSpacesQuery, Result<CursorPage<SpaceDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceSpacesQueryHandler(IWorkspaceDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<SpaceDto>>> Handle(GetWorkspaceSpacesQuery request, CancellationToken ct)
    {
        // Private Spaces are not listed to principals who cannot see them (GOV-029).
        var visible = await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (visible.Count == 0) return Result<CursorPage<SpaceDto>>.Success(new CursorPage<SpaceDto>([], null));

        var limit = request.Limit ?? NameCursor.DefaultLimit;
        var query = _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == request.WorkspaceId && s.DeletedAt == null)
            .Where(s => visible.Contains(s.Id));

        // Keyset on (name, id): the next page starts after the last row the previous one ended on.
        if (NameCursor.TryDecode(request.Cursor, out var cursor))
        {
            var (name, id) = cursor;
            query = query.Where(s => string.Compare(s.Name, name) > 0 || (s.Name == name && s.Id.CompareTo(id) > 0));
        }

        var rows = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var page = hasMore ? rows.Take(limit).ToList() : rows;

        var result = page.Select(s => new SpaceDto(
            s.Id,
            s.Name,
            s.Description,
            s.Visibility.ToString(),
            s.SpaceType.ToString(),
            s.Status == SpaceStatus.Archived,
            s.CreatedAt.UtcDateTime,
            s.Version)).ToList();

        return Result<CursorPage<SpaceDto>>.Success(
            new CursorPage<SpaceDto>(result, hasMore ? NameCursor.Encode(page[^1].Name, page[^1].Id) : null));
    }
}

public class GetWorkspaceSpacesQueryValidator : AbstractValidator<GetWorkspaceSpacesQuery>
{
    public GetWorkspaceSpacesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, NameCursor.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => NameCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid space list cursor.");
    }
}
