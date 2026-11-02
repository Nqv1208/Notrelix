using System.Buffers.Text;
using System.Globalization;
using System.Text;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;

/// <summary>
/// The Workspace's invitations as a cursor page (LD-06): newest first, id on ties, default 50 and at most
/// 200 per page. A stored Pending invitation past its expiry is reported as Expired (LD-04). Pass
/// <c>NextCursor</c> back as <c>Cursor</c> for the next page. The rows carry invited e-mail addresses, so only
/// the people who may invite (Owner, Admin) read them: a Member or Guest is Forbidden (lead decision 2026-11-02).
/// </summary>
public record GetWorkspaceInvitationsQuery(Guid WorkspaceId, string? Cursor = null, int? Limit = null) : IQuery<Result<CursorPage<WorkspaceInvitationDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.InviteMember;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceInvitationsQueryHandler : IRequestHandler<GetWorkspaceInvitationsQuery, Result<CursorPage<WorkspaceInvitationDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetWorkspaceInvitationsQueryHandler(IWorkspaceDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CursorPage<WorkspaceInvitationDto>>> Handle(GetWorkspaceInvitationsQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && w.DeletedAt == null, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var limit = request.Limit ?? GetWorkspaceInvitationsQuery.DefaultLimit;
        var query = _context.WorkspaceInvitations
            .AsNoTracking()
            .Where(invitation => invitation.WorkspaceId == request.WorkspaceId);

        if (InvitationCursor.TryDecode(request.Cursor, out var cursor))
        {
            var createdAt = cursor.CreatedAt;
            var id = cursor.Id;
            query = query.Where(i => i.CreatedAt < createdAt || (i.CreatedAt == createdAt && i.Id.CompareTo(id) < 0));
        }

        var rows = await query
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ThenByDescending(invitation => invitation.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var invitations = hasMore ? rows.Take(limit).ToList() : rows;
        var now = _dateTimeProvider.UtcNow;

        var result = invitations.Select(invitation => new WorkspaceInvitationDto(
            invitation.Id,
            invitation.Email,
            invitation.Role.ToString(),
            invitation.ExpiresAt.UtcDateTime,
            invitation.Status == WorkspaceInvitationStatus.Accepted,
            invitation.CreatedAt.UtcDateTime,
            invitation.Status == WorkspaceInvitationStatus.Pending && invitation.ExpiresAt <= now
                ? WorkspaceInvitationStatus.Expired.ToString()
                : invitation.Status.ToString(),
            invitation.ResourceGrant?.ResourceKind,
            invitation.ResourceGrant?.ResourceId,
            invitation.ResourceGrant?.Level,
            invitation.InitialEntries.Count == 0
                ? null
                : invitation.InitialEntries
                    .Select(e => new WorkspaceInvitationEntryDto(e.Kind.ToString(), e.TargetId, e.Role))
                    .ToList(),
            invitation.Version
        )).ToList();

        var nextCursor = hasMore ? InvitationCursor.Encode(invitations[^1].CreatedAt, invitations[^1].Id) : null;
        return Result<CursorPage<WorkspaceInvitationDto>>.Success(new CursorPage<WorkspaceInvitationDto>(result, nextCursor));
    }
}

public class GetWorkspaceInvitationsQueryValidator : AbstractValidator<GetWorkspaceInvitationsQuery>
{
    public GetWorkspaceInvitationsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, GetWorkspaceInvitationsQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => InvitationCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid invitation list cursor.");
    }
}

/// <summary>
/// Opaque keyset cursor over (created time, invitation id), newest first: base64url of
/// <c>"&lt;created-utc-ticks&gt;:&lt;invitation-id-N&gt;"</c>.
/// </summary>
internal readonly record struct InvitationCursor(DateTimeOffset CreatedAt, Guid Id)
{
    public static string Encode(DateTimeOffset createdAt, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{createdAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}"));

    public static bool TryDecode(string? value, out InvitationCursor cursor)
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
            || !Guid.TryParseExact(parts[1], "N", out var id))
        {
            return false;
        }

        cursor = new InvitationCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
