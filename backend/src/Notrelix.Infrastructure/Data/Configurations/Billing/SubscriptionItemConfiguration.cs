using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class SubscriptionItemConfiguration : IEntityTypeConfiguration<SubscriptionItem>
{
    public void Configure(EntityTypeBuilder<SubscriptionItem> builder)
    {
        builder.ToTable("subscription_items", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.SubscriptionId).HasColumnName("subscription_id").IsRequired();
        builder.Property(x => x.PlanPriceId).HasColumnName("plan_price_id").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();

        builder.HasIndex(x => x.SubscriptionId).HasDatabaseName("idx_subscription_items_subscription_id");
        builder.HasIndex(x => x.PlanPriceId).HasDatabaseName("idx_subscription_items_plan_price_id");
    }
}
