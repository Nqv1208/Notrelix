using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Enums;

namespace Notrelix.Infrastructure.Data.Configurations.Boards;

public class BoardViewConfiguration : IEntityTypeConfiguration<BoardView>
{
    public void Configure(EntityTypeBuilder<BoardView> builder)
    {
        builder.ToTable("board_views");

        // Composite PK
        builder.HasKey(e => new { e.BoardId, e.UserId });

        builder.Property(e => e.ViewMode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ViewMode.Kanban);

        builder.Property(e => e.Filters)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.HasOne(e => e.Board)
            .WithMany()
            .HasForeignKey(e => e.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
