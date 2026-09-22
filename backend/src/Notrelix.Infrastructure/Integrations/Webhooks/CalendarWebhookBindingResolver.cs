using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Domain.Integrations;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Infrastructure.Data.Rls;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// C6 — cross-tenant calendar webhook binding resolution and derived-tenant
/// adoption. This is the allowlisted bootstrap counterpart of
/// ResourceLocator/TenantBootstrapStore: the owning tenant is unknown at the
/// transport boundary, so the WebhookPath binding is resolved by stable
/// identity with query filters bypassed — before the RLS session context can
/// exist — and the binding's tenant is then adopted into ICurrentTenantContext
/// only after the caller has verified the callback signature.
///
/// ConnectionId closure: the binding's ConnectionId is the trusted
/// provenance/binding identity. The bootstrap follows
/// WebhookPath → CalendarIntegration → ConnectionId → IntegrationConnection
/// and validates the connection lifecycle (exists, not deleted, Active) and
/// that its Account/Workspace/Provider match the calendar binding. If the
/// connection is unknown, revoked/expired/errored, or mismatched, the
/// resolution fails closed — no binding is returned and no tenant is adopted.
/// </summary>
public sealed class CalendarWebhookBindingResolver : ICalendarWebhookBindingResolver
{
    private readonly IIntegrationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly RlsSessionContext _rls;

    public CalendarWebhookBindingResolver(
        IIntegrationDbContext context,
        ICurrentTenantContext tenant,
        RlsSessionContext rls)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
    }

    public async Task<CalendarWebhookBindingSnapshot?> ResolveActiveAsync(
        string webhookPath,
        CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters only affects EF. The explicit locator context is
        // applied on the same physical connection so PostgreSQL FORCE RLS
        // permits exactly the route-matched bootstrap row and nothing broader.
        await _rls.ApplyWebhookPathAsync(webhookPath, cancellationToken);

        try
        {
            // The WebhookPath is a globally-unique locator resolved across
            // tenants by stable identity, exactly like the ResourceLocator.
            // Security is provided by signature verification — never by the
            // path string alone — while PostgreSQL RLS limits this query to the
            // exact locator context applied above.
            var binding = await _context.CalendarIntegrations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ci =>
                    ci.WebhookPath == webhookPath && ci.DeletedAt == null, cancellationToken);

            if (binding is null || !binding.IsActive)
            {
                return null;
            }

            await _rls.ApplyWebhookConnectionAsync(binding.ConnectionId, cancellationToken);

            // The calendar binding's ConnectionId must resolve to a live
            // IntegrationConnection that still owns the same tenant/provider.
            // A revoked, expired, errored, deleted, or mismatched connection
            // fails closed — its callbacks must no longer be trusted.
            var connection = await _context.IntegrationConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == binding.ConnectionId, cancellationToken);

            if (connection is null
                || connection.DeletedAt != null
                || connection.Status != IntegrationConnectionStatus.Active
                || connection.AccountId != binding.AccountId
                || connection.WorkspaceId != binding.WorkspaceId
                || !MatchesCalendarProvider(connection.Provider, binding.Provider))
            {
                return null;
            }

            return new CalendarWebhookBindingSnapshot(
                binding.ConnectionId,
                binding.AccountId,
                binding.WorkspaceId,
                binding.Provider);
        }
        finally
        {
            await _rls.ClearWebhookBootstrapAsync(cancellationToken);
        }
    }

    public async Task AdoptDerivedTenantAsync(
        CalendarWebhookBindingSnapshot binding,
        CancellationToken cancellationToken)
    {
        _tenant.SetWorkspace(binding.AccountId, binding.WorkspaceId, null);
        // AdoptDerivedTenant changes the in-memory context; this explicit call
        // applies the complete Account/Workspace scope to the active database
        // transaction before receipt/outbox writes continue.
        await _rls.ApplyAsync(cancellationToken);
    }

    private static bool MatchesCalendarProvider(IntegrationProvider connectionProvider, CalendarProvider calendarProvider)
    {
        return connectionProvider switch
        {
            IntegrationProvider.Google => calendarProvider == CalendarProvider.Google,
            IntegrationProvider.Microsoft => calendarProvider == CalendarProvider.Outlook,
            _ => false,
        };
    }
}
