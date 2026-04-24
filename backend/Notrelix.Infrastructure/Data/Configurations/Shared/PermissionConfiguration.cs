using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Shared;

namespace Notrelix.Infrastructure.Data.Configurations.Shared;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.ResourceType)
            .HasColumnName("resource_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.Property(p => p.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(p => p.SubjectType)
            .HasColumnName("subject_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.SubjectId)
            .HasColumnName("subject_id")
            .IsRequired();

        builder.Property(p => p.Level)
            .HasColumnName("level")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.GrantedBy)
            .HasColumnName("granted_by");

        builder.Property(p => p.ExpiresAt)
            .HasColumnName("expires_at");

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by");

        // Indexes - composite unique
        builder.HasIndex(p => new { p.WorkspaceId, p.ResourceType, p.ResourceId, p.SubjectType, p.SubjectId })
            .IsUnique();

        builder.HasIndex(p => new { p.ResourceType, p.ResourceId, p.SubjectType, p.SubjectId })
            .HasDatabaseName("idx_permissions_resource");
        builder.HasIndex(p => new { p.SubjectType, p.SubjectId });

        builder.HasOne<Notrelix.Domain.Entities.Workspacess.Workspace>()
            .WithMany()
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}
