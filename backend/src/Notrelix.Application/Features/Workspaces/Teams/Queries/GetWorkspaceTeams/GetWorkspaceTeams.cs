using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;

/// <summary>
/// The Workspace's Teams (active and archived) as a cursor page (LD-06): ordered by name, id on ties, default 50
/// and at most 200 per page. Pass <c>NextCursor</c> back as <c>Cursor</c> for the next page.
/// </summary>
public record GetWorkspaceTeamsQuery(
    Guid WorkspaceId,
    string? Cursor = null,
    int? Limit = null
) : IQuery<Result<CursorPage<TeamDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceTeamsQueryHandler : IRequestHandler<GetWorkspaceTeamsQuery, Result<CursorPage<TeamDto>>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceTeamsQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CursorPage<TeamDto>>> Handle(GetWorkspaceTeamsQuery request, CancellationToken ct)
    {
        var limit = request.Limit ?? NameCursor.DefaultLimit;
        var query = _context.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.DeletedAt == null);

        // Keyset on (name, id): the next page starts after the last row the previous one ended on.
        if (NameCursor.TryDecode(request.Cursor, out var cursor))
        {
            var (name, id) = cursor;
            query = query.Where(t => string.Compare(t.Name, name) > 0 || (t.Name == name && t.Id.CompareTo(id) > 0));
        }

        var rows = await query
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var teams = hasMore ? rows.Take(limit).ToList() : rows;

        var teamIds = teams.Select(t => t.Id).ToList();
        var memberCounts = teamIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.TeamMembers
                .AsNoTracking()
                .Where(m => teamIds.Contains(m.TeamId) && m.Status == TeamMemberStatus.Active)
                .GroupBy(m => m.TeamId)
                .Select(g => new { TeamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TeamId, x => x.Count, ct);

        var result = teams.Select(t => new TeamDto(
            t.Id,
            t.Name,
            t.Description,
            t.Status.ToString(),
            memberCounts.GetValueOrDefault(t.Id),
            t.Version,
            t.CreatedAt.UtcDateTime)).ToList();

        return Result<CursorPage<TeamDto>>.Success(
            new CursorPage<TeamDto>(result, hasMore ? NameCursor.Encode(teams[^1].Name, teams[^1].Id) : null));
    }
}

public class GetWorkspaceTeamsQueryValidator : AbstractValidator<GetWorkspaceTeamsQuery>
{
    public GetWorkspaceTeamsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, NameCursor.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => NameCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid team list cursor.");
    }
}
