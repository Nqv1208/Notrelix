using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.SubscriptionId).HasColumnName("subscription_id").IsRequired();
        builder.Property(x => x.Number).HasColumnName("number").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.DueAt).HasColumnName("due_at").IsRequired();

        builder.OwnsOne(x => x.Amount, p =>
        {
            p.Property(m => m.Amount).HasColumnName("amount_value").HasColumnType("decimal(18,2)").IsRequired();
            p.Property(m => m.Currency).HasColumnName("amount_currency").IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        });

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_invoices_workspace_id");
        builder.HasIndex(x => x.SubscriptionId).HasDatabaseName("idx_invoices_subscription_id");
    }
}
