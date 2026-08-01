using Notrelix.Domain.Automation.Templates;

namespace Notrelix.Infrastructure.Data.Configurations.Automation;

public class AutomationTemplateConfiguration : IEntityTypeConfiguration<AutomationTemplate>
{
    public void Configure(EntityTypeBuilder<AutomationTemplate> builder)
    {
        builder.ToTable("automation_templates", DbSchemas.Automation);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(x => x.Category).HasColumnName("category").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Definition).HasColumnName("definition").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.Category).HasDatabaseName("idx_automation_templates_category");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_automation_templates_status");
    }
}
