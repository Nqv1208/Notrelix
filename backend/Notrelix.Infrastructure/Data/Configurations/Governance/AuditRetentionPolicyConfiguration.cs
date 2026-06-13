using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Audit;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class AuditRetentionPolicyConfiguration : IEntityTypeConfiguration<AuditRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<AuditRetentionPolicy> builder)
    {
        builder.ToTable("audit_retention_policies", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RetentionDays).HasColumnName("retention_days").IsRequired().HasDefaultValue(365);
        builder.Property(x => x.ExportBeforeDelete).HasColumnName("export_before_delete");
        builder.Property(x => x.PolicyJson).HasColumnName("policy_json").HasColumnType("jsonb").IsRequired();

        builder.HasIndex(x => x.WorkspaceId).IsUnique().HasDatabaseName("idx_audit_retention_policies_workspace_id");
    }
}
