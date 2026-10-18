using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("spaces", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // WSP-021: one acyclic tree per Workspace. The parent is a same-context
        // shadow foreign key (no navigation); Spaces are soft-deleted, never purged
        // through the ORM, so the reference restricts deletion.
        builder.Property(x => x.ParentSpaceId).HasColumnName("parent_space_id");
        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(x => x.ParentSpaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_spaces_parent_space");

        builder.Property(x => x.Position)
            .HasColumnName("position")
            .HasColumnType("text")
            .UseCollation("C")
            .IsRequired();

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_spaces_workspace_id");
        builder.HasIndex(x => new { x.WorkspaceId, x.ParentSpaceId })
            .HasDatabaseName("ix_workspace_spaces_parent");
        builder.HasIndex(x => new { x.WorkspaceId, x.ParentSpaceId, x.Position })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_workspace_spaces_parent_position_active");
    }
}
