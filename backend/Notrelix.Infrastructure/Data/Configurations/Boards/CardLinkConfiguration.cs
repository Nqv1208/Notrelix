using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Enums;

namespace Notrelix.Infrastructure.Data.Configurations.Boards;

public class CardLinkConfiguration : IEntityTypeConfiguration<CardLink>
{
    public void Configure(EntityTypeBuilder<CardLink> builder)
    {
        builder.ToTable("card_links");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LinkType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(CardLinkType.RelatesTo);

        // Prevent duplicate links
        builder.HasIndex(e => new { e.SourceCardId, e.TargetCardId, e.LinkType })
            .IsUnique();

        builder.HasOne(e => e.SourceCard)
            .WithMany()
            .HasForeignKey(e => e.SourceCardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TargetCard)
            .WithMany()
            .HasForeignKey(e => e.TargetCardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
