using Notrelix.Domain.Collaboration.ReadStates;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public sealed class ResourceReadStateConfiguration : IEntityTypeConfiguration<ResourceReadState>
{
    public void Configure(EntityTypeBuilder<ResourceReadState> builder)
    {
        builder.ToTable("resource_read_states", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.ResourceKind).HasColumnName("resource_type").HasMaxLength(160).IsRequired();
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.LastReadAt).HasColumnName("last_read_at");
        builder.Property(x => x.LastReadCommentId).HasColumnName("last_read_comment_id");
        builder.Property(x => x.UnreadCount).HasColumnName("unread_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.ResourceKind, x.ResourceId }).IsUnique().HasDatabaseName("ux_collab_resource_read_states_user_resource");
        builder.HasIndex(x => new { x.WorkspaceId, x.UserId, x.UnreadCount, x.UpdatedAt }).HasDatabaseName("ix_collab_resource_read_states_user");
    }
}
