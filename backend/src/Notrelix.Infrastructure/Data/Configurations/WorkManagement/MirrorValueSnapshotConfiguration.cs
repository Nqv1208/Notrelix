using Notrelix.Domain.WorkManagement.Relations;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class MirrorValueSnapshotConfiguration : IEntityTypeConfiguration<MirrorValueSnapshot>
{
    public void Configure(EntityTypeBuilder<MirrorValueSnapshot> builder)
    {
        builder.ToTable("mirror_value_snapshots", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RelationId).HasColumnName("relation_id").IsRequired();
        builder.Property(x => x.ConnectionId).HasColumnName("connection_id").IsRequired();
        builder.Property(x => x.SourceFieldId).HasColumnName("source_field_id").IsRequired();
        builder.Property(x => x.MirroredFieldId).HasColumnName("mirrored_field_id");
        builder.Property(x => x.ValueJson).HasColumnName("value_json");
        builder.Property(x => x.ValueHash).HasColumnName("value_hash");
        builder.Property(x => x.IsStale).HasColumnName("is_stale");
        builder.Property(x => x.ComputedAt).HasColumnName("computed_at").IsRequired();

        builder.HasOne<BoardItemConnection>()
            .WithMany()
            .HasForeignKey(x => x.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ConnectionId, x.SourceFieldId }).IsUnique().HasDatabaseName("idx_mirror_snapshots_connection_field");
        builder.HasIndex(x => x.IsStale).HasDatabaseName("idx_mirror_snapshots_stale");
    }
}
