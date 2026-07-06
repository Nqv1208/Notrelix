using Notrelix.Domain.Integrations.Calendar;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class CalendarEventLinkConfiguration : IEntityTypeConfiguration<CalendarEventLink>
{
    public void Configure(EntityTypeBuilder<CalendarEventLink> builder)
    {
        builder.ToTable("calendar_event_links", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.IntegrationId).HasColumnName("integration_id").IsRequired();
        builder.Property(x => x.InternalEventId).HasColumnName("internal_event_id").IsRequired();
        builder.Property(x => x.ExternalEventId).HasColumnName("external_event_id").IsRequired().HasMaxLength(512);
        builder.Property(x => x.ETag).HasColumnName("etag").HasMaxLength(256);

        builder.HasIndex(x => x.IntegrationId).HasDatabaseName("idx_calendar_event_links_integration_id");
    }
}
