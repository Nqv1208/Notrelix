using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardViewUserPreferenceConfiguration : IEntityTypeConfiguration<BoardViewUserPreference>
{
    public void Configure(EntityTypeBuilder<BoardViewUserPreference> builder)
    {
        builder.ToTable("board_view_user_preferences", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ViewId).HasColumnName("view_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.GroupRule).HasColumnName("group_rule_id");

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.Version).HasColumnName("version");

        builder.OwnsMany(x => x.FilterRules, f =>
        {
            f.WithOwner().HasForeignKey("preference_id");
            f.ToTable("board_view_filter_rules", DbSchemas.Work);
            f.Property<Guid>("Id");
            f.HasKey("Id");
            f.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
            f.Property(x => x.Operator).HasColumnName("operator").HasMaxLength(50);
            f.Property(x => x.Value).HasColumnName("value");
        });

        builder.OwnsMany(x => x.SortRules, s =>
        {
            s.WithOwner().HasForeignKey("preference_id");
            s.ToTable("board_view_sort_rules", DbSchemas.Work);
            s.Property<Guid>("Id");
            s.HasKey("Id");
            s.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
            s.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(50);
        });

        builder.HasOne<BoardView>()
            .WithMany()
            .HasForeignKey(x => x.ViewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ViewId, x.UserId }).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("idx_board_view_user_prefs_view_user");
    }
}
