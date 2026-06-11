using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemMemberConfiguration : IEntityTypeConfiguration<BoardItemMember>
{
    public void Configure(EntityTypeBuilder<BoardItemMember> builder)
    {
        builder.ToTable("board_item_members");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at").IsRequired();

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ItemId, x.UserId }).IsUnique().HasDatabaseName("idx_board_item_members_item_user");
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_board_item_members_user_id");
    }
}
