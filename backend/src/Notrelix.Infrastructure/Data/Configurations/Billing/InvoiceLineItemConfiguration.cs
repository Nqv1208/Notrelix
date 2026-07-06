using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("invoice_line_items", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").IsRequired().HasMaxLength(500);
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.UnitAmount).HasColumnName("unit_amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").IsRequired().HasMaxLength(3);

        builder.HasIndex(x => x.InvoiceId).HasDatabaseName("idx_invoice_line_items_invoice_id");
    }
}
