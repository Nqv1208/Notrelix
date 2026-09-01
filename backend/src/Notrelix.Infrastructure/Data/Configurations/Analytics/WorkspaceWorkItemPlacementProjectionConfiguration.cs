using Notrelix.Domain.Analytics.Placements;

namespace Notrelix.Infrastructure.Data.Configurations.Analytics;

public class WorkspaceWorkItemPlacementProjectionConfiguration : IEntityTypeConfiguration<WorkspaceWorkItemPlacementProjection>
{
    public void Configure(EntityTypeBuilder<WorkspaceWorkItemPlacementProjection> builder)
    {
        builder.ToTable("workspace_work_item_placements", DbSchemas.Reporting);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").IsRequired();
        builder.Property(x => x.SourceRevision).HasColumnName("source_revision").IsRequired();
        builder.Property(x => x.LastOccurredAt).HasColumnName("last_occurred_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.ItemId })
            .IsUnique()
            .HasDatabaseName("ux_workspace_work_item_placements_workspace_item");
    }
}
