using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;

/// <summary>
/// CAL-CONN-001 — the first mutation is always the CalendarIntegration
/// lifecycle authority: <c>CalendarIntegration.Deactivate</c>. The generic
/// IntegrationConnection is revoked ONLY when no active Calendar binding of
/// any kind still references it — one active binding retains the connection;
/// the last binding revokes it. Revoking a provider whose outcome is unknown
/// is not attempted: provider cleanup is outside the M8 DB transaction.
/// </summary>
public record DisconnectCalendarCommand(Guid IntegrationId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("integrations.calendar-integration"), IntegrationId);
    public PermissionAction Action => PermissionAction.ManageIntegrations;
}

public class DisconnectCalendarCommandHandler : IRequestHandler<DisconnectCalendarCommand, Result>
{
    private readonly IIntegrationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public DisconnectCalendarCommandHandler(
        IIntegrationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result> Handle(DisconnectCalendarCommand request, CancellationToken cancellationToken)
    {
        var actorId = _requestContext.UserId;
        var now = _clock.UtcNow;

        var calendar = await _context.CalendarIntegrations
            .FirstOrDefaultAsync(ci => ci.Id == request.IntegrationId && ci.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException(nameof(CalendarIntegration), request.IntegrationId);

        var connectionId = calendar.ConnectionId;

        // 1. The binding lifecycle authority deactivates the calendar first.
        calendar.Deactivate(actorId, now);

        // 2. CAL-CONN-001 retention: revoke the generic connection only when
        // no active binding of ANY kind still references it.
        var remainingActiveBindings = await _context.CalendarIntegrations
            .CountAsync(ci =>
                ci.ConnectionId == connectionId
                && ci.Id != calendar.Id
                && ci.IsActive
                && ci.DeletedAt == null, cancellationToken);

        if (remainingActiveBindings == 0)
        {
            var connection = await _context.IntegrationConnections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.DeletedAt == null, cancellationToken);
            connection?.Disconnect(actorId, now);
        }

        return Result.Success();
    }
}
