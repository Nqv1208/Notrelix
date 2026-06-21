using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Policies;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class WorkspacePolicyConfiguration : IEntityTypeConfiguration<WorkspacePolicy>
{
    public void Configure(EntityTypeBuilder<WorkspacePolicy> builder)
    {
        builder.ToTable("workspace_policies", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();

        builder.OwnsOne(x => x.GuestPolicy, policy =>
        {
            policy.Property(p => p.AllowGuestInvites).HasColumnName("guest_allow_invites");
        });

        builder.OwnsOne(x => x.ResourcePolicy, policy =>
        {
            policy.Property(p => p.AllowPublicSharing).HasColumnName("resource_allow_public_sharing");
        });

        builder.OwnsOne(x => x.SharingPolicy, policy =>
        {
            policy.Property(p => p.AllowPublicSharing).HasColumnName("sharing_allow_public");
            policy.Property(p => p.AllowExternalInvite).HasColumnName("sharing_allow_external_invite");
        });

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_workspace_policies_workspace_id");
    }
}
