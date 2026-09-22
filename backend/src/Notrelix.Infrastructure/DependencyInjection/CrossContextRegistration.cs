using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Identity.Ports.Bootstrap;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement;
using Notrelix.Infrastructure.CrossContext.Automation.WorkManagement;
using Notrelix.Infrastructure.CrossContext.Identity.Bootstrap;
using Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration;

namespace Notrelix.Infrastructure;

/// <summary>
/// Cross-context runtime composition. Owns the DI registrations that bind a
/// consumer context's cross-context Application ports to the adapter that
/// reaches the producer context's approved Public surface.
///
/// This is runtime composition only — no product semantics are changed here.
/// Persistence specifics (ApplicationDbContext, bounded-context DbContext
/// interface mappings, RLS, data session, outbox persistence) remain in
/// PersistenceRegistration.
/// </summary>
public static class CrossContextRegistration
{
    public static IServiceCollection AddCrossContextBindings(
        this IServiceCollection services)
    {
        // Cross-context read ports.
        services.AddScoped<IWorkManagementCollaborationReadPort, WorkManagementCollaborationReadAdapter>();
        services.AddScoped<IIdentityBootstrapReadPort, IdentityBootstrapReadAdapter>();

        // Cross-context target-action port: Automation -> WorkManagement
        services.AddScoped<IWorkActionPort, WorkItemActionAdapter>();

        // Cross-context projection-source port: Analytics -> WorkManagement.
        // The Analytics-owned port binds to the delegate adapter, which reaches
        // the producer-owned Public IWorkItemProjectionSource contract
        // (registered by the WorkManagement Application).
        services.AddScoped<IWorkItemProjectionSourceAdapter, WorkItemProjectionSourceAdapter>();

        return services;
    }
}
