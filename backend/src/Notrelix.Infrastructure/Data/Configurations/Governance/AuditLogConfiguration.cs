using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Audit;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
            target.Property(t => t.ResourceId).HasColumnName("resource_id");
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.ResourceType, t.ResourceId }).HasDatabaseName("idx_audit_logs_resource");
        });

        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.Property(m => m.IpAddress).HasColumnName("metadata_ip_address").HasMaxLength(45);
            metadata.Property(m => m.UserAgent).HasColumnName("metadata_user_agent").HasMaxLength(512);
            metadata.Property(m => m.TraceId).HasColumnName("metadata_trace_id").HasMaxLength(128);
        });

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_audit_logs_workspace_id");
        builder.HasIndex(x => x.Timestamp).HasDatabaseName("idx_audit_logs_timestamp");
    }
}
