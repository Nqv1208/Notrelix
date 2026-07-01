using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardRelationConfiguration : IEntityTypeConfiguration<BoardRelation>
{
    public void Configure(EntityTypeBuilder<BoardRelation> builder)
    {
        builder.ToTable("board_relations", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SourceBoardId).HasColumnName("source_board_id").IsRequired();
        builder.Property(x => x.TargetBoardId).HasColumnName("target_board_id").IsRequired();
        builder.Property(x => x.SourceFieldId).HasColumnName("source_field_id");
        builder.Property(x => x.TargetFieldId).HasColumnName("target_field_id");
        builder.Property(x => x.RelationType).HasColumnName("relation_type").IsRequired();
        builder.Property(x => x.Direction).HasColumnName("direction").IsRequired();
        builder.Property(x => x.SyncMode).HasColumnName("sync_mode").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").IsRequired();
        builder.Property(x => x.ConfigJson).HasColumnName("config_json").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version");

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.SourceBoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.TargetBoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SourceBoardId).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_board_relations_source_board");
        builder.HasIndex(x => x.TargetBoardId).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_board_relations_target_board");
    }
}
