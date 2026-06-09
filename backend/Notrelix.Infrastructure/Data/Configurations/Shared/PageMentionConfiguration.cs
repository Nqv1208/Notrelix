using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Shared;

namespace Notrelix.Infrastructure.Data.Configurations.Shared;

public class PageMentionConfiguration : IEntityTypeConfiguration<PageMention>
{
    public void Configure(EntityTypeBuilder<PageMention> builder)
    {
        builder.ToTable("page_mentions");

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.PageId)
            .HasColumnName("page_id")
            .IsRequired();

        builder.Property(e => e.BlockId)
            .HasColumnName("block_id");

        builder.Property(e => e.MentionedUserId)
            .HasColumnName("mentioned_user_id")
            .IsRequired();

        builder.Property(e => e.MentionedBy)
            .HasColumnName("mentioned_by")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(e => e.PageId);
        builder.HasIndex(e => e.MentionedUserId);
        builder.HasIndex(e => e.MentionedBy);
    }
}
