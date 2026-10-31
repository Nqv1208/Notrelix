using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class CustomRoleConfiguration : IEntityTypeConfiguration<CustomRole>
{
    public void Configure(EntityTypeBuilder<CustomRole> builder)
    {
        builder.ToTable("custom_roles", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.BaseRole).HasColumnName("base_role").IsRequired();
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

        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().HasDefaultValue(1L);

        builder.HasIndex(x => x.AccountId).HasDatabaseName("idx_custom_roles_account_id");
    }
}
