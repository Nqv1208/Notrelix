using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Audit;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.ResourceTitle).HasColumnName("resource_title").HasMaxLength(512);
        builder.Property(x => x.Details).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_audit_logs_workspace_id");
        builder.HasIndex(x => new { x.ResourceType, x.ResourceId }).HasDatabaseName("idx_audit_logs_resource");
        builder.HasIndex(x => x.Timestamp).HasDatabaseName("idx_audit_logs_timestamp");
    }
}
