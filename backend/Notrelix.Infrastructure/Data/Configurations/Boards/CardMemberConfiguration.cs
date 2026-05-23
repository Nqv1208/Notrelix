using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Boards;

namespace Notrelix.Infrastructure.Data.Configurations.Boards;

public class CardMemberConfiguration : IEntityTypeConfiguration<CardMember>
{
    public void Configure(EntityTypeBuilder<CardMember> builder)
    {
        builder.ToTable("card_members");
        builder.HasKey(x => new { x.CardId, x.UserId });

        builder.Property(x => x.CardId).HasColumnName("card_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at");
        builder.Property(x => x.AssignedBy).HasColumnName("assigned_by");

        builder.HasOne(x => x.Card).WithMany(c => c.Members).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
    }
}
