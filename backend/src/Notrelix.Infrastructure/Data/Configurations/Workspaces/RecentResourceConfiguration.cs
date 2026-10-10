using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class RecentResourceConfiguration : IEntityTypeConfiguration<RecentResource>
{
    public void Configure(EntityTypeBuilder<RecentResource> builder)
    {
        builder.ToTable("recent_resources", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ResourceKind).HasColumnName("resource_kind").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.LastOpenedAt).HasColumnName("last_opened_at").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.ResourceKind, x.ResourceId })
            .IsUnique()
            .HasDatabaseName("ux_workspace_recent_resources_member_resource");
        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.LastOpenedAt })
            .HasDatabaseName("ix_workspace_recent_resources_member_last_opened");
    }
}
