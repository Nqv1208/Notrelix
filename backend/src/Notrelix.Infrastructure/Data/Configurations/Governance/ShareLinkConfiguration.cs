using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class ShareLinkConfiguration : IEntityTypeConfiguration<ShareLink>
{
    public void Configure(EntityTypeBuilder<ShareLink> builder)
    {
        builder.ToTable("share_links", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.AccessMode).HasColumnName("access_mode").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

        builder.OwnsOne(x => x.TokenHash, token =>
        {
            token.Property(t => t.Hash).HasColumnName("token_hash").IsRequired();
        });

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.ResourceId).HasDatabaseName("idx_share_links_resource_id");
    }
}
