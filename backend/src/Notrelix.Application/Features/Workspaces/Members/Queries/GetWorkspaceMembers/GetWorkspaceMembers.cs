using System.Buffers.Text;
using System.Globalization;
using System.Text;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;

/// <summary>
/// The Workspace's people as a cursor page (LD-06): joined time first, user id on ties, default 50
/// and at most 200 per page, removed members never listed. Pass <c>NextCursor</c> back as
/// <c>Cursor</c> for the next page.
/// </summary>
public record GetWorkspaceMembersQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null) : IQuery<Result<CursorPage<WorkspaceMemberDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, Result<CursorPage<WorkspaceMemberDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IMemberPhoneLookup _phoneLookup;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceMembersQueryHandler(
        IWorkspaceDbContext context,
        IActorLookupService actorLookup,
        IMemberPhoneLookup phoneLookup,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _phoneLookup = phoneLookup;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<WorkspaceMemberDto>>> Handle(GetWorkspaceMembersQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var limit = request.Limit ?? GetWorkspaceMembersQuery.DefaultLimit;
        var query = _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == request.WorkspaceId && m.Status != WorkspaceMemberStatus.Removed);

        if (MemberCursor.TryDecode(request.Cursor, out var cursor))
        {
            var joinedAt = cursor.JoinedAt;
            var userId = cursor.UserId;
            query = query.Where(m => m.CreatedAt > joinedAt || (m.CreatedAt == joinedAt && m.UserId.CompareTo(userId) > 0));
        }

        var rows = await query
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.UserId)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var members = hasMore ? rows.Take(limit).ToList() : rows;

        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        // A phone is visible only to a non-guest member of this Workspace and only for people who
        // opted in (D15). A guest reader gets none; a guest subject's phone is not shared either.
        // The reader is read on its own: it need not be on this page.
        var reader = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == request.WorkspaceId
                && m.UserId == _requestContext.UserId
                && m.Status != WorkspaceMemberStatus.Removed, ct);
        var phoneCandidates = reader is { IsInternalParticipant: true }
            ? members.Where(m => m.IsInternalParticipant).Select(m => m.UserId).ToList()
            : [];
        var phones = await _phoneLookup.FindSharedPhonesAsync(request.WorkspaceId, phoneCandidates, ct);

        var result = members.Select(m =>
        {
            actorMap.TryGetValue(m.UserId, out var actor);
            phones.TryGetValue(m.UserId, out var phone);
            return new WorkspaceMemberDto(
                m.UserId,
                actor?.Name ?? "Unknown",
                actor?.AvatarUrl,
                m.Role.ToString(),
                m.CreatedAt.UtcDateTime,
                m.Status.ToString(),
                m.JobTitle,
                m.Version,
                phone
            );
        }).ToList();

        var nextCursor = hasMore ? MemberCursor.Encode(members[^1].CreatedAt, members[^1].UserId) : null;
        return Result<CursorPage<WorkspaceMemberDto>>.Success(new CursorPage<WorkspaceMemberDto>(result, nextCursor));
    }
}

public class GetWorkspaceMembersQueryValidator : AbstractValidator<GetWorkspaceMembersQuery>
{
    public GetWorkspaceMembersQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetWorkspaceMembersQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => MemberCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid member list cursor.");
    }
}

/// <summary>
/// Opaque keyset cursor over (joined time, user id): base64url of
/// <c>"&lt;joined-utc-ticks&gt;:&lt;user-id-N&gt;"</c>.
/// </summary>
internal readonly record struct MemberCursor(DateTimeOffset JoinedAt, Guid UserId)
{
    public static string Encode(DateTimeOffset joinedAt, Guid userId) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{joinedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{userId:N}"));

    public static bool TryDecode(string? value, out MemberCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(value) || value.Length > 128)
            return false;

        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = raw.Split(':');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParseExact(parts[1], "N", out var userId))
        {
            return false;
        }

        cursor = new MemberCursor(new DateTimeOffset(ticks, TimeSpan.Zero), userId);
        return true;
    }
}
