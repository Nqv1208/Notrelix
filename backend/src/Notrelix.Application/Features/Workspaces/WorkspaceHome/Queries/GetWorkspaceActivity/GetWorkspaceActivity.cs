using System.Buffers.Text;
using System.Globalization;
using System.Text;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.WorkspaceActivity;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;

/// <summary>One line of the workspace feed. <c>Type</c> is the stable key a client words in its own language; <c>Title</c> is the English fallback.</summary>
public record WorkspaceActivityEntryDto(
    Guid Id,
    string Type,
    Guid? ActorUserId,
    string? ActorName,
    string? ActorAvatarUrl,
    string Title,
    string SubjectType,
    Guid SubjectId,
    string? SubjectName,
    DateTime OccurredAt);

/// <summary>
/// The workspace activity feed (LD-15, UC-WSP-N03): what Collaboration recorded, newest first, as a cursor page (default 20, at most 50).
/// <c>ViewMembers</c> through the policy engine: an Owner, Admin or Member reads it, a Guest is forbidden (the feed names people, and the
/// member list is not guest-safe), a person outside the workspace is told it does not exist. A fact about a Board or Page the caller cannot
/// read is left out, and a fact that does not say which Board or Page it is about is never shown.
/// </summary>
public record GetWorkspaceActivityQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<WorkspaceActivityEntryDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceActivityQueryHandler : IRequestHandler<GetWorkspaceActivityQuery, Result<CursorPage<WorkspaceActivityEntryDto>>>
{
    // Facts the caller cannot read are dropped after the read, so one page may need several batches. The scan is bounded:
    // past it the page is returned short with a cursor, and the next request continues from the last fact scanned.
    private const int ScanBatchSize = 100;
    private const int MaxScanBatches = 5;

    private readonly IWorkspaceDbContext _context;
    private readonly IWorkspaceActivityFeed _feed;
    private readonly IResourceVisibility _visibility;
    private readonly IActorLookupService _actorLookup;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceActivityQueryHandler(
        IWorkspaceDbContext context,
        IWorkspaceActivityFeed feed,
        IResourceVisibility visibility,
        IActorLookupService actorLookup,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _feed = feed;
        _visibility = visibility;
        _actorLookup = actorLookup;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<WorkspaceActivityEntryDto>>> Handle(GetWorkspaceActivityQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var limit = request.Limit ?? GetWorkspaceActivityQuery.DefaultLimit;
        WorkspaceActivityPosition? scanned = ActivityCursor.TryDecode(request.Cursor, out var cursor) ? cursor : null;
        var userId = _requestContext.UserId;

        HashSet<Guid>? boards = null;
        HashSet<Guid>? pages = null;
        async Task<bool> ReadableAsync(WorkspaceActivityScope scope)
        {
            switch (scope.Kind)
            {
                case WorkspaceActivityScopeKind.Workspace:
                    return true;
                case WorkspaceActivityScopeKind.Board:
                    boards ??= (await _visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, ct))
                        .Concat(await _visibility.VisibleArchivedBoardIdsAsync(request.WorkspaceId, userId, ct)).ToHashSet();
                    return boards.Contains(scope.ResourceId!.Value);
                case WorkspaceActivityScopeKind.Page:
                    pages ??= (await _visibility.VisiblePageIdsAsync(request.WorkspaceId, userId, ct))
                        .Concat(await _visibility.VisibleTrashedPageIdsAsync(request.WorkspaceId, userId, ct)).ToHashSet();
                    return pages.Contains(scope.ResourceId!.Value);
                default:
                    return false;
            }
        }

        var kept = new List<WorkspaceActivityFact>();
        var more = false;
        for (var batch = 0; batch < MaxScanBatches && kept.Count < limit; batch++)
        {
            var rows = await _feed.ListAsync(request.WorkspaceId, scanned, ScanBatchSize, ct);
            more = rows.Count == ScanBatchSize;
            foreach (var row in rows)
            {
                if (kept.Count == limit)
                {
                    more = true;
                    break;
                }

                scanned = row.Position;
                if (await ReadableAsync(row.Scope)) kept.Add(row);
            }
        }

        var actors = (await _actorLookup.FindManyAsync(
                kept.Where(f => f.ActorUserId.HasValue).Select(f => f.ActorUserId!.Value).Distinct().ToList(), ct))
            .ToDictionary(a => a.UserId);

        var entries = kept.Select(f =>
        {
            ActorSnapshot? actor = null;
            if (f.ActorUserId is { } actorId) actors.TryGetValue(actorId, out actor);
            return new WorkspaceActivityEntryDto(
                f.Id, f.Type, f.ActorUserId, actor?.Name, actor?.AvatarUrl, f.Title, f.SubjectType, f.SubjectId, f.SubjectName,
                f.OccurredAt.UtcDateTime);
        }).ToList();

        var nextCursor = more && scanned is { } last ? ActivityCursor.Encode(last) : null;
        return Result<CursorPage<WorkspaceActivityEntryDto>>.Success(new CursorPage<WorkspaceActivityEntryDto>(entries, nextCursor));
    }
}

public class GetWorkspaceActivityQueryValidator : AbstractValidator<GetWorkspaceActivityQuery>
{
    public GetWorkspaceActivityQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetWorkspaceActivityQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => ActivityCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid activity cursor.");
    }
}

/// <summary>Opaque keyset cursor over (time, id): base64url of <c>"&lt;utc-ticks&gt;:&lt;id-N&gt;"</c>.</summary>
internal static class ActivityCursor
{
    public static string Encode(WorkspaceActivityPosition position) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{position.OccurredAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{position.Id:N}"));

    public static bool TryDecode(string? value, out WorkspaceActivityPosition position)
    {
        position = default;
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
            || !Guid.TryParseExact(parts[1], "N", out var id))
        {
            return false;
        }

        position = new WorkspaceActivityPosition(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
