using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class FeatureUsageLedgerConfiguration : IEntityTypeConfiguration<FeatureUsageLedger>
{
    public void Configure(EntityTypeBuilder<FeatureUsageLedger> builder)
    {
        builder.ToTable("feature_usage_ledger", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.FeatureCode).HasColumnName("feature_code").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Delta).HasColumnName("delta").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.ReferenceResource).HasColumnName("reference_resource").HasMaxLength(500);
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_feature_usage_ledger_workspace_id");
    }
}
