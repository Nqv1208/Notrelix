using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemValueConfiguration : IEntityTypeConfiguration<BoardItemValue>
{
    public void Configure(EntityTypeBuilder<BoardItemValue> builder)
    {
        builder.ToTable("board_item_values", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();

        builder.OwnsOne(x => x.Value, val =>
        {
            val.Property(v => v.Data).HasColumnName("value").HasColumnType("jsonb").IsRequired();
        });

        builder.HasOne<BoardItem>()
            .WithMany(x => x.FieldValues)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Notrelix.Domain.WorkManagement.Fields.BoardField>()
            .WithMany()
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ItemId, x.FieldId }).IsUnique().HasDatabaseName("idx_board_item_values_item_field");
    }
}
