using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Activity;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(100);
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
            target.Property(t => t.ResourceId).HasColumnName("resource_id");
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.ResourceType, t.ResourceId }).HasDatabaseName("idx_activity_logs_resource");
        });

        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.Property(m => m.Data).HasColumnName("metadata").HasColumnType("jsonb");
        });

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_activity_logs_workspace_id");
        builder.HasIndex(x => x.Timestamp).HasDatabaseName("idx_activity_logs_timestamp");
    }
}
