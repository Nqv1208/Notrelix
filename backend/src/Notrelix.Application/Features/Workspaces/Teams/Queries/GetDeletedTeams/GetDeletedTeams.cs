using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetDeletedTeams;

/// <param name="MemberCount">Members whose participation has not ended; they return with the Team on restore.</param>
/// <param name="DeletedByName">The display name of who deleted the Team; null when that person is no longer known.</param>
public record DeletedTeamDto(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    DateTimeOffset DeletedAt,
    Guid? DeletedBy,
    string? DeletedByName);

/// <summary>
/// The Workspace's deleted Teams (UC-WSP-L09), newest deletion first, so an Owner or Admin can restore one without
/// the toast that follows the delete. Restore stays always allowed (LD-16), so this list needs the same right as
/// the restore itself: <c>ManageTeams</c>. A Member or Guest is refused and a person outside the Workspace is told it
/// does not exist (the pipeline), never a handler role check. At most <see cref="Limit"/> Teams are returned.
/// </summary>
public record GetDeletedTeamsQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<DeletedTeamDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int Limit = 200;

    PermissionAction IRequirePermission.Action => PermissionAction.ManageTeams;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetDeletedTeamsQueryHandler : IRequestHandler<GetDeletedTeamsQuery, Result<IReadOnlyList<DeletedTeamDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IActorLookupService _actors;

    public GetDeletedTeamsQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext, IActorLookupService actors)
    {
        _context = context;
        _requestContext = requestContext;
        _actors = actors;
    }

    public async Task<Result<IReadOnlyList<DeletedTeamDto>>> Handle(GetDeletedTeamsQuery request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();

        // The Team query filter hides deleted rows (and the tenant); both are stated here: this Workspace of this Account.
        var teams = await _context.Teams
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.AccountId == accountId && t.DeletedAt != null)
            .OrderByDescending(t => t.DeletedAt)
            .ThenBy(t => t.Id)
            .Take(GetDeletedTeamsQuery.Limit)
            .ToListAsync(ct);

        var teamIds = teams.Select(t => t.Id).ToList();
        var memberCounts = teamIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.TeamMembers
                .AsNoTracking()
                .Where(m => teamIds.Contains(m.TeamId) && m.Status == TeamMemberStatus.Active)
                .GroupBy(m => m.TeamId)
                .Select(g => new { TeamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TeamId, x => x.Count, ct);

        var deleterIds = teams.Where(t => t.DeletedBy.HasValue).Select(t => t.DeletedBy!.Value).Distinct().ToList();
        var names = deleterIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _actors.FindManyAsync(deleterIds, ct)).ToDictionary(a => a.UserId, a => a.Name);

        IReadOnlyList<DeletedTeamDto> rows = teams.Select(t => new DeletedTeamDto(
            t.Id,
            t.Name,
            t.Description,
            memberCounts.GetValueOrDefault(t.Id),
            t.DeletedAt!.Value,
            t.DeletedBy,
            t.DeletedBy is { } by ? names.GetValueOrDefault(by) : null)).ToList();
        return Result<IReadOnlyList<DeletedTeamDto>>.Success(rows);
    }
}

public class GetDeletedTeamsQueryValidator : AbstractValidator<GetDeletedTeamsQuery>
{
    public GetDeletedTeamsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
