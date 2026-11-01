using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class CustomRoleChangeConfiguration : IEntityTypeConfiguration<CustomRoleChange>
{
    public void Configure(EntityTypeBuilder<CustomRoleChange> builder)
    {
        builder.ToTable("custom_role_changes", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(x => x.Kind).HasColumnName("kind").IsRequired().HasMaxLength(30);
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.SubjectUserId).HasColumnName("subject_user_id");
        builder.Property(x => x.Summary).HasColumnName("summary").IsRequired().HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.HasIndex(x => new { x.AccountId, x.RoleId, x.OccurredAt }).HasDatabaseName("idx_custom_role_changes_role");
    }
}
