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

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.BoardId).HasColumnName("board_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .HasDefaultValue("Main table");

        builder.Property(e => e.ViewMode)
            .HasColumnName("view_mode")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ViewMode.Kanban);

        builder.Property(e => e.Filters)
            .HasColumnName("filters")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.Config)
            .HasColumnName("config")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.Position).HasColumnName("position").HasColumnType("float8");
        builder.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(e => e.Board)
            .WithMany()
            .HasForeignKey(e => e.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.BoardId, e.Position })
            .HasDatabaseName("idx_board_views_board_position");

        builder.HasIndex(e => new { e.BoardId, e.UserId, e.ViewMode })
            .HasDatabaseName("idx_board_views_user_mode");

        builder.Ignore(e => e.DomainEvents);
    }
}
