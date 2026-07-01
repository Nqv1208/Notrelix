using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class PlanPriceConfiguration : IEntityTypeConfiguration<PlanPrice>
{
    public void Configure(EntityTypeBuilder<PlanPrice> builder)
    {
        builder.ToTable("plan_prices", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.PlanId).HasColumnName("plan_id").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").IsRequired().HasMaxLength(3);
        builder.Property(x => x.BillingInterval).HasColumnName("billing_interval").IsRequired().HasMaxLength(40);
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => new { x.PlanId, x.Currency, x.BillingInterval }).IsUnique().HasDatabaseName("ux_plan_prices_plan_currency_interval");
    }
}
