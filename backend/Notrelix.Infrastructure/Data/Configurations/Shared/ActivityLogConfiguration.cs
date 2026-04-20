using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Shared;

namespace Notrelix.Infrastructure.Data.Configurations.Shared;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.Property(a => a.ActorId)
            .HasColumnName("actor_id")
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ResourceType)
            .HasColumnName("resource_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(a => a.ResourceTitle)
            .HasColumnName("resource_title")
            .HasMaxLength(500);

        builder.Property(a => a.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.WorkspaceId);
        builder.HasIndex(a => a.ActorId);
        builder.HasIndex(a => new { a.ResourceType, a.ResourceId });
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.WorkspaceId, a.CreatedAt });
    }
}
