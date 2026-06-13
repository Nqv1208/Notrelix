using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class SavedFilterConfiguration : IEntityTypeConfiguration<SavedFilter>
{
    public void Configure(EntityTypeBuilder<SavedFilter> builder)
    {
        builder.ToTable("saved_filters", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ViewId).HasColumnName("view_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Visibility).HasColumnName("visibility").IsRequired();
        builder.Property(x => x.GroupRule).HasColumnName("group_rule_id");

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
        builder.Property(x => x.Version).HasColumnName("version");

        builder.OwnsMany(x => x.Rules, r =>
        {
            r.WithOwner().HasForeignKey("saved_filter_id");
            r.ToTable("saved_filter_rules", DbSchemas.Work);
            r.Property<Guid>("Id");
            r.HasKey("Id");
            r.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
            r.Property(x => x.Operator).HasColumnName("operator").HasMaxLength(50);
            r.Property(x => x.Value).HasColumnName("value");
        });

        builder.OwnsMany(x => x.SortRules, s =>
        {
            s.WithOwner().HasForeignKey("saved_filter_id");
            s.ToTable("saved_filter_sort_rules", DbSchemas.Work);
            s.Property<Guid>("Id");
            s.HasKey("Id");
            s.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
            s.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(50);
        });

        builder.HasOne<BoardView>()
            .WithMany()
            .HasForeignKey(x => x.ViewId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BoardId, x.Name }).HasFilter("is_deleted = false").HasDatabaseName("idx_saved_filters_board_name");
    }
}
