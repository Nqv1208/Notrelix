using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class CustomRoleConfiguration : IEntityTypeConfiguration<CustomRole>
{
    public void Configure(EntityTypeBuilder<CustomRole> builder)
    {
        builder.ToTable("custom_roles", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);
        builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();

        builder.HasMany(x => x.Permissions)
            .WithOne()
            .HasForeignKey(x => x.CustomRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Permissions)
            .HasField("_permissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_custom_roles_workspace_id");
    }
}
