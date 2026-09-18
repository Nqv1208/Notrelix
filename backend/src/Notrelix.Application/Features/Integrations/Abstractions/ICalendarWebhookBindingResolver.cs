using Notrelix.Domain.Integrations;

namespace Notrelix.Application.Features.Integrations.Abstractions;

/// <summary>
/// Snapshot of an active calendar webhook binding that owns a WebhookPath
/// locator, resolved across tenant boundaries by stable identity.
/// </summary>
public sealed record CalendarWebhookBindingSnapshot(
    Guid AccountId,
    Guid WorkspaceId,
    CalendarProvider Provider);

/// <summary>
/// C6 — cross-tenant calendar webhook binding resolution and derived-tenant
/// adoption. The owning tenant is NOT known at the transport boundary, so the
/// binding must be resolved by its stable WebhookPath before any payload trust
/// and before the canonical RLS session context exists; after the caller has
/// verified the callback, the binding's owning Account/Workspace becomes the
/// derived execution tenant. Both concerns are Infrastructure-owned (query
/// filters bypassed; ICurrentTenantContext written), exactly like
/// ResourceLocator/TenantBootstrapStore — the handler depends only on this
/// port and never touches tenant context or cross-tenant DbSets directly.
/// </summary>
public interface ICalendarWebhookBindingResolver
{
    /// <summary>
    /// Resolves the active, non-deleted binding for a webhook path across
    /// tenant boundaries, or null when the path must fail closed (unknown,
    /// deactivated, or deleted binding).
    /// </summary>
    Task<CalendarWebhookBindingSnapshot?> ResolveActiveAsync(
        string webhookPath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adopts the binding's owning Account/Workspace as the derived execution
    /// tenant. Callers MUST invoke this only after signature verification has
    /// succeeded; every fail-closed branch must avoid it.
    /// </summary>
    void AdoptDerivedTenant(CalendarWebhookBindingSnapshot binding);
}