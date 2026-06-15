using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Events;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class BillingEventConfiguration : IEntityTypeConfiguration<BillingEvent>
{
    public void Configure(EntityTypeBuilder<BillingEvent> builder)
    {
        builder.ToTable("billing_events", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ProviderEventId).HasColumnName("provider_event_id").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.RawData).HasColumnName("raw_data").HasColumnType("jsonb").HasConversion<JsonValueConverter>().IsRequired();
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();
        builder.Property(x => x.Error).HasColumnName("error");

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.ProviderEventId).HasDatabaseName("idx_billing_events_provider_event_id");
        builder.HasIndex(x => x.Type).HasDatabaseName("idx_billing_events_type");
    }
}
