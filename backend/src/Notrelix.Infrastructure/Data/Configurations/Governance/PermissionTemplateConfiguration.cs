using Notrelix.Domain.Governance.Templates;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class PermissionTemplateConfiguration : IEntityTypeConfiguration<PermissionTemplate>
{
    public void Configure(EntityTypeBuilder<PermissionTemplate> builder)
    {
        builder.ToTable("permission_templates", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(x => x.TargetResourceKind).HasColumnName("target_resource_type").HasConversion<Notrelix.Infrastructure.Data.Converters.ResourceKindConverter>().HasMaxLength(128);
        builder.Property(x => x.Definition).HasColumnName("permissions_json").HasColumnType("jsonb").IsRequired()
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<PermissionTemplateDefinition>(v, (System.Text.Json.JsonSerializerOptions?)null)!);
        builder.Property(x => x.Scope).HasColumnName("scope").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasFilter("workspace_id IS NOT NULL").HasDatabaseName("idx_permission_templates_workspace_id");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_permission_templates_name");
    }
}
