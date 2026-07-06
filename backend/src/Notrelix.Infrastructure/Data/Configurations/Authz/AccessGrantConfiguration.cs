using Notrelix.Infrastructure.Data.Authz;

namespace Notrelix.Infrastructure.Data.Configurations.Authz;

public sealed class AccessGrantConfiguration : IEntityTypeConfiguration<AccessGrant>
{
    public void Configure(EntityTypeBuilder<AccessGrant> builder)
    {
        builder.ToTable("access_grants", DbSchemas.Authz);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.SourceContext).HasColumnName("source_context").IsRequired().HasMaxLength(80).HasDefaultValue("Workspace");
        builder.Property(x => x.MembershipStatus).HasColumnName("membership_status").IsRequired().HasMaxLength(40);
        builder.Property(x => x.RoleCodes).HasColumnName("role_codes").HasColumnType("text[]").IsRequired().HasDefaultValueSql("'{}'::text[]");
        builder.Property(x => x.PermissionCodes).HasColumnName("permission_codes").HasColumnType("text[]").IsRequired().HasDefaultValueSql("'{}'::text[]");
        builder.Property(x => x.IsAccountAdmin).HasColumnName("is_account_admin").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsWorkspaceAdmin).HasColumnName("is_workspace_admin").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.GrantedAt).HasColumnName("granted_at").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id");
        builder.Property(x => x.SourceVersion).HasColumnName("source_version").IsRequired().HasDefaultValue(1L);
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasConversion<string>().IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(x => new { x.AccountId, x.WorkspaceId, x.UserId }).IsUnique().HasDatabaseName("ux_access_grants_account_workspace_user");
        builder.HasIndex(x => new { x.UserId, x.AccountId })
            .HasFilter("\"membership_status\" = 'Active' AND \"revoked_at\" IS NULL")
            .HasDatabaseName("ix_access_grants_user_account_active");
    }
}
