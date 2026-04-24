using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Boardss;

namespace Notrelix.Infrastructure.Data.Configurations.Boards;

public class CardLabelConfiguration : IEntityTypeConfiguration<CardLabel>
{
    public void Configure(EntityTypeBuilder<CardLabel> builder)
    {
        builder.ToTable("card_labels");
        builder.HasKey(x => new { x.CardId, x.LabelId });

        builder.Property(x => x.CardId).HasColumnName("card_id");
        builder.Property(x => x.LabelId).HasColumnName("label_id");

        builder.HasOne(x => x.Card).WithMany(c => c.Labels).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Label).WithMany().HasForeignKey(x => x.LabelId).OnDelete(DeleteBehavior.Cascade);
    }
}
