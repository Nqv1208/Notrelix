using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Calendar;

namespace Notrelix.Infrastructure.Data.Configurations.Calendar;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events");

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.IntegrationId)
            .HasColumnName("integration_id");

        builder.Property(e => e.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ResourceType)
            .HasColumnName("resource_type")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.ResourceId)
            .HasColumnName("resource_id");

        builder.Property(e => e.SyncHash)
            .HasColumnName("sync_hash")
            .HasMaxLength(128);

        builder.Property(e => e.SyncedAt)
            .HasColumnName("synced_at");

        // Unique: one mapping per integration + external event
        builder.HasIndex(e => new { e.IntegrationId, e.ExternalEventId })
            .IsUnique();

        builder.HasIndex(e => new { e.ResourceType, e.ResourceId });

        builder.HasOne(e => e.Integration)
            .WithMany()
            .HasForeignKey(e => e.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
