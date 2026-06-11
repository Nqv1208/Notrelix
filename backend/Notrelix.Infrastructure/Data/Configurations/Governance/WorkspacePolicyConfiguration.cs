using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Policies;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class WorkspacePolicyConfiguration : IEntityTypeConfiguration<WorkspacePolicy>
{
    public void Configure(EntityTypeBuilder<WorkspacePolicy> builder)
    {
        builder.ToTable("workspace_policies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Key).HasColumnName("key").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Value).HasColumnName("value").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);

        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => new { x.WorkspaceId, x.Key }).IsUnique().HasDatabaseName("idx_workspace_policies_key");
    }
}
