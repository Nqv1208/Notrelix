using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Domain.Integrations.Calendar;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Domain.Integrations.Sync;
using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Domain.Integrations.Webhooks.Events;

namespace Notrelix.Application.Tests.Features.Integrations;

/// <summary>
/// Test-only Integrations context on EF InMemory, for the connection and secret-version rows that the OAuth
/// callback reads and writes. Only those two sets are public; the other interface members are explicit and
/// unmapped. InMemory proves orchestration semantics only, never PostgreSQL or row-level security.
/// </summary>
public sealed class TestIntegrationDbContext(DbContextOptions<TestIntegrationDbContext> options)
    : DbContext(options), IIntegrationDbContext
{
    public DbSet<IntegrationConnection> IntegrationConnections { get; set; } = null!;
    public DbSet<IntegrationSecretVersion> IntegrationSecretVersions { get; set; } = null!;
    public DbSet<IntegrationScope> IntegrationScopes { get; set; } = null!;

    DbSet<IntegrationSyncCursor> IIntegrationDbContext.IntegrationSyncCursors => null!;
    DbSet<WebhookSubscription> IIntegrationDbContext.WebhookSubscriptions => null!;
    DbSet<WebhookDelivery> IIntegrationDbContext.WebhookDeliveries => null!;
    DbSet<OutboundDeliveryLogEntry> IIntegrationDbContext.OutboundDeliveryLog => null!;
    DbSet<InboundWebhookEvent> IIntegrationDbContext.InboundWebhookEvents => null!;
    DbSet<CalendarIntegration> IIntegrationDbContext.CalendarIntegrations => null!;
    DbSet<CalendarEvent> IIntegrationDbContext.CalendarEvents => null!;
    DbSet<CalendarEventLink> IIntegrationDbContext.CalendarEventLinks => null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Sets the handler never touches are not modelled; their entity graphs need providers this context lacks.
        modelBuilder.Ignore<IntegrationSyncCursor>();
        modelBuilder.Ignore<WebhookSubscription>();
        modelBuilder.Ignore<WebhookDelivery>();
        modelBuilder.Ignore<OutboundDeliveryLogEntry>();
        modelBuilder.Ignore<InboundWebhookEvent>();
        modelBuilder.Ignore<CalendarIntegration>();
        modelBuilder.Ignore<CalendarEvent>();
        modelBuilder.Ignore<CalendarEventLink>();

        modelBuilder.Entity<IntegrationConnection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.DomainEvents);
            entity.Ignore(x => x.Scopes);
            entity.Property(x => x.CurrentSecretRef).HasConversion(
                secret => secret == null ? null : secret.Value,
                value => value == null ? null : SecretRef.Create(value));
        });

        modelBuilder.Entity<IntegrationScope>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<IntegrationSecretVersion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.DomainEvents);
            entity.Property(x => x.SecretReference).HasConversion(
                secret => secret.Value,
                value => SecretRef.Create(value));
        });
    }
}
