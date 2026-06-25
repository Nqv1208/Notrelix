using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardViewPinConfiguration : IEntityTypeConfiguration<BoardViewPin>
{
    public void Configure(EntityTypeBuilder<BoardViewPin> builder)
    {
        builder.ToTable("board_view_pins", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.BoardId).HasColumnName("board_id").IsRequired();
        builder.Property(x => x.BoardViewId).HasColumnName("board_view_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.PinScope).HasColumnName("pin_scope").IsRequired();
        builder.Property(x => x.Position).HasColumnName("position").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");

        builder.HasOne<BoardView>()
            .WithMany()
            .HasForeignKey(x => x.BoardViewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.UserId, x.PinScope }).HasDatabaseName("idx_board_view_pins_board_user_scope");
        builder.HasIndex(x => x.BoardViewId).HasDatabaseName("idx_board_view_pins_view_id");
    }
}
