using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class RelationFieldConfigConfiguration : IEntityTypeConfiguration<RelationFieldConfig>
{
    public void Configure(EntityTypeBuilder<RelationFieldConfig> builder)
    {
        builder.ToTable("relation_field_configs", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
        builder.Property(x => x.SourceBoardId).HasColumnName("source_board_id").IsRequired();
        builder.Property(x => x.TargetBoardId).HasColumnName("target_board_id").IsRequired();
        builder.Property(x => x.AllowMultiple).HasColumnName("allow_multiple");
        builder.Property(x => x.CreateBacklink).HasColumnName("create_backlink");
        builder.Property(x => x.BacklinkFieldId).HasColumnName("backlink_field_id");
        builder.Property(x => x.Direction).HasColumnName("direction").IsRequired();

        builder.HasIndex(x => x.FieldId).IsUnique().HasDatabaseName("idx_relation_field_configs_field_id");
    }
}
