using Notrelix.Domain.Workspaces.Spaces;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class SpaceMemberConfiguration : IEntityTypeConfiguration<SpaceMember>
{
    public void Configure(EntityTypeBuilder<SpaceMember> builder)
    {
        builder.ToTable("space_members", DbSchemas.Workspace);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SpaceId).HasColumnName("space_id").IsRequired();
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // One entry per subject per Space (WSP-024).
        builder.HasIndex(x => new { x.SpaceId, x.SubjectType, x.SubjectId })
            .IsUnique()
            .HasDatabaseName("ux_workspace_space_members_space_subject");

        // Access evaluation looks up a principal's (and its Teams') entries.
        builder.HasIndex(x => new { x.WorkspaceId, x.SubjectType, x.SubjectId })
            .HasDatabaseName("ix_workspace_space_members_subject");
    }
}
