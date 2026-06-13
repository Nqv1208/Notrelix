using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class CustomRolePermissionConfiguration : IEntityTypeConfiguration<CustomRolePermission>
{
    public void Configure(EntityTypeBuilder<CustomRolePermission> builder)
    {
        builder.ToTable("custom_role_permissions", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.CustomRoleId).HasColumnName("custom_role_id").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsAllowed).HasColumnName("is_allowed");
        builder.Property(x => x.Conditions).HasColumnName("conditions").HasColumnType("jsonb").IsRequired();

        builder.HasIndex(x => x.CustomRoleId).HasDatabaseName("idx_custom_role_permissions_role_id");
    }
}
