using Notrelix.Domain.Billing.Customers;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class BillingCustomerConfiguration : IEntityTypeConfiguration<BillingCustomer>
{
    public void Configure(EntityTypeBuilder<BillingCustomer> builder)
    {
        builder.ToTable("billing_customers", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.ProviderCustomerId).HasColumnName("provider_customer_id").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(40);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.AccountId).IsUnique().HasDatabaseName("ux_billing_customers_account_id");
        builder.HasIndex(x => x.ProviderCustomerId).HasDatabaseName("idx_billing_customers_provider");
    }
}
