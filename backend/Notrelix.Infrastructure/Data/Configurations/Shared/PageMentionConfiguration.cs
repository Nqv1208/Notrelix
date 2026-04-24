using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Shared;

namespace Notrelix.Infrastructure.Data.Configurations.Shared;

public class PageMentionConfiguration : IEntityTypeConfiguration<PageMention>
{
    public void Configure(EntityTypeBuilder<PageMention> builder)
    {
        builder.ToTable("page_mentions");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.PageId);
        builder.HasIndex(e => e.MentionedUserId);
        builder.HasIndex(e => e.MentionedBy);
    }
}
