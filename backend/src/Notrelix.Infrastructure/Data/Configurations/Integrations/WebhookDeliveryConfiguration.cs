using Notrelix.Domain.Integrations.Webhooks;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Integrations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries", DbSchemas.Integration);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.WebhookSubscriptionId).HasColumnName("webhook_subscription_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasConversion<string>().IsRequired().HasMaxLength(100);
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").HasConversion<JsonValueConverter>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ResponseStatusCode).HasColumnName("response_status_code");
        builder.Property(x => x.ResponseBody).HasColumnName("response_body");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        builder.Property(x => x.NextRetryAt).HasColumnName("next_retry_at");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_webhook_deliveries_workspace_id");
        builder.HasIndex(x => x.WebhookSubscriptionId).HasDatabaseName("idx_webhook_deliveries_subscription_id");
    }
}
