using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class PinnedResourceConfiguration : IEntityTypeConfiguration<PinnedResource>
{
    public void Configure(EntityTypeBuilder<PinnedResource> builder)
    {
        builder.ToTable("pinned_resources", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ResourceKind).HasColumnName("resource_kind").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.PinnedAt).HasColumnName("pinned_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.ResourceKind, x.ResourceId })
            .IsUnique()
            .HasDatabaseName("ux_workspace_pinned_resources_member_resource");
        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.PinnedAt })
            .HasDatabaseName("ix_workspace_pinned_resources_member_pinned");
    }
}
