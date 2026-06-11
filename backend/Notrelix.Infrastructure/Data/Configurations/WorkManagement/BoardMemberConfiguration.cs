using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.ToTable("board_members");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.JoinedAt).HasColumnName("joined_at").IsRequired();

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.UserId }).IsUnique().HasDatabaseName("idx_board_members_board_user");
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_board_members_user_id");
    }
}
