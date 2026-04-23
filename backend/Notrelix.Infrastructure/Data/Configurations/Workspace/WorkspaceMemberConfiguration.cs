using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Workspace;

namespace Notrelix.Infrastructure.Data.Configurations.Workspace;

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();

        // Indexes - composite unique
        builder.HasIndex(m => new { m.WorkspaceId, m.UserId })
            .IsUnique();

        builder.HasIndex(m => m.UserId);

        // Ignore domain events
        builder.Ignore(m => m.DomainEvents);
    }
}
