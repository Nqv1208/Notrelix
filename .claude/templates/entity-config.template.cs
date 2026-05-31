// Template: Entity Configuration
// Replace placeholders: {{EntityName}}, {{TableName}}, {{DomainName}}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.{{DomainName}};

namespace Notrelix.Infrastructure.Data.Configurations;

public class {{EntityName}}Configuration : IEntityTypeConfiguration<{{EntityName}}>
{
    public void Configure(EntityTypeBuilder<{{EntityName}}> builder)
    {
        // Table name
        builder.ToTable("{{TableName}}");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // TODO: Add properties
        // builder.Property(x => x.Title)
        //     .HasColumnName("title")
        //     .HasMaxLength(500)
        //     .IsRequired();

        // Position (if applicable)
        // builder.Property(x => x.Position)
        //     .HasColumnName("position")
        //     .HasColumnType("float8")
        //     .IsRequired()
        //     .HasDefaultValue(0);

        // JSONB properties (if applicable)
        // builder.Property(x => x.Properties)
        //     .HasColumnName("properties")
        //     .HasColumnType("jsonb")
        //     .HasDefaultValue("{}");

        // Audit fields
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by");

        // Soft delete
        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        // TODO: Add relationships
        // builder.HasOne(x => x.Parent)
        //     .WithMany(x => x.Children)
        //     .HasForeignKey(x => x.ParentId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // TODO: Add indexes
        // builder.HasIndex(x => x.SomeColumn)
        //     .HasFilter("is_deleted = false")
        //     .HasDatabaseName("idx_{{TableName}}_some_column");
    }
}
