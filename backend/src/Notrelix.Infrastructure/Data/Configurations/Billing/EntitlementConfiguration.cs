using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class EntitlementConfiguration : IEntityTypeConfiguration<Entitlement>
{
    public void Configure(EntityTypeBuilder<Entitlement> builder)
    {
        builder.ToTable("entitlements", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.TargetScope).HasColumnName("target_scope").HasConversion<string>().IsRequired().HasMaxLength(40).HasDefaultValue(EntitlementTargetScope.Account);
        builder.Property(x => x.TargetWorkspaceId).HasColumnName("target_workspace_id");
        builder.Property(x => x.Feature).HasColumnName("feature_code").HasConversion(v => v.Code, v => FeatureCode.Create(v)).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Limit).HasColumnName("limit_value").IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.RevokedBy).HasColumnName("revoked_by");

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

        builder.HasIndex(x => x.AccountId).HasDatabaseName("idx_entitlements_account_id");
        builder.HasIndex(x => new { x.TargetScope, x.TargetWorkspaceId }).HasDatabaseName("idx_entitlements_target");
    }
}
