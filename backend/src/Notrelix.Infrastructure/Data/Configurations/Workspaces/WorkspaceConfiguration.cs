using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Slug).HasColumnName("slug").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsPersonal).HasColumnName("is_personal");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();

        builder.OwnsOne(x => x.Settings, settings =>
        {
            settings.Property(s => s.AllowPublicSharing).HasColumnName("settings_allow_public_sharing").HasDefaultValue(false);
            settings.Property(s => s.EnforceMfa).HasColumnName("settings_enforce_mfa").HasDefaultValue(false);
        });

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("idx_workspaces_slug");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_workspaces_name");

        // One personal workspace per user (soft-delete aware)
        builder.HasIndex(x => x.CreatedBy)
            .IsUnique()
            .HasFilter("is_personal = true AND deleted_at IS NULL")
            .HasDatabaseName("idx_workspaces_personal_per_user");
    }
}
