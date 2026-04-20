using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Workspace;

namespace Notrelix.Infrastructure.Data.Configurations.Workspace;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Domain.Entities.Workspace.Workspace>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Workspace.Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id");

        builder.Property(w => w.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(w => w.IsPersonal)
            .HasColumnName("is_personal")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(w => w.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(w => w.Plan)
            .HasColumnName("plan")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(Notrelix.Domain.Enums.WorkspacePlan.Free)
            .IsRequired();

        builder.Property(w => w.Settings)
            .HasColumnName("settings")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();

        // Icon as owned type (Value Object)
        builder.OwnsOne(w => w.Icon, icon =>
        {
            icon.Property(i => i.Value)
                .HasColumnName("icon_value")
                .HasMaxLength(100);

            icon.Property(i => i.Type)
                .HasColumnName("icon_type")
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        builder.Property(w => w.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false);

        // Audit fields
        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(w => w.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(w => w.UpdatedBy)
            .HasColumnName("updated_by");

        // Relationships
        builder.HasMany(w => w.Members)
            .WithOne(m => m.Workspace)
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(w => w.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField);

        // Indexes
        builder.HasIndex(w => w.OwnerId);
        builder.HasIndex(w => w.Slug).IsUnique().HasDatabaseName("idx_workspaces_slug");

        // Ignore domain events
        builder.Ignore(w => w.DomainEvents);
    }
}
