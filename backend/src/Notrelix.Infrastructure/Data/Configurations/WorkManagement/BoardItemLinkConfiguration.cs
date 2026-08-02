using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class BoardItemLinkConfiguration : IEntityTypeConfiguration<BoardItemLink>
{
    public void Configure(EntityTypeBuilder<BoardItemLink> builder)
    {
        builder.ToTable("board_item_links", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SourceItemId).HasColumnName("source_item_id").IsRequired();
        builder.Property(x => x.LinkType).HasColumnName("link_type").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.Kind).HasColumnName("target_type").HasConversion(v => v.Value, v => LegacyResourceTypeMappings.ParseResourceKind(v)).IsRequired().HasMaxLength(128);
            target.Property(t => t.ResourceId).HasColumnName("target_id").IsRequired();
        });

        builder.HasOne<BoardItem>()
            .WithMany()
            .HasForeignKey(x => x.SourceItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SourceItemId).HasDatabaseName("idx_board_item_links_source_item");
    }
}
