using Notrelix.Domain.Integrations.Calendar;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.IntegrationId).HasColumnName("integration_id").IsRequired();
        builder.Property(x => x.ExternalEventId).HasColumnName("external_event_id").HasMaxLength(512);

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
            target.Property(t => t.ResourceId).HasColumnName("resource_id");
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.ResourceType, t.ResourceId }).HasDatabaseName("idx_calendar_events_resource");
        });

        builder.OwnsOne(x => x.SyncHash, hash =>
        {
            hash.Property(h => h.Value).HasColumnName("sync_hash").HasMaxLength(64);
        });

        builder.HasOne<CalendarIntegration>()
            .WithMany()
            .HasForeignKey(x => x.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.IntegrationId, x.ExternalEventId }).IsUnique().HasFilter("external_event_id IS NOT NULL").HasDatabaseName("idx_calendar_events_external");
    }
}
