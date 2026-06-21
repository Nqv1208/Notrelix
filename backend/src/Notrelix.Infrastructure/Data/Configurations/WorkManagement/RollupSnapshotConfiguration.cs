using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Rollups;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class RollupSnapshotConfiguration : IEntityTypeConfiguration<RollupSnapshot>
{
    public void Configure(EntityTypeBuilder<RollupSnapshot> builder)
    {
        builder.ToTable("rollup_snapshots", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
        builder.Property(x => x.Value).HasColumnName("value").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ItemId, x.FieldId }).IsUnique().HasDatabaseName("idx_rollup_snapshots_item_field");
    }
}
