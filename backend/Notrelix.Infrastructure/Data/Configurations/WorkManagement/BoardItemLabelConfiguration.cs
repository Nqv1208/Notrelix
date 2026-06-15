using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemLabelConfiguration : IEntityTypeConfiguration<BoardItemLabel>
{
    public void Configure(EntityTypeBuilder<BoardItemLabel> builder)
    {
        builder.ToTable("board_item_labels", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.LabelId).HasColumnName("label_id").IsRequired();

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Label>()
            .WithMany()
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ItemId, x.LabelId }).IsUnique().HasDatabaseName("idx_board_item_labels_item_label");
    }
}
