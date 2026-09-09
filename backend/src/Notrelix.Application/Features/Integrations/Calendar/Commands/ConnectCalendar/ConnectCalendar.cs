using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Public.Secrets;

namespace Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;

/// <summary>
/// TAC-AI-FLOW-05 — the frozen connect sequence: trusted Account/Workspace →
/// ManageIntegrations authorization (canonical pipeline) → secret-store port
/// (physical persistence outside the aggregates) → IntegrationSecretVersion
/// creation → IntegrationConnection create/reuse → CalendarIntegration
/// binding → single Integrations DB commit. The physical secret lives behind
/// an opaque SecretReference owned by IIntegrationSecretStore; the blob and
/// the aggregates share ONE scoped context and one transaction fate — no
/// compensation path exists or is needed (frozen: atomicity over
/// compensation).
/// </summary>
public record ConnectCalendarCommand(
    string Provider,
    string AccessToken,
    Guid WorkspaceId,
    Guid? ProviderAccountId,
    string SyncDirection
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ConnectCalendarCommandHandler : IRequestHandler<ConnectCalendarCommand, Result<Guid>>
{
    private readonly IIntegrationDbContext _context;
    private readonly IIntegrationSecretStore _secretStore;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public ConnectCalendarCommandHandler(
        IIntegrationDbContext context,
        IIntegrationSecretStore secretStore,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _secretStore = secretStore;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(ConnectCalendarCommand request, CancellationToken cancellationToken)
    {
        var provider = Enum.Parse<IntegrationProvider>(request.Provider, ignoreCase: true);
        var calendarProvider = provider switch
        {
            IntegrationProvider.Google => Domain.Integrations.CalendarProvider.Google,
            IntegrationProvider.Microsoft => Domain.Integrations.CalendarProvider.Outlook,
            _ => throw new BusinessRuleException(
                $"Provider '{request.Provider}' is not a calendar provider."),
        };
        var syncDirection = Enum.Parse<CalendarSyncDirection>(request.SyncDirection, ignoreCase: true);

        var now = _clock.UtcNow;
        var workspaceId = request.WorkspaceId;
        var accountId = _requestContext.RequireAccountId();
        var actorId = _requestContext.UserId;

        // The secret blob is staged on the SAME scoped context as the
        // workflow aggregates — the DataSession transaction commits blob +
        // aggregates as one fate. No orphan secret is possible, so no
        // compensation path is required (frozen: atomicity over compensation).
        var secretReference = SecretRef.Create(
            await _secretStore.StoreAsync(request.AccessToken, cancellationToken));

        {
            // Reuse an existing ACTIVE connection for the workspace when one
            // exists; otherwise create a new generic provider relationship and
            // advance its secret version pointer to the new reference.
            var connection = await _context.IntegrationConnections
                .FirstOrDefaultAsync(c =>
                    c.WorkspaceId == workspaceId
                    && c.Provider == provider
                    && c.DeletedAt == null
                    && c.Status == IntegrationConnectionStatus.Active, cancellationToken);

            string version;
            if (connection is not null)
            {
                // Reuse of an ACTIVE connection is an intentional
                // reauthorization: the supplied secret rotates to a new
                // version AND the request's provider account is applied —
                // never silently ignored.
                version = (int.Parse(connection.CurrentSecretVersion ?? "0") + 1).ToString();
                connection.Reconnect(request.ProviderAccountId?.ToString(), expiresAt: null, actorId, now);
                connection.RotateSecret(version, secretReference, actorId, now);
            }
            else
            {
                connection = IntegrationConnection.Create(
                    accountId,
                    workspaceId,
                    provider,
                    actorId,
                    now,
                    request.ProviderAccountId?.ToString());
                version = "1";
                connection.RotateSecret(version, secretReference, actorId, now);
                _context.IntegrationConnections.Add(connection);
            }

            var secretVersion = IntegrationSecretVersion.Create(
                connection.Id, version, secretReference, now);
            _context.IntegrationSecretVersions.Add(secretVersion);

            // One-of-many: a workspace binds one Calendar integration per
            // provider. An existing binding is reused only when it still
            // points at the active connection — a stale binding (its
            // connection was revoked by CAL-CONN-001) is deleted and a fresh
            // binding is created on the current connection.
            var calendar = await _context.CalendarIntegrations
                .FirstOrDefaultAsync(ci =>
                    ci.WorkspaceId == workspaceId
                    && ci.Provider == calendarProvider
                    && ci.DeletedAt == null, cancellationToken);

            Guid calendarIntegrationId;
            if (calendar is not null && calendar.ConnectionId == connection.Id)
            {
                calendar.Activate(actorId, now);
                // A successful reconnect applies the requested sync direction
                // instead of silently keeping the previous one.
                if (calendar.SyncDirection != syncDirection)
                {
                    calendar.ChangeSyncDirection(syncDirection, actorId, now);
                }
                calendarIntegrationId = calendar.Id;
            }
            else
            {
                calendar?.Delete(actorId, now);
                calendar = CalendarIntegration.Create(
                    accountId,
                    workspaceId,
                    connection.Id,
                    calendarProvider,
                    syncDirection,
                    actorId,
                    now);
                _context.CalendarIntegrations.Add(calendar);
                calendarIntegrationId = calendar.Id;
            }

            // The request data session owns the commit (IWriteRequest);
            // returning lets the pipeline persist all staged aggregates.
            return Result<Guid>.Success(calendarIntegrationId);
        }
    }
}
