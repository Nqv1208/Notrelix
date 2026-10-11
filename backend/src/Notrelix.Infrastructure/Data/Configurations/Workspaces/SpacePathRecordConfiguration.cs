using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.Data.Workspaces;

namespace Notrelix.Infrastructure.Data.Configurations.Workspaces;

public class SpacePathRecordConfiguration : IEntityTypeConfiguration<SpacePathRecord>
{
    public void Configure(EntityTypeBuilder<SpacePathRecord> builder)
    {
        builder.ToTable("space_paths", DbSchemas.Workspace);

        builder.HasKey(x => new { x.AncestorId, x.DescendantId }).HasName("pk_space_paths");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.AncestorId).HasColumnName("ancestor_id").IsRequired();
        builder.Property(x => x.DescendantId).HasColumnName("descendant_id").IsRequired();
        builder.Property(x => x.Depth).HasColumnName("depth").IsRequired();

        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(x => x.AncestorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_space_paths_ancestor");
        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(x => x.DescendantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_space_paths_descendant");

        // "Ancestors of S" is the hot authorization lookup.
        builder.HasIndex(x => new { x.DescendantId, x.Depth })
            .HasDatabaseName("ix_workspace_space_paths_descendant");
    }
}
