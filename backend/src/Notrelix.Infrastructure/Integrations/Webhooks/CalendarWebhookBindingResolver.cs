using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// C6 — cross-tenant calendar webhook binding resolution and derived-tenant
/// adoption. This is the allowlisted bootstrap counterpart of
/// ResourceLocator/TenantBootstrapStore: the owning tenant is unknown at the
/// transport boundary, so the WebhookPath binding is resolved by stable
/// identity with query filters bypassed — before the RLS session context can
/// exist — and the binding's tenant is then adopted into ICurrentTenantContext
/// only after the caller has verified the callback signature.
/// </summary>
public sealed class CalendarWebhookBindingResolver : ICalendarWebhookBindingResolver
{
    private readonly IIntegrationDbContext _context;
    private readonly ICurrentTenantContext _tenant;

    public CalendarWebhookBindingResolver(
        IIntegrationDbContext context,
        ICurrentTenantContext tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<CalendarWebhookBindingSnapshot?> ResolveActiveAsync(
        string webhookPath,
        CancellationToken cancellationToken)
    {
        // The WebhookPath is a globally-unique locator resolved across tenants
        // by stable identity, exactly like the ResourceLocator; security is
        // provided by the subsequent signature verification — never by the
        // path string itself.
        var binding = await _context.CalendarIntegrations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ci =>
                ci.WebhookPath == webhookPath && ci.DeletedAt == null, cancellationToken);

        if (binding is null || !binding.IsActive)
        {
            return null;
        }

        return new CalendarWebhookBindingSnapshot(
            binding.AccountId,
            binding.WorkspaceId,
            binding.Provider);
    }

    public void AdoptDerivedTenant(CalendarWebhookBindingSnapshot binding)
    {
        _tenant.SetWorkspace(binding.AccountId, binding.WorkspaceId, null);
    }
}