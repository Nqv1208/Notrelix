using System.Buffers.Text;
using System.Globalization;
using System.Text;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Queries.ListConnectionDeliveryLog;

/// <summary>
/// One row of a connection's delivery log. It carries the identity, the event type, the status, the attempt
/// count, the last response status and the failure class only: never the payload, the receiver's body, a header
/// or a secret.
/// </summary>
public sealed record OutboundDeliveryLogEntryDto(
    Guid Id,
    string EventType,
    string Status,
    int AttemptCount,
    int? ResponseStatusCode,
    string? FailureClass,
    string? SkipReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeliveredAt);

/// <summary>
/// The delivery log of one outgoing webhook connection, newest first, as a bounded cursor page (default 50, max 200).
/// Readable by the members who manage integrations, under the same permission as the connection's other reads. A
/// connection of another workspace, or one that does not exist, is NotFound.
/// </summary>
public sealed record ListConnectionDeliveryLogQuery(Guid WorkspaceId, Guid ConnectionId, string? Cursor = null, int? Limit = null)
    : IQuery<Result<CursorPage<OutboundDeliveryLogEntryDto>>>, IReadRequest, IWorkspaceRequest, IAuthenticatedRequest, IRequirePermission
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public sealed class ListConnectionDeliveryLogQueryHandler
    : IRequestHandler<ListConnectionDeliveryLogQuery, Result<CursorPage<OutboundDeliveryLogEntryDto>>>
{
    private readonly IIntegrationDbContext _context;

    public ListConnectionDeliveryLogQueryHandler(IIntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CursorPage<OutboundDeliveryLogEntryDto>>> Handle(
        ListConnectionDeliveryLogQuery request, CancellationToken ct)
    {
        var connectionExists = await _context.IntegrationConnections.AsNoTracking().AnyAsync(c =>
            c.Id == request.ConnectionId && c.WorkspaceId == request.WorkspaceId, ct);
        if (!connectionExists)
        {
            throw new NotFoundException(nameof(IntegrationConnection), request.ConnectionId);
        }

        var limit = request.Limit ?? ListConnectionDeliveryLogQuery.DefaultLimit;
        var rows = _context.OutboundDeliveryLog.AsNoTracking()
            .Where(d => d.WorkspaceId == request.WorkspaceId && d.ConnectionId == request.ConnectionId);

        if (OutboundDeliveryCursor.TryDecode(request.Cursor, out var after))
        {
            rows = rows.Where(d => d.CreatedAt < after.CreatedAt
                                   || (d.CreatedAt == after.CreatedAt && d.Id.CompareTo(after.Id) < 0));
        }

        var page = await rows
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = page.Count > limit;
        var items = page.Take(limit).Select(d => new OutboundDeliveryLogEntryDto(
            d.Id,
            d.EventType,
            d.Status.ToString(),
            d.AttemptCount,
            d.ResponseStatusCode,
            d.FailureClass?.ToString(),
            d.SkipReason?.ToString(),
            d.CreatedAt,
            d.UpdatedAt,
            d.DeliveredAt)).ToList();

        var nextCursor = hasMore ? OutboundDeliveryCursor.Encode(page[limit - 1].CreatedAt, page[limit - 1].Id) : null;
        return Result<CursorPage<OutboundDeliveryLogEntryDto>>.Success(new CursorPage<OutboundDeliveryLogEntryDto>(items, nextCursor));
    }
}

public sealed class ListConnectionDeliveryLogQueryValidator : AbstractValidator<ListConnectionDeliveryLogQuery>
{
    public ListConnectionDeliveryLogQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ListConnectionDeliveryLogQuery.MaxLimit)
            .When(x => x.Limit.HasValue);
        RuleFor(x => x.Cursor)
            .Must(c => OutboundDeliveryCursor.TryDecode(c, out _))
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Cursor is not a valid delivery cursor.");
    }
}

/// <summary>Opaque keyset cursor over (created-at, id): base64url of <c>"&lt;utc-ticks&gt;:&lt;id-N&gt;"</c>.</summary>
internal static class OutboundDeliveryCursor
{
    public static string Encode(DateTimeOffset createdAt, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{createdAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}"));

    public static bool TryDecode(string? value, out (DateTimeOffset CreatedAt, Guid Id) position)
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
            || !Guid.TryParseExact(parts[1], "N", out var id)
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks)
        {
            return false;
        }

        position = (new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
