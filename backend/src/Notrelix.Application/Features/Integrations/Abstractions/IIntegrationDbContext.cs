using Notrelix.Domain.Integrations.Sync;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Integrations.Abstractions;

public interface IIntegrationDbContext
{
    DbSet<IntegrationConnection> IntegrationConnections { get; }
    DbSet<IntegrationScope> IntegrationScopes { get; }
    DbSet<IntegrationSecretVersion> IntegrationSecretVersions { get; }
    DbSet<IntegrationSyncCursor> IntegrationSyncCursors { get; }
    DbSet<WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<WebhookDelivery> WebhookDeliveries { get; }
    DbSet<InboundWebhookEvent> InboundWebhookEvents { get; }
    DbSet<CalendarIntegration> CalendarIntegrations { get; }
    DbSet<CalendarEvent> CalendarEvents { get; }
    DbSet<CalendarEventLink> CalendarEventLinks { get; }
}