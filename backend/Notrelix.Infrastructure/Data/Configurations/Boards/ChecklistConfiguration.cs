using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Boardss;

namespace Notrelix.Infrastructure.Data.Configurations.Boards;

public class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("checklists");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CardId).HasColumnName("card_id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).HasDefaultValue("Checklist");
        builder.Property(x => x.Position).HasColumnName("position");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasOne(x => x.Card).WithMany(c => c.Checklists).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}
