using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class FieldOptionConfiguration : IEntityTypeConfiguration<FieldOption>
{
    public void Configure(EntityTypeBuilder<FieldOption> builder)
    {
        builder.ToTable("field_options");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.FieldId).HasColumnName("field_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(50);
        builder.Property(x => x.Position).HasColumnName("position").HasColumnType("float8").IsRequired();

        builder.HasOne<BoardField>()
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FieldId).HasDatabaseName("idx_field_options_field_id");
    }
}
