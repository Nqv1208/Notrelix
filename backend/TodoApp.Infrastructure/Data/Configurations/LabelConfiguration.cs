using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("labels");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BoardId).HasColumnName("board_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}
