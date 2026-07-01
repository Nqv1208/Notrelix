using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemConnectionConfiguration : IEntityTypeConfiguration<BoardItemConnection>
{
    public void Configure(EntityTypeBuilder<BoardItemConnection> builder)
    {
        builder.ToTable("board_item_connections", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RelationId).HasColumnName("relation_id").IsRequired();
        builder.Property(x => x.SourceBoardId).HasColumnName("source_board_id").IsRequired();
        builder.Property(x => x.SourceItemId).HasColumnName("source_item_id").IsRequired();
        builder.Property(x => x.TargetBoardId).HasColumnName("target_board_id").IsRequired();
        builder.Property(x => x.TargetItemId).HasColumnName("target_item_id").IsRequired();
        builder.Property(x => x.SyncStatus).HasColumnName("sync_status").IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<BoardRelation>()
            .WithMany()
            .HasForeignKey(x => x.RelationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RelationId).HasDatabaseName("idx_board_item_connections_relation_id");
        builder.HasIndex(x => x.SourceItemId).HasDatabaseName("idx_board_item_connections_source_item");
        builder.HasIndex(x => x.TargetItemId).HasDatabaseName("idx_board_item_connections_target_item");
    }
}
