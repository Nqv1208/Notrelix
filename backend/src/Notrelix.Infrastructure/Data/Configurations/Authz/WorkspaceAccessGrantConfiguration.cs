using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Authz;

namespace Notrelix.Infrastructure.Data.Configurations.Authz;

public sealed class WorkspaceAccessGrantConfiguration : IEntityTypeConfiguration<WorkspaceAccessGrant>
{
    public void Configure(EntityTypeBuilder<WorkspaceAccessGrant> builder)
    {
        builder.ToTable("workspace_access_grants", DbSchemas.Authz);

        builder.HasKey(x => new { x.WorkspaceId, x.UserId });
        builder.Property(x => x.WorkspaceId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.SourceContext).IsRequired().HasMaxLength(80).HasDefaultValue("Workspace");
        builder.Property(x => x.MembershipStatus).IsRequired().HasMaxLength(40);
        builder.Property(x => x.RoleCodes).HasColumnType("text[]").IsRequired().HasDefaultValueSql("'{}'::text[]");
        builder.Property(x => x.PermissionCodes).HasColumnType("text[]").IsRequired().HasDefaultValueSql("'{}'::text[]");
        builder.Property(x => x.IsWorkspaceAdmin).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.GrantedAt).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.SourceEventId);
        builder.Property(x => x.SourceVersion);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(x => new { x.UserId, x.WorkspaceId })
            .HasFilter("\"membership_status\" = 'Active' AND \"revoked_at\" IS NULL");
        builder.HasIndex(x => new { x.WorkspaceId, x.UserId })
            .HasFilter("\"membership_status\" = 'Active' AND \"revoked_at\" IS NULL");
    }
}
