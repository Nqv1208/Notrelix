using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Calendar;

namespace Notrelix.Infrastructure.Data.Configurations.Calendar;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalEventId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ResourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.SyncHash)
            .HasMaxLength(128);

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
