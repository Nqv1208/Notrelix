using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities;

namespace Notrelix.Infrastructure.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.ActorId)
            .HasColumnName("actor_id")
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(a => a.TargetTitle)
            .HasColumnName("target_title")
            .HasMaxLength(500);

        builder.Property(a => a.WorkspaceId)
            .HasColumnName("workspace_id");

        builder.Property(a => a.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(a => a.WorkspaceId);
        builder.HasIndex(a => a.ActorId);
        builder.HasIndex(a => new { a.TargetType, a.TargetId });
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.WorkspaceId, a.CreatedAt });

        // Ignore domain events
        builder.Ignore(a => a.DomainEvents);
    }
}
