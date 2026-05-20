using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Boards;

namespace Notrelix.Infrastructure.Data.Configurations.Boards;

public class BoardColumnConfiguration : IEntityTypeConfiguration<BoardColumn>
{
    public void Configure(EntityTypeBuilder<BoardColumn> builder)
    {
        builder.ToTable("board_columns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BoardId).HasColumnName("board_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.FieldType).HasColumnName("field_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Settings).HasColumnName("settings").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.Position).HasColumnName("position").HasColumnType("float8");
        builder.Property(x => x.IsHidden).HasColumnName("is_hidden").HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Board)
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BoardId, x.Position })
            .HasDatabaseName("idx_board_columns_board_position");

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DomainEvents);
    }
}
