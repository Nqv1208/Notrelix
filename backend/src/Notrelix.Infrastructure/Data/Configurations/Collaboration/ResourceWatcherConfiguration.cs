using Notrelix.Domain.Collaboration.Watchers;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class ResourceWatcherConfiguration : IEntityTypeConfiguration<ResourceWatcher>
{
    public void Configure(EntityTypeBuilder<ResourceWatcher> builder)
    {
        builder.ToTable("resource_watchers", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Level).HasColumnName("watch_level").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Target, t =>
        {
            t.Property(p => p.ResourceType).HasColumnName("target_type").IsRequired().HasMaxLength(50);
            t.Property(p => p.ResourceId).HasColumnName("target_id").IsRequired();
            t.HasIndex(p => new { p.ResourceType, p.ResourceId }).HasDatabaseName("idx_resource_watchers_target");
        });

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => new { x.UserId, x.WorkspaceId }).HasDatabaseName("idx_resource_watchers_user_workspace");
    }
}
