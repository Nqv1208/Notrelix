using Notrelix.Domain.Collaboration.Reactions;

using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.Kind).HasColumnName("resource_type").HasConversion<ResourceKindConverter>().IsRequired().HasMaxLength(128);
            target.Property(t => t.ResourceId).HasColumnName("resource_id").IsRequired();
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.Kind, t.ResourceId }).HasDatabaseName("idx_reactions_resource");
            target.HasIndex(t => new { t.ResourceId }).HasDatabaseName("idx_reactions_target_resource_id");
        });

        builder.OwnsOne(x => x.Emoji, emoji =>
        {
            emoji.Property(e => e.Code).HasColumnName("emoji").IsRequired().HasMaxLength(50);
        });

        builder.HasIndex(x => new { x.UserId }).HasDatabaseName("idx_reactions_user_id");
    }
}
