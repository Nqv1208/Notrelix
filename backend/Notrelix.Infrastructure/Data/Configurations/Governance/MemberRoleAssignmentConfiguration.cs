using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class MemberRoleAssignmentConfiguration : IEntityTypeConfiguration<MemberRoleAssignment>
{
    public void Configure(EntityTypeBuilder<MemberRoleAssignment> builder)
    {
        builder.ToTable("member_role_assignments", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.MemberId).HasColumnName("member_id").IsRequired();
        builder.Property(x => x.CustomRoleId).HasColumnName("custom_role_id").IsRequired();
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at").IsRequired();

        builder.HasIndex(x => x.MemberId).HasDatabaseName("idx_member_role_assignments_member_id");
        builder.HasIndex(x => x.CustomRoleId).HasDatabaseName("idx_member_role_assignments_role_id");
        builder.HasIndex(x => new { x.MemberId, x.CustomRoleId }).IsUnique().HasDatabaseName("idx_member_role_assignments_unique");
    }
}
