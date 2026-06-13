using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Infrastructure.Data.Configurations.Identity;

public class ScimDirectorySyncConfiguration : IEntityTypeConfiguration<ScimDirectorySync>
{
    public void Configure(EntityTypeBuilder<ScimDirectorySync> builder)
    {
        builder.ToTable("scim_directory_syncs", DbSchemas.Identity);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.LastSyncAt).HasColumnName("last_sync_at");
        builder.Property(x => x.CursorJson).HasColumnName("cursor").IsRequired().HasDefaultValue("{}");
        builder.Property(x => x.ConfigJson).HasColumnName("config").IsRequired().HasDefaultValue("{}");

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

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_scim_directory_syncs_workspace_id");
    }
}
